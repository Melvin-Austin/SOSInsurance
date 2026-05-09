using System.Collections.Generic;
using Unity.Collections;

namespace SOSInsurance
{
    public static class PhoneHelper
    {
        private static readonly Dictionary<string, int> BUTTON_MAP = new Dictionary<string, int>
        {
            { "1", 0 }, { "2", 1 }, { "3", 2 }, { "4", 3 }, { "5", 4 }, { "6", 5 },
            { "7", 6 }, { "8", 7 }, { "9", 8 }, { "CLEAR", 9 }, { "0", 10 }, { "CALL", 11 }
        };

        public static void GoToPostAction(HyenaQuest.PhoneController phone, List<string> messages)
        {
            InsuranceManager.State = MenuState.PostAction;
            SetButtonsLocked(phone, false);
            SetSpecificButtonsLocked(phone,
                new[] { "4", "5", "6", "7", "8", "9", "0", "CALL", "CLEAR" }, true);
            ChatRPC(phone, messages);
        }

        public static void SetSpecificButtonsLocked(HyenaQuest.PhoneController phone, string[] buttons, bool locked)
        {
            try
            {
                var buttonsField = phone.GetType().GetField("phoneButtons",
                    System.Reflection.BindingFlags.Public |
                    System.Reflection.BindingFlags.Instance);

                if (buttonsField == null) { Plugin.Log.LogError("phoneButtons not found!"); return; }

                var phoneButtons = buttonsField.GetValue(phone) as List<HyenaQuest.entity_button>;
                if (phoneButtons == null) { Plugin.Log.LogError("phoneButtons is null!"); return; }

                foreach (var btn in buttons)
                {
                    if (BUTTON_MAP.TryGetValue(btn, out int idx) && idx < phoneButtons.Count)
                        phoneButtons[idx].SetLocked(locked);
                }
            }
            catch (System.Exception e)
            {
                Plugin.Log.LogError("SetSpecificButtonsLocked error: " + e.ToString());
            }
        }

        public static void SetButtonsLocked(HyenaQuest.PhoneController phone, bool locked)
        {
            try
            {
                var method = phone.GetType().GetMethod("SetButtonsLocked",
                    System.Reflection.BindingFlags.NonPublic |
                    System.Reflection.BindingFlags.Instance);

                if (method == null) { Plugin.Log.LogError("SetButtonsLocked not found!"); return; }

                method.Invoke(phone, new object[] { locked });
            }
            catch (System.Exception e)
            {
                Plugin.Log.LogError("SetButtonsLocked error: " + e.ToString());
            }
        }

        public static void ChatRPC(HyenaQuest.PhoneController phone, List<string> messages)
        {
            try
            {
                var method = phone.GetType().GetMethod("ChatRPC",
                    System.Reflection.BindingFlags.NonPublic |
                    System.Reflection.BindingFlags.Instance);

                if (method == null) { Plugin.Log.LogError("ChatRPC not found!"); return; }

                var nsType = method.GetParameters()[0].ParameterType;
                var dataField = nsType.GetField("data");
                if (dataField == null) { Plugin.Log.LogError("NetworkStrings.data not found!"); return; }

                var ns = System.Activator.CreateInstance(nsType);
                var arr = System.Array.CreateInstance(typeof(FixedString512Bytes), messages.Count);
                for (int i = 0; i < messages.Count; i++)
                    arr.SetValue(new FixedString512Bytes(messages[i]), i);
                dataField.SetValue(ns, arr);

                method.Invoke(phone, new object[] { ns });
            }
            catch (System.Exception e)
            {
                Plugin.Log.LogError("ChatRPC error: " + e.ToString());
            }
        }
    }
}