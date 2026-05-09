using BepInEx;
using BepInEx.Logging;
using BepInEx.Configuration;
using HarmonyLib;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SOSInsurance
{
    [BepInPlugin("sosinsurance", "SOS Insurance", "1.0.0")]
    public class Plugin : BaseUnityPlugin
    {
        public static Plugin Instance = null!;
        public static ManualLogSource Log = null!;
        private Harmony harmony = null!;

        public static ConfigEntry<bool> SaleAnnouncement = null!;
        public static ConfigEntry<int> ExpiryWarningTakeoffs = null!;
        public static ConfigEntry<int> BaseCost = null!;
        public static ConfigEntry<int> SaleCost = null!;
        public static ConfigEntry<int> SaleChance = null!;
        public static ConfigEntry<int> DurationTakeoffs = null!;
        public static ConfigEntry<int> MaxStackedTakeoffs = null!;
        public static ConfigEntry<int> DailyLossPercent = null!;
        public static ConfigEntry<int> MaxLossPercent = null!;
        public static ConfigEntry<int> MinRetentionPercent = null!;
        public static ConfigEntry<bool> UseRandomLoss = null!;
        public static ConfigEntry<int> RandomLossMin = null!;
        public static ConfigEntry<int> RandomLossMax = null!;
        public static ConfigEntry<int> InactivityTimeout = null!;
        public static ConfigEntry<bool> DebugLogging = null!;

        void Awake()
        {
            Instance = this;
            Log = Logger;

            SaleAnnouncement = Config.Bind("General", "SaleAnnouncement", true, "Show a message when insurance is on sale.");
            ExpiryWarningTakeoffs = Config.Bind("General", "ExpiryWarningTakeoffs", 2, "Warn player when this many takeoffs remain.");
            InactivityTimeout = Config.Bind("General", "InactivityTimeout", 120, "Seconds before an idle call is ended automatically.");
            DebugLogging = Config.Bind("General", "DebugLogging", false, "Enable verbose debug logging for ScrapController calls.");
            BaseCost = Config.Bind("Cost", "BaseCost", 350, "Normal cost of insurance.");
            SaleCost = Config.Bind("Cost", "SaleCost", 150, "Sale cost of insurance.");
            SaleChance = Config.Bind("Cost", "SaleChance", 10, "Percent chance of sale per session.");
            DurationTakeoffs = Config.Bind("Coverage", "DurationTakeoffs", 7, "How many takeoffs insurance lasts.");
            MaxStackedTakeoffs = Config.Bind("Coverage", "MaxStackedTakeoffs", 21, "Maximum takeoffs you can stack.");
            DailyLossPercent = Config.Bind("Loss", "DailyLossPercent", 5, "Percent of scrap lost per takeoff.");
            MaxLossPercent = Config.Bind("Loss", "MaxLossPercent", 100, "Maximum scrap loss percent.");
            MinRetentionPercent = Config.Bind("Loss", "MinRetentionPercent", 10, "Minimum scrap kept even at max loss.");
            UseRandomLoss = Config.Bind("Loss", "UseRandomLoss", false, "Use random loss instead of daily scaling.");
            RandomLossMin = Config.Bind("Loss", "RandomLossMin", 0, "Minimum random loss percent.");
            RandomLossMax = Config.Bind("Loss", "RandomLossMax", 100, "Maximum random loss percent.");

            int roll = Random.Range(0, 100);
            InsuranceManager.IsSaleActive = roll < SaleChance.Value;
            Log.LogInfo("SOS Insurance loaded! Sale active: " + InsuranceManager.IsSaleActive);

            harmony = new Harmony("sosinsurance");
            harmony.PatchAll();
        }

        public static IEnumerator UnlockButtonsDelayed(HyenaQuest.PhoneController phone)
        {
            yield return new WaitForSeconds(0.15f);
            PhoneHelper.SetButtonsLocked(phone, false);
            PhoneHelper.SetSpecificButtonsLocked(phone,
                new[] { "4", "5", "6", "7", "8", "9", "0", "CLEAR" }, true);
            Log.LogInfo("Buttons unlocked for main menu.");
        }

        public static IEnumerator InactivityWatcher(HyenaQuest.PhoneController phone)
        {
            Log.LogInfo("Inactivity watcher started.");
            while (InsuranceManager.State != MenuState.None)
            {
                yield return new WaitForSeconds(1f);

                if (InsuranceManager.State == MenuState.None)
                    yield break;

                if (Time.time - InsuranceManager.LastInputTime > InactivityTimeout.Value)
                {
                    Log.LogInfo("Inactivity timeout — ending call.");
                    InsuranceManager.State = MenuState.None;
                    InsuranceManager.WasOurCall = false;
                    PhoneHelper.SetButtonsLocked(phone, true);
                    PhoneHelper.ChatRPC(phone, new List<string>
                    {
                        "S.O.S INSURANCE",
                        "CALL TIMED OUT.",
                        "STAY SAFE!"
                    });
                    yield break;
                }
            }
            Log.LogInfo("Inactivity watcher ended (state cleared).");
        }
    }
}