using HarmonyLib;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace SOSInsurance
{
    [HarmonyPatch]
    public class PhoneButtonPatch
    {
        static MethodBase TargetMethod()
        {
            return AccessTools.Method(typeof(HyenaQuest.PhoneController), "OnButtonPress");
        }

        static bool Prefix(HyenaQuest.PhoneController __instance, HyenaQuest.entity_player caller, string number)
        {
            if (InsuranceManager.State == MenuState.None)
                return true;

            InsuranceManager.LastInputTime = Time.time;
            Plugin.Log.LogInfo($"OnButtonPress: number={number} state={InsuranceManager.State}");

            if (number == "CLEAR")
                return false;

            if (InsuranceManager.State == MenuState.MainMenu)
            {
                if (number == "CALL")
                    return false;

                switch (number)
                {
                    case "1":
                        PhoneHelper.GoToPostAction(__instance, new List<string>
                        {
                            "S.O.S INSURANCE",
                            InsuranceManager.CurrentTakeoffsRemaining > 0 ? "STATUS: ACTIVE" : "STATUS: INACTIVE",
                            InsuranceManager.CurrentTakeoffsRemaining > 0
                                ? InsuranceManager.CurrentTakeoffsRemaining + " TAKEOFFS LEFT"
                                : "NO COVERAGE",
                            "1: MENU  2: BUY  3: QUIT"
                        });
                        break;

                    case "2":
                        if (InsuranceManager.CurrentTakeoffsRemaining >= Plugin.MaxStackedTakeoffs.Value)
                        {
                            PhoneHelper.GoToPostAction(__instance, new List<string>
                            {
                                "S.O.S INSURANCE",
                                "MAX COVERAGE REACHED",
                                "1: MENU  3: QUIT"
                            });
                            break;
                        }
                        int cost = InsuranceManager.IsSaleActive ? Plugin.SaleCost.Value : Plugin.BaseCost.Value;

                        var currencyCtrl = HyenaQuest.NetController<HyenaQuest.CurrencyController>.Instance;
                        if (currencyCtrl == null || !currencyCtrl.CanPay(cost))
                        {
                            PhoneHelper.GoToPostAction(__instance, new List<string>
                            {
                                "S.O.S INSURANCE",
                                "INSUFFICIENT FUNDS",
                                "COST: $" + cost,
                                "1: MENU  3: QUIT"
                            });
                            break;
                        }

                        InsuranceManager.State = MenuState.ConfirmBuy;
                        PhoneHelper.SetButtonsLocked(__instance, false);
                        PhoneHelper.SetSpecificButtonsLocked(__instance,
                            new[] { "1", "2", "3", "4", "5", "6", "7", "8", "9", "CLEAR" }, true);
                        PhoneHelper.ChatRPC(__instance, new List<string>
                        {
                            "S.O.S INSURANCE",
                            "COST: $" + cost,
                            "ADDS " + Plugin.DurationTakeoffs.Value + " TAKEOFFS",
                            "OK: CONFIRM",
                            "0: CANCEL"
                        });
                        break;

                    case "3":
                        InsuranceManager.State = MenuState.None;
                        InsuranceManager.WasOurCall = false;
                        PhoneHelper.SetSpecificButtonsLocked(__instance, new[] { "3" }, false);
                        PhoneHelper.SetButtonsLocked(__instance, true);
                        PhoneHelper.ChatRPC(__instance, new List<string>
                        {
                            "S.O.S INSURANCE",
                            "STAY SAFE!"
                        });
                        break;
                }
                return false;
            }

            if (InsuranceManager.State == MenuState.ConfirmBuy)
            {
                switch (number)
                {
                    case "CALL":
                        int cost = InsuranceManager.IsSaleActive ? Plugin.SaleCost.Value : Plugin.BaseCost.Value;
                        var currencyCtrl = HyenaQuest.NetController<HyenaQuest.CurrencyController>.Instance;

                        if (currencyCtrl == null)
                        {
                            Plugin.Log.LogError("CurrencyController not found!");
                            PhoneHelper.GoToPostAction(__instance, new List<string>
                            {
                                "S.O.S INSURANCE",
                                "ERROR: TRY AGAIN",
                                "1: MENU  3: QUIT"
                            });
                            break;
                        }

                        if (!currencyCtrl.Pay(cost))
                        {
                            PhoneHelper.GoToPostAction(__instance, new List<string>
                            {
                                "S.O.S INSURANCE",
                                "INSUFFICIENT FUNDS",
                                "1: MENU  3: QUIT"
                            });
                            break;
                        }

                        int newTotal = Mathf.Min(
                            InsuranceManager.CurrentTakeoffsRemaining + Plugin.DurationTakeoffs.Value,
                            Plugin.MaxStackedTakeoffs.Value
                        );
                        InsuranceManager.CurrentTakeoffsRemaining = newTotal;
                        Plugin.Log.LogInfo($"Insurance purchased for ${cost}! Takeoffs: {newTotal}");

                        PhoneHelper.GoToPostAction(__instance, new List<string>
                        {
                            "NOW PROTECTED.",
                            "$" + cost + " DEDUCTED",
                            newTotal + " TAKEOFFS LEFT",
                            "1: MENU  2: BUY MORE  3: QUIT"
                        });
                        break;

                    case "0":
                        PhoneHelper.GoToPostAction(__instance, new List<string>
                        {
                            "S.O.S INSURANCE",
                            "PURCHASE CANCELLED",
                            "1: MENU  2: BUY  3: QUIT"
                        });
                        break;
                }
                return false;
            }

            if (InsuranceManager.State == MenuState.PostAction)
            {
                if (number == "CALL")
                    return false;

                switch (number)
                {
                    case "1":
                        int cost = InsuranceManager.IsSaleActive ? Plugin.SaleCost.Value : Plugin.BaseCost.Value;
                        InsuranceManager.State = MenuState.MainMenu;
                        PhoneHelper.SetButtonsLocked(__instance, false);
                        PhoneHelper.SetSpecificButtonsLocked(__instance,
                            new[] { "4", "5", "6", "7", "8", "9", "0", "CALL", "CLEAR" }, true);
                        PhoneHelper.ChatRPC(__instance, new List<string>
                        {
                            "S.O.S INSURANCE",
                            InsuranceManager.IsSaleActive ? "TODAY: ON SALE!" : "DIAL FOR OPTIONS",
                            "1: CHECK STATUS",
                            "2: BUY/RENEW $" + cost,
                            "3: QUIT"
                        });
                        break;

                    case "2":
                        if (InsuranceManager.CurrentTakeoffsRemaining >= Plugin.MaxStackedTakeoffs.Value)
                        {
                            PhoneHelper.GoToPostAction(__instance, new List<string>
                            {
                                "S.O.S INSURANCE",
                                "MAX COVERAGE REACHED",
                                "1: MENU  3: QUIT"
                            });
                            break;
                        }
                        int buyCost = InsuranceManager.IsSaleActive ? Plugin.SaleCost.Value : Plugin.BaseCost.Value;

                        var currencyCtrl2 = HyenaQuest.NetController<HyenaQuest.CurrencyController>.Instance;
                        if (currencyCtrl2 == null || !currencyCtrl2.CanPay(buyCost))
                        {
                            PhoneHelper.GoToPostAction(__instance, new List<string>
                            {
                                "S.O.S INSURANCE",
                                "INSUFFICIENT FUNDS",
                                "COST: $" + buyCost,
                                "1: MENU  3: QUIT"
                            });
                            break;
                        }

                        InsuranceManager.State = MenuState.ConfirmBuy;
                        PhoneHelper.SetButtonsLocked(__instance, false);
                        PhoneHelper.SetSpecificButtonsLocked(__instance,
                            new[] { "1", "2", "3", "4", "5", "6", "7", "8", "9", "CLEAR" }, true);
                        PhoneHelper.ChatRPC(__instance, new List<string>
                        {
                            "S.O.S INSURANCE",
                            "COST: $" + buyCost,
                            "ADDS " + Plugin.DurationTakeoffs.Value + " TAKEOFFS",
                            "OK: CONFIRM",
                            "0: CANCEL"
                        });
                        break;

                    case "3":
                        InsuranceManager.State = MenuState.None;
                        InsuranceManager.WasOurCall = false;
                        PhoneHelper.SetSpecificButtonsLocked(__instance, new[] { "3" }, false);
                        PhoneHelper.SetButtonsLocked(__instance, true);
                        PhoneHelper.ChatRPC(__instance, new List<string>
                        {
                            "S.O.S INSURANCE",
                            "STAY SAFE!"
                        });
                        break;
                }
                return false;
            }

            return false;
        }
    }
}