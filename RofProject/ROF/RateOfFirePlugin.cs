using System;
using System.Collections.Generic;
using System.Reflection;
using BrilliantSkies.Blocks.Weapons.Uis;
using BrilliantSkies.Modding;
using Harmony;
using UnityEngine;
using Console = BrilliantSkies.Ui.Special.Overlays.Console;

namespace ROF
{
    // This is required, and provides necessary information to FTD, and provides some important hooks.
    public class ROFPlugin : GamePlugin
    {
        //public static Dictionary<int, float> culmROF = new Dictionary<int, float>();

        /// <summary>
        /// This Delimiter is Ooalroos, my best attempt at converting "Walrus" to a number.
        /// </summary>
        /// <param name="DelimiterLevel"></param>
        /// <returns></returns>
        private const int THIS_MOD_UNIQUE_DELIMITER = 00412105;

        public void OnLoad()
        {
            var harmony = HarmonyInstance.Create("com.walrusjones.rof");
            //harmony.PatchAll(Assembly.GetExecutingAssembly());
            Debug.Log("[ROF UI] 0/4");
            harmony.Patch(
                typeof(AdvCannonAutoloader).GetMethod("Secondary", BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly),
                postfix: new HarmonyMethod(((Func<InteractionReturn, AdvCannonAutoloader, InteractionReturn>)RateOfFirePredictor.Postfix).Method));
            Debug.Log("[ROF UI] 1/4");
            harmony.Patch(
                typeof(AdvCannonBeltFeedAutoloader).GetMethod("Secondary", new Type[] { }),
                postfix: new HarmonyMethod(((Func<InteractionReturn, AdvCannonBeltFeedAutoloader, InteractionReturn>)BurstFirePredictor.Postfix).Method));
            Debug.Log("[ROF UI] 2/4");
            harmony.Patch(
                typeof(AdvCannonFiringPiece).GetMethod("Secondary", new Type[] { }),
                postfix: new HarmonyMethod(((Func<InteractionReturn, AdvCannonFiringPiece, InteractionReturn>)ApsInfo.Postfix).Method));
            Debug.Log("[ROF UI] 3/4");
            harmony.Patch(
                typeof(ApsTab).GetMethod("Build", BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly),
                prefix: new HarmonyMethod(((Action<ApsTab, AdvCannonFiringPiece>)AdvFirePredictor.Prefix).Method));
            Debug.Log("[ROF UI] 4/4");
            Console.WriteToConsole("Enjoy the UI upgrade!");
            Debug.Log("[ROF UI] The ROF UI is loaded");
        }

        public void OnSave()
        {
        }

        // Required.
        public string name => "ROF UI";

        // Also required.
        public Version version => new Version("0.1.0");
    }
}
