using BepInEx.Logging;
using flanne;
using flanne.Core;
using flanne.Pickups;
using flanne.UI;
using flanne.PowerupSystem;
using HarmonyLib;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using Logger = BepInEx.Logging.Logger;
using Object = UnityEngine.Object;

namespace BetterMagnetPlugin
{
    static class MagnetPatch
    {
        static ManualLogSource logSource = new ManualLogSource("BetterMagnetPlugin");

        static GameController gameController;

        [HarmonyPatch(typeof(PlayerController), "Start")]
        [HarmonyPrefix]
        static void PlayerStart(PlayerController __instance)
        {
            gameController = Object.FindObjectOfType<GameController>();
        }

        [HarmonyPatch(typeof(PlayerController), "Update")]
        [HarmonyPrefix]
        static void PlayerUpdate(PlayerController __instance)
        {
            if (!pullTowards || !(gameController.CurrentState is CombatState))
            {
                return;
            }

            for (int i = 0; i < xpPickups.Count; i++)
            {
                XPPickup xpPickup = xpPickups[i];

                if (xpPickup == null)
                {
                    xpPickups.Remove(xpPickup);
                    continue;
                }

                Vector3 direction = __instance.transform.position - xpPickup.transform.position;
                direction /= direction.sqrMagnitude;

                if (direction.magnitude < 0.05f)
                {
                    direction = direction.normalized * 0.05f;
                } else if (direction.magnitude > 0.8f)
                {
                    direction = direction.normalized * 0.8f;
                }

                xpPickup.transform.position += direction;
            }

            if (xpPickups.Count <= 0)
            {
                pullTowards = false;
            }
        }

        [HarmonyPatch(typeof(XPPickup), "UsePickup")]
        [HarmonyPrefix]
        static void BuffOnXPStart(XPPickup __instance)
        {
            xpPickups.Remove(__instance);
        }

        static AccessTools.FieldRef<PowerupDescription, TMP_Text> textFieldRef = AccessTools.FieldRefAccess<PowerupDescription, TMP_Text>("descriptionTMP");

        [HarmonyPatch(typeof(PowerupDescription), "Refresh")]
        [HarmonyPostfix]
        static void PowerupGeneratorConstructor(PowerupDescription __instance)
        { 
            if (__instance.data)
            {
                if (__instance.data.nameString == "Excitement")
                {
                    //Replace the default text by the most hacky thing I could think of
                    textFieldRef(__instance).SetText("Pickup Range <color=#f5d6c1>+20%</color><br>After picking up XP, gain <color=#f5d6c1>35%</color> Fire Rate for <color=#f5d6c1>1</color> second.<br>Every <color=#f5d6c1>40 seconds</color>, pull all XP on the ground toward you.");
                }
            }
        }

        static List<XPPickup> xpPickups = new List<XPPickup>();

        [HarmonyPatch(typeof(BuffOnXP), "Start")]
        [HarmonyPrefix]
        static void BuffOnXPStartWithCoroutine(BuffOnXP __instance)
        {
            // I can't access Update, so I recursive coroutine instead :)
            __instance.StartCoroutine(GatherXP(__instance));
        }

        static bool pullTowards = false;

        static private IEnumerator GatherXP(BuffOnXP __instance)
        {
            yield return new WaitForSeconds(40.0f);

            xpPickups = new List<XPPickup>(Object.FindObjectsOfType<XPPickup>());
            pullTowards = true;

            // Who needs an exit condition anyways?
            __instance.StartCoroutine(GatherXP(__instance));
        }
        
        static void Log(string m) {
            logSource.LogInfo(m);
        }
        private static void EndLog()
        {
            Logger.Sources.Remove(logSource);
        }

        private static void StartLog()
        {
            Logger.Sources.Add(logSource);
        }

    }
}
