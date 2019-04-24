using System;
using System.Linq;
using System.Reflection;
using BrilliantSkies.Blocks.Weapons.Uis;
using BrilliantSkies.Core.Help;
using BrilliantSkies.Ui.Consoles.Getters;
using BrilliantSkies.Ui.Consoles.Interpretters.Subjective;
using BrilliantSkies.Ui.Consoles.Interpretters.Subjective.Buttons;
using BrilliantSkies.Ui.Tips;
using Harmony;
using UnityEngine;

namespace ROF
{
    [HarmonyPatch(typeof(AdvCannonAutoloader), "Secondary")]
    public class RateOfFirePredictor
    {
        public static MethodInfo TargetMethod() => typeof(AdvCannonAutoloader).GetMethod("Secondary", BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly);

        public static InteractionReturn Postfix(InteractionReturn __result, AdvCannonAutoloader __instance)
        {
            var self = __instance;
            var ShellRackModel = self.ShellRackModel;
            var Node = self.Node;
            var interactionReturn = __result;
            float LengthCapacity = self.LengthCapacity;

            //Debug.Log("[Walrus Shells Ballistic Madness] Autoloader UI working");
            float lengthMod = Rounding.R2(Mathf.Min(1f / Mathf.Sqrt(LengthCapacity), 1f));

            //Debug.Log("[Walrus Shells Ballistic Madness] Checking For Loaded shell...");
            if (!(ShellRackModel.LoadedShell == null))
            {
                if (ShellRackModel.ContainerCount == 0)
                {
                    lengthMod *= 1.5f;
                }

                float loadtime = ShellConstants.LoadTimeForShellVolume(new ShellModel_Propellant(ShellRackModel.LoadedShell).GetTotalVolume());
                interactionReturn.AddExtraLine(string.Format("<b>Sustained Rate of fire estimate (all):</b> {0} ", Rounding.R2(60 / (loadtime) / (ShellRackModel as ShellRackModel).MultipleDirectionsSpeedUpFactor() * (Node.ShellRacks.Racks.Count() - 1) / ShellRackModel.LoadingTimeComplexityModifier / lengthMod) + "<b>rpm</b>"));
                interactionReturn.AddExtraLine("This estimate is only accurate if all autoloaders use the same shell, and have the same magazine configuration as this one.");
                interactionReturn.AddExtraLine(string.Format("<b>Sustained Rate of fire estimate (just this):</b> {0} ", Rounding.R2(60 / (loadtime) / (ShellRackModel as ShellRackModel).MultipleDirectionsSpeedUpFactor() / ShellRackModel.LoadingTimeComplexityModifier / lengthMod) + "<b>rpm</b>"));
                interactionReturn.AddExtraLine(string.Format("<b>Ammo inputs per autoloader needed:</b> {0} ", Mathf.CeilToInt(2f / ((ShellRackModel as ShellRackModel).MultipleDirectionsSpeedUpFactor() * ShellRackModel.LoadingTimeComplexityModifier * lengthMod))));
                interactionReturn.AddExtraLine(string.Format("<b>Autoloaders total:</b> {0} ", (Node.ShellRacks.Racks.Count() - 1) + "<b> autoloaders found</b>"));

            }
            //else Debug.Log("[Walrus Shells Ballistic Madness] No Shell loaded.");

            return interactionReturn;
        }

    }

    [HarmonyPatch(typeof(AdvCannonBeltFeedAutoloader), "Secondary")]
    public class BurstFirePredictor
    {
        public static MethodInfo TargetMethod() => typeof(AdvCannonBeltFeedAutoloader).GetMethod("Secondary", BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly);

        public static InteractionReturn Postfix(InteractionReturn __result, AdvCannonBeltFeedAutoloader __instance)
        {
            var self = __instance;
            var ShellRackModel = self.ShellRackModel;
            var Node = self.Node;
            var interactionReturn = __result;

            //Debug.Log("[Walrus Shells Ballistic Madness] Belt Autoloader UI working");
            //Debug.Log("[Walrus Shells Ballistic Madness] Checking For Loaded shell...");
            if (!(ShellRackModel.LoadedShell == null))
            {
                float loadtime = ShellConstants.LoadTimeForShellVolume(new ShellModel_Propellant(ShellRackModel.LoadedShell).GetTotalVolume());
                interactionReturn.AddExtraLine(string.Format("<b>Burst Rate of fire estimate (all):</b> {0} ", Rounding.R2(60 / (loadtime) * (Node.ShellRacks.Racks.Count() - 1) / ShellRackModel.LoadingTimeComplexityModifier / 0.2f) + "<b>rpm</b>"));
                interactionReturn.AddExtraLine("This estimate is only accurate if all autoloaders use the same shell, are beltfed, and are all loaded.");
                interactionReturn.AddExtraLine(string.Format("<b>Burst Rate of fire estimate (just this):</b> {0} ", Rounding.R2(60 / (loadtime) / ShellRackModel.LoadingTimeComplexityModifier / 0.2f) + "<b>rpm</b>"));
                interactionReturn.AddExtraLine(string.Format("<b>Autoloaders total:</b> {0} ", (Node.ShellRacks.Racks.Count() - 1) + "<b> autoloaders found</b>"));

            }
            //else Debug.Log("[Walrus Shells Ballistic Madness] No Shell loaded.");

            return interactionReturn;
        }
    }

    [HarmonyPatch(typeof(AdvCannonFiringPiece), "Secondary")]
    public class ApsInfo
    {
        public static MethodInfo TargetMethod() => typeof(AdvCannonFiringPiece).GetMethod("Secondary", BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly);

        public static InteractionReturn Postfix(InteractionReturn __result, AdvCannonFiringPiece __instance)
        {
            var self = __instance;
            var ShellRackModel = self.ShellRackModel;
            var Node = self.Node;
            var interactionReturn = __result;

            const int maxClipsPerLoader = 4;
            int[] nbLoaders = new int[maxClipsPerLoader + 1];

            int nbBeltLoaders = 0;
            for (int i = 0; i < 5; i++)
            {
                nbLoaders[i] = 0;
            }

            for (int i = 1; i < self.Node.ShellRacks.Racks.Count; i++)
            {
                BeltFeedShellRackModel BeltFeed = self.Node.ShellRacks.Racks[i] as BeltFeedShellRackModel;
                if (null != BeltFeed)
                {
                    nbBeltLoaders++;
                    continue;
                }

                int NbClips = self.Node.ShellRacks.Racks[i].ContainerCount;
                if (NbClips < 0)
                    continue;

                if (NbClips > maxClipsPerLoader)
                    NbClips = maxClipsPerLoader;

                nbLoaders[NbClips]++;
            }

            for (int nbClips = 0; nbClips < (maxClipsPerLoader); nbClips++)
            {
                if (nbLoaders[nbClips] > 0)
                    interactionReturn.AddExtraLine(nbLoaders[nbClips].ToString() + " autoloaders with " + nbClips.ToString() + ((1 == nbClips) ? " clip" : " clips"));
            }
            if (nbLoaders[maxClipsPerLoader] > 0)
                interactionReturn.AddExtraLine(nbLoaders[maxClipsPerLoader].ToString() + " autoloaders with " + maxClipsPerLoader.ToString() + " clips or more");

            if (nbBeltLoaders > 0)
                interactionReturn.AddExtraLine(nbBeltLoaders.ToString() + " belt feed autoloaders");

            interactionReturn.AddExtraLine(self.BarrelSystem.Coolers.ToString() + " coolers. " + self.Node.nRailgunChargers + " railgun chargers and " + self.Node.nRailgunMagnets + " railgun magnets");
            //else Debug.Log("[Walrus Shells Ballistic Madness] No Shell loaded.");

            return interactionReturn;
        }
    }

    [HarmonyPatch(typeof(ApsTab), "Build")]
    public class AdvFirePredictor
    {
        public static MethodInfo TargetMethod() => typeof(ApsTab).GetMethod("Build", BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly);

        public static void Prefix(ApsTab __instance, AdvCannonFiringPiece ____focus)
        {
            var self = __instance;
            var focus = ____focus;
            var ShellRackModel = focus.ShellRackModel;
            var Node = focus.Node;
            var barrelSystem = focus.BarrelSystem as CannonMultiBarrelSystem;

            string GetLoaderROF()
            {
                float culmROF = 0;
                if (Node.ShellRacks.Racks.Count > 1)
                {
                    foreach (var thisRack in Node.ShellRacks.Racks)
                    {
                        //Debug.Log("[Walrus Shells Ballistic Madness] Checking For Loaded Shell.");

                        if (thisRack.LoadedShell != null)
                        {
                            //Debug.Log("[Walrus Shells Ballistic Madness] Calculating Load Time.");

                            float loadtime = ShellConstants.LoadTimeForShellVolume(new ShellModel_Propellant(thisRack.LoadedShell).GetTotalVolume());

                            //Debug.Log("[Walrus Shells Ballistic Madness] Is it a beltfed rack?");

                            if (thisRack as BeltFeedShellRackModel != null)
                            {
                                BeltFeedShellRackModel belty = thisRack as BeltFeedShellRackModel;
                                //Debug.Log("[Walrus Shells Ballistic Madness] Doing Beltfed Math?");
                                culmROF += 60 / loadtime / belty.LoadingTimeComplexityModifier / 0.2f;
                            }
                            else
                            {
                                //Debug.Log("[Walrus Shells Ballistic Madness] Is it a standard rack?");

                                if (thisRack as ShellRackModel != null)
                                {
                                    ShellRackModel standardRack = thisRack as ShellRackModel;
                                    //Debug.Log("[Walrus Shells Ballistic Madness] Doing Normal Math?");
                                    culmROF += 60 / loadtime / standardRack.MultipleDirectionsSpeedUpFactor() / standardRack.LoadingTimeComplexityModifier / Rounding.R2(Mathf.Min(1f / Mathf.Sqrt(standardRack.LengthCapacity), 1f));
                                }
                            }
                        }
                    }
                }
                return $"Semi-Stable RoF Estimation: <b>{culmROF:0.##} rounds/min</b>";
            }

            string GetBarrelROF()
            {
                var nextShell = Node.ShellRacks.GetNextShell(false);
                if (nextShell == null)
                    return "Burst RoF: No shell loaded";
                float baseCooldown = ShellConstants.CooldownTimeFromVolumeOfPropellant(nextShell.Propellant.GetNormalisedVolumeOfPropellant());
                float cooldownFactor = Traverse.Create(barrelSystem).Method("CooldownFactor").GetValue<float>();
                float rof = 60 / (baseCooldown * cooldownFactor) * barrelSystem.BarrelCount;
                return $"Burst RoF: <b>{rof:0.##} rounds/min</b>";
            }

            string GetRecoilInfo()
            {
                float refresh = Node.HydraulicRefresh;
                float capacity = Node.HydraulicCapacity;
                float dt = 60 / focus.Data.MaxFireRatePerMinute;

                var shell = Node.ShellRacks.GetNextShell(false);
                if (shell == null)
                    return "No shell loaded, recoil information unavailable";

                float overclockFactor = Mathf.Max(focus.Data.CooldownOverClock + 1, 1);

                float railReloadTime = focus.RailReloadTime();
                float railFraction = (railReloadTime < float.PositiveInfinity && railReloadTime > 0)
                    ? Mathf.Clamp01(dt / railReloadTime) : 0;

                float total = overclockFactor * focus.GetRecoilForce(
                    shell.Propellant.GetNormalisedVolumeOfPropellant(),
                    focus.RailgunDraw() * railFraction);

                float reduction = Math.Min(refresh * dt, capacity);
                float actual = total - reduction;

                return
                    $"Recoil per shot at current RoF ({dt:0.####}s between shots): {total:0} - {reduction:0} = <b>{actual:0}</b>\n" +
                    $"Maximum recoil reduction is {capacity:0} and recovers {refresh:0.#} per second";
            }

            string GetAmmoInfo()
            {
                var shell = Node.ShellRacks.GetNextShell(false);
                if (shell == null)
                    return "Ammo Consumption: No shell loaded";

                float rof = focus.Data.MaxFireRatePerMinute / 60;
                float cost = shell.AmmoCost.GetAmmoCost();
                return $"Ammo Consumption: <b>{cost * rof:0} ammo/s</b>";
            }

            //Tuple<float, float> GetReductionForHydraulicLength(int len)
            //{
            //    return new Tuple<float, float>(1250f * len * len, Rounding.R0(10000f * Mathf.Pow(len, 2 / 3f)));
            //}
            //string recoilData = string.Join("\n", new[] { 1, 2, 4, 6, 8 }.Select(x => string.Join(", ", GetReductionForHydraulicLength(x))));

            self.CreateHeader("Rate Of Fire Predictor", new ToolTip(""));
            var seg1 = self.CreateStandardSegment();

            seg1.AddInterpretter(SubjectiveDisplay<AdvCannonFiringPiece>.Quick(focus, M.m<AdvCannonFiringPiece>(x => GetLoaderROF()),
                "Prediction of the cannon's actual rate of fire."));

            seg1.AddInterpretter(SubjectiveDisplay<AdvCannonFiringPiece>.Quick(focus, M.m<AdvCannonFiringPiece>(x => GetBarrelROF()),
                "The maximum burst RoF of the cannon without overclocking. Based on cooldown time."));

            seg1.AddInterpretter(SubjectiveDisplay<AdvCannonFiringPiece>.Quick(focus, M.m<AdvCannonFiringPiece>(x => GetRecoilInfo()),
                "Hydraulic Recoil Absorber data:\n\n" +
                "Length	║ Capacity	│ Refresh Rate\n" +
                "1m	║ 1250		│ 10000\n" +
                "2m	║ 5000		│ 15874\n" +
                "4m	║ 20000		│ 25198\n" +
                "6m	║ 45000		│ 33019\n" +
                "8m	║ 80000		│ 40000"));

            seg1.AddInterpretter(SubjectiveDisplay<AdvCannonFiringPiece>.Quick(focus, M.m<AdvCannonFiringPiece>(x => GetAmmoInfo()),
                "The ammunition requirement of the cannon at current RoF."));
        }
    }
}
