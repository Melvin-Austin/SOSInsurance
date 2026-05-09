using System;
using System.Collections.Generic;
using System.Linq;
using Steamworks;
using Unity.Netcode;
using UnityEngine;

namespace HyenaQuest;

[DisallowMultipleComponent]
[RequireComponent(typeof(NetworkObject))]
[DefaultExecutionOrder(-70)]
public class StatsController : NetController<StatsController>
{
	public GameEvent<STEAM_STATS, int> OnLocalPlayerStatUpdate = new GameEvent<STEAM_STATS, int>();

	public GameEvent OnLocalPlayerStatsLoaded = new GameEvent();

	public GameEvent<STEAM_ACHIEVEMENTS, bool> OnAchievementUpdate = new GameEvent<STEAM_ACHIEVEMENTS, bool>();

	public GameEvent OnAchievementsUpdate = new GameEvent();

	private readonly Dictionary<byte, int> _totalScrapRegistry = new Dictionary<byte, int>();

	private readonly Dictionary<byte, int> _totalDeliveryRegistry = new Dictionary<byte, int>();

	private readonly Dictionary<STEAM_STATS, int> _localPlayerStats = new Dictionary<STEAM_STATS, int>
	{
		{
			STEAM_STATS.DEATHS,
			0
		},
		{
			STEAM_STATS.SCRAPS,
			0
		},
		{
			STEAM_STATS.DELIVERIES,
			0
		},
		{
			STEAM_STATS.ARRESTED,
			0
		},
		{
			STEAM_STATS.ROUNDS,
			0
		}
	};

	private readonly Dictionary<STEAM_ACHIEVEMENTS, bool> _localPlayerAchievements = new Dictionary<STEAM_ACHIEVEMENTS, bool>
	{
		{
			STEAM_ACHIEVEMENTS.ACHIEVEMENT_PIZZA,
			false
		},
		{
			STEAM_ACHIEVEMENTS.ACHIEVEMENT_FORBIDDEN_PIZZA,
			false
		},
		{
			STEAM_ACHIEVEMENTS.ACHIEVEMENT_MISSING_TEXTURE,
			false
		},
		{
			STEAM_ACHIEVEMENTS.ACHIEVEMENT_OUT_OF_BOUNDS,
			false
		},
		{
			STEAM_ACHIEVEMENTS.ACHIEVEMENT_SURFER,
			false
		},
		{
			STEAM_ACHIEVEMENTS.ACHIEVEMENT_FORBIDDEN_LOVE,
			false
		},
		{
			STEAM_ACHIEVEMENTS.ACHIEVEMENT_BRICKY,
			false
		},
		{
			STEAM_ACHIEVEMENTS.ACHIEVEMENT_PRACTICE,
			false
		},
		{
			STEAM_ACHIEVEMENTS.ACHIEVEMENT_UNIVERSE_LOAD,
			false
		},
		{
			STEAM_ACHIEVEMENTS.ACHIEVEMENT_SPEEDRUN,
			false
		},
		{
			STEAM_ACHIEVEMENTS.ACHIEVEMENT_URCHIN,
			false
		},
		{
			STEAM_ACHIEVEMENTS.ACHIEVEMENT_CHEAT_DEATH,
			false
		},
		{
			STEAM_ACHIEVEMENTS.ACHIEVEMENT_SQUEAKY_CLEAN,
			false
		},
		{
			STEAM_ACHIEVEMENTS.ACHIEVEMENT_PIZZA_TUNA,
			false
		},
		{
			STEAM_ACHIEVEMENTS.ACHIEVEMENT_PIZZA_PEPPERONI,
			false
		},
		{
			STEAM_ACHIEVEMENTS.ACHIEVEMENT_PIZZA_VEGGIE,
			false
		},
		{
			STEAM_ACHIEVEMENTS.ACHIEVEMENT_STAN,
			false
		},
		{
			STEAM_ACHIEVEMENTS.ACHIEVEMENT_MAP_APARTMENTS,
			false
		},
		{
			STEAM_ACHIEVEMENTS.ACHIEVEMENT_MAP_TRAIN,
			false
		},
		{
			STEAM_ACHIEVEMENTS.ACHIEVEMENT_MAP_CITY,
			false
		},
		{
			STEAM_ACHIEVEMENTS.ACHIEVEMENT_MAP_FRACTURE,
			false
		},
		{
			STEAM_ACHIEVEMENTS.ACHIEVEMENT_MAP_TRENCHES,
			false
		},
		{
			STEAM_ACHIEVEMENTS.ACHIEVEMENT_SCRAPPER,
			false
		},
		{
			STEAM_ACHIEVEMENTS.ACHIEVEMENT_DELIVERY,
			false
		},
		{
			STEAM_ACHIEVEMENTS.ACHIEVEMENT_ARRESTED,
			false
		},
		{
			STEAM_ACHIEVEMENTS.ACHIEVEMENT_MASTER_MIND,
			false
		},
		{
			STEAM_ACHIEVEMENTS.ACHIEVEMENT_DEATH,
			false
		}
	};

	protected Callback<UserStatsReceived_t> _statsReceivedCallback;

	private readonly NetVar<int> _totalCollectedScrap = new NetVar<int>(0);

	private readonly NetVar<int> _totalCollectedDeliveries = new NetVar<int>(0);

	private bool _statsLoaded;

	public override void OnNetworkSpawn()
	{
		base.OnNetworkSpawn();
		if (base.IsClient)
		{
			LoadPlayerStats();
		}
		if (base.IsServer)
		{
			CoreController.WaitFor(delegate(IngameController ingameCtrl)
			{
				ingameCtrl.OnStatusUpdated += new Action<INGAME_STATUS, bool>(OnIngameStatusUpdated);
				ingameCtrl.OnRoundUpdate += new Action<byte, bool>(OnRoundUpdate);
			});
			CoreController.WaitFor(delegate(PlayerController plyCtrl)
			{
				plyCtrl.OnPlayerDeath += new Action<entity_player, bool>(OnPlayerDeath);
				plyCtrl.OnPlayerRemoved += new Action<entity_player, bool>(OnPlayerRemoved);
			});
		}
	}

	public override void OnNetworkDespawn()
	{
		base.OnNetworkDespawn();
		UpdatePlayerStats();
		if (base.IsServer)
		{
			if ((bool)NetController<IngameController>.Instance)
			{
				NetController<IngameController>.Instance.OnStatusUpdated -= new Action<INGAME_STATUS, bool>(OnIngameStatusUpdated);
				NetController<IngameController>.Instance.OnRoundUpdate -= new Action<byte, bool>(OnRoundUpdate);
			}
			if ((bool)MonoController<PlayerController>.Instance)
			{
				MonoController<PlayerController>.Instance.OnPlayerDeath -= new Action<entity_player, bool>(OnPlayerDeath);
				MonoController<PlayerController>.Instance.OnPlayerRemoved -= new Action<entity_player, bool>(OnPlayerRemoved);
			}
		}
	}

	public new void OnDestroy()
	{
		if (_statsReceivedCallback != null)
		{
			_statsReceivedCallback.Dispose();
		}
		base.OnDestroy();
	}

	[Server]
	public void RegisterScrap(byte playerId, int scrap)
	{
		if (!base.IsServer)
		{
			throw new UnityException("RegisterScrap can only be called on the server.");
		}
		entity_player entity_player2 = MonoController<PlayerController>.Instance?.GetPlayerEntityByID(playerId);
		if ((bool)entity_player2 && !entity_player2.IsDead())
		{
			_totalScrapRegistry.TryAdd(playerId, 0);
			_totalScrapRegistry[playerId] += scrap;
			_totalCollectedScrap.Value += scrap;
			if (!NETController.SV_CHEATS)
			{
				AddStatRPC(STEAM_STATS.SCRAPS, scrap, base.RpcTarget.Single(entity_player2.GetConnectionID(), RpcTargetUse.Temp));
			}
		}
	}

	[Server]
	public void RegisterDelivery(byte playerId, int delivery)
	{
		if (!base.IsServer)
		{
			throw new UnityException("RegisterDelivery can only be called on the server.");
		}
		entity_player entity_player2 = MonoController<PlayerController>.Instance?.GetPlayerEntityByID(playerId);
		if ((bool)entity_player2 && !entity_player2.IsDead())
		{
			_totalDeliveryRegistry.TryAdd(playerId, 0);
			_totalDeliveryRegistry[playerId] += delivery;
			_totalCollectedDeliveries.Value += delivery;
			if (!NETController.SV_CHEATS)
			{
				AddStatRPC(STEAM_STATS.DELIVERIES, delivery, base.RpcTarget.Single(entity_player2.GetConnectionID(), RpcTargetUse.Temp));
			}
		}
	}

	public (byte playerId, int amount) GetMostScrapper(byte[] filterIDs)
	{
		return GetAliveBestAt(_totalScrapRegistry, filterIDs);
	}

	public (byte playerId, int amount) GetMostDeliverer(byte[] filterIDs)
	{
		return GetAliveBestAt(_totalDeliveryRegistry, filterIDs);
	}

	[Server]
	public void Clear()
	{
		if (!base.IsServer)
		{
			throw new UnityException("Clear can only be called on the server.");
		}
		_totalScrapRegistry.Clear();
		_totalDeliveryRegistry.Clear();
	}

	[Server]
	public void SetTotalCollectedScrap(int total)
	{
		if (!base.IsServer)
		{
			throw new UnityException("SetTotalCollectedScrap can only be called on the server.");
		}
		_totalCollectedScrap.SetSpawnValue(total);
	}

	[Server]
	public void SetTotalCollectedDeliveries(int total)
	{
		if (!base.IsServer)
		{
			throw new UnityException("SetTotalCollectedDeliveries can only be called on the server.");
		}
		_totalCollectedDeliveries.SetSpawnValue(total);
	}

	public int GetTotalCollectedScrap()
	{
		return _totalCollectedScrap.Value;
	}

	public int GetTotalCollectedDeliveries()
	{
		return _totalCollectedDeliveries.Value;
	}

	[Client]
	public int GetLocalPlayerStat(STEAM_STATS stat)
	{
		if (!base.IsClient && base.IsSpawned)
		{
			throw new UnityException("GetLocalPlayerStat can only be called on the client.");
		}
		return _localPlayerStats.GetValueOrDefault(stat, 0);
	}

	[Client]
	public bool GetLocalPlayerAchievements(STEAM_ACHIEVEMENTS achievement)
	{
		if (!base.IsClient && base.IsSpawned)
		{
			throw new UnityException("GetLocalPlayerAchievements can only be called on the client.");
		}
		return _localPlayerAchievements.GetValueOrDefault(achievement, defaultValue: false);
	}

	[Client]
	private void UnlockAchievement(STEAM_ACHIEVEMENTS achievement)
	{
		if (!base.IsClient && base.IsSpawned)
		{
			throw new UnityException("UnlockAchievement can only be called on the client.");
		}
		if ((int)achievement >= 100)
		{
			throw new UnityException("Cannot unlock stat-controlled achievements manually.");
		}
		if (_localPlayerAchievements.TryGetValue(achievement, out var value) && value)
		{
			return;
		}
		if (!SteamworksController.IsSteamRunning)
		{
			Debug.LogError("Steam not initialized, cannot unlock achievements.");
			return;
		}
		string text = achievement.ToString();
		if (!SteamUserStats.SetAchievement(text))
		{
			Debug.LogError("Failed to set achievement " + text);
			return;
		}
		if (!SteamUserStats.StoreStats())
		{
			Debug.LogError("Failed to store stats for local player.");
		}
		_localPlayerAchievements[achievement] = true;
		OnAchievementUpdate.Invoke(achievement, param2: true);
		OnAchievementsUpdate.Invoke();
	}

	[Rpc(SendTo.ClientsAndHost)]
	private void InternalUnlockGlobalAchievementRPC(STEAM_ACHIEVEMENTS achievement)
	{
		NetworkManager networkManager = base.NetworkManager;
		if ((object)networkManager == null || !networkManager.IsListening)
		{
			Debug.LogError("Rpc methods can only be invoked after starting the NetworkManager!");
			return;
		}
		if (__rpc_exec_stage != __RpcExecStage.Execute)
		{
			RpcAttribute.RpcAttributeParams attributeParams = default(RpcAttribute.RpcAttributeParams);
			RpcParams rpcParams = default(RpcParams);
			FastBufferWriter bufferWriter = __beginSendRpc(1762328988u, rpcParams, attributeParams, SendTo.ClientsAndHost, RpcDelivery.Reliable);
			bufferWriter.WriteValueSafe(in achievement, default(FastBufferWriter.ForEnums));
			__endSendRpc(ref bufferWriter, 1762328988u, rpcParams, attributeParams, SendTo.ClientsAndHost, RpcDelivery.Reliable);
		}
		if (__rpc_exec_stage == __RpcExecStage.Execute)
		{
			__rpc_exec_stage = __RpcExecStage.Send;
			UnlockAchievement(achievement);
		}
	}

	[Rpc(SendTo.SpecifiedInParams)]
	private void InternalUnlockAchievementRPC(STEAM_ACHIEVEMENTS achievement, RpcParams rpcParams)
	{
		NetworkManager networkManager = base.NetworkManager;
		if ((object)networkManager == null || !networkManager.IsListening)
		{
			Debug.LogError("Rpc methods can only be invoked after starting the NetworkManager!");
			return;
		}
		if (__rpc_exec_stage != __RpcExecStage.Execute)
		{
			RpcAttribute.RpcAttributeParams attributeParams = default(RpcAttribute.RpcAttributeParams);
			FastBufferWriter bufferWriter = __beginSendRpc(2107608505u, rpcParams, attributeParams, SendTo.SpecifiedInParams, RpcDelivery.Reliable);
			bufferWriter.WriteValueSafe(in achievement, default(FastBufferWriter.ForEnums));
			__endSendRpc(ref bufferWriter, 2107608505u, rpcParams, attributeParams, SendTo.SpecifiedInParams, RpcDelivery.Reliable);
		}
		if (__rpc_exec_stage == __RpcExecStage.Execute)
		{
			__rpc_exec_stage = __RpcExecStage.Send;
			UnlockAchievement(achievement);
		}
	}

	[Server]
	public void UnlockAchievementSV(STEAM_ACHIEVEMENTS achievement, ulong clientID = ulong.MaxValue)
	{
		if (!base.IsServer)
		{
			throw new UnityException("UnlockAchievementSV can only be called on the server.");
		}
		if (NETController.SV_CHEATS)
		{
			Debug.LogWarning($"Cheats are enabled, achievement '{achievement}' skipped");
		}
		else if (clientID == ulong.MaxValue)
		{
			InternalUnlockGlobalAchievementRPC(achievement);
		}
		else
		{
			InternalUnlockAchievementRPC(achievement, base.RpcTarget.Single(clientID, RpcTargetUse.Temp));
		}
	}

	[Rpc(SendTo.SpecifiedInParams)]
	public void UnlockAchievementRPC(STEAM_ACHIEVEMENTS achievement, RpcParams rpcParams)
	{
		NetworkManager networkManager = base.NetworkManager;
		if ((object)networkManager == null || !networkManager.IsListening)
		{
			Debug.LogError("Rpc methods can only be invoked after starting the NetworkManager!");
			return;
		}
		if (__rpc_exec_stage != __RpcExecStage.Execute)
		{
			RpcParams rpcParams2 = rpcParams;
			RpcAttribute.RpcAttributeParams attributeParams = default(RpcAttribute.RpcAttributeParams);
			FastBufferWriter bufferWriter = __beginSendRpc(1162557379u, rpcParams2, attributeParams, SendTo.SpecifiedInParams, RpcDelivery.Reliable);
			bufferWriter.WriteValueSafe(in achievement, default(FastBufferWriter.ForEnums));
			__endSendRpc(ref bufferWriter, 1162557379u, rpcParams, attributeParams, SendTo.SpecifiedInParams, RpcDelivery.Reliable);
		}
		if (__rpc_exec_stage == __RpcExecStage.Execute)
		{
			__rpc_exec_stage = __RpcExecStage.Send;
			if (!base.IsServer)
			{
				throw new UnityException("UnlockAchievementRPC can only be called on the server.");
			}
			if (!NETController.SV_CHEATS)
			{
				UnlockAchievementSV(achievement, rpcParams.Receive.SenderClientId);
			}
		}
	}

	[Rpc(SendTo.ClientsAndHost)]
	private void SaveAllStatsRPC()
	{
		NetworkManager networkManager = base.NetworkManager;
		if ((object)networkManager == null || !networkManager.IsListening)
		{
			Debug.LogError("Rpc methods can only be invoked after starting the NetworkManager!");
			return;
		}
		if (__rpc_exec_stage != __RpcExecStage.Execute)
		{
			RpcAttribute.RpcAttributeParams attributeParams = default(RpcAttribute.RpcAttributeParams);
			RpcParams rpcParams = default(RpcParams);
			FastBufferWriter bufferWriter = __beginSendRpc(2988736472u, rpcParams, attributeParams, SendTo.ClientsAndHost, RpcDelivery.Reliable);
			__endSendRpc(ref bufferWriter, 2988736472u, rpcParams, attributeParams, SendTo.ClientsAndHost, RpcDelivery.Reliable);
		}
		if (__rpc_exec_stage == __RpcExecStage.Execute)
		{
			__rpc_exec_stage = __RpcExecStage.Send;
			UpdatePlayerStats();
		}
	}

	[Rpc(SendTo.SpecifiedInParams)]
	private void AddStatRPC(STEAM_STATS stat, int value, RpcParams rpcParams)
	{
		NetworkManager networkManager = base.NetworkManager;
		if ((object)networkManager == null || !networkManager.IsListening)
		{
			Debug.LogError("Rpc methods can only be invoked after starting the NetworkManager!");
			return;
		}
		if (__rpc_exec_stage != __RpcExecStage.Execute)
		{
			RpcAttribute.RpcAttributeParams attributeParams = default(RpcAttribute.RpcAttributeParams);
			FastBufferWriter bufferWriter = __beginSendRpc(2803608687u, rpcParams, attributeParams, SendTo.SpecifiedInParams, RpcDelivery.Reliable);
			bufferWriter.WriteValueSafe(in stat, default(FastBufferWriter.ForEnums));
			BytePacker.WriteValueBitPacked(bufferWriter, value);
			__endSendRpc(ref bufferWriter, 2803608687u, rpcParams, attributeParams, SendTo.SpecifiedInParams, RpcDelivery.Reliable);
		}
		if (__rpc_exec_stage == __RpcExecStage.Execute)
		{
			__rpc_exec_stage = __RpcExecStage.Send;
			AddStat(stat, value);
		}
	}

	[Client]
	private void AddStat(STEAM_STATS stat, int value)
	{
		if (!base.IsClient)
		{
			throw new UnityException("AddStat can only be called on the client.");
		}
		if ((int)stat > 200)
		{
			throw new UnityException($"Invalid stat {stat}");
		}
		if (value > 0)
		{
			_localPlayerStats[stat] += value;
			switch (stat)
			{
			case STEAM_STATS.ARRESTED:
				NETController.AddInstantTimelineEvent("Arrested", "Arrested", TIMELINE_EVENT_ICON.JAIL, 1u, featured: true);
				break;
			case STEAM_STATS.ROUNDS:
				NETController.AddInstantTimelineEvent("Round", "Round", TIMELINE_EVENT_ICON.STEAM_CROWN, 1u, featured: true);
				break;
			case STEAM_STATS.DEATHS:
				NETController.AddInstantTimelineEvent("Death", "Death", TIMELINE_EVENT_ICON.DEATH, 1u, featured: true);
				break;
			case STEAM_STATS.DELIVERIES:
				NETController.AddInstantTimelineEvent("Delivery", "Delivery", TIMELINE_EVENT_ICON.DELIVERY, 1u, featured: true);
				break;
			}
			OnLocalPlayerStatUpdate.Invoke(stat, _localPlayerStats[stat]);
		}
	}

	[Rpc(SendTo.SpecifiedInParams)]
	private void IncreaseStatOnlyRPC(STEAM_STATS stat, int value, RpcParams rpcParams)
	{
		NetworkManager networkManager = base.NetworkManager;
		if ((object)networkManager == null || !networkManager.IsListening)
		{
			Debug.LogError("Rpc methods can only be invoked after starting the NetworkManager!");
			return;
		}
		if (__rpc_exec_stage != __RpcExecStage.Execute)
		{
			RpcAttribute.RpcAttributeParams attributeParams = default(RpcAttribute.RpcAttributeParams);
			FastBufferWriter bufferWriter = __beginSendRpc(390837084u, rpcParams, attributeParams, SendTo.SpecifiedInParams, RpcDelivery.Reliable);
			bufferWriter.WriteValueSafe(in stat, default(FastBufferWriter.ForEnums));
			BytePacker.WriteValueBitPacked(bufferWriter, value);
			__endSendRpc(ref bufferWriter, 390837084u, rpcParams, attributeParams, SendTo.SpecifiedInParams, RpcDelivery.Reliable);
		}
		if (__rpc_exec_stage == __RpcExecStage.Execute)
		{
			__rpc_exec_stage = __RpcExecStage.Send;
			IncreaseStatOnly(stat, value);
		}
	}

	[Client]
	private bool IncreaseStatOnly(STEAM_STATS stat, int value)
	{
		if (!base.IsClient)
		{
			throw new UnityException("AddStat can only be called on the client.");
		}
		if ((int)stat > 200)
		{
			throw new UnityException($"Invalid stat {stat}");
		}
		if (value < _localPlayerStats[stat])
		{
			return false;
		}
		_localPlayerStats[stat] = value;
		OnLocalPlayerStatUpdate.Invoke(stat, value);
		return true;
	}

	[Client]
	private void UpdatePlayerStats()
	{
		if (!base.IsClient)
		{
			throw new UnityException("UpdatePlayerStats can only be called on the client.");
		}
		if (!SteamworksController.IsSteamRunning)
		{
			Debug.LogWarning("Steam not initialized, cannot save player stats.");
			return;
		}
		if (!_statsLoaded)
		{
			Debug.LogWarning("Stats were never loaded, skipping save to avoid overwriting.");
			return;
		}
		Debug.Log($"Saving player stats for {SteamUser.GetSteamID()}...");
		foreach (KeyValuePair<STEAM_STATS, int> localPlayerStat in _localPlayerStats)
		{
			if (localPlayerStat.Value > 0 && (int)localPlayerStat.Key <= 200 && !SteamUserStats.SetStat(localPlayerStat.Key.ToString().ToLower(), localPlayerStat.Value))
			{
				Debug.LogWarning($"Failed to set stat {localPlayerStat.Key} with value {localPlayerStat.Value}");
			}
		}
		if (!SteamUserStats.StoreStats())
		{
			Debug.LogWarning("Failed to store stats for local player.");
		}
	}

	[Client]
	private void LoadPlayerStats()
	{
		if (!base.IsClient)
		{
			throw new UnityException("LoadPlayerStats can only be called on the client.");
		}
		if (!SteamworksController.IsSteamRunning)
		{
			Debug.LogWarning("Steam not initialized, cannot load player stats.");
			return;
		}
		_statsReceivedCallback = Callback<UserStatsReceived_t>.Create(OnLoadStatsReceived);
		SteamUserStats.RequestUserStats(SteamUser.GetSteamID());
		Debug.Log($"Requesting player stats for {SteamUser.GetSteamID()}...");
	}

	private void OnLoadStatsReceived(UserStatsReceived_t result)
	{
		if (result.m_eResult != EResult.k_EResultOK)
		{
			Debug.LogWarning("Failed to gather player's stats & achievements");
			return;
		}
		foreach (STEAM_STATS value in Enum.GetValues(typeof(STEAM_STATS)))
		{
			if (!SteamUserStats.GetStat(value.ToString().ToLower(), out int pData))
			{
				Debug.LogWarning($"Failed to get stat {value}, defaulting to 0");
				pData = 0;
			}
			_localPlayerStats[value] = pData;
			OnLocalPlayerStatUpdate.Invoke(value, pData);
		}
		_statsLoaded = true;
		foreach (STEAM_ACHIEVEMENTS value2 in Enum.GetValues(typeof(STEAM_ACHIEVEMENTS)))
		{
			if (value2 != 0 && (int)value2 < 230)
			{
				if (!SteamUserStats.GetAchievement(value2.ToString(), out var pbAchieved))
				{
					Debug.LogWarning($"Failed to get achievement {value2}, defaulting to false");
					pbAchieved = false;
				}
				_localPlayerAchievements[value2] = pbAchieved;
				OnAchievementUpdate.Invoke(value2, pbAchieved);
			}
		}
		OnAchievementsUpdate.Invoke();
		OnLocalPlayerStatsLoaded?.Invoke();
	}

	[Client]
	public void ResetAllStatsAndAchievements(bool resetAchievements)
	{
		if (!base.IsClient)
		{
			throw new UnityException("ResetAllStatsAndAchievements can only be called on the client.");
		}
		if (!SteamworksController.IsSteamRunning)
		{
			Debug.LogWarning("Steam not initialized, cannot reset player stats.");
			return;
		}
		SteamUserStats.ResetAllStats(resetAchievements);
		SteamUserStats.StoreStats();
		foreach (STEAM_STATS value in Enum.GetValues(typeof(STEAM_STATS)))
		{
			if ((int)value <= 200)
			{
				_localPlayerStats[value] = 0;
				OnLocalPlayerStatUpdate.Invoke(value, 0);
			}
		}
		foreach (STEAM_ACHIEVEMENTS value2 in Enum.GetValues(typeof(STEAM_ACHIEVEMENTS)))
		{
			if (value2 != 0 && (int)value2 < 230)
			{
				_localPlayerAchievements[value2] = false;
				OnAchievementUpdate.Invoke(value2, param2: false);
			}
		}
		OnAchievementsUpdate.Invoke();
	}

	private void OnPlayerRemoved(entity_player ply, bool server)
	{
		if (server && (bool)ply)
		{
			_totalDeliveryRegistry.Remove(ply.GetPlayerID());
			_totalScrapRegistry.Remove(ply.GetPlayerID());
		}
	}

	private void OnPlayerDeath(entity_player ply, bool server)
	{
		if (server && (bool)ply && !NETController.SV_CHEATS)
		{
			AddStatRPC(STEAM_STATS.DEATHS, 1, base.RpcTarget.Single(ply.GetConnectionID(), RpcTargetUse.Temp));
		}
	}

	private (byte playerId, int amount) GetAliveBestAt(Dictionary<byte, int> registry, byte[] filterIDs)
	{
		byte item = byte.MaxValue;
		int num = 0;
		foreach (KeyValuePair<byte, int> item2 in registry)
		{
			Player player = MonoController<PlayerController>.Instance.GetPlayer(item2.Key);
			if ((bool)player.player && !player.player.IsDead() && item2.Value > num && (filterIDs == null || !filterIDs.Contains(item2.Key)))
			{
				num = item2.Value;
				item = item2.Key;
			}
		}
		return (playerId: item, amount: num);
	}

	private void OnRoundUpdate(byte round, bool server)
	{
		if (server && !NETController.SV_CHEATS)
		{
			IncreaseStatOnlyRPC(STEAM_STATS.ROUNDS, round, base.RpcTarget.ClientsAndHost);
		}
	}

	private void OnIngameStatusUpdated(INGAME_STATUS status, bool server)
	{
		if (!server)
		{
			return;
		}
		switch (status)
		{
		case INGAME_STATUS.PLAYING:
			Clear();
			break;
		case INGAME_STATUS.GAMEOVER:
			if (!NETController.SV_CHEATS)
			{
				AddStatRPC(STEAM_STATS.ARRESTED, 1, base.RpcTarget.ClientsAndHost);
				SaveAllStatsRPC();
			}
			break;
		case INGAME_STATUS.ROUND_END:
			if (!NETController.SV_CHEATS)
			{
				SaveAllStatsRPC();
			}
			break;
		}
	}

	protected override void __initializeVariables()
	{
		if (_totalCollectedScrap == null)
		{
			throw new Exception("StatsController._totalCollectedScrap cannot be null. All NetworkVariableBase instances must be initialized.");
		}
		_totalCollectedScrap.Initialize(this);
		__nameNetworkVariable(_totalCollectedScrap, "_totalCollectedScrap");
		NetworkVariableFields.Add(_totalCollectedScrap);
		if (_totalCollectedDeliveries == null)
		{
			throw new Exception("StatsController._totalCollectedDeliveries cannot be null. All NetworkVariableBase instances must be initialized.");
		}
		_totalCollectedDeliveries.Initialize(this);
		__nameNetworkVariable(_totalCollectedDeliveries, "_totalCollectedDeliveries");
		NetworkVariableFields.Add(_totalCollectedDeliveries);
		base.__initializeVariables();
	}

	protected override void __initializeRpcs()
	{
		__registerRpc(1762328988u, __rpc_handler_1762328988, "InternalUnlockGlobalAchievementRPC", RpcInvokePermission.Everyone);
		__registerRpc(2107608505u, __rpc_handler_2107608505, "InternalUnlockAchievementRPC", RpcInvokePermission.Everyone);
		__registerRpc(1162557379u, __rpc_handler_1162557379, "UnlockAchievementRPC", RpcInvokePermission.Everyone);
		__registerRpc(2988736472u, __rpc_handler_2988736472, "SaveAllStatsRPC", RpcInvokePermission.Everyone);
		__registerRpc(2803608687u, __rpc_handler_2803608687, "AddStatRPC", RpcInvokePermission.Everyone);
		__registerRpc(390837084u, __rpc_handler_390837084, "IncreaseStatOnlyRPC", RpcInvokePermission.Everyone);
		base.__initializeRpcs();
	}

	private static void __rpc_handler_1762328988(NetworkBehaviour target, FastBufferReader reader, __RpcParams rpcParams)
	{
		NetworkManager networkManager = target.NetworkManager;
		if ((object)networkManager != null && networkManager.IsListening)
		{
			reader.ReadValueSafe(out STEAM_ACHIEVEMENTS value, default(FastBufferWriter.ForEnums));
			target.__rpc_exec_stage = __RpcExecStage.Execute;
			((StatsController)target).InternalUnlockGlobalAchievementRPC(value);
			target.__rpc_exec_stage = __RpcExecStage.Send;
		}
	}

	private static void __rpc_handler_2107608505(NetworkBehaviour target, FastBufferReader reader, __RpcParams rpcParams)
	{
		NetworkManager networkManager = target.NetworkManager;
		if ((object)networkManager != null && networkManager.IsListening)
		{
			reader.ReadValueSafe(out STEAM_ACHIEVEMENTS value, default(FastBufferWriter.ForEnums));
			RpcParams ext = rpcParams.Ext;
			target.__rpc_exec_stage = __RpcExecStage.Execute;
			((StatsController)target).InternalUnlockAchievementRPC(value, ext);
			target.__rpc_exec_stage = __RpcExecStage.Send;
		}
	}

	private static void __rpc_handler_1162557379(NetworkBehaviour target, FastBufferReader reader, __RpcParams rpcParams)
	{
		NetworkManager networkManager = target.NetworkManager;
		if ((object)networkManager != null && networkManager.IsListening)
		{
			reader.ReadValueSafe(out STEAM_ACHIEVEMENTS value, default(FastBufferWriter.ForEnums));
			RpcParams ext = rpcParams.Ext;
			target.__rpc_exec_stage = __RpcExecStage.Execute;
			((StatsController)target).UnlockAchievementRPC(value, ext);
			target.__rpc_exec_stage = __RpcExecStage.Send;
		}
	}

	private static void __rpc_handler_2988736472(NetworkBehaviour target, FastBufferReader reader, __RpcParams rpcParams)
	{
		NetworkManager networkManager = target.NetworkManager;
		if ((object)networkManager != null && networkManager.IsListening)
		{
			target.__rpc_exec_stage = __RpcExecStage.Execute;
			((StatsController)target).SaveAllStatsRPC();
			target.__rpc_exec_stage = __RpcExecStage.Send;
		}
	}

	private static void __rpc_handler_2803608687(NetworkBehaviour target, FastBufferReader reader, __RpcParams rpcParams)
	{
		NetworkManager networkManager = target.NetworkManager;
		if ((object)networkManager != null && networkManager.IsListening)
		{
			reader.ReadValueSafe(out STEAM_STATS value, default(FastBufferWriter.ForEnums));
			ByteUnpacker.ReadValueBitPacked(reader, out int value2);
			RpcParams ext = rpcParams.Ext;
			target.__rpc_exec_stage = __RpcExecStage.Execute;
			((StatsController)target).AddStatRPC(value, value2, ext);
			target.__rpc_exec_stage = __RpcExecStage.Send;
		}
	}

	private static void __rpc_handler_390837084(NetworkBehaviour target, FastBufferReader reader, __RpcParams rpcParams)
	{
		NetworkManager networkManager = target.NetworkManager;
		if ((object)networkManager != null && networkManager.IsListening)
		{
			reader.ReadValueSafe(out STEAM_STATS value, default(FastBufferWriter.ForEnums));
			ByteUnpacker.ReadValueBitPacked(reader, out int value2);
			RpcParams ext = rpcParams.Ext;
			target.__rpc_exec_stage = __RpcExecStage.Execute;
			((StatsController)target).IncreaseStatOnlyRPC(value, value2, ext);
			target.__rpc_exec_stage = __RpcExecStage.Send;
		}
	}

	protected internal override string __getTypeName()
	{
		return "StatsController";
	}
}
