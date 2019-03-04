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
            var LengthCapacity = self.LengthCapacity;
            var interactionReturn = __result;

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

            float culmROF = 0;
            float barrelROF = 0;

            void ResetROF()
            {
                culmROF = 0;
                if (Node.ShellRacks.Racks.Count > 1)
                {
                    foreach (var thisRack in Node.ShellRacks.Racks)
                    {
                        Debug.Log("[Walrus Shells Ballistic Madness] Checking For Loaded Shell.");

                        if (thisRack.LoadedShell != null)
                        {
                            Debug.Log("[Walrus Shells Ballistic Madness] Calculating Load Time.");

                            float loadtime = ShellConstants.LoadTimeForShellVolume(new ShellModel_Propellant(thisRack.LoadedShell).GetTotalVolume());

                            Debug.Log("[Walrus Shells Ballistic Madness] Is it a beltfed rack?");

                            if (thisRack as BeltFeedShellRackModel != null)
                            {
                                BeltFeedShellRackModel belty = thisRack as BeltFeedShellRackModel;
                                Debug.Log("[Walrus Shells Ballistic Madness] Doing Beltfed Math?");
                                culmROF += 60 / loadtime / belty.LoadingTimeComplexityModifier / 0.2f;
                            }
                            else
                            {
                                Debug.Log("[Walrus Shells Ballistic Madness] Is it a standard rack?");

                                if (thisRack as ShellRackModel != null)
                                {
                                    ShellRackModel standardRack = thisRack as ShellRackModel;
                                    Debug.Log("[Walrus Shells Ballistic Madness] Doing Normal Math?");
                                    culmROF += 60 / loadtime / standardRack.MultipleDirectionsSpeedUpFactor() / standardRack.LoadingTimeComplexityModifier / Rounding.R2(Mathf.Min(1f / Mathf.Sqrt(standardRack.LengthCapacity), 1f));
                                }
                            }
                        }
                    }
                }
            }

            void ResetBarrelROF()
            {
                var nextShell = Node.ShellRacks.GetNextShell(false);
                if (nextShell == null)
                    return;
                float baseCooldown = ShellConstants.CooldownTimeFromVolumeOfPropellant(nextShell.Propellant.GetNormalisedVolumeOfPropellant());
                float cooldownFactor = Traverse.Create(barrelSystem).Method("CooldownFactor").GetValue<float>();
                barrelROF = 60 / (baseCooldown * cooldownFactor) * barrelSystem.BarrelCount;
            }

            if (culmROF == 0)
                ResetROF();
            if (barrelROF == 0)
                ResetBarrelROF();

            self.CreateHeader("Rate Of Fire Predictor", new ToolTip(""));

            var seg1 = self.CreateStandardHorizontalSegment();
            seg1.AddInterpretter(SubjectiveDisplay<AdvCannonFiringPiece>.Quick(focus, M.m<AdvCannonFiringPiece>(x => "Semi-Stable RoF Estimation: <b>" + Rounding.R2(culmROF) + " rounds/min</b>"), "Prediction of the cannon's actual rate of fire."));
            seg1.AddInterpretter(SubjectiveButton<AdvCannonFiringPiece>.Quick(focus, "Update", new ToolTip("Recalculate the estimation"), x =>
            {
                ResetROF();
            }));

            var seg2 = self.CreateStandardHorizontalSegment();
            seg2.AddInterpretter(SubjectiveDisplay<AdvCannonFiringPiece>.Quick(focus, M.m<AdvCannonFiringPiece>(x => "Barrel Maximum RoF: <b>" + Rounding.R2(barrelROF) + " rounds/min</b>"), "The maximum instantaneous RoF of the cannon without overclocking. Add Cooling Vents to increase."));
            seg2.AddInterpretter(SubjectiveButton<AdvCannonFiringPiece>.Quick(focus, "Update", new ToolTip("Recalculate"), x =>
            {
                ResetBarrelROF();
            }));
        }
    }
}
