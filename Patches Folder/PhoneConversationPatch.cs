using HarmonyLib;

namespace SOSInsurance
{
    [HarmonyPatch(typeof(HyenaQuest.PhoneController))]
    [HarmonyPatch("OnConversationComplete")]
    public class PhoneConversationCompletePatch
    {
        static bool Prefix(HyenaQuest.PhoneController __instance)
        {
            if (!InsuranceManager.WasOurCall)
                return true; // Not our call — let original run normally

            Plugin.Log.LogInfo("OnConversationComplete intercepted. State: " + InsuranceManager.State);

            // Suppress the original. If we let it run, it calls SetStatus(IDLE) which
            // wipes the phone display and unlocks all buttons, blowing up our menu state.
            // PhoneButtonPatch handles the actual hangup when the player presses 3/Quit,
            // at which point it sets WasOurCall = false so the NEXT OnConversationComplete
            // (from the goodbye message) falls through to the original correctly.
            return false;
        }
    }
}