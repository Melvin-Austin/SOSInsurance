using BepInEx.Configuration;
using UnityEngine;

namespace SOSInsurance
{
    public static class InsuranceManager
    {
        public static bool IsSaleActive = false;
        public static int CurrentTakeoffsRemaining = 0;
        public static MenuState State = MenuState.None;
        public static bool WasOurCall = false;
        public static float LastInputTime = 0f;

        public static float CalculateRetentionPercent()
        {
            if (Plugin.UseRandomLoss.Value)
            {
                int randomLoss = Random.Range(Plugin.RandomLossMin.Value, Plugin.RandomLossMax.Value);
                return Mathf.Clamp(1f - randomLoss / 100f, Plugin.MinRetentionPercent.Value / 100f, 1f);
            }

            int takeoffsUsed = Plugin.DurationTakeoffs.Value - CurrentTakeoffsRemaining;
            float lossPercent = Mathf.Clamp(takeoffsUsed * Plugin.DailyLossPercent.Value, 0, Plugin.MaxLossPercent.Value);
            float retention = Mathf.Clamp((100f - lossPercent) / 100f, Plugin.MinRetentionPercent.Value / 100f, 1f);
            return retention;
        }
    }
}