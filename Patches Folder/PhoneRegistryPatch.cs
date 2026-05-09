using HarmonyLib;
using System.Collections.Generic;
using UnityEngine;

namespace SOSInsurance
{
    [HarmonyPatch(typeof(HyenaQuest.PhoneController))]
    [HarmonyPatch("OnNetworkSpawn")]
    public class PhoneRegistryPatch
    {
        static void Postfix(HyenaQuest.PhoneController __instance)
        {
            var registryField = __instance.GetType().GetField("_phoneRegistry",
                System.Reflection.BindingFlags.NonPublic |
                System.Reflection.BindingFlags.Instance);

            if (registryField == null) { Plugin.Log.LogError("Could not find _phoneRegistry!"); return; }

            var registry = (Dictionary<string, System.Func<HyenaQuest.entity_player, List<string>>>?)registryField.GetValue(__instance);

            if (registry == null) { Plugin.Log.LogError("_phoneRegistry is null!"); return; }

            registry["505"] = (player) =>
            {
                int cost = InsuranceManager.IsSaleActive ? Plugin.SaleCost.Value : Plugin.BaseCost.Value;
                InsuranceManager.State = MenuState.MainMenu;
                InsuranceManager.LastInputTime = Time.time;
                InsuranceManager.WasOurCall = true;

                Plugin.Instance.StartCoroutine(Plugin.UnlockButtonsDelayed(__instance));
                Plugin.Instance.StartCoroutine(Plugin.InactivityWatcher(__instance));

                return new List<string>
                {
                    "S.O.S INSURANCE",
                    InsuranceManager.IsSaleActive ? "TODAY: ON SALE!" : "DIAL FOR OPTIONS",
                    "1: CHECK STATUS",
                    "2: BUY/RENEW $" + cost,
                    "3: QUIT"
                };
            };

            Plugin.Log.LogInfo("505 registered!");
        }
    }
}