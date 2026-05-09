using HarmonyLib;
using System.Collections;
using UnityEngine;

namespace SOSInsurance
{
    [HarmonyPatch(typeof(HyenaQuest.ScrapController))]
    [HarmonyPatch("OnIngameStatusUpdated")]
    public class ScrapProtectionPatch
    {
        static int _savedScrap = 0;
        static bool _pendingRestore = false;

        static void Prefix(
            HyenaQuest.ScrapController __instance,
            HyenaQuest.INGAME_STATUS status,
            bool server)
        {
            if (!server) return;

            if (status == HyenaQuest.INGAME_STATUS.ROUND_END)
            {
                if (InsuranceManager.CurrentTakeoffsRemaining <= 0) return;

                int currentScrap = __instance.GetClaimedScrap();
                if (currentScrap <= 0) return;

                float retention = InsuranceManager.CalculateRetentionPercent();
                _savedScrap = Mathf.FloorToInt(currentScrap * retention);
                int lost = currentScrap - _savedScrap;
                _pendingRestore = true;

                InsuranceManager.CurrentTakeoffsRemaining--;
                Plugin.Log.LogInfo($"[SOS] Round end: saving {_savedScrap} scrap ({retention * 100f:F0}% retention), losing {lost}. Takeoffs remaining: {InsuranceManager.CurrentTakeoffsRemaining}");
            }
        }

        static void Postfix(
            HyenaQuest.ScrapController __instance,
            HyenaQuest.INGAME_STATUS status,
            bool server)
        {
            if (!server) return;

            // IDLE fires after ROUND_END and calls SetScrap(0) internally.
            // By the time our Postfix runs here, that SetScrap(0) has already happened.
            // This is the safe point to restore.
            if (status == HyenaQuest.INGAME_STATUS.IDLE && _pendingRestore)
            {
                _pendingRestore = false;
                int amount = _savedScrap;
                Plugin.Log.LogInfo($"[SOS] IDLE reached, restoring {amount} scrap now.");
                Plugin.Instance.StartCoroutine(RestoreNextFrame(amount));
            }
        }

        static IEnumerator RestoreNextFrame(int amount)
        {
            // Wait one frame to make sure all IDLE handlers have finished.
            yield return null;

            var scrapCtrl = HyenaQuest.NetController<HyenaQuest.ScrapController>.Instance;
            if (scrapCtrl == null)
            {
                Plugin.Log.LogError("[SOS] ScrapController instance gone at restore time!");
                yield break;
            }

            try
            {
                scrapCtrl.Add(amount);
                Plugin.Log.LogInfo($"[SOS] Restored {amount} scrap via Add().");
                BroadcastNotification(amount);
            }
            catch (System.Exception e)
            {
                Plugin.Log.LogError("[SOS] Restore failed: " + e);
            }
        }

        static void SpawnVacuumWithScrap(int amount)
        {
            try
            {
                var storeCtrl = HyenaQuest.NetController<HyenaQuest.StoreController>.Instance;
                if (storeCtrl == null)
                {
                    Plugin.Log.LogError("[SOS] StoreController not found! Falling back to direct restore.");
                    FallbackRestore(amount);
                    return;
                }

                var lookup = storeCtrl.GetStoreItemLookup();
                if (!lookup.TryGetValue("item_vacuum", out var storeItem) || storeItem.itemPrefab == null)
                {
                    Plugin.Log.LogError("[SOS] item_vacuum prefab not found! Falling back to direct restore.");
                    FallbackRestore(amount);
                    return;
                }

                var spawnPos = storeCtrl.itemSpawnPosition != null
                    ? storeCtrl.itemSpawnPosition.position
                    : Vector3.zero;

                var obj = GameObject.Instantiate(storeItem.itemPrefab, spawnPos, Quaternion.identity);
                if (obj == null)
                {
                    Plugin.Log.LogError("[SOS] Failed to instantiate vacuum! Falling back.");
                    FallbackRestore(amount);
                    return;
                }

                var netObj = obj.GetComponent<Unity.Netcode.NetworkObject>();
                if (netObj == null)
                {
                    GameObject.Destroy(obj);
                    Plugin.Log.LogError("[SOS] No NetworkObject on vacuum! Falling back.");
                    FallbackRestore(amount);
                    return;
                }

                netObj.Spawn(destroyWithScene: true);

                var vacuum = obj.GetComponent<HyenaQuest.entity_item_vacuum>();
                if (vacuum != null)
                {
                    vacuum.SetScrap(amount);
                    Plugin.Log.LogInfo($"[SOS] Spawned vacuum with {amount} scrap.");
                    BroadcastNotification(amount);
                }
                else
                {
                    Plugin.Log.LogError("[SOS] entity_item_vacuum component not found! Falling back.");
                    FallbackRestore(amount);
                }
            }
            catch (System.Exception e)
            {
                Plugin.Log.LogError("[SOS] SpawnVacuumWithScrap failed, falling back: " + e);
                FallbackRestore(amount);
            }
        }

        static void FallbackRestore(int amount)
        {
            var scrapCtrl = HyenaQuest.NetController<HyenaQuest.ScrapController>.Instance;
            if (scrapCtrl != null)
            {
                scrapCtrl.Add(amount);
                Plugin.Log.LogInfo($"[SOS] Fallback restored {amount} scrap via Add().");
                BroadcastNotification(amount);
            }
            else
            {
                Plugin.Log.LogError("[SOS] ScrapController not found for fallback restore!");
            }
        }

        static void BroadcastNotification(int amount)
        {
            var notifCtrl = HyenaQuest.NetController<HyenaQuest.NotificationController>.Instance;
            notifCtrl?.BroadcastAllRPC(new HyenaQuest.NotificationData
            {
                id = "sos-claim",
                text = $"SOS INSURANCE SAVED {amount}",
                duration = 6f
            });
        }
    }
}