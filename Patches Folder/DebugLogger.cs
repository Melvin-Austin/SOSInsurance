using HarmonyLib;
using System.Diagnostics;

namespace SOSInsurance
{
    [HarmonyPatch(typeof(HyenaQuest.ScrapController))]
    [HarmonyPatch("SetScrap")]
    public class Debug_SetScrap
    {
        static void Prefix(int scrap)
        {
            if (!Plugin.DebugLogging.Value) return;
            Plugin.Log.LogInfo($"[DEBUG] SetScrap({scrap})\n{new StackTrace()}");
        }
    }

    [HarmonyPatch(typeof(HyenaQuest.ScrapController))]
    [HarmonyPatch("Add")]
    public class Debug_Add
    {
        static void Prefix(int scrap)
        {
            if (!Plugin.DebugLogging.Value) return;
            Plugin.Log.LogInfo($"[DEBUG] Add({scrap})\n{new StackTrace()}");
        }
    }

    [HarmonyPatch(typeof(HyenaQuest.ScrapController))]
    [HarmonyPatch("Pay")]
    public class Debug_Pay
    {
        static void Prefix(int scrap)
        {
            if (!Plugin.DebugLogging.Value) return;
            Plugin.Log.LogInfo($"[DEBUG] Pay({scrap})\n{new StackTrace()}");
        }
    }

    [HarmonyPatch(typeof(HyenaQuest.ScrapController))]
    [HarmonyPatch("OnIngameStatusUpdated")]
    public class Debug_OnIngameStatusUpdated
    {
        static void Prefix(HyenaQuest.INGAME_STATUS status, bool server)
        {
            if (!Plugin.DebugLogging.Value) return;
            Plugin.Log.LogInfo($"[DEBUG] OnIngameStatusUpdated(status={status}, server={server})\n{new StackTrace()}");
        }
    }

    [HarmonyPatch(typeof(HyenaQuest.ScrapController))]
    [HarmonyPatch("GetClaimedScrap")]
    public class Debug_GetClaimedScrap
    {
        static void Postfix(int __result)
        {
            if (!Plugin.DebugLogging.Value) return;
            Plugin.Log.LogInfo($"[DEBUG] GetClaimedScrap() = {__result}");
        }
    }
}