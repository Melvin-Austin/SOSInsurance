using System;
using System.Collections.Generic;
using FailCake;
using TMPro;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;
using ZLinq;
using ZLinq.Linq;

namespace HyenaQuest;

[DisallowMultipleComponent]
[DefaultExecutionOrder(-70)]
[RequireComponent(typeof(NetworkObject))]
public class SaveController : NetController<SaveController>
{
	private static readonly int Status = Animator.StringToHash("STATUS");

	private static readonly int MAX_SAVES = 1;

	private static readonly List<string> BLACKLISTED = new List<string> { "item_vacuum", "item_save" };

	public Transform spawnPos;

	public GameObject tapePrefab;

	public NetworkAnimator animator;

	public entity_button confirm;

	public entity_prop_phys_item_place place;

	public TextMeshPro modeLabel;

	public entity_lever modeLever;

	private readonly NetVar<SaveMODE> _mode = new NetVar<SaveMODE>(SaveMODE.SAVE);

	private readonly List<entity_item_save> _spawnedTapes = new List<entity_item_save>();

	private entity_item_save _currentSave;

	private util_timer _tapeSpawner;

	private util_timer _loadTimer;

	private util_timer _blinkTimer;

	private byte _loadedRound = 1;

	public new void Awake()
	{
		base.Awake();
		if (!spawnPos)
		{
			throw new UnityException("Missing spawnPos");
		}
		if (!tapePrefab)
		{
			throw new UnityException("Missing tapePrefab");
		}
		if (!animator)
		{
			throw new UnityException("Missing NetworkAnimator");
		}
		if (!confirm)
		{
			throw new UnityException("Missing entity_button");
		}
		if (!place)
		{
			throw new UnityException("Missing entity_prop_phys_item_place");
		}
		if (!modeLabel)
		{
			throw new UnityException("Missing TextMeshPro");
		}
		if (!modeLever)
		{
			throw new UnityException("Missing entity_lever");
		}
	}

	public new void OnDestroy()
	{
		_loadedRound = 1;
		base.OnDestroy();
	}

	public override void OnNetworkSpawn()
	{
		base.OnNetworkSpawn();
		if (base.IsServer)
		{
			CreateTapes();
			CoreController.WaitFor(delegate(IngameController ingameCtrl)
			{
				ingameCtrl.OnStatusUpdated += new Action<INGAME_STATUS, bool>(OnIngameStatusUpdated);
			});
			modeLever.OnUSE += new Action<entity_player, bool>(SwitchMode);
			confirm.OnUSE += new Action<entity_player>(OnLoadSaveConfirm);
			place.OnItemUpdate += new Action<entity_item, bool, bool>(OnItemUpdate);
		}
	}

	private void SwitchMode(entity_player ply, bool active)
	{
		_mode.SetSpawnValue((_mode.Value == SaveMODE.SAVE) ? SaveMODE.LOAD : SaveMODE.SAVE);
		UpdateStatus();
	}

	public override void OnNetworkDespawn()
	{
		base.OnNetworkDespawn();
		_blinkTimer?.Stop();
		if ((bool)MonoController<LocalizationController>.Instance)
		{
			MonoController<LocalizationController>.Instance.Cleanup("save.label.mode");
		}
		if (base.IsServer)
		{
			_tapeSpawner?.Stop();
			_loadTimer?.Stop();
			ClearTapes();
			if ((bool)NetController<IngameController>.Instance)
			{
				NetController<IngameController>.Instance.OnStatusUpdated -= new Action<INGAME_STATUS, bool>(OnIngameStatusUpdated);
			}
			if ((bool)modeLever)
			{
				modeLever.OnUSE -= new Action<entity_player, bool>(SwitchMode);
			}
			if ((bool)confirm)
			{
				confirm.OnUSE -= new Action<entity_player>(OnLoadSaveConfirm);
			}
			if ((bool)place)
			{
				place.OnItemUpdate -= new Action<entity_item, bool, bool>(OnItemUpdate);
			}
		}
	}

	protected override void OnNetworkPostSpawn()
	{
		base.OnNetworkPostSpawn();
		if (!base.IsClient)
		{
			return;
		}
		_mode.RegisterOnValueChanged(delegate(SaveMODE _, SaveMODE newValue)
		{
			if ((bool)MonoController<LocalizationController>.Instance && (bool)modeLabel)
			{
				MonoController<LocalizationController>.Instance.Cleanup("save.label.mode");
				MonoController<LocalizationController>.Instance.Get("save.label.mode", (newValue == SaveMODE.LOAD) ? "ingame.ui.hints.load" : "ingame.ui.hints.save", delegate(string s)
				{
					if ((bool)modeLabel)
					{
						modeLabel.text = s;
					}
				});
			}
		});
	}

	public override void OnNetworkPreDespawn()
	{
		base.OnNetworkPreDespawn();
		if (base.IsClient)
		{
			_mode.OnValueChanged = null;
		}
	}

	[Server]
	private void CreateTapes()
	{
		if (!base.IsServer)
		{
			throw new UnityException("Server only");
		}
		if (!spawnPos)
		{
			throw new UnityException("Missing spawnPos");
		}
		if (!tapePrefab)
		{
			throw new UnityException("Missing tapePrefab");
		}
		ClearTapes();
		List<SaveData> saves = MonoController<SettingsController>.Instance.GetSaves();
		_tapeSpawner?.Stop();
		_tapeSpawner = util_timer.Create(MAX_SAVES, 1f, delegate(int t)
		{
			if ((bool)tapePrefab)
			{
				Vector3 position = spawnPos.position;
				Quaternion rotation = base.transform.rotation * Quaternion.Euler(UnityEngine.Random.Range(-45, 45), UnityEngine.Random.Range(0f, 360f), UnityEngine.Random.Range(-45, 45));
				GameObject obj = UnityEngine.Object.Instantiate(tapePrefab, position, rotation);
				if (!obj)
				{
					throw new UnityException("Failed to spawn tape prefab");
				}
				NetworkObject component = obj.GetComponent<NetworkObject>();
				if (!component)
				{
					throw new UnityException("Failed to get NetworkObject component");
				}
				entity_item_save component2 = obj.GetComponent<entity_item_save>();
				if (!component2)
				{
					throw new UnityException("Failed to get entity_item_save component");
				}
				component.Spawn();
				if (t < saves.Count)
				{
					component2.SetData(saves[t]);
				}
				_spawnedTapes.Add(component2);
				NetController<SoundController>.Instance.Play3DSound("Ingame/Store/store_fwomp.ogg", position, new AudioData
				{
					distance = 1.5f,
					volume = 0.1f,
					pitch = 0.8f * (float)t + 1f
				}, broadcast: true);
			}
		});
	}

	[Server]
	private void ClearTapes()
	{
		if (!base.IsServer)
		{
			throw new UnityException("Server only");
		}
		foreach (entity_item_save spawnedTape in _spawnedTapes)
		{
			if ((bool)spawnedTape && spawnedTape.IsSpawned)
			{
				spawnedTape.NetworkObject.Despawn();
			}
		}
		_spawnedTapes.Clear();
	}

	[Server]
	private void OnLoadSaveConfirm(entity_player obj)
	{
		if (!base.IsServer)
		{
			throw new UnityException("Server only");
		}
		if (!obj || obj.IsDead())
		{
			return;
		}
		if (!_currentSave || NetController<IngameController>.Instance.Status() != 0)
		{
			NetController<SoundController>.Instance?.Play3DSound("Ingame/Entities/Terminal/142608__autistic-lucario__error.ogg", confirm.transform.position, new AudioData
			{
				distance = 5f,
				pitch = UnityEngine.Random.Range(0.8f, 1.2f),
				volume = UnityEngine.Random.Range(0.7f, 0.8f)
			}, broadcast: true);
			return;
		}
		if (obj.GetConnectionID() != 0L)
		{
			NetController<NotificationController>.Instance?.BroadcastRPC(new NotificationData
			{
				id = "save-load-error",
				text = "ingame.ui.notification.save.host-only",
				duration = 2f,
				soundEffect = "Ingame/Entities/Terminal/142608__autistic-lucario__error.ogg",
				soundVolume = 0.05f
			}, obj.GetConnectionID());
			return;
		}
		switch (_mode.Value)
		{
		case SaveMODE.SAVE:
		{
			byte b = NetController<IngameController>.Instance?.GetCurrentRound() ?? 1;
			SaveData? data = _currentSave.GetData();
			if (b <= 1 || (data.HasValue && data.Value.round == b) || _loadedRound == b)
			{
				NetController<NotificationController>.Instance?.BroadcastRPC(new NotificationData
				{
					id = "save-load-error-round",
					text = "ingame.ui.notification.save-error-round",
					duration = 2f,
					soundEffect = "Ingame/Entities/Terminal/142608__autistic-lucario__error.ogg",
					soundVolume = 0.05f
				}, obj.GetConnectionID());
			}
			else
			{
				Save();
			}
			break;
		}
		case SaveMODE.LOAD:
			if (!_currentSave.GetData().HasValue)
			{
				NetController<NotificationController>.Instance?.BroadcastRPC(new NotificationData
				{
					id = "save-load-error-round",
					text = "ingame.ui.notification.save-no-data",
					duration = 2f,
					soundEffect = "Ingame/Entities/Terminal/142608__autistic-lucario__error.ogg",
					soundVolume = 0.05f
				}, obj.GetConnectionID());
			}
			else if (NetController<IngameController>.Instance.GetCurrentRound() != 1)
			{
				NetController<NotificationController>.Instance?.BroadcastRPC(new NotificationData
				{
					id = "save-load-error-round",
					text = "ingame.ui.notification.save-error-load",
					duration = 2f,
					soundEffect = "Ingame/Entities/Terminal/142608__autistic-lucario__error.ogg",
					soundVolume = 0.05f
				}, obj.GetConnectionID());
			}
			else
			{
				Load();
			}
			break;
		}
	}

	[Server]
	private void Load()
	{
		if (!base.IsServer)
		{
			throw new UnityException("Server only");
		}
		if (!_currentSave || !_currentSave.GetData().HasValue || NetController<IngameController>.Instance.Status() != 0)
		{
			return;
		}
		SaveData data = _currentSave.GetData().Value;
		NetController<LightController>.Instance?.ExecuteAllLightCommand(LightCommand.FLICKER);
		LoadEffectRPC();
		_loadTimer?.Stop();
		_loadTimer = util_timer.Simple(0.15f, delegate
		{
			if ((bool)_currentSave && _currentSave.GetData().HasValue)
			{
				foreach (entity_player alivePlayer in MonoController<PlayerController>.Instance.GetAlivePlayers())
				{
					if ((bool)alivePlayer)
					{
						alivePlayer.GetInventory()?.DropAllItems();
					}
				}
				_loadTimer = util_timer.Simple(0.05f, delegate
				{
					if ((bool)_currentSave && _currentSave.GetData().HasValue)
					{
						NETController.Instance.SetCheats(data.cheated);
						List<SaveDataItems> items = data.items;
						if (items != null && items.Count > 0)
						{
							List<entity_phys> list = (from itm in UnityEngine.Object.FindObjectsByType<entity_phys>(FindObjectsInactive.Exclude, FindObjectsSortMode.None).AsValueEnumerable()
								where (bool)itm && itm.IsSpawned
								select itm).ToList();
							HashSet<entity_item> hashSet = new HashSet<entity_item>();
							foreach (entity_phys item in list)
							{
								if ((bool)item && item.IsSpawned && !item.LoadData(data).HasValue && item is entity_item entity_item2 && !BLACKLISTED.Contains(entity_item2.GetID().ToLowerInvariant()))
								{
									hashSet.Add(entity_item2);
								}
							}
							if (data.items.Count > 0)
							{
								NetController<StoreController>.Instance.SpawnItems(data.items);
							}
							foreach (entity_item item2 in hashSet)
							{
								if ((bool)item2 && item2.IsSpawned)
								{
									item2.NetworkObject.Despawn();
								}
							}
						}
						_loadedRound = data.round;
						NetController<IngameController>.Instance?.SetRound(data.round);
						NetController<IngameController>.Instance?.SetHealth(data.health);
						NetController<IngameController>.Instance?.SetAutoRevives(data.autoRevives);
						NetController<CurrencyController>.Instance?.SetCurrency(data.currency);
						NetController<UpgradeController>.Instance?.Load(data.upgrades);
						NetController<StatsController>.Instance?.SetTotalCollectedScrap(data.totalScrap);
						NetController<StatsController>.Instance?.SetTotalCollectedDeliveries(data.totalDeliveries);
						NetController<SpeedrunController>.Instance?.LoadSave(data.totalTimeMs);
						NetController<StoreController>.Instance?.ClearStore();
						MonoController<SettingsController>.Instance.DeleteSave(data);
						_currentSave.ClearData();
						place.Eject(new Vector3(0f, 0f, 2f));
						_currentSave = null;
						UpdateStatus();
						NetController<StatsController>.Instance?.UnlockAchievementSV(STEAM_ACHIEVEMENTS.ACHIEVEMENT_UNIVERSE_LOAD, ulong.MaxValue);
					}
				});
			}
		});
	}

	[Rpc(SendTo.ClientsAndHost)]
	private void LoadEffectRPC()
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
			FastBufferWriter bufferWriter = __beginSendRpc(3163965345u, rpcParams, attributeParams, SendTo.ClientsAndHost, RpcDelivery.Reliable);
			__endSendRpc(ref bufferWriter, 3163965345u, rpcParams, attributeParams, SendTo.ClientsAndHost, RpcDelivery.Reliable);
		}
		if (__rpc_exec_stage != __RpcExecStage.Execute)
		{
			return;
		}
		__rpc_exec_stage = __RpcExecStage.Send;
		if ((bool)PlayerController.LOCAL)
		{
			NetController<ShakeController>.Instance?.LocalShake(ShakeMode.SHAKE_ALL, 0.5f, 0.1f);
			MonoController<UIController>.Instance?.SetFade(fadeIn: false, 0f);
			_blinkTimer?.Stop();
			_blinkTimer = util_timer.Simple(0.25f, delegate
			{
				MonoController<UIController>.Instance?.SetFade(fadeIn: false, 0.15f);
			});
			NetController<SoundController>.Instance?.PlaySound("Ingame/Save/load.ogg", new AudioData
			{
				pitch = UnityEngine.Random.Range(0.8f, 0.9f),
				volume = 0.5f
			});
		}
	}

	[Server]
	private void Save()
	{
		if (!base.IsServer)
		{
			throw new UnityException("Server only");
		}
		if (NetController<IngameController>.Instance.Status() != 0)
		{
			return;
		}
		if (!MonoController<SettingsController>.Instance)
		{
			throw new UnityException("Missing SettingsController");
		}
		if (!NetController<CurrencyController>.Instance)
		{
			throw new UnityException("Missing CurrencyController");
		}
		if (!NetController<UpgradeController>.Instance)
		{
			throw new UnityException("Missing UpgradeController");
		}
		ValueEnumerable<ArrayWhere<entity_phys>, entity_phys> valueEnumerable = from itm in UnityEngine.Object.FindObjectsByType<entity_phys>(FindObjectsInactive.Exclude, FindObjectsSortMode.None).AsValueEnumerable()
			where (bool)itm && itm.IsSpawned
			select itm;
		List<SaveDataItems> list = new List<SaveDataItems>();
		using (ValueEnumerator<ArrayWhere<entity_phys>, entity_phys> valueEnumerator = ValueEnumerableExtensions.GetEnumerator(in valueEnumerable))
		{
			while (valueEnumerator.MoveNext())
			{
				entity_phys current = valueEnumerator.Current;
				if (!current || !current.IsSpawned)
				{
					continue;
				}
				SaveDataItems saveDataItems = default(SaveDataItems);
				saveDataItems.position = current.transform.position;
				saveDataItems.rotation = current.transform.rotation;
				SaveDataItems item = saveDataItems;
				if (current is entity_item entity_item2)
				{
					item.id = entity_item2.GetID();
					if (BLACKLISTED.Contains(item.id.ToLowerInvariant()))
					{
						continue;
					}
					item.data = entity_item2.Save();
				}
				else
				{
					entity_save_data component = current.GetComponent<entity_save_data>();
					if ((bool)component)
					{
						item.id = component.UNIQUE_ID;
						item.data = current.Save();
					}
				}
				if (string.IsNullOrEmpty(item.id))
				{
					Debug.Log("Missing id, skipping save on item " + current.name);
				}
				else
				{
					list.Add(item);
				}
			}
		}
		SaveData saveData = default(SaveData);
		saveData.slot = 0;
		saveData.date = DateTime.Now.ToString("dd-MM-yyyy HH:mm:ss");
		saveData.cheated = NETController.SV_CHEATS;
		saveData.currency = NetController<CurrencyController>.Instance.GetCurrency();
		saveData.autoRevives = NetController<IngameController>.Instance.GetAutoRevives();
		saveData.round = NetController<IngameController>.Instance.GetCurrentRound();
		saveData.health = NetController<IngameController>.Instance.GetHealth();
		saveData.totalScrap = NetController<StatsController>.Instance.GetTotalCollectedScrap();
		saveData.totalDeliveries = NetController<StatsController>.Instance.GetTotalCollectedDeliveries();
		saveData.items = list;
		saveData.upgrades = NetController<UpgradeController>.Instance.GetBoughtUpgrades();
		saveData.totalTimeMs = NetController<SpeedrunController>.Instance?.GetTotalTimeMs() ?? 0;
		SaveData data = saveData;
		if (MonoController<SettingsController>.Instance.SaveGame(data))
		{
			NetController<LightController>.Instance?.ExecuteAllLightCommand(LightCommand.FLICKER);
			NetController<ShakeController>.Instance?.ShakeRPC(ShakeMode.SHAKE_ALL, 0.25f, 0.01f);
			NetController<SoundController>.Instance?.PlaySound("Ingame/Save/save.ogg", new AudioData
			{
				pitch = UnityEngine.Random.Range(0.8f, 0.9f)
			}, broadcast: true);
			_currentSave.SetData(data);
			place.Eject(new Vector3(0f, 0f, 2f));
		}
	}

	private void OnIngameStatusUpdated(INGAME_STATUS ingameStatus, bool server)
	{
		if (server)
		{
			UpdateStatus();
		}
	}

	private void OnItemUpdate(entity_item item, bool set, bool server)
	{
		if (!server)
		{
			return;
		}
		if (set)
		{
			if (!(item is entity_item_save currentSave))
			{
				return;
			}
			_currentSave = currentSave;
		}
		else
		{
			_currentSave = null;
		}
		UpdateStatus();
	}

	[Server]
	private void UpdateStatus()
	{
		if (!confirm)
		{
			throw new UnityException("confirm is not set");
		}
		if (!animator)
		{
			throw new UnityException("animator is not set");
		}
		bool flag = (bool)_currentSave && NetController<IngameController>.Instance.Status() == INGAME_STATUS.IDLE;
		bool flag2 = _mode.Value == SaveMODE.LOAD;
		if (flag)
		{
			if (flag2)
			{
				animator.Animator.SetInteger(Status, 2);
			}
			else
			{
				animator.Animator.SetInteger(Status, 1);
			}
		}
		else
		{
			animator.Animator.SetInteger(Status, 0);
		}
		modeLever.SetHint(flag2 ? "ingame.ui.hints.load" : "ingame.ui.hints.save");
		confirm.SetHint((!flag) ? "" : (flag2 ? "ingame.ui.hints.load" : "ingame.ui.hints.save"));
		confirm.SetLocked(!flag);
	}

	protected override void __initializeVariables()
	{
		if (_mode == null)
		{
			throw new Exception("SaveController._mode cannot be null. All NetworkVariableBase instances must be initialized.");
		}
		_mode.Initialize(this);
		__nameNetworkVariable(_mode, "_mode");
		NetworkVariableFields.Add(_mode);
		base.__initializeVariables();
	}

	protected override void __initializeRpcs()
	{
		__registerRpc(3163965345u, __rpc_handler_3163965345, "LoadEffectRPC", RpcInvokePermission.Everyone);
		base.__initializeRpcs();
	}

	private static void __rpc_handler_3163965345(NetworkBehaviour target, FastBufferReader reader, __RpcParams rpcParams)
	{
		NetworkManager networkManager = target.NetworkManager;
		if ((object)networkManager != null && networkManager.IsListening)
		{
			target.__rpc_exec_stage = __RpcExecStage.Execute;
			((SaveController)target).LoadEffectRPC();
			target.__rpc_exec_stage = __RpcExecStage.Send;
		}
	}

	protected internal override string __getTypeName()
	{
		return "SaveController";
	}
}
