using System;
using System.Collections.Generic;
using System.Linq;
using FailCake;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Localization.Components;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using ZLinq;
using ZLinq.Linq;

namespace HyenaQuest;

[DefaultExecutionOrder(-90)]
[DisallowMultipleComponent]
[RequireComponent(typeof(NetworkObject))]
public class IngameController : NetController<IngameController>
{
	public static readonly byte MAX_INVENTORY_SLOTS = 3;

	public static readonly byte START_INVENTORY_SLOTS = 1;

	public static readonly int STARTING_CURRENCY = 50;

	public static readonly int WARNING_TIMER = 30;

	public static readonly int MAX_WORLD_TIME = 360 + WARNING_TIMER;

	public static readonly int EXTRA_WORLD_SOLO_TIME = 45;

	public static readonly int MAX_SOLO_REVIVES = 2;

	public static readonly byte START_HEALTH = 2;

	public static readonly byte MAX_HEALTH = 3;

	public Light sun;

	public GameObject ashesPrefab;

	public Transform shipPosition;

	public BoxCollider shipArea;

	public entity_door shipElevator;

	public GameObject shipElevatorSquash;

	public List<entity_button_emission> shipElevatorButtons = new List<entity_button_emission>();

	public entity_door blastDoor;

	public entity_door cityLiftoff;

	public entity_door frontBlastDoor;

	public GameObject shipShellMDL;

	public entity_button_confirm startGameButton;

	public entity_led startGameLED;

	public GameObject startGameForceField;

	public GameObject jailWhiteWall;

	public GameObject explosionVFX;

	public Transform cameraJailPosition;

	public Transform playerJailTeleportPosition;

	public TextMeshPro jailEndgameText;

	public AudioSource jailMusicSource;

	public List<AudioClip> jailSadClownMusic = new List<AudioClip>();

	public Button retryButton;

	public BoxCollider veggieArea;

	public GameEvent<INGAME_STATUS, bool> OnStatusUpdated = new GameEvent<INGAME_STATUS, bool>();

	public GameEvent<byte, bool> OnStoreHealthUpdate = new GameEvent<byte, bool>();

	public GameEvent<byte, bool> OnRoundUpdate = new GameEvent<byte, bool>();

	public GameEvent<uint, bool> OnWorldTimerUpdate = new GameEvent<uint, bool>();

	public GameEvent<bool, bool> OnWorldTimerWarningUpdate = new GameEvent<bool, bool>();

	public GameEvent<int, bool> OnPlayerCountUpdate = new GameEvent<int, bool>();

	private util_timer _worldTimer;

	private util_timer _worldStartTimer;

	private util_timer _roundEndTimer;

	private int _extraWorldTime;

	private Coroutine _worldGenerateCoroutine;

	private util_timer _gameOverTimer;

	private readonly List<NetworkObject> _spawnedAshes = new List<NetworkObject>();

	private util_timer _soloRevive;

	private readonly Dictionary<entity_player, util_timer> _autoRevive = new Dictionary<entity_player, util_timer>();

	private int _soloRevives = MAX_SOLO_REVIVES;

	private util_timer _elevatorTimer;

	private readonly NetVar<INGAME_STATUS> _status = new NetVar<INGAME_STATUS>(INGAME_STATUS.IDLE);

	private readonly NetVar<byte> _debtHealth = new NetVar<byte>(START_HEALTH);

	private readonly NetVar<byte> _currentRound = new NetVar<byte>(1);

	private readonly NetVar<byte> _maxInventory = new NetVar<byte>(START_INVENTORY_SLOTS);

	private readonly NetVar<uint> _worldTime = new NetVar<uint>(0u);

	private readonly NetVar<bool> _worldTimeWarning = new NetVar<bool>(value: false);

	private readonly NetVar<byte> _connectedPlayers = new NetVar<byte>(1);

	private readonly NetVar<bool> _elevator = new NetVar<bool>(value: false);

	private readonly NetVar<EntryRenderSettings> _exteriorRenderSettings = new NetVar<EntryRenderSettings>();

	private readonly NetVar<byte> _endGameRank = new NetVar<byte>(byte.MaxValue);

	public new void Awake()
	{
		base.Awake();
		if (!blastDoor)
		{
			throw new UnityException("Missing bunker door");
		}
		if (!frontBlastDoor)
		{
			throw new UnityException("Missing front blast door");
		}
		if (!shipPosition)
		{
			throw new UnityException("Missing ship position");
		}
		if (!shipArea)
		{
			throw new UnityException("Missing ship area");
		}
		if (!shipElevator)
		{
			throw new UnityException("Missing ship elevator");
		}
		if (!shipElevatorSquash)
		{
			throw new UnityException("Missing ship elevator squash");
		}
		shipElevatorSquash.SetActive(value: false);
		if (!cityLiftoff)
		{
			throw new UnityException("Missing ship liftoff");
		}
		cityLiftoff.gameObject.SetActive(Status() == INGAME_STATUS.IDLE);
		if (!shipShellMDL)
		{
			throw new UnityException("Missing ship shell model");
		}
		shipShellMDL.SetActive(value: false);
		if (!jailWhiteWall)
		{
			throw new UnityException("Missing jail white wall");
		}
		jailWhiteWall.SetActive(value: false);
		if (!cameraJailPosition)
		{
			throw new UnityException("Missing camera jail position");
		}
		if (!playerJailTeleportPosition)
		{
			throw new UnityException("Missing player jail teleport position");
		}
		if (!jailEndgameText)
		{
			throw new UnityException("Missing jailEndgameText");
		}
		jailEndgameText.text = "";
		if (!jailMusicSource)
		{
			throw new UnityException("Missing jailMusicSource");
		}
		List<AudioClip> list = jailSadClownMusic;
		if (list == null || list.Count <= 0)
		{
			throw new UnityException("Missing jailSadClownMusic");
		}
		if (!explosionVFX)
		{
			throw new UnityException("Missing explosionVFX");
		}
		explosionVFX.SetActive(value: false);
		if (!retryButton)
		{
			throw new UnityException("Missing retryButton");
		}
		if (!startGameButton)
		{
			throw new UnityException("Missing startGameButton");
		}
		if (!startGameLED)
		{
			throw new UnityException("Missing startGameLED");
		}
		if (!startGameForceField)
		{
			throw new UnityException("Missing startGameForceField");
		}
		if (!ashesPrefab)
		{
			throw new UnityException("Missing ashesPrefab");
		}
		if (!sun)
		{
			throw new UnityException("Missing sun");
		}
		if (!veggieArea)
		{
			throw new UnityException("Missing veggieArea");
		}
		SDK.GetCurrentRound = GetCurrentRound;
		NETController.SetRichPresence("Waiting for next contract");
		NETController.SetTimelineStatus(playing: false);
	}

	protected override void OnNetworkPostSpawn()
	{
		base.OnNetworkPostSpawn();
		if (!base.IsClient)
		{
			return;
		}
		_connectedPlayers.RegisterOnValueChanged(delegate(byte _, byte newValue)
		{
			NETController.UpdateRichPresenceCount(newValue);
			OnPlayerCountUpdate?.Invoke(newValue, param2: false);
		});
		_elevator.RegisterOnValueChanged(delegate(bool _, bool newValue)
		{
			if ((bool)shipElevator && (bool)shipElevatorSquash)
			{
				shipElevator.SetOpen(newValue);
				shipElevatorSquash.SetActive(newValue);
			}
		});
		_exteriorRenderSettings.RegisterOnValueChanged(delegate(EntryRenderSettings _, EntryRenderSettings newValue)
		{
			if ((bool)blastDoor)
			{
				blastDoor.openRotation = ((newValue.doorOpenAngle == Vector3.zero) ? new Vector3(0f, -138.3f, 0f) : newValue.doorOpenAngle);
			}
			if ((bool)shipShellMDL)
			{
				shipShellMDL.SetActive(newValue.exterior);
			}
			if ((bool)sun)
			{
				sun.gameObject.SetActive(newValue.sun.intensity > 0f);
				sun.intensity = newValue.sun.intensity;
				sun.color = newValue.sun.color;
				sun.useColorTemperature = false;
				sun.transform.localEulerAngles = newValue.sun.angle;
			}
		});
		_status.RegisterOnValueChanged(delegate(INGAME_STATUS _, INGAME_STATUS newValue)
		{
			bool newValue2 = newValue == INGAME_STATUS.PLAYING || newValue == INGAME_STATUS.WAITING_PLAY_CONFIRMATION || newValue == INGAME_STATUS.GAMEOVER;
			switch (newValue)
			{
			case INGAME_STATUS.GENERATE:
			case INGAME_STATUS.WAITING_PLAY_CONFIRMATION:
				blastDoor.speed = 0.15f;
				break;
			case INGAME_STATUS.GAMEOVER:
				blastDoor.speed = 20f;
				break;
			default:
				blastDoor.speed = 0.35f;
				break;
			}
			blastDoor.SetOpen(newValue2);
			frontBlastDoor.SetOpen(newValue == INGAME_STATUS.IDLE, delegate(bool doorStatus)
			{
				cityLiftoff?.gameObject.SetActive(doorStatus);
			});
			cityLiftoff.SetOpen(newValue == INGAME_STATUS.IDLE);
			startGameLED.SetActive(newValue == INGAME_STATUS.WAITING_PLAY_CONFIRMATION);
			startGameForceField.SetActive(newValue == INGAME_STATUS.WAITING_PLAY_CONFIRMATION);
			jailWhiteWall.SetActive(newValue == INGAME_STATUS.GAMEOVER);
			NetController<MapController>.Instance?.SetAmbientMusic(newValue == INGAME_STATUS.PLAYING);
			switch (newValue)
			{
			case INGAME_STATUS.IDLE:
				MonoController<BloodController>.Instance.ClearBlood();
				NETController.SetTimelineStatus(playing: false);
				NETController.SetRichPresence("Waiting for next contract");
				break;
			case INGAME_STATUS.PLAYING:
				NETController.SetTimelineStatus(playing: true);
				NETController.SetRichPresence("Performing deliveries");
				MonoController<DiscordController>.Instance?.StartNewTime();
				break;
			case INGAME_STATUS.ROUND_END:
				MonoController<DiscordController>.Instance?.ClearTime();
				NETController.SetRichPresence($"Round {_currentRound.Value} ended");
				break;
			case INGAME_STATUS.WAITING_PLAY_CONFIRMATION:
				OnWaitingConfirmationCLIENT();
				break;
			case INGAME_STATUS.GAMEOVER:
				NETController.SetRichPresence("Arrested");
				OnGameOverCLIENT();
				break;
			}
			OnStatusUpdated?.Invoke(newValue, param2: false);
		});
		_debtHealth.RegisterOnValueChanged(delegate(byte _, byte newValue)
		{
			OnStoreHealthUpdate?.Invoke(newValue, param2: false);
		});
		_worldTime.RegisterOnValueChanged(delegate(uint _, uint newValue)
		{
			OnWorldTimerUpdate?.Invoke(newValue, param2: false);
		});
		_worldTimeWarning.RegisterOnValueChanged(delegate(bool _, bool newValue)
		{
			OnWorldTimerWarningUpdate?.Invoke(newValue, param2: false);
		});
		_currentRound.RegisterOnValueChanged(delegate(byte _, byte newValue)
		{
			NETController.SetCurrentRound(newValue);
			OnRoundUpdate?.Invoke(newValue, param2: false);
		});
		_maxInventory.RegisterOnValueChanged(delegate(byte oldValue, byte newValue)
		{
			if (oldValue != newValue)
			{
				MonoController<UIController>.Instance?.RebuildInventory();
			}
		});
	}

	public override void OnNetworkPreDespawn()
	{
		base.OnNetworkPreDespawn();
		if (base.IsClient)
		{
			_connectedPlayers.OnValueChanged = null;
			_elevator.OnValueChanged = null;
			_exteriorRenderSettings.OnValueChanged = null;
			_status.OnValueChanged = null;
			_debtHealth.OnValueChanged = null;
			_worldTime.OnValueChanged = null;
			_worldTimeWarning.OnValueChanged = null;
			_currentRound.OnValueChanged = null;
			_maxInventory.OnValueChanged = null;
		}
	}

	public override void OnNetworkSpawn()
	{
		base.OnNetworkSpawn();
		if (!MonoController<PlayerController>.Instance)
		{
			throw new UnityException("Missing PlayerController");
		}
		if (!NETController.Instance)
		{
			throw new UnityException("Missing NETController");
		}
		GameObject gameObject = GameObject.Find("[SERVER-RETRY]");
		if (!gameObject)
		{
			throw new UnityException("Retry button not found");
		}
		if (!base.IsServer)
		{
			UnityEngine.Object.DestroyImmediate(gameObject);
			return;
		}
		LocalizeStringEvent[] array = UnityEngine.Object.FindObjectsByType<LocalizeStringEvent>(FindObjectsInactive.Include, FindObjectsSortMode.None);
		for (int i = 0; i < array.Length; i++)
		{
			array[i].RefreshString();
		}
		shipElevator.OnDoorUpdate += new Action<bool>(OnElevatorArrive);
		foreach (entity_button_emission shipElevatorButton in shipElevatorButtons)
		{
			if ((bool)shipElevatorButton)
			{
				shipElevatorButton.OnUSE += new Action<entity_player>(OnElevatorCalled);
			}
		}
		CoreController.WaitFor(delegate(PlayerController plyCtrl)
		{
			plyCtrl.OnPlayerCreated += new Action<entity_player, bool>(OnPlayerCreated);
			plyCtrl.OnPlayerRemoved += new Action<entity_player, bool>(OnPlayerRemoved);
			plyCtrl.OnPlayerDeath += new Action<entity_player, bool>(OnPlayerDeath);
		});
		NETController.Instance.OnClientConnectedCallback += OnClientConnectionUpdate;
		NETController.Instance.OnClientDisconnectCallback += OnClientConnectionUpdate;
		OnClientConnectionUpdate((byte)NETController.Instance.ConnectedClients.Count);
		startGameButton.OnUSE += new Action(OnStartGameButton);
		retryButton.onClick.AddListener(OnServerRetryButton);
	}

	[Server]
	public int GetAutoRevives()
	{
		if (base.IsSpawned && !base.IsServer)
		{
			throw new UnityException("Server only");
		}
		return _soloRevives;
	}

	[Server]
	public void SetAutoRevives(int revives)
	{
		if (base.IsSpawned && !base.IsServer)
		{
			throw new UnityException("Server only");
		}
		_soloRevives = revives;
	}

	[Server]
	private void OnElevatorCalled(entity_player obj)
	{
		if (!base.IsServer)
		{
			throw new UnityException("Server only");
		}
		foreach (entity_button_emission shipElevatorButton in shipElevatorButtons)
		{
			shipElevatorButton?.SetLocked(newVal: true);
		}
		_elevator.Value = !_elevator.Value;
	}

	[Server]
	private void OnClientConnectionUpdate(ulong obj)
	{
		if (!base.IsServer)
		{
			throw new UnityException("Server only");
		}
		_connectedPlayers.Value = (byte)NETController.Instance.ConnectedClients.Count;
		OnPlayerCountUpdate?.Invoke(NETController.Instance.ConnectedClients.Count, param2: true);
	}

	public int GetConnectedPlayers()
	{
		return _connectedPlayers.Value;
	}

	[Server]
	private void OnServerRetryButton()
	{
		if (!base.IsServer)
		{
			throw new UnityException("Server only");
		}
		if (Status() != INGAME_STATUS.GAMEOVER)
		{
			throw new UnityException("Status should be GAMEOVER when calling OnServerRetryButton");
		}
		MonoController<StartupController>.Instance.ReleaseCursor("GAMEOVER");
		NETController.Instance.SceneManager.LoadScene("INGAME", LoadSceneMode.Single);
	}

	public override void OnNetworkDespawn()
	{
		base.OnNetworkDespawn();
		if (!base.IsServer)
		{
			return;
		}
		_worldTimer?.Stop();
		_worldStartTimer?.Stop();
		_gameOverTimer?.Stop();
		_soloRevive?.Stop();
		_roundEndTimer?.Stop();
		if (_worldGenerateCoroutine != null)
		{
			StopCoroutine(_worldGenerateCoroutine);
		}
		foreach (util_timer value in _autoRevive.Values)
		{
			value?.Stop();
		}
		_autoRevive.Clear();
		if ((bool)MonoController<PlayerController>.Instance)
		{
			MonoController<PlayerController>.Instance.OnPlayerCreated -= new Action<entity_player, bool>(OnPlayerCreated);
			MonoController<PlayerController>.Instance.OnPlayerRemoved -= new Action<entity_player, bool>(OnPlayerRemoved);
			MonoController<PlayerController>.Instance.OnPlayerDeath -= new Action<entity_player, bool>(OnPlayerDeath);
		}
		if ((bool)NETController.Instance)
		{
			NETController.Instance.OnClientConnectedCallback -= OnClientConnectionUpdate;
			NETController.Instance.OnClientDisconnectCallback -= OnClientConnectionUpdate;
		}
		_elevatorTimer?.Stop();
		shipElevator.OnDoorUpdate -= new Action<bool>(OnElevatorArrive);
		foreach (entity_button_emission shipElevatorButton in shipElevatorButtons)
		{
			if ((bool)shipElevatorButton)
			{
				shipElevatorButton.OnUSE -= new Action<entity_player>(OnElevatorCalled);
			}
		}
		if ((bool)startGameButton)
		{
			startGameButton.OnUSE -= new Action(OnStartGameButton);
		}
		if ((bool)retryButton)
		{
			retryButton.onClick.RemoveAllListeners();
		}
	}

	[Server]
	private void OnElevatorArrive(bool arrived)
	{
		if (!base.IsServer)
		{
			throw new UnityException("Server only");
		}
		_elevatorTimer?.Stop();
		_elevatorTimer = util_timer.Simple(1f, delegate
		{
			foreach (entity_button_emission shipElevatorButton in shipElevatorButtons)
			{
				shipElevatorButton?.SetLocked(newVal: false);
			}
		});
	}

	public override void OnDestroy()
	{
		NETController.ValidatePlayerJoin = null;
		SDK.GetCurrentRound = null;
		base.OnDestroy();
	}

	[Server]
	public void SpawnPlayerAshes(Vector3 deathPosition)
	{
		if (!base.IsServer)
		{
			throw new UnityException("SpawnPlayerAshes can only be called on server!");
		}
		if (!ashesPrefab)
		{
			throw new UnityException("Ashes prefab is not set!");
		}
		if (Status() == INGAME_STATUS.PLAYING)
		{
			GameObject obj = UnityEngine.Object.Instantiate(ashesPrefab, deathPosition + Vector3.up * 0.1f, ashesPrefab.transform.rotation);
			if (!obj)
			{
				throw new UnityException("Failed to instantiate ashes prefab!");
			}
			NetworkObject component = obj.GetComponent<NetworkObject>();
			if (!component)
			{
				throw new UnityException("Ashes prefab does not have a NetworkObject component!");
			}
			NetController<EffectController>.Instance.PlayEffectRPC(EffectType.SMOKE_LARGE, deathPosition + Vector3.up * 0.2f, new EffectSettings
			{
				playSound = false
			});
			component.Spawn();
			_spawnedAshes.Add(component);
		}
	}

	[Server]
	public void CleanPlayerAshes()
	{
		if (!base.IsServer)
		{
			throw new UnityException("CleanPlayerAshes can only be called on server!");
		}
		if (_spawnedAshes.Count == 0)
		{
			return;
		}
		foreach (NetworkObject spawnedAsh in _spawnedAshes)
		{
			if ((bool)spawnedAsh && spawnedAsh.IsSpawned)
			{
				spawnedAsh.Despawn();
			}
		}
		_spawnedAshes.Clear();
	}

	[Server]
	public void RespawnDeadPlayers()
	{
		if (!base.IsServer)
		{
			throw new UnityException("RestorePlayersHP can only be called on server!");
		}
		List<entity_player> deadPlayers = MonoController<PlayerController>.Instance.GetDeadPlayers();
		if (deadPlayers == null || deadPlayers.Count == 0)
		{
			return;
		}
		foreach (entity_player item in deadPlayers)
		{
			if ((bool)item)
			{
				item.Revive();
			}
		}
	}

	[Server]
	public void RestorePlayersHP()
	{
		if (!base.IsServer)
		{
			throw new UnityException("RestorePlayersHP can only be called on server!");
		}
		List<entity_player> alivePlayers = MonoController<PlayerController>.Instance.GetAlivePlayers();
		if (alivePlayers == null || alivePlayers.Count == 0)
		{
			return;
		}
		foreach (entity_player item in alivePlayers)
		{
			if ((bool)item)
			{
				item.SetHealthRPC(100);
			}
		}
	}

	[Server]
	private void OnPlayerCreated(entity_player ply, bool server)
	{
		if ((bool)ply && server)
		{
			INGAME_STATUS value = _status.Value;
			if (value == INGAME_STATUS.PLAYING || value == INGAME_STATUS.GAMEOVER)
			{
				ply.Kill(DamageType.INSTANT);
			}
			if (_status.Value == INGAME_STATUS.GAMEOVER)
			{
				SetPrisonOutfit();
			}
			ContractController instance = NetController<ContractController>.Instance;
			if ((object)instance != null && instance.IsHorrorMode())
			{
				ply.SetFlashlight(on: true);
			}
			NetController<NotificationController>.Instance?.BroadcastAllRPC(new NotificationData
			{
				id = $"player-join-{ply.GetConnectionID()}",
				duration = 3f,
				text = "ingame.ui.notification.player-join||" + ply.GetPlayerName()
			});
		}
	}

	[Server]
	private void OnPlayerRemoved(entity_player ply, bool server)
	{
		if (server && (bool)ply)
		{
			CheckGameover(ply, server: true);
			NetController<NotificationController>.Instance?.BroadcastAllRPC(new NotificationData
			{
				id = $"player-leave-{ply.GetConnectionID()}",
				duration = 3f,
				text = "ingame.ui.notification.player-leave||" + ply.GetPlayerName()
			});
		}
	}

	[Server]
	public void RequestNewRound()
	{
		switch (_status.Value)
		{
		case INGAME_STATUS.IDLE:
			SetGameStatus(INGAME_STATUS.GENERATE);
			break;
		case INGAME_STATUS.PLAYING:
			SetGameStatus(INGAME_STATUS.ROUND_END);
			break;
		default:
			throw new UnityException($"Invalid status {_status.Value} when requesting new round!");
		}
	}

	[Shared]
	public byte GetCurrentRound()
	{
		return _currentRound.Value;
	}

	[Server]
	public void SetRound(byte round)
	{
		if (!base.IsServer)
		{
			throw new UnityException("SetRound can only be called on server!");
		}
		byte b = Math.Clamp(round, (byte)1, byte.MaxValue);
		_currentRound.Value = b;
		OnRoundUpdate.Invoke(b, param2: true);
	}

	[Server]
	public void SetMaxInventorySlots(byte slots)
	{
		if (base.IsSpawned && !base.IsServer)
		{
			throw new UnityException("SetMaxInventorySlots can only be called on server!");
		}
		if (slots < 1)
		{
			throw new UnityException("Slots must be greater than 0!");
		}
		if (slots > MAX_INVENTORY_SLOTS)
		{
			throw new UnityException($"Slots must be less than {MAX_INVENTORY_SLOTS}!");
		}
		_maxInventory.SetSpawnValue(slots);
	}

	public byte GetMaxInventorySlots()
	{
		return _maxInventory.Value;
	}

	[Server]
	public void SetGameStatus(INGAME_STATUS status)
	{
		if (Status() != status)
		{
			_status.Value = status;
			switch (status)
			{
			case INGAME_STATUS.IDLE:
				OnIngameIdle();
				break;
			case INGAME_STATUS.GENERATE:
				OnIngameGeneration();
				break;
			case INGAME_STATUS.WAITING_PLAY_CONFIRMATION:
				OnIngameWaitingConfirmation();
				break;
			case INGAME_STATUS.PLAYING:
				OnIngamePlaying();
				break;
			case INGAME_STATUS.ROUND_END:
				OnIngameRoundEnd();
				break;
			case INGAME_STATUS.GAMEOVER:
				OnGameOver();
				break;
			}
			OnStatusUpdated?.Invoke(status, param2: true);
		}
	}

	public INGAME_STATUS Status()
	{
		return _status.Value;
	}

	public INGAME_STATUS OldStatus()
	{
		return _status.PrevValue;
	}

	public bool IsShipArea(entity_player ply)
	{
		if ((bool)shipArea)
		{
			return shipArea.bounds.Contains(ply.transform.position);
		}
		throw new UnityException("Missing ship area");
	}

	[Server]
	private void OnIngameIdle()
	{
		if (Status() != 0)
		{
			throw new UnityException("Status should be set to IDLE when calling 'onIngameIdle'");
		}
		RespawnDeadPlayers();
		RestorePlayersHP();
		startGameButton.SetLocked(newVal: true);
	}

	[Server]
	private void OnIngameGeneration()
	{
		if (Status() != INGAME_STATUS.GENERATE)
		{
			throw new UnityException("Status should be set to GENERATE when calling 'onGenerateWorld'");
		}
		NetController<LightController>.Instance.ExecuteAllLightCommand(LightCommand.FLICKER);
		NetController<SoundController>.Instance.PlaySound("Ingame/Ship/ship_start.ogg", new AudioData
		{
			volume = 0.6f
		}, broadcast: true);
		NetController<ShakeController>.Instance.ShakeRPC(ShakeMode.SHAKE_ALL, 0.1f, 0.35f);
		util_timer.Simple(4.38f, delegate
		{
			NetController<ShakeController>.Instance.ShakeRPC(ShakeMode.SHAKE_ALL, 0.1f, 0.35f);
			GenerateWorld(MapRenderingSettings, delegate
			{
				SetGameStatus(INGAME_STATUS.WAITING_PLAY_CONFIRMATION);
			});
		});
	}

	[Server]
	private void GenerateWorld(Action<EntryRenderSettings> preGen, Action onComplete)
	{
		if (!NetController<MapController>.Instance)
		{
			throw new UnityException("Missing MapController");
		}
		WorldSettings worldSettings = null;
		string worldSeedOverride = ConsoleController.WorldSeedOverride;
		if (!NETController.SV_CHEATS && MonoController<SettingsController>.Instance.IsFirstTimer())
		{
			Debug.Log("New player detected, forcing apartments until completed");
			worldSettings = NetController<MapController>.Instance.GetWorld("apartments");
		}
		if (!worldSettings)
		{
			if (!string.IsNullOrEmpty(ConsoleController.WorldIDOverride))
			{
				worldSettings = NetController<MapController>.Instance.GetWorld(ConsoleController.WorldIDOverride);
			}
			else
			{
				List<WorldSettings> worlds = NetController<MapController>.Instance.GetWorlds(GetCurrentRound());
				if (worlds == null || worlds.Count <= 0)
				{
					throw new UnityException("No valid worlds found");
				}
				float num = worlds.AsValueEnumerable().Sum((WorldSettings w) => w.weight);
				WorldSettings worldSettings2;
				if (!(num <= 0f))
				{
					worldSettings2 = worlds.AsValueEnumerable().Aggregate((0f, UnityEngine.Random.Range(0f, num), null), ((float cumWeight, float randomValue, WorldSettings result) acc, WorldSettings world) => (!acc.result) ? ((!(acc.cumWeight + world.weight >= acc.randomValue)) ? (cumWeight: acc.cumWeight + world.weight, randomValue: acc.randomValue, result: null) : (cumWeight: acc.cumWeight, randomValue: acc.randomValue, result: world)) : acc).result;
					if ((object)worldSettings2 == null)
					{
						worldSettings2 = worlds[worlds.Count - 1];
					}
				}
				else
				{
					worldSettings2 = worlds[UnityEngine.Random.Range(0, worlds.Count)];
				}
				worldSettings = worldSettings2;
			}
		}
		if (!worldSettings)
		{
			throw new UnityException("Failed to find a valid world!");
		}
		if (_worldGenerateCoroutine != null)
		{
			StopCoroutine(_worldGenerateCoroutine);
		}
		_worldGenerateCoroutine = StartCoroutine(NetController<MapController>.Instance.Generate(worldSettings, worldSeedOverride, preGen, onComplete));
	}

	public bool IsTimerWarning()
	{
		return _worldTimeWarning.Value;
	}

	[Server]
	private void TriggerTimerWarning()
	{
		if (!base.IsServer)
		{
			throw new UnityException("TriggerTimerWarning can only be called on server!");
		}
		if (!_worldTimeWarning.Value)
		{
			NetController<ShakeController>.Instance.Shake3DRPC(NetController<IngameController>.Instance.shipPosition.position, ShakeMode.SHAKE_ALL, 0.2f, 0.05f, ShakeSoundMode.OFF, 10f);
			NetController<SoundController>.Instance.Play3DSound("Ingame/Ship/detection_start.ogg", NetController<IngameController>.Instance.shipPosition.position, new AudioData
			{
				distance = 10f
			}, broadcast: true);
			NetController<NotificationController>.Instance.BroadcastAllRPC(new NotificationData
			{
				duration = 4f,
				id = "world_timer_warning",
				text = $"ingame.ui.notification.police||{WARNING_TIMER}",
				soundEffect = "Ingame/Notifications/UI_Notification_Denied_05_stereo.ogg"
			});
			SetTimerWarning(warning: true);
		}
	}

	[Server]
	private void SetTimerWarning(bool warning)
	{
		if (!base.IsServer)
		{
			throw new UnityException("SetTimerWarning can only be called on server!");
		}
		if (_worldTimeWarning.Value != warning)
		{
			if ((bool)NetController<PowerController>.Instance)
			{
				NetController<PowerController>.Instance.SetPoweredArea(PowerGrid.BASE, !warning);
			}
			_worldTimeWarning.Value = warning;
			OnWorldTimerWarningUpdate?.Invoke(warning, param2: true);
		}
	}

	[Server]
	public bool TakeShipHealth(byte amount)
	{
		if (!base.IsSpawned && base.IsServer)
		{
			throw new UnityException("Server only");
		}
		if (GetHealth() <= 0)
		{
			return false;
		}
		SetHealth((byte)(_debtHealth.Value - amount));
		return _debtHealth.Value <= 0;
	}

	[Server]
	public void AddHealth(byte amount)
	{
		if (!base.IsSpawned && base.IsServer)
		{
			throw new UnityException("Server only");
		}
		if (GetHealth() < 3)
		{
			SetHealth((byte)(_debtHealth.Value + amount));
		}
	}

	[Server]
	public void SetHealth(byte amount)
	{
		if (!base.IsSpawned && base.IsServer)
		{
			throw new UnityException("Server only");
		}
		_debtHealth.SetSpawnValue((byte)Math.Clamp((int)amount, 0, (int)MAX_HEALTH));
		OnStoreHealthUpdate?.Invoke(amount, param2: true);
	}

	public byte GetHealth()
	{
		return _debtHealth.Value;
	}

	[Server]
	public void MapRenderingSettings(EntryRenderSettings render)
	{
		if (!base.IsServer)
		{
			throw new UnityException("RenderShipShell can only be called on server!");
		}
		_exteriorRenderSettings.Value = render;
	}

	[Server]
	private void OnStartGameButton()
	{
		if (!base.IsServer)
		{
			throw new UnityException("Server only");
		}
		startGameButton.SetLocked(newVal: true);
		_worldStartTimer?.Stop();
		_worldStartTimer = util_timer.Create(4, 1f, delegate(int i)
		{
			NetController<SoundController>.Instance.Play3DSound((i >= 1) ? "Ingame/Ship/ship_gate_start.ogg" : "Ingame/Ship/ship_gate_go.ogg", startGameButton.transform.position, new AudioData
			{
				distance = 8f,
				volume = 0.75f
			}, broadcast: true);
		}, delegate
		{
			NetController<SoundController>.Instance.Play3DSound("Ingame/Power/forcefield_down.ogg", startGameForceField.transform.position, new AudioData
			{
				distance = 6f,
				volume = 0.75f,
				pitch = UnityEngine.Random.Range(0.75f, 1.25f)
			}, broadcast: true);
			SetGameStatus(INGAME_STATUS.PLAYING);
		});
	}

	[Server]
	private void OnIngameWaitingConfirmation()
	{
		if (!base.IsServer)
		{
			throw new UnityException("Server only");
		}
		startGameButton.SetLocked(newVal: false);
		RespawnDeadPlayers();
	}

	[Server]
	private void OnIngamePlaying()
	{
		if (!base.IsServer)
		{
			throw new UnityException("Server only");
		}
		NetController<DeliveryController>.Instance.GenerateAddresses();
		NetController<ContractController>.Instance.GenerateRoundTasks();
		_worldStartTimer?.Stop();
		UpdateNetworkTime(GetWorldTime());
		_worldTimer?.Stop();
		_worldTimer = util_timer.Create(GetWorldTime(), 1f, UpdateNetworkTime, delegate
		{
			NetController<NotificationController>.Instance?.BroadcastAllRPC(new NotificationData
			{
				id = "ship-leaving",
				text = "ingame.ui.notification.police-arrived",
				duration = 8f
			});
			SetHealth(0);
			SetGameStatus(INGAME_STATUS.ROUND_END);
		});
	}

	[Server]
	private void UpdateNetworkTime(int timer)
	{
		if (!base.IsServer)
		{
			throw new UnityException("Server only");
		}
		if (timer == WARNING_TIMER)
		{
			TriggerTimerWarning();
		}
		_worldTime.Value = (uint)timer;
		OnWorldTimerUpdate?.Invoke((uint)timer, param2: true);
	}

	[Server]
	private void OnIngameRoundEnd()
	{
		if (!base.IsServer)
		{
			throw new UnityException("Server only");
		}
		if (Status() != INGAME_STATUS.ROUND_END)
		{
			throw new UnityException("Status should be set to ROUND_END when calling 'onIngameEnd'");
		}
		if (!NetController<CurrencyController>.Instance)
		{
			throw new UnityException("Missing CurrencyController instance");
		}
		if (!NetController<MapController>.Instance)
		{
			throw new UnityException("Missing MapController instance");
		}
		if (!NetController<EndController>.Instance)
		{
			throw new UnityException("Missing EndController instance");
		}
		if (!NetController<SpeedrunController>.Instance)
		{
			throw new UnityException("Missing SpeedrunController instance");
		}
		if (!NetController<SoundController>.Instance)
		{
			throw new UnityException("Missing SoundController instance");
		}
		if (!MonoController<PlayerController>.Instance)
		{
			throw new UnityException("Missing PlayerController instance");
		}
		List<entity_player> alivePlayers = MonoController<PlayerController>.Instance.GetAlivePlayers();
		ReportStatus endGameStatus = ReportStatus.SUCCESS;
		if (!NetController<CurrencyController>.Instance.PaidDebt())
		{
			endGameStatus = ReportStatus.FAIL_DEBT;
		}
		else if (alivePlayers.Count == 0)
		{
			endGameStatus = ReportStatus.FAIL_NO_ALIVE;
		}
		if (endGameStatus == ReportStatus.SUCCESS)
		{
			string generatedWorldID = NetController<MapController>.Instance.GetGeneratedWorldID();
			foreach (entity_player item in alivePlayers)
			{
				if ((bool)item && !item.IsDead())
				{
					switch (generatedWorldID)
					{
					case "apartments":
						NetController<StatsController>.Instance.UnlockAchievementSV(STEAM_ACHIEVEMENTS.ACHIEVEMENT_MAP_APARTMENTS, item.GetConnectionID());
						break;
					case "train":
						NetController<StatsController>.Instance.UnlockAchievementSV(STEAM_ACHIEVEMENTS.ACHIEVEMENT_MAP_TRAIN, item.GetConnectionID());
						break;
					case "city":
						NetController<StatsController>.Instance.UnlockAchievementSV(STEAM_ACHIEVEMENTS.ACHIEVEMENT_MAP_CITY, item.GetConnectionID());
						break;
					case "fracture":
						NetController<StatsController>.Instance.UnlockAchievementSV(STEAM_ACHIEVEMENTS.ACHIEVEMENT_MAP_FRACTURE, item.GetConnectionID());
						break;
					case "trenches":
						NetController<StatsController>.Instance.UnlockAchievementSV(STEAM_ACHIEVEMENTS.ACHIEVEMENT_MAP_TRENCHES, item.GetConnectionID());
						break;
					}
				}
			}
			NetController<StatsController>.Instance.UnlockAchievementSV(STEAM_ACHIEVEMENTS.ACHIEVEMENT_WELCOME, ulong.MaxValue);
			if (NetController<ScrapController>.Instance.GetWorldScrap(prev: true) == 0)
			{
				NetController<StatsController>.Instance.UnlockAchievementSV(STEAM_ACHIEVEMENTS.ACHIEVEMENT_SQUEAKY_CLEAN, ulong.MaxValue);
			}
			using (ValueEnumerator<GroupBy<ArrayWhere<entity_phys_prop_scrap_urchin>, entity_phys_prop_scrap_urchin, entity_player>, IGrouping<entity_player, entity_phys_prop_scrap_urchin>> valueEnumerator = (from u in UnityEngine.Object.FindObjectsByType<entity_phys_prop_scrap_urchin>(FindObjectsInactive.Exclude, FindObjectsSortMode.None).AsValueEnumerable()
				where u.GetAttachedPlayer()
				group u by u.GetAttachedPlayer()).GetEnumerator<GroupBy<ArrayWhere<entity_phys_prop_scrap_urchin>, entity_phys_prop_scrap_urchin, entity_player>, IGrouping<entity_player, entity_phys_prop_scrap_urchin>>())
			{
				while (valueEnumerator.MoveNext())
				{
					IGrouping<entity_player, entity_phys_prop_scrap_urchin> current2 = valueEnumerator.Current;
					if (current2.AsValueEnumerable().Count() >= 8)
					{
						entity_player key = current2.Key;
						if ((bool)key && !key.IsDead() && IsShipArea(key))
						{
							NetController<StatsController>.Instance.UnlockAchievementSV(STEAM_ACHIEVEMENTS.ACHIEVEMENT_URCHIN, key.GetConnectionID());
						}
					}
				}
			}
			uint totalTime = NetController<SpeedrunController>.Instance.GetTotalTime();
			int currentRound = GetCurrentRound();
			if (totalTime <= 900 && currentRound == 5)
			{
				NetController<StatsController>.Instance.UnlockAchievementSV(STEAM_ACHIEVEMENTS.ACHIEVEMENT_SPEEDRUN, ulong.MaxValue);
			}
		}
		ResetWorldTimer();
		_roundEndTimer?.Stop();
		_roundEndTimer = util_timer.Simple(3f, delegate
		{
			CleanupWorld();
			MapRenderingSettings(default(EntryRenderSettings));
			if (GetHealth() <= 0)
			{
				TriggerGameOverScene();
			}
			else
			{
				NetController<SoundController>.Instance.PlaySound("Ingame/EndGame/intro.ogg", new AudioData
				{
					volume = 0.45f,
					mixer = SoundMixer.MUSIC
				}, broadcast: true);
				_roundEndTimer?.Stop();
				_roundEndTimer = util_timer.Simple(2f, delegate
				{
					NetController<EndController>.Instance.Report(endGameStatus, delegate
					{
						if (endGameStatus != 0)
						{
							if (TakeShipHealth(1))
							{
								TriggerGameOverScene();
								return;
							}
						}
						else
						{
							SetRound((byte)(_currentRound.Value + 1));
						}
						NetController<CurrencyController>.Instance.SetDebt(0);
						SetGameStatus(INGAME_STATUS.IDLE);
					});
				});
			}
		});
	}

	private void OnWaitingConfirmationCLIENT()
	{
		if (!base.IsClient)
		{
			throw new UnityException("Client only");
		}
		if ((bool)PlayerController.LOCAL && IsShipArea(PlayerController.LOCAL))
		{
			NetController<ShakeController>.Instance.LocalShake(ShakeMode.SHAKE_ALL, 0.1f, 0.35f);
			NetController<SoundController>.Instance.PlaySound("Ingame/Ship/ship_stop.ogg", new AudioData
			{
				volume = 0.55f
			});
			NetController<SoundController>.Instance.Play3DSound("Ingame/Ship/announcements/arriving_annoucement.ogg", shipPosition.position, new AudioData
			{
				distance = 10f,
				volume = 0.8f
			});
			PlayerController.LOCAL.Shove(-shipPosition.transform.right, 5f);
		}
	}

	[Server]
	public void TriggerGameOverScene()
	{
		if (!base.IsServer)
		{
			throw new UnityException("Server only");
		}
		RespawnDeadPlayers();
		RestorePlayersHP();
		startGameButton.SetLocked(newVal: true);
		_endGameRank.Value = (byte)UnityEngine.Random.Range(0, 255);
		util_timer.Simple(2f, delegate
		{
			util_timer.Create(3, 1.5f, delegate
			{
				NetController<SoundController>.Instance.Play3DSound($"Ingame/Music/Gameover/zebra-ram-{UnityEngine.Random.Range(0, 3)}.ogg", startGameForceField.transform.position, new AudioData
				{
					distance = 10f,
					volume = 1f
				}, broadcast: true);
				NetController<ShakeController>.Instance.Shake3DRPC(startGameForceField.transform.position, ShakeMode.SHAKE_ALL, 0.2f, 0.05f, ShakeSoundMode.OFF, 10f);
				NetController<LightController>.Instance.ExecuteAllLightCommand(LightCommand.FLICKER);
			}, delegate
			{
				SetGameStatus(INGAME_STATUS.GAMEOVER);
			});
		});
	}

	[Server]
	private void OnGameOver()
	{
		if (!base.IsServer)
		{
			throw new UnityException("Server only");
		}
		util_timer.Simple(8f, delegate
		{
			MonoController<StartupController>.Instance.RequestCursor("GAMEOVER", CURSOR_REQUEST.UI_CONTROL);
		});
	}

	[Client]
	private void SetPrisonOutfit()
	{
		if (!base.IsClient)
		{
			throw new UnityException("Client only");
		}
		foreach (entity_player allPlayer in MonoController<PlayerController>.Instance.GetAllPlayers())
		{
			if ((bool)allPlayer)
			{
				allPlayer.ForcePrisonSkin();
			}
		}
	}

	[Client]
	private void OnGameOverCLIENT()
	{
		if (!base.IsClient)
		{
			throw new UnityException("Client only");
		}
		if (!MonoController<LocalizationController>.Instance)
		{
			throw new UnityException("Missing LocalizationController");
		}
		if (!NetController<SpeedrunController>.Instance)
		{
			throw new UnityException("Missing SpeedrunController");
		}
		if (!PlayerController.LOCAL)
		{
			return;
		}
		string totalTime = TimeUtils.SecondsToTime(NetController<SpeedrunController>.Instance.GetTotalTime());
		jailEndgameText.text = "";
		NetController<SoundController>.Instance.Play3DSound("Ingame/Music/Gameover/zebra-ram-open.ogg", startGameForceField.transform.position, new AudioData
		{
			distance = 10f,
			volume = 1f
		});
		explosionVFX.SetActive(value: true);
		MonoController<UIController>.Instance.SetFade(fadeIn: true, 0.7f);
		_gameOverTimer?.Stop();
		_gameOverTimer = util_timer.Simple(5f, delegate
		{
			if (!cameraJailPosition)
			{
				throw new UnityException("Missing camera jail position");
			}
			if (!playerJailTeleportPosition)
			{
				throw new UnityException("Missing player jail teleport position");
			}
			Transform child = playerJailTeleportPosition.GetChild(PlayerController.LOCAL.GetPlayerID() % playerJailTeleportPosition.childCount);
			if (!child)
			{
				child = playerJailTeleportPosition.GetChild(0);
			}
			if (!child)
			{
				throw new UnityException("Missing player jail teleport position");
			}
			PlayerController.LOCAL.SetHealth(100, DamageType.GENERIC);
			PlayerController.LOCAL.SetPosition(child.position, child.rotation);
			PlayerController.LOCAL.RenderPlayerHead(render: true);
			SetPrisonOutfit();
			entity_player_camera camera = PlayerController.LOCAL.GetCamera();
			if (!camera)
			{
				throw new UnityException("Missing player camera");
			}
			camera.ForceLookAt(cameraJailPosition.position, cameraJailPosition.rotation);
			MonoController<UIController>.Instance.HideHUD(hidden: true);
			MonoController<UIController>.Instance.SetFade(fadeIn: false);
			_gameOverTimer?.Stop();
			_gameOverTimer = util_timer.Simple(2f, delegate
			{
				int totalCollectedScrap = NetController<StatsController>.Instance.GetTotalCollectedScrap();
				int value = _currentRound.Value;
				string[] hyenaPrisonRank = GetHyenaPrisonRank();
				string text = "";
				if (hyenaPrisonRank != null && hyenaPrisonRank.Length > 0)
				{
					text = hyenaPrisonRank[_endGameRank.Value % hyenaPrisonRank.Length];
				}
				List<string> statusText = new List<string>
				{
					"<alpha=#66><size=80%>" + MonoController<LocalizationController>.Instance.Get("ingame.gameover.stolen-goods") + "</size><alpha=#FF>\n",
					"<align=right>" + totalCollectedScrap + " <rotate=-90>€</rotate></align>\n",
					"<alpha=#66><size=80%>" + MonoController<LocalizationController>.Instance.Get("ingame.gameover.rounds") + "</size><alpha=#FF>\n",
					"<align=right>" + value + "</align>\n",
					"<alpha=#66><size=80%>" + MonoController<LocalizationController>.Instance.Get("ingame.speedrun.total") + "</size><alpha=#FF>\n",
					"<align=right>" + totalTime + "</align>\n",
					"<alpha=#66><size=80%>" + MonoController<LocalizationController>.Instance.Get("ingame.gameover.score") + "</size><alpha=#FF>\n\n",
					"<b><align=center><shake a=0.01>" + text + "</shake></align></b>"
				};
				_gameOverTimer?.Stop();
				_gameOverTimer = util_timer.Create(statusText.Count, 1f, delegate(int t)
				{
					jailEndgameText.text += statusText[statusText.Count - t - 1];
					NetController<SoundController>.Instance.Play3DSound("General/Entities/Place/remove_entity_0.ogg", jailEndgameText.transform.position, new AudioData
					{
						distance = 10f
					});
				});
			});
			jailMusicSource.loop = true;
			jailMusicSource.clip = jailSadClownMusic[UnityEngine.Random.Range(0, jailSadClownMusic.Count)];
			jailMusicSource.Play();
		});
	}

	private string[] GetHyenaPrisonRank()
	{
		if (!NetController<StatsController>.Instance)
		{
			throw new UnityException("Missing StatsController");
		}
		if (!MonoController<LocalizationController>.Instance)
		{
			throw new UnityException("Missing LocalizationController");
		}
		float num = NetController<StatsController>.Instance.GetTotalCollectedDeliveries() * 50;
		float num2 = GetCurrentRound() * 150;
		int num3 = Mathf.RoundToInt(num + num2);
		string text = ((num3 >= 1600) ? ((num3 >= 2500) ? "ingame.gameover.tier-0" : ((num3 < 2000) ? "ingame.gameover.tier-2" : "ingame.gameover.tier-1")) : ((num3 < 950) ? "ingame.gameover.tier-4" : "ingame.gameover.tier-3"));
		string key = text;
		string text2 = MonoController<LocalizationController>.Instance.Get(key);
		if (!string.IsNullOrEmpty(text2))
		{
			return text2.Split(new string[1] { "<##>" }, StringSplitOptions.None);
		}
		throw new UnityException("Could not get rank tier data!");
	}

	[Server]
	private void ResetWorldTimer()
	{
		_worldTime.Value = 0u;
		_worldTimer?.Stop();
		_worldStartTimer?.Stop();
		_roundEndTimer?.Stop();
		_soloRevive?.Stop();
		OnWorldTimerUpdate.Invoke(0u, param2: true);
	}

	[Server]
	public int GetWorldTime()
	{
		if (!base.IsServer)
		{
			throw new UnityException("GetWorldTime can only be called on server!");
		}
		return MAX_WORLD_TIME + _extraWorldTime + ((MonoController<PlayerController>.Instance.players.Count <= 1) ? EXTRA_WORLD_SOLO_TIME : 0);
	}

	[Server]
	public void SetPermaWorldTime(int time)
	{
		if (base.IsSpawned && !base.IsServer)
		{
			throw new UnityException("AddExtraWorldTime can only be called on server!");
		}
		_extraWorldTime = time;
	}

	[Server]
	public void AddTemporaryWorldTime(int time)
	{
		if (!base.IsServer)
		{
			throw new UnityException("AddTemporaryWorldTime can only be called on server!");
		}
		if (_worldTimer == null)
		{
			throw new UnityException("Missing world timer");
		}
		_worldTimer.SetTicksLeft(_worldTimer.GetTicksLeft() + time);
		UpdateNetworkTime(_worldTimer.GetTicksLeft());
	}

	[Server]
	private void CleanupWorld()
	{
		if (base.IsServer)
		{
			if (!MonoController<PurgeController>.Instance)
			{
				throw new UnityException("Missing PurgeController");
			}
			MonoController<PurgeController>.Instance.Purge();
			StartCoroutine(NetController<MapController>.Instance.Cleanup());
			_soloRevive?.Stop();
			startGameButton.SetLocked(newVal: true);
			SetTimerWarning(warning: false);
			CleanPlayerAshes();
			NetController<LightController>.Instance.ExecuteAllLightCommand(LightCommand.ON);
		}
	}

	private void OnPlayerDeath(entity_player ply, bool server)
	{
		if (!server || !ply)
		{
			return;
		}
		if (_status.Value != INGAME_STATUS.PLAYING)
		{
			if (_autoRevive.TryGetValue(ply, out var value))
			{
				value?.Stop();
			}
			INGAME_STATUS value2 = _status.Value;
			if (value2 == INGAME_STATUS.ROUND_END || value2 == INGAME_STATUS.GAMEOVER)
			{
				return;
			}
			_autoRevive[ply] = util_timer.Simple(2f, delegate
			{
				if ((bool)ply)
				{
					ply.Revive();
				}
			});
			return;
		}
		IReadOnlyList<ulong> connectedClientsIds = NETController.Instance.ConnectedClientsIds;
		if ((bool)ply && connectedClientsIds.Count <= 1)
		{
			if (_soloRevives > 0)
			{
				_soloRevives = Mathf.Max(0, _soloRevives - 1);
				_soloRevive?.Stop();
				_soloRevive = util_timer.Simple(2f, delegate
				{
					NetController<NotificationController>.Instance?.BroadcastAllRPC(new NotificationData
					{
						id = "solo-player-revive",
						text = $"ingame.ui.notification.solo-revive||{_soloRevives}",
						duration = 8f,
						soundEffect = "Ingame/Notifications/gui_submit_7.ogg"
					});
					ply.Revive();
				});
				return;
			}
		}
		else
		{
			_soloRevives = 0;
		}
		CheckGameover(ply, server: true);
	}

	[Server]
	private void CheckGameover(entity_player ply, bool server)
	{
		if (server && (bool)ply && MonoController<PlayerController>.Instance.GetAlivePlayers((!ply) ? null : new entity_player[1] { ply }).Count == 0)
		{
			NetController<NotificationController>.Instance?.BroadcastAllRPC(new NotificationData
			{
				id = "all-players-dead",
				text = "ingame.ui.notification.all-players-dead",
				duration = 8f,
				soundEffect = "Ingame/Notifications/warning-1.ogg"
			});
			SetHealth(0);
			SetGameStatus(INGAME_STATUS.ROUND_END);
		}
	}

	protected override void __initializeVariables()
	{
		if (_status == null)
		{
			throw new Exception("IngameController._status cannot be null. All NetworkVariableBase instances must be initialized.");
		}
		_status.Initialize(this);
		__nameNetworkVariable(_status, "_status");
		NetworkVariableFields.Add(_status);
		if (_debtHealth == null)
		{
			throw new Exception("IngameController._debtHealth cannot be null. All NetworkVariableBase instances must be initialized.");
		}
		_debtHealth.Initialize(this);
		__nameNetworkVariable(_debtHealth, "_debtHealth");
		NetworkVariableFields.Add(_debtHealth);
		if (_currentRound == null)
		{
			throw new Exception("IngameController._currentRound cannot be null. All NetworkVariableBase instances must be initialized.");
		}
		_currentRound.Initialize(this);
		__nameNetworkVariable(_currentRound, "_currentRound");
		NetworkVariableFields.Add(_currentRound);
		if (_maxInventory == null)
		{
			throw new Exception("IngameController._maxInventory cannot be null. All NetworkVariableBase instances must be initialized.");
		}
		_maxInventory.Initialize(this);
		__nameNetworkVariable(_maxInventory, "_maxInventory");
		NetworkVariableFields.Add(_maxInventory);
		if (_worldTime == null)
		{
			throw new Exception("IngameController._worldTime cannot be null. All NetworkVariableBase instances must be initialized.");
		}
		_worldTime.Initialize(this);
		__nameNetworkVariable(_worldTime, "_worldTime");
		NetworkVariableFields.Add(_worldTime);
		if (_worldTimeWarning == null)
		{
			throw new Exception("IngameController._worldTimeWarning cannot be null. All NetworkVariableBase instances must be initialized.");
		}
		_worldTimeWarning.Initialize(this);
		__nameNetworkVariable(_worldTimeWarning, "_worldTimeWarning");
		NetworkVariableFields.Add(_worldTimeWarning);
		if (_connectedPlayers == null)
		{
			throw new Exception("IngameController._connectedPlayers cannot be null. All NetworkVariableBase instances must be initialized.");
		}
		_connectedPlayers.Initialize(this);
		__nameNetworkVariable(_connectedPlayers, "_connectedPlayers");
		NetworkVariableFields.Add(_connectedPlayers);
		if (_elevator == null)
		{
			throw new Exception("IngameController._elevator cannot be null. All NetworkVariableBase instances must be initialized.");
		}
		_elevator.Initialize(this);
		__nameNetworkVariable(_elevator, "_elevator");
		NetworkVariableFields.Add(_elevator);
		if (_exteriorRenderSettings == null)
		{
			throw new Exception("IngameController._exteriorRenderSettings cannot be null. All NetworkVariableBase instances must be initialized.");
		}
		_exteriorRenderSettings.Initialize(this);
		__nameNetworkVariable(_exteriorRenderSettings, "_exteriorRenderSettings");
		NetworkVariableFields.Add(_exteriorRenderSettings);
		if (_endGameRank == null)
		{
			throw new Exception("IngameController._endGameRank cannot be null. All NetworkVariableBase instances must be initialized.");
		}
		_endGameRank.Initialize(this);
		__nameNetworkVariable(_endGameRank, "_endGameRank");
		NetworkVariableFields.Add(_endGameRank);
		base.__initializeVariables();
	}

	protected override void __initializeRpcs()
	{
		base.__initializeRpcs();
	}

	protected internal override string __getTypeName()
	{
		return "IngameController";
	}
}
