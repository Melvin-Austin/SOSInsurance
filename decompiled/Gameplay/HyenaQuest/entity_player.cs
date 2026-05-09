using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using ECM2;
using FailCake;
using MetaVoiceChat;
using MetaVoiceChat.Input.Mic;
using MetaVoiceChat.Output;
using SaintsField;
using Steamworks;
using Unity.Collections;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;
using UnityEngine.InputSystem;
using ZLinq;

namespace HyenaQuest;

[RequireComponent(typeof(NetworkObject))]
public class entity_player : NetworkBehaviour
{
	private static readonly (ACCESSORY_TYPE type, int chance)[] AccessoryConfigs = new(ACCESSORY_TYPE, int)[7]
	{
		(ACCESSORY_TYPE.MASK, 3),
		(ACCESSORY_TYPE.HAT, 80),
		(ACCESSORY_TYPE.GOOGLES, 100),
		(ACCESSORY_TYPE.NECK, 30),
		(ACCESSORY_TYPE.CHEST, 50),
		(ACCESSORY_TYPE.PANTS, 50),
		(ACCESSORY_TYPE.TAIL, 15)
	};

	private static readonly Dictionary<PlayerSpecies, STEAM_ACHIEVEMENTS> SpeciesAchievements = new Dictionary<PlayerSpecies, STEAM_ACHIEVEMENTS> { 
	{
		PlayerSpecies.RAT,
		STEAM_ACHIEVEMENTS.ACHIEVEMENT_UNIVERSE_LOAD
	} };

	private static readonly int Grabbing = Animator.StringToHash("Grabbing");

	private static readonly int TauntID = Animator.StringToHash("TauntID");

	private static readonly int TauntOffset = Animator.StringToHash("TauntOffset");

	private static readonly int LookAtArm = Animator.StringToHash("LookAtArm");

	private static readonly int Wear = Shader.PropertyToID("_Wear");

	public static readonly byte MAX_HEALTH = 100;

	public InputActionReference grabAction;

	public InputActionReference woopAction;

	public InputActionReference questionAction;

	public InputActionReference danceAction;

	public InputActionReference jumpAction;

	public InputActionReference crouchAction;

	public InputActionReference moveAction;

	public InputActionReference useAction;

	public InputActionReference voiceAction;

	public Transform model;

	public Transform head;

	public Transform chest;

	public Transform hips;

	public Transform leftHand;

	public Transform rightHand;

	public Transform leftShoulder;

	public Transform tail;

	public Transform view;

	public Transform networkCamera;

	public Transform spectate;

	public Transform neck;

	public SaintsDictionary<PlayerSpecies, List<Material>> skinMaterials = new SaintsDictionary<PlayerSpecies, List<Material>>();

	public List<PLAYER_JUMPSUITS> jumpsuitMaterials = new List<PLAYER_JUMPSUITS>();

	public SaintsDictionary<PlayerSpecies, List<SkinnedMeshRenderer>> headRenderers = new SaintsDictionary<PlayerSpecies, List<SkinnedMeshRenderer>>();

	public SaintsDictionary<PlayerSpecies, List<SkinnedMeshRenderer>> tailRenderers = new SaintsDictionary<PlayerSpecies, List<SkinnedMeshRenderer>>();

	public List<SkinnedMeshRenderer> skinRenderers = new List<SkinnedMeshRenderer>();

	public List<SkinnedMeshRenderer> itemRenderers = new List<SkinnedMeshRenderer>();

	public float shoveCooldown = 1.5f;

	public float shoveDistance = 1.2f;

	public List<AudioClip> deathSnd = new List<AudioClip>();

	public List<AudioClip> reviveSnd = new List<AudioClip>();

	public List<AudioClip> shoveSnd = new List<AudioClip>();

	public GameEvent<int, bool> OnHealthUpdate = new GameEvent<int, bool>();

	public GameEvent<bool> OnHealthStatusUpdate = new GameEvent<bool>();

	public GameEvent<CURSE_TYPE, bool, bool> OnPlayerCurse = new GameEvent<CURSE_TYPE, bool, bool>();

	public GameEvent<PlayerAction, bool> OnPlayerAction = new GameEvent<PlayerAction, bool>();

	public GameEvent OnAccessoriesLoaded = new GameEvent();

	private entity_player_movement _movement;

	private entity_player_camera _camera;

	private entity_player_physgun _physgun;

	private entity_player_badge _badge;

	private entity_player_flashlight _flashlight;

	private entity_volume_affector _volume;

	private entity_player_inventory _inventory;

	private entity_player_tracker _tracker;

	private entity_player_vacuum _vacuum;

	private entity_footsteps _footsteps;

	private entity_ragdoll _ragdoll;

	private NetworkTransform _networkTransform;

	private NetworkAnimator _networkAnimator;

	private Animator _animator;

	private entity_animator_event _animatorEvent;

	private Texture2D _playerAvatar;

	private util_timer _settingsTimer;

	private int _suitSkin;

	private int _headSkin;

	private int _tailSkin;

	private bool _accessoriesReady;

	private readonly List<InstancedAccessory> _accessories = new List<InstancedAccessory>();

	private readonly List<SkinnedMeshRenderer> _selectedAccessories = new List<SkinnedMeshRenderer>();

	private Renderer[] _renderers;

	private PlayerAction _lastAction;

	private readonly float _maxInteractionDistance = 6f;

	private readonly float _interactionDistance = 2f;

	private readonly int _maxFallVelocity = 12;

	private ulong _connectionID = ulong.MaxValue;

	private readonly List<SkinnedMeshRenderer> _ownerOnlyAccessories = new List<SkinnedMeshRenderer>();

	private int _interactMask;

	private int _playerMask;

	private int _groundMask;

	private int _groundLayer;

	private int _playerLayer;

	private bool _holdingUse;

	private InteractionData _interaction;

	private RaycastHit? _aimingHit;

	private util_timer _tauntTimer;

	private PlayerTauntAnim _currentTaunt;

	private float _shoveCooldown;

	private entity_vehicle_seat _vehicle;

	private MetaVc _voiceChat;

	private VcMicAudioInput _voiceChatInput;

	private VcAudioOutput _voiceChatOutput;

	private VCAudioOutputAmplitude _voiceChatFilter;

	private AudioSource _voiceChatOutputSource;

	private AudioLowPassFilter _voiceLowPass;

	private float _micIntensity;

	private bool _pressedUse;

	private entity_usable _usingObject;

	private float _pressDebounce;

	private entity_boner _bones;

	private bool _lookingAtItemArm;

	private float _lookingAtItemArmSmooth;

	private Collider _collider;

	private Rigidbody _body;

	private util_timer _wallhackTimer;

	private util_timer _woopTimer;

	private readonly NetVar<byte> _plyID = new NetVar<byte>(byte.MaxValue);

	private readonly NetVar<ulong> _plySteamID = new NetVar<ulong>(ulong.MaxValue);

	private readonly NetVar<FixedString64Bytes> _playerName = new NetVar<FixedString64Bytes>("Player");

	private readonly NetVar<bool> _freeze = new NetVar<bool>(value: false);

	private readonly NetVar<bool> _flashActive = new NetVar<bool>(value: false);

	private readonly NetworkList<CurseNetwork> _playerCurses = new NetworkList<CurseNetwork>();

	private readonly List<Curse> _clientCurses = new List<Curse>();

	private readonly NetVar<NetworkBehaviourReference> _inVehicle = new NetVar<NetworkBehaviourReference>();

	private readonly NetVar<int> _statsDeaths = new NetVar<int>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

	private readonly NetVar<int> _statsScraps = new NetVar<int>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

	private readonly NetVar<int> _statsDeliveries = new NetVar<int>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

	private readonly NetVar<int> _statsBadges = new NetVar<int>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

	public static bool RENDER_PLAYERS = true;

	private bool _inOutfitMode;

	private bool _isOutfitFlipped;

	private bool _renderPlayerHead = true;

	private Dictionary<string, Transform> _boneMap = new Dictionary<string, Transform>();

	public const int ACCESSORY_SHIFT = 16;

	public const int ACCESSORY_BITS = 5;

	public const int ACCESSORY_MASK = 31;

	public const byte ACCESSORY_NONE = 31;

	public const int SUIT_SHIFT = 10;

	public const int SUIT_MASK = 63;

	public const ulong SKIRT_FLAG = 1uL;

	public const ulong BOBS_FLAG = 2uL;

	public const ulong MUSTACHE_FLAG = 2251799813685248uL;

	private readonly NetVar<ulong> _outfit = new NetVar<ulong>(0uL, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

	private readonly NetVar<HEALTH> _health = new NetVar<HEALTH>(new HEALTH
	{
		health = 100
	}, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

	public bool HasSkirt => (_outfit.Value & 1) != 0;

	public bool HasBobs => (_outfit.Value & 2) != 0;

	public bool HasMustache => (_outfit.Value & 0x8000000000000L) != 0;

	public byte Species => (byte)((_outfit.Value >> 2) & 0xFF);

	public bool HasCustomAccessories => _outfit.Value >> 16 != 0;

	public byte SuitSkin => (byte)((_outfit.Value >> 10) & 0x3F);

	public void Awake()
	{
		_interactMask = LayerMask.GetMask("entity_usable", "entity_enemy", "entity_phys", "entity_phys_item");
		_playerMask = LayerMask.GetMask("entity_player");
		_groundMask = LayerMask.GetMask("entity_ground");
		_playerLayer = LayerMask.NameToLayer("entity_player");
		_groundLayer = LayerMask.NameToLayer("entity_ground");
		if (!view)
		{
			throw new UnityException("Missing view transform");
		}
		if (!networkCamera)
		{
			throw new UnityException("Missing networkCamera transform");
		}
		_bones = GetComponentInChildren<entity_boner>(includeInactive: true);
		if (!_bones)
		{
			throw new UnityException("Invalid entity_player, missing entity_boner object");
		}
		Animator componentInChildren = GetComponentInChildren<Animator>(includeInactive: true);
		if (!componentInChildren)
		{
			throw new UnityException("Invalid entity_player, missing Animator object");
		}
		_networkAnimator = GetComponent<NetworkAnimator>();
		if (!_networkAnimator)
		{
			throw new UnityException("Invalid entity_player, missing NetworkAnimator object");
		}
		SetAnimator(componentInChildren);
		_collider = GetComponent<Collider>();
		if (!_collider)
		{
			throw new UnityException("Invalid entity_player, missing Collider object");
		}
		_body = GetComponent<Rigidbody>();
		if (!_body)
		{
			throw new UnityException("Invalid entity_player, missing Rigidbody object");
		}
		_inventory = GetComponent<entity_player_inventory>();
		if (!_inventory)
		{
			throw new UnityException("Invalid entity_player, missing entity_player_inventory object");
		}
		_physgun = GetComponent<entity_player_physgun>();
		if (!_physgun)
		{
			throw new UnityException("Invalid entity_player, missing entity_player_physgun object");
		}
		_movement = GetComponent<entity_player_movement>();
		if (!_movement)
		{
			throw new UnityException("Invalid entity_player, missing entity_player_movement object");
		}
		_movement.characterMovement.enabled = false;
		_tracker = GetComponentInChildren<entity_player_tracker>(includeInactive: true);
		if (!_tracker)
		{
			throw new UnityException("Invalid entity_player, missing entity_player_tracker object");
		}
		_vacuum = GetComponentInChildren<entity_player_vacuum>(includeInactive: true);
		if (!_vacuum)
		{
			throw new UnityException("Invalid entity_player, missing entity_player_scrapper object");
		}
		_voiceChat = GetComponentInChildren<MetaVc>(includeInactive: true);
		if (!_voiceChat)
		{
			throw new UnityException("Invalid entity_player, missing MetaVc object");
		}
		_voiceChatInput = GetComponentInChildren<VcMicAudioInput>(includeInactive: true);
		if (!_voiceChatInput)
		{
			throw new UnityException("Invalid entity_player, missing VcMicAudioInput object");
		}
		_voiceChatOutput = GetComponentInChildren<VcAudioOutput>(includeInactive: true);
		if (!_voiceChatOutput)
		{
			throw new UnityException("Invalid entity_player, missing VcAudioOutput object");
		}
		_voiceChatOutputSource = _voiceChatOutput.GetComponent<AudioSource>();
		if (!_voiceChatOutputSource)
		{
			throw new UnityException("Invalid entity_player, missing AudioSource object on VcAudioOutput");
		}
		_voiceChatFilter = GetComponentInChildren<VCAudioOutputAmplitude>(includeInactive: true);
		if (!_voiceChatFilter)
		{
			throw new UnityException("Invalid entity_player, missing entity_player_microphone object");
		}
		_networkTransform = GetComponent<NetworkTransform>();
		if (!_networkTransform)
		{
			throw new UnityException("Invalid entity_player, missing NetworkTransform object");
		}
		_volume = GetComponent<entity_volume_affector>();
		if (!_volume)
		{
			throw new UnityException("Invalid entity_player, missing entity_volume_affector object");
		}
		_badge = GetComponentInChildren<entity_player_badge>(includeInactive: true);
		if (!_badge)
		{
			throw new UnityException("Invalid entity_player, missing entity_player_badge object");
		}
		_badge.SetOwner(this);
		_flashlight = GetComponentInChildren<entity_player_flashlight>(includeInactive: true);
		if (!_flashlight)
		{
			throw new UnityException("Invalid entity_player, missing entity_player_flashlight object");
		}
		_animatorEvent = GetComponentInChildren<entity_animator_event>(includeInactive: true);
		if (!_animatorEvent)
		{
			throw new UnityException("Invalid entity_player, missing entity_animator_event object");
		}
		_camera = GetComponentInChildren<entity_player_camera>(includeInactive: true);
		if (!_camera)
		{
			throw new UnityException("Invalid entity_player, missing entity_player_camera object");
		}
		_camera.enabled = false;
		_footsteps = GetComponentInChildren<entity_footsteps>(includeInactive: true);
		if (!_footsteps)
		{
			throw new Exception("Invalid entity_player, missing entity_footsteps object");
		}
		_footsteps.gameObject.SetActive(value: false);
		CoreController.WaitFor<PlayerController>(delegate
		{
			SetupAccessoriesTask().Forget();
		});
		_renderers = GetComponentsInChildren<Renderer>(includeInactive: true);
	}

	protected override void OnNetworkPostSpawn()
	{
		base.OnNetworkPostSpawn();
		if (!base.IsClient)
		{
			return;
		}
		_statsDeaths.RegisterOnValueChanged(delegate(int _, int newValue)
		{
			if ((bool)_badge)
			{
				_badge.SetDeathStats(newValue);
			}
		});
		_statsDeliveries.RegisterOnValueChanged(delegate(int _, int newValue)
		{
			if ((bool)_badge)
			{
				_badge.SetDeliveryStats(newValue);
			}
		});
		_statsScraps.RegisterOnValueChanged(delegate(int _, int newValue)
		{
			if ((bool)_badge)
			{
				_badge.SetScrapStats(newValue);
			}
		});
		_statsBadges.RegisterOnValueChanged(delegate(int _, int newValue)
		{
			if ((bool)_badge)
			{
				_badge.SetBadges(newValue);
			}
		});
		_playerName.RegisterOnValueChanged(delegate(FixedString64Bytes _, FixedString64Bytes newValue)
		{
			base.name = newValue.ToString();
			if ((bool)_badge)
			{
				_badge.SetPlayerName(newValue.ToString(), IsDeveloperOrFriend());
			}
		});
		_plyID.RegisterOnValueChanged(delegate(byte _, byte newValue)
		{
			if ((bool)_badge)
			{
				_badge.SetPlayerID(newValue);
			}
		});
		_health.RegisterOnValueChanged(delegate(HEALTH oldValue, HEALTH newValue)
		{
			if (base.IsSpawned)
			{
				bool flag = oldValue.health <= 0;
				bool flag2 = newValue.health <= 0;
				if (!flag && flag2)
				{
					HealthStatusUpdate(dead: true);
					OnHealthStatusUpdate.Invoke(param1: true);
				}
				else if (flag && !flag2)
				{
					HealthStatusUpdate(dead: false);
					OnHealthStatusUpdate.Invoke(param1: false);
				}
				else
				{
					float num = oldValue.health - newValue.health;
					if (num > 0f)
					{
						MonoController<BloodController>.Instance.SpawnBlood(base.transform.position, new Vector2(num * 0.07f, num * 0.12f));
					}
				}
				if ((bool)_badge)
				{
					_badge.SetHealth(newValue.health);
				}
				OnHealthUpdate.Invoke(newValue.health, param2: false);
			}
		});
		_plySteamID.RegisterOnValueChanged(delegate(ulong oldVal, ulong newVal)
		{
			if (oldVal != newVal)
			{
				LoadPlayerAvatarTask().Forget();
			}
		});
		_outfit.RegisterOnValueChanged(delegate
		{
			UpdateOutfitTask().Forget();
		});
		if (base.IsOwner)
		{
			_freeze.RegisterOnValueChanged(delegate(bool _, bool newValue)
			{
				_movement.Pause(newValue);
			});
		}
		_flashActive.RegisterOnValueChanged(delegate(bool _, bool newValue)
		{
			if ((bool)_flashlight)
			{
				_flashlight.SetEnabled(newValue);
			}
		});
		_playerCurses.OnListChanged += OnPlayerCurseUpdate;
		_inVehicle.RegisterOnValueChanged(delegate(NetworkBehaviourReference _, NetworkBehaviourReference newValue)
		{
			_vehicle = NETController.Get<entity_vehicle_seat>(newValue);
			SetNetworkTransform(!_vehicle);
			if (base.IsOwner)
			{
				if ((bool)_camera)
				{
					_camera.SetVehicle(_vehicle);
				}
				if ((bool)_movement)
				{
					_movement.SetVehicle(_vehicle);
				}
			}
		});
	}

	public override void OnNetworkPreDespawn()
	{
		base.OnNetworkPreDespawn();
		if (base.IsClient)
		{
			_statsDeaths.OnValueChanged = null;
			_statsDeliveries.OnValueChanged = null;
			_statsScraps.OnValueChanged = null;
			_statsBadges.OnValueChanged = null;
			_playerName.OnValueChanged = null;
			_health.OnValueChanged = null;
			_plySteamID.OnValueChanged = null;
			_plyID.OnValueChanged = null;
			_freeze.OnValueChanged = null;
			_flashActive.OnValueChanged = null;
			_playerCurses.OnListChanged -= OnPlayerCurseUpdate;
			_inVehicle.OnValueChanged = null;
		}
	}

	public override void OnNetworkDespawn()
	{
		base.OnNetworkDespawn();
		_woopTimer?.Stop();
		_wallhackTimer?.Stop();
		_tauntTimer?.Stop();
		if (!base.IsClient)
		{
			return;
		}
		if ((bool)MonoController<PlayerController>.Instance)
		{
			MonoController<PlayerController>.Instance.OnPlayerRemove(this, server: false);
		}
		if (base.IsOwner)
		{
			grabAction.action.performed -= OnGrab;
			useAction.action.performed -= OnUseStart;
			useAction.action.canceled -= OnUseEnd;
			voiceAction.action.performed -= OnVoiceKeyStart;
			voiceAction.action.canceled -= OnVoiceKeyEnd;
			woopAction.action.performed -= OnWoopPerformed;
			questionAction.action.performed -= OnQuestionPerformed;
			danceAction.action.performed -= OnDancePerformed;
			jumpAction.action.performed -= OnJump;
			crouchAction.action.performed -= OnCrouch;
			moveAction.action.performed -= OnMove;
			if ((bool)_movement)
			{
				_movement.Landed -= OnPlayerLand;
			}
			if ((bool)NetController<StatsController>.Instance)
			{
				NetController<StatsController>.Instance.OnLocalPlayerStatUpdate -= new Action<STEAM_STATS, int>(OnLocalPlayerStatUpdate);
				NetController<StatsController>.Instance.OnAchievementsUpdate -= new Action(OnAchievementsUpdate);
			}
			if ((bool)MonoController<SettingsController>.Instance)
			{
				MonoController<SettingsController>.Instance.OnSettingsUpdated -= new Action(OnSettingsUpdated);
			}
			if ((bool)MonoController<UIController>.Instance)
			{
				MonoController<UIController>.Instance.OnOptionsToggle -= new Action<bool>(OnOptionsToggle);
			}
			MonoController<StartupController>.Instance?.ReleaseCursor("OUTFIT");
			RENDER_PLAYERS = true;
		}
		if ((bool)_volume)
		{
			_volume.OnVolumeUpdate -= new Action<VolumeType, VolumeImmersionType>(OnVolumeUpdate);
		}
		if ((bool)_animatorEvent)
		{
			_animatorEvent.OnAnimationEvent -= new Action<string>(OnAnimationEvent);
		}
	}

	public override void OnNetworkSpawn()
	{
		base.OnNetworkSpawn();
		if (!base.IsClient)
		{
			return;
		}
		if (!base.IsOwner)
		{
			CharacterMovement characterMovement = _movement.characterMovement;
			UnityEngine.Object.Destroy(_movement);
			UnityEngine.Object.Destroy(characterMovement);
			_movement = null;
			UnityEngine.Object.Destroy(_camera);
			_camera = null;
			UnityEngine.Object.Destroy(_footsteps.gameObject);
			_footsteps = null;
			UnityEngine.Object.Destroy(_tracker.gameObject);
			_tracker = null;
			if ((bool)_voiceChat?.audioOutput)
			{
				_voiceLowPass = _voiceChat.audioOutput.gameObject.AddComponent<AudioLowPassFilter>();
				_voiceLowPass.cutoffFrequency = 10000f;
			}
			if (!RENDER_PLAYERS)
			{
				SetRenderers(state: false);
			}
		}
		else
		{
			PlayerController.SetLocalPlayer(this);
			if (!_footsteps)
			{
				throw new UnityException("Invalid entity_player, missing entity_footsteps object");
			}
			_footsteps.gameObject.SetActive(value: true);
			if (!_camera)
			{
				throw new UnityException("Invalid entity_player, missing entity_player_camera object");
			}
			_camera.enabled = true;
			_camera.Setup();
			if (!_movement)
			{
				throw new UnityException("Invalid entity_player, missing entity_player_movement object");
			}
			_movement.enabled = true;
			_movement.SetOwner(this);
			_movement.characterMovement.enabled = true;
			_movement.camera = SDK.MainCamera;
			SetupControls();
			CoreController.WaitFor(delegate(StatsController statCtrl)
			{
				statCtrl.OnLocalPlayerStatUpdate += new Action<STEAM_STATS, int>(OnLocalPlayerStatUpdate);
				statCtrl.OnAchievementsUpdate += new Action(OnAchievementsUpdate);
				_statsDeaths.Value = statCtrl.GetLocalPlayerStat(STEAM_STATS.DEATHS);
				_statsScraps.Value = statCtrl.GetLocalPlayerStat(STEAM_STATS.SCRAPS);
				_statsDeliveries.Value = statCtrl.GetLocalPlayerStat(STEAM_STATS.DELIVERIES);
				OnAchievementsUpdate();
			});
			CoreController.WaitFor(delegate(UIController uiCtrl)
			{
				uiCtrl.OnOptionsToggle += new Action<bool>(OnOptionsToggle);
			});
			CoreController.WaitFor(delegate(SettingsController settingsCtrl)
			{
				settingsCtrl.OnSettingsUpdated += new Action(OnSettingsUpdated);
				_settingsTimer?.Stop();
				_settingsTimer = util_timer.Simple(1f, OnSettingsUpdated);
			});
			_movement.Landed += OnPlayerLand;
			RenderPlayerHead(render: false);
			Debug.Log("Local player initialized");
		}
		_volume.OnVolumeUpdate += new Action<VolumeType, VolumeImmersionType>(OnVolumeUpdate);
		_animatorEvent.OnAnimationEvent += new Action<string>(OnAnimationEvent);
		CoreController.WaitFor(delegate(PlayerController ctrl)
		{
			ctrl.OnPlayerCreate(this, server: false);
		});
	}

	public override void OnDestroy()
	{
		_settingsTimer?.Stop();
		_accessories.Clear();
		_selectedAccessories.Clear();
		_ownerOnlyAccessories.Clear();
		if ((bool)_playerAvatar)
		{
			UnityEngine.Object.Destroy(_playerAvatar);
		}
		base.OnDestroy();
	}

	public void Update()
	{
		if (base.IsOwner && (bool)MonoController<UIController>.Instance)
		{
			CheckUsable();
			MonoController<UIController>.Instance.SetInteraction(_interaction);
			CurseTick();
		}
	}

	public void LateUpdate()
	{
		HandleVacuumAnimation();
		if (base.IsOwner)
		{
			networkCamera.position = ((!IsDead()) ? new Vector3(0f, 3000f, 0f) : (SDK.MainCamera?.transform.position ?? Vector3.zero));
			HandleAnimation();
			return;
		}
		if ((bool)_vehicle)
		{
			base.transform.position = GetVehicleLocation();
		}
		if ((bool)_voiceLowPass)
		{
			_voiceLowPass.cutoffFrequency = (HasMask() ? 180 : 10000);
		}
		_bones.SetBone(HumanBodyBones.Head, Quaternion.Lerp(view.localRotation, Quaternion.identity, 0.7f));
		PlayerSpeaking();
	}

	public bool IsCub()
	{
		if (_statsScraps.Value > 0)
		{
			return _statsDeliveries.Value <= 0;
		}
		return true;
	}

	[Client]
	private void OnLocalPlayerStatUpdate(STEAM_STATS stat, int value)
	{
		if (!base.IsOwner)
		{
			throw new UnityException("Owner only method");
		}
		switch (stat)
		{
		case STEAM_STATS.DEATHS:
			_statsDeaths.Value = value;
			break;
		case STEAM_STATS.SCRAPS:
			_statsScraps.Value = value;
			break;
		case STEAM_STATS.DELIVERIES:
			_statsDeliveries.Value = value;
			break;
		}
	}

	[Client]
	private void OnAchievementsUpdate()
	{
		if (!base.IsOwner)
		{
			throw new UnityException("Owner only method");
		}
		int num = 0;
		int num2 = 0;
		foreach (STEAM_ACHIEVEMENTS value in Enum.GetValues(typeof(STEAM_ACHIEVEMENTS)))
		{
			if ((int)value < 100 && NetController<StatsController>.Instance.GetLocalPlayerAchievements(value))
			{
				num |= 1 << num2;
			}
			num2++;
		}
		_statsBadges.Value = num;
	}

	[Client]
	private void OnPlayerLand(Vector3 landVelocity)
	{
		if (!base.IsOwner || !_movement)
		{
			return;
		}
		IngameController instance = NetController<IngameController>.Instance;
		if ((object)instance != null && instance.Status() == INGAME_STATUS.PLAYING)
		{
			int num = (int)landVelocity.y;
			if (num <= -_maxFallVelocity)
			{
				byte amount = (byte)Mathf.Clamp((Math.Abs(num) - _maxFallVelocity) * 12, 0, MAX_HEALTH);
				TakeHealth(amount, DamageType.FALL);
			}
		}
	}

	public Animator GetAnimator()
	{
		return _animator;
	}

	public NetworkAnimator GetNetworkAnimator()
	{
		return _networkAnimator;
	}

	public void SetAnimator(Animator animator)
	{
		_animator = animator;
		if ((bool)_networkTransform)
		{
			_networkAnimator.Animator = animator;
		}
	}

	public Vector3 GetCameraPosition()
	{
		return networkCamera.position;
	}

	[Server]
	public void SetFlashlight(bool on)
	{
		if (!base.IsServer)
		{
			throw new UnityException("Not Server");
		}
		_flashActive.Value = on;
	}

	public bool HasCurse(CURSE_TYPE type)
	{
		foreach (CurseNetwork playerCurse in _playerCurses)
		{
			if (playerCurse.curseType == type)
			{
				return true;
			}
		}
		return false;
	}

	public int GetCurseCount(CURSE_TYPE type)
	{
		int num = 0;
		foreach (CurseNetwork playerCurse in _playerCurses)
		{
			if (playerCurse.curseType == type)
			{
				num++;
			}
		}
		return num;
	}

	[Server]
	public void AddCurse(Curse baseCurse)
	{
		if (!base.IsServer)
		{
			throw new UnityException("AddCurse can only be called on the server.");
		}
		_playerCurses.Add(new CurseNetwork
		{
			curseType = baseCurse.GetCurseType()
		});
		baseCurse.OnCurseStart(server: true);
		OnPlayerCurse.Invoke(baseCurse.GetCurseType(), param2: true, param3: true);
	}

	[Server]
	public void RemoveCurse(Curse curse)
	{
		if (!base.IsServer)
		{
			throw new UnityException("RemoveCurse can only be called on the server.");
		}
		for (int i = 0; i < _playerCurses.Count; i++)
		{
			if (_playerCurses[i].curseType == curse.GetCurseType())
			{
				_playerCurses.RemoveAt(i);
				OnPlayerCurse.Invoke(curse.GetCurseType(), param2: false, param3: true);
				return;
			}
		}
		throw new UnityException($"Curse {curse.GetCurseType()} not found in {GetPlayerName()}'s curse list");
	}

	[Server]
	public void ClearCurses()
	{
		if (!base.IsServer)
		{
			throw new UnityException("ClearCurses can only be called on the server.");
		}
		NetController<CurseController>.Instance.ClearCurses(this);
	}

	[Client]
	private void CurseTick()
	{
		if (!base.IsClient)
		{
			throw new UnityException("CurseTick can only be called on the client.");
		}
		if (base.IsOwner)
		{
			for (int num = _clientCurses.Count - 1; num >= 0; num--)
			{
				_clientCurses[num]?.OnTick(server: false);
			}
		}
	}

	[Client]
	private void OnPlayerCurseUpdate(NetworkListEvent<CurseNetwork> changeEvent)
	{
		if (!base.IsClient)
		{
			throw new UnityException("OnPlayerCurseUpdate can only be called on the client.");
		}
		switch (changeEvent.Type)
		{
		case NetworkListEvent<CurseNetwork>.EventType.Add:
		{
			Curse curse = CurseController.CreateCurseInstance(changeEvent.Value.curseType, this, false);
			if (curse == null)
			{
				throw new UnityException($"Failed to instance curse {changeEvent.Value.curseType}");
			}
			curse.OnCurseStart(server: false);
			_clientCurses.Add(curse);
			OnPlayerCurse.Invoke(changeEvent.Value.curseType, param2: true, param3: false);
			break;
		}
		case NetworkListEvent<CurseNetwork>.EventType.Remove:
		case NetworkListEvent<CurseNetwork>.EventType.RemoveAt:
		{
			for (int num = _clientCurses.Count - 1; num >= 0; num--)
			{
				if (_clientCurses[num].GetCurseType() == changeEvent.Value.curseType)
				{
					_clientCurses[num]?.OnCurseEnd(server: false);
					_clientCurses.RemoveAt(num);
					break;
				}
			}
			OnPlayerCurse.Invoke(changeEvent.Value.curseType, param2: false, param3: false);
			break;
		}
		}
	}

	[Shared]
	private void SetNetworkTransform(bool enable)
	{
		if (!_networkTransform)
		{
			throw new UnityException("Missing NetworkTransform on player");
		}
		if (_networkTransform.enabled != enable)
		{
			_networkTransform.enabled = enable;
			if (enable && base.IsOwner)
			{
				Physics.SyncTransforms();
				_networkTransform.SetState(base.transform.position, base.transform.rotation, base.transform.localScale, teleportDisabled: false);
			}
		}
	}

	[Server]
	public void SetVehicle(entity_vehicle_seat seat)
	{
		if (!base.IsServer)
		{
			throw new UnityException("SetVehicle can only be called on the server.");
		}
		_inVehicle.Value = seat;
		SetFreeze(seat);
	}

	public Vector3 GetVehicleLocation()
	{
		if (!_vehicle)
		{
			return Vector3.zero;
		}
		return _vehicle.GetSeatPos() + _vehicle.GetSeatOffsetPos();
	}

	public RaycastHit? GetAimingHit()
	{
		return _aimingHit;
	}

	public Vector3 GetHeadPosition()
	{
		return head.position;
	}

	[Client]
	public entity_player_camera GetCamera()
	{
		return _camera;
	}

	public entity_player_physgun GetPhysgun()
	{
		return _physgun;
	}

	[Client]
	public entity_player_movement GetMovement()
	{
		return _movement;
	}

	public entity_player_inventory GetInventory()
	{
		return _inventory;
	}

	public entity_player_tracker GetTracker()
	{
		return _tracker;
	}

	public entity_player_vacuum GetVacuum()
	{
		return _vacuum;
	}

	public MetaVc GetVoice()
	{
		return _voiceChat;
	}

	public VcMicAudioInput GetVoiceInput()
	{
		return _voiceChatInput;
	}

	public VcAudioOutput GetVoiceOutput()
	{
		return _voiceChatOutput;
	}

	public AudioSource GetVoiceOutputSource()
	{
		return _voiceChatOutputSource;
	}

	public entity_ragdoll GetRagdoll()
	{
		return _ragdoll;
	}

	public Transform GetHeadTransform()
	{
		return head;
	}

	public Transform GetChestTransform()
	{
		return chest;
	}

	public Transform GetRightHandTransform()
	{
		return rightHand;
	}

	public Transform GetLeftHandTransform()
	{
		return leftHand;
	}

	public Transform GetHipsTransform()
	{
		return hips;
	}

	private void OnVolumeUpdate(VolumeType type, VolumeImmersionType immersion)
	{
		if (!base.IsClient)
		{
			return;
		}
		if (base.IsOwner)
		{
			NetController<SoundController>.Instance.SetInsideVolume(type, immersion == VolumeImmersionType.FULL);
			MonoController<FOGController>.Instance.SetInsideVolume(type, immersion == VolumeImmersionType.FULL);
			return;
		}
		SkinnedMeshRenderer itemRenderer = GetItemRenderer(PlayerItemRenderer.MASK);
		if (!itemRenderer)
		{
			throw new UnityException("Invalid entity_player, missing mask renderer");
		}
		itemRenderer.gameObject.SetActive(ShouldRenderMask(type, immersion));
	}

	[Client]
	public entity_volume_affector GetVolumeAffector()
	{
		return _volume;
	}

	public bool HasMask()
	{
		return ShouldRenderMask(_volume.GetCurrentVolumeType(), _volume.GetCurrentImmersion());
	}

	public bool IsUnderWater()
	{
		if (_volume.GetCurrentVolumeType() == VolumeType.WATER)
		{
			return _volume.GetCurrentImmersion() == VolumeImmersionType.FULL;
		}
		return false;
	}

	private bool ShouldRenderMask(VolumeType type, VolumeImmersionType immersion)
	{
		if (type == VolumeType.WORLD_TOXIC || type == VolumeType.WATER || type == VolumeType.QUICKSAND)
		{
			return immersion == VolumeImmersionType.FULL;
		}
		return false;
	}

	private void OnSettingsUpdated()
	{
		if (!_voiceChat)
		{
			throw new UnityException("Missing Voice Chat");
		}
		if ((bool)MonoController<SettingsController>.Instance && base.IsOwner)
		{
			PlayerSettings currentSettings = MonoController<SettingsController>.Instance.CurrentSettings;
			_voiceChat.isInputMuted.Value = currentSettings.microphoneMode != VoiceChatMode.OPEN;
			_outfit.SetSpawnValue(currentSettings.accessories);
			string text = Microphone.devices?.AsValueEnumerable().ElementAtOrDefault(currentSettings.microphoneDevice);
			if (!string.IsNullOrEmpty(text))
			{
				_voiceChatInput.SetSelectedDevice(text);
			}
		}
	}

	private Material GetSkinMaterials(PlayerSkinType type, byte skin)
	{
		List<Material> list = skinMaterials[(PlayerSpecies)Species];
		if (list == null || list.Count <= 0)
		{
			throw new UnityException("Missing species body skins");
		}
		return type switch
		{
			PlayerSkinType.SKIN => list[skin % list.Count], 
			PlayerSkinType.JUMPSUIT => jumpsuitMaterials[skin % jumpsuitMaterials.Count].skin, 
			_ => throw new UnityException("Invalid skin type"), 
		};
	}

	public SkinnedMeshRenderer GetHeadRenderer()
	{
		List<SkinnedMeshRenderer> list = headRenderers[(PlayerSpecies)Species];
		if (list.Count == 0)
		{
			throw new UnityException("No head renderers for species");
		}
		return list[Math.Min(_headSkin, list.Count - 1)];
	}

	public SkinnedMeshRenderer GetTailRenderer()
	{
		List<SkinnedMeshRenderer> list = tailRenderers[(PlayerSpecies)Species];
		if (list.Count == 0)
		{
			throw new UnityException("No tail renderers for species");
		}
		return list[Math.Min(_tailSkin, list.Count - 1)];
	}

	public SkinnedMeshRenderer GetSkinRenderer(PlayerRenderer render)
	{
		if ((int)render < skinRenderers.Count)
		{
			return skinRenderers[(int)render];
		}
		throw new UnityException("Invalid index");
	}

	public SkinnedMeshRenderer GetItemRenderer(PlayerItemRenderer render)
	{
		if ((int)render < itemRenderers.Count)
		{
			return itemRenderers[(int)render];
		}
		throw new UnityException("Invalid index");
	}

	private async UniTaskVoid SetupAccessoriesTask()
	{
		if (!MonoController<PlayerController>.Instance)
		{
			throw new UnityException("Missing PlayerController instance");
		}
		SkinnedMeshRenderer skinRenderer = GetSkinRenderer(PlayerRenderer.BODY);
		if (!skinRenderer)
		{
			throw new UnityException("Missing body render");
		}
		_boneMap = skinRenderer.bones.AsValueEnumerable().ToDictionary((Transform t) => t.name, (Transform t) => t);
		Dictionary<string, Transform> boneMap = _boneMap;
		if (boneMap == null || boneMap.Count <= 0)
		{
			throw new UnityException("Missing body transform bones");
		}
		foreach (KeyValuePair<ACCESSORY_TYPE, List<PlayerAccessory>> kvp in MonoController<PlayerController>.Instance.accessories)
		{
			foreach (PlayerAccessory data in kvp.Value)
			{
				if (!data || !data.obj || !data.preview)
				{
					continue;
				}
				AsyncInstantiateOperation<GameObject> op = UnityEngine.Object.InstantiateAsync(data.obj, model);
				await op.ToUniTask();
				if (!this)
				{
					return;
				}
				GameObject obj = op.Result[0];
				if (!obj)
				{
					throw new UnityException("Missing accessory object: " + data.obj.name);
				}
				SkinnedMeshRenderer componentInChildren = obj.GetComponentInChildren<SkinnedMeshRenderer>(includeInactive: true);
				if (!componentInChildren)
				{
					throw new UnityException("Missing SkinnedMeshRenderer in accessory object: " + data.obj.name);
				}
				Transform[] bones = componentInChildren.bones;
				if (bones == null || bones.Length <= 0)
				{
					throw new UnityException("Missing bones on accessory");
				}
				Transform[] bones2 = componentInChildren.bones;
				for (int i = 0; i < bones2.Length; i++)
				{
					if (_boneMap.TryGetValue(bones2[i].name, out var value))
					{
						bones2[i] = value;
					}
				}
				componentInChildren.updateWhenOffscreen = true;
				componentInChildren.gameObject.layer = _playerLayer;
				componentInChildren.bones = bones2;
				componentInChildren.gameObject.SetActive(value: false);
				_accessories.Add(new InstancedAccessory
				{
					type = kvp.Key,
					renderer = componentInChildren,
					data = data
				});
			}
		}
		_renderers = GetComponentsInChildren<Renderer>(includeInactive: true);
		_accessoriesReady = true;
		OnAccessoriesLoaded?.Invoke();
	}

	public bool InOutfitMode()
	{
		return _inOutfitMode;
	}

	public void OutfitModeFlip(bool flip)
	{
		if (!base.IsOwner)
		{
			throw new UnityException("Owner only");
		}
		if (_inOutfitMode && _isOutfitFlipped != flip)
		{
			_isOutfitFlipped = flip;
			SetPosition(base.transform.position, base.transform.localRotation * Quaternion.Euler(0f, flip ? 180 : (-180), 0f), resetPlayer: false);
		}
	}

	public void SetInOutfitMode(bool set)
	{
		if (!base.IsOwner)
		{
			throw new UnityException("Owner only");
		}
		if (_inOutfitMode == set)
		{
			return;
		}
		if (!MonoController<PlayerController>.Instance)
		{
			throw new UnityException("Missing PlayerController instance");
		}
		if (!MonoController<UIController>.Instance)
		{
			throw new UnityException("Missing UIController instance");
		}
		if (!MonoController<StartupController>.Instance)
		{
			throw new UnityException("Missing StartupController instance");
		}
		if (!NetController<OutfitController>.Instance)
		{
			throw new UnityException("Missing OutfitController instance");
		}
		if (set)
		{
			INGAME_STATUS iNGAME_STATUS = NetController<IngameController>.Instance.Status();
			if (iNGAME_STATUS == INGAME_STATUS.ROUND_END || iNGAME_STATUS == INGAME_STATUS.GAMEOVER || iNGAME_STATUS == INGAME_STATUS.PLAYING)
			{
				return;
			}
		}
		_inOutfitMode = set;
		_isOutfitFlipped = false;
		entity_player_camera camera = GetCamera();
		if (!camera)
		{
			throw new UnityException("Missing player entity_player_camera");
		}
		entity_player_movement movement = GetMovement();
		if (!movement)
		{
			throw new UnityException("Missing player entity_player_movement");
		}
		CancelGrabbing();
		CancelTaunt();
		MonoController<UIController>.Instance.HideHUD(set);
		MonoController<UIController>.Instance.ShowOutfitMenu(set);
		movement.UnCrouch();
		movement.ResetMovementState();
		movement.Pause(set);
		HidePlayers(set);
		RenderPlayerHead(set);
		if (set)
		{
			camera.ForceLookAt(NetController<OutfitController>.Instance.cameraPos.position, NetController<OutfitController>.Instance.cameraPos.rotation);
			MonoController<StartupController>.Instance.RequestCursor("OUTFIT");
			SetPosition(NetController<OutfitController>.Instance.insidePos.position, NetController<OutfitController>.Instance.insidePos.rotation, resetPlayer: false);
		}
		else
		{
			MonoController<StartupController>.Instance.ReleaseCursor("OUTFIT");
			SetPosition(NetController<OutfitController>.Instance.outsidePos.position, NetController<OutfitController>.Instance.outsidePos.rotation);
		}
	}

	[Client]
	private void HidePlayers(bool hide)
	{
		if (!base.IsOwner)
		{
			throw new UnityException("Owner only");
		}
		RENDER_PLAYERS = !hide;
		foreach (entity_player allPlayer in MonoController<PlayerController>.Instance.GetAllPlayers())
		{
			if ((bool)allPlayer && !(allPlayer == this))
			{
				allPlayer.SetRenderers(!hide);
			}
		}
	}

	[Server]
	public void ToggleBobs()
	{
		if (!base.IsServer)
		{
			throw new UnityException("Server only");
		}
		SetOutfitFlagsRPC(_outfit.Value ^ 2);
	}

	[Server]
	public void ToggleSkirt()
	{
		if (!base.IsServer)
		{
			throw new UnityException("Server only");
		}
		SetOutfitFlagsRPC(_outfit.Value ^ 1);
	}

	[Server]
	public void ToggleMustache()
	{
		if (!base.IsServer)
		{
			throw new UnityException("Server only");
		}
		SetOutfitFlagsRPC(_outfit.Value ^ 0x8000000000000L);
	}

	[Client]
	public void SpeciesTF(PlayerSpecies species)
	{
		if (!base.IsOwner)
		{
			throw new UnityException("Owner only");
		}
		if ((bool)MonoController<SettingsController>.Instance && CanPlaySpecies(species))
		{
			ulong value = _outfit.Value;
			value = (value & 0xFFFFFFFFFFFFFC03uL) | ((ulong)species << 2);
			PlayerSettings currentSettings = MonoController<SettingsController>.Instance.CurrentSettings;
			currentSettings.accessories = value;
			MonoController<SettingsController>.Instance.CurrentSettings = currentSettings;
		}
	}

	public byte GetAccessory(ACCESSORY_TYPE type)
	{
		byte accessoryChoice = GetAccessoryChoice(type);
		if (accessoryChoice == 31)
		{
			return accessoryChoice;
		}
		return (byte)(accessoryChoice - 1);
	}

	public Dictionary<string, Transform> GetBoneMap()
	{
		return _boneMap;
	}

	[Client]
	public void SetSuitSkin(byte index)
	{
		if (!base.IsOwner)
		{
			throw new UnityException("Owner only");
		}
		int num = jumpsuitMaterials.Count - 1;
		if (num > 0)
		{
			if (index >= num)
			{
				index = 0;
			}
			ulong num2 = 18446744073709487103uL;
			_outfit.Value = (_outfit.Value & num2) | ((ulong)index << 10);
			SaveOutfit();
		}
	}

	[Client]
	public void SetAccessory(ACCESSORY_TYPE type, byte index)
	{
		if (!base.IsOwner)
		{
			throw new UnityException("Owner only");
		}
		if (type == ACCESSORY_TYPE.GOOGLES && index == 31)
		{
			return;
		}
		if (index != 31)
		{
			int accessoryCount = GetAccessoryCount(type);
			if (accessoryCount <= 0)
			{
				return;
			}
			if (index >= accessoryCount)
			{
				index = (byte)(accessoryCount - 1);
			}
			if (!CanWearAccessory(type, index))
			{
				return;
			}
			index++;
		}
		int num = 16 + (int)type * 5;
		ulong num2 = (ulong)(~(31L << num));
		_outfit.SetSpawnValue((_outfit.Value & num2) | ((ulong)index << num));
		SaveOutfit();
	}

	[Client]
	private void SaveOutfit()
	{
		if ((bool)MonoController<SettingsController>.Instance)
		{
			if (!base.IsOwner)
			{
				throw new UnityException("Owner only");
			}
			PlayerSettings currentSettings = MonoController<SettingsController>.Instance.CurrentSettings;
			currentSettings.accessories = _outfit.Value;
			MonoController<SettingsController>.Instance.CurrentSettings = currentSettings;
		}
	}

	private bool CanWearAccessory(ACCESSORY_TYPE type, int index)
	{
		int num = 0;
		foreach (InstancedAccessory accessory in _accessories)
		{
			if (accessory.type != type)
			{
				continue;
			}
			if (num == index)
			{
				if (accessory.data.achievement == STEAM_ACHIEVEMENTS.NONE)
				{
					return true;
				}
				return (bool)NetController<StatsController>.Instance && NetController<StatsController>.Instance.GetLocalPlayerAchievements(accessory.data.achievement);
			}
			num++;
		}
		return false;
	}

	public List<InstancedAccessory> GetAccessories()
	{
		return _accessories;
	}

	public bool CanPlaySpecies(PlayerSpecies species)
	{
		if (!SpeciesAchievements.TryGetValue(species, out var value))
		{
			return true;
		}
		if ((bool)NetController<StatsController>.Instance)
		{
			return NetController<StatsController>.Instance.GetLocalPlayerAchievements(value);
		}
		return false;
	}

	public int GetAccessoryCount(ACCESSORY_TYPE type)
	{
		int num = 0;
		foreach (InstancedAccessory accessory in _accessories)
		{
			if (accessory.type == type)
			{
				num++;
			}
		}
		return num;
	}

	public byte GetAccessoryChoice(ACCESSORY_TYPE type)
	{
		return (byte)((_outfit.Value >> 16 + (int)type * 5) & 0x1F);
	}

	private void WriteAccessorySlot(ref ulong outfit, ACCESSORY_TYPE type, byte value)
	{
		int num = 16 + (int)type * 5;
		outfit = (outfit & (ulong)(~(31L << num))) | ((ulong)value << num);
	}

	public List<AccessoryData> GetAllAccessories(ACCESSORY_TYPE type)
	{
		List<AccessoryData> list = new List<AccessoryData>();
		if (type != ACCESSORY_TYPE.GOOGLES)
		{
			list.Add(new AccessoryData
			{
				index = 31,
				type = type,
				locked = false,
				preview = null
			});
		}
		int num = 0;
		foreach (InstancedAccessory accessory in _accessories)
		{
			if (accessory.type == type)
			{
				list.Add(new AccessoryData
				{
					index = num,
					locked = !CanWearAccessory(type, num),
					type = type,
					preview = accessory.data.preview
				});
				num++;
			}
		}
		return list;
	}

	private InstancedAccessory? PickAccessoryInstance(ACCESSORY_TYPE type, int index)
	{
		int num = 0;
		foreach (InstancedAccessory accessory in _accessories)
		{
			if (accessory.type == type)
			{
				if (num == index)
				{
					return accessory;
				}
				num++;
			}
		}
		return null;
	}

	public PlayerSpecies GetSpecies()
	{
		return (PlayerSpecies)Species;
	}

	[Rpc(SendTo.Owner, InvokePermission = RpcInvokePermission.Server)]
	private void SetOutfitFlagsRPC(ulong outfit)
	{
		NetworkManager networkManager = base.NetworkManager;
		if ((object)networkManager == null || !networkManager.IsListening)
		{
			Debug.LogError("Rpc methods can only be invoked after starting the NetworkManager!");
			return;
		}
		if (__rpc_exec_stage != __RpcExecStage.Execute)
		{
			RpcAttribute.RpcAttributeParams attributeParams = new RpcAttribute.RpcAttributeParams
			{
				InvokePermission = RpcInvokePermission.Server
			};
			RpcParams rpcParams = default(RpcParams);
			FastBufferWriter bufferWriter = __beginSendRpc(482732586u, rpcParams, attributeParams, SendTo.Owner, RpcDelivery.Reliable);
			BytePacker.WriteValueBitPacked(bufferWriter, outfit);
			__endSendRpc(ref bufferWriter, 482732586u, rpcParams, attributeParams, SendTo.Owner, RpcDelivery.Reliable);
		}
		if (__rpc_exec_stage == __RpcExecStage.Execute)
		{
			__rpc_exec_stage = __RpcExecStage.Send;
			if ((bool)MonoController<SettingsController>.Instance)
			{
				PlayerSettings currentSettings = MonoController<SettingsController>.Instance.CurrentSettings;
				currentSettings.accessories = outfit;
				MonoController<SettingsController>.Instance.CurrentSettings = currentSettings;
			}
		}
	}

	[Client]
	public void SetRenderers(bool state)
	{
		if ((bool)model)
		{
			model.gameObject.SetActive(state);
		}
	}

	public bool IsDeveloperOrFriend()
	{
		string text = _plySteamID.Value.ToString();
		switch (text)
		{
		default:
			return text == "76561198018658865";
		case "76561198001836909":
		case "76561197990597851":
		case "76561198011124655":
			return true;
		}
	}

	private async UniTaskVoid UpdateOutfitTask()
	{
		await UniTask.Yield(PlayerLoopTiming.PostLateUpdate);
		if ((bool)this && base.IsSpawned)
		{
			await UniTask.WaitUntil(() => _accessoriesReady || !this || !base.IsSpawned);
			if ((bool)this && base.IsSpawned)
			{
				UpdateOutfit();
			}
		}
	}

	private bool ApplyAccessories(System.Random rng)
	{
		bool flag = !HasCustomAccessories;
		ulong outfit = _outfit.Value;
		bool result = true;
		bool flag2 = true;
		bool flag3 = true;
		_ownerOnlyAccessories.Clear();
		_selectedAccessories.Clear();
		foreach (InstancedAccessory accessory in _accessories)
		{
			if ((bool)accessory.renderer)
			{
				accessory.renderer.gameObject.SetActive(value: false);
			}
		}
		InstancedAccessory? instancedAccessory = null;
		InstancedAccessory? instancedAccessory2 = null;
		(ACCESSORY_TYPE, int)[] accessoryConfigs = AccessoryConfigs;
		for (int i = 0; i < accessoryConfigs.Length; i++)
		{
			(ACCESSORY_TYPE, int) tuple = accessoryConfigs[i];
			ACCESSORY_TYPE item = tuple.Item1;
			int item2 = tuple.Item2;
			int accessoryCount = GetAccessoryCount(item);
			if (accessoryCount <= 0)
			{
				continue;
			}
			int num;
			if (flag)
			{
				if (rng.Next(0, 100) >= item2)
				{
					WriteAccessorySlot(ref outfit, item, 31);
					continue;
				}
				num = rng.Next(0, accessoryCount);
				WriteAccessorySlot(ref outfit, item, (byte)(num + 1));
			}
			else
			{
				byte accessoryChoice = GetAccessoryChoice(item);
				if (accessoryChoice == 31)
				{
					continue;
				}
				num = Math.Min(accessoryChoice - 1, accessoryCount - 1);
			}
			InstancedAccessory? instancedAccessory3 = PickAccessoryInstance(item, num);
			if (instancedAccessory3.HasValue && (bool)instancedAccessory3.Value.renderer)
			{
				if (instancedAccessory3.Value.data.hideHair)
				{
					result = false;
				}
				if (instancedAccessory3.Value.data.hideGoogles)
				{
					flag2 = false;
				}
				if (instancedAccessory3.Value.data.hideHat)
				{
					flag3 = false;
				}
				switch (item)
				{
				case ACCESSORY_TYPE.GOOGLES:
					instancedAccessory = instancedAccessory3;
					break;
				case ACCESSORY_TYPE.HAT:
					instancedAccessory2 = instancedAccessory3;
					break;
				default:
					ActivateAccessory(instancedAccessory3.Value, item);
					break;
				}
			}
		}
		if (instancedAccessory2.HasValue && flag3)
		{
			ActivateAccessory(instancedAccessory2.Value, ACCESSORY_TYPE.HAT);
		}
		if (instancedAccessory.HasValue && flag2)
		{
			ActivateAccessory(instancedAccessory.Value, ACCESSORY_TYPE.GOOGLES);
		}
		if (flag && base.IsOwner)
		{
			ulong num2 = 18446744073709487103uL;
			outfit = (outfit & num2) | (ulong)((long)_suitSkin << 10);
			_outfit.SetSpawnValue(outfit);
		}
		return result;
	}

	private void SetupAccessoryBlendShapes()
	{
		foreach (SkinnedMeshRenderer selectedAccessory in _selectedAccessories)
		{
			if (!selectedAccessory)
			{
				continue;
			}
			foreach (SkinnedMeshRenderer skinRenderer in skinRenderers)
			{
				if ((bool)skinRenderer)
				{
					CopyBlendShapes(selectedAccessory, skinRenderer);
				}
			}
			CopyBlendShapes(selectedAccessory, GetHeadRenderer());
		}
	}

	private void ActivateAccessory(InstancedAccessory selected, ACCESSORY_TYPE type)
	{
		SkinnedMeshRenderer renderer = selected.renderer;
		if (base.IsOwner && (type == ACCESSORY_TYPE.GOOGLES || type == ACCESSORY_TYPE.HAT || type == ACCESSORY_TYPE.MASK))
		{
			_ownerOnlyAccessories.Add(renderer);
			renderer.enabled = _renderPlayerHead;
		}
		renderer.gameObject.SetActive(value: true);
		_selectedAccessories.Add(renderer);
		SetupAccessoryBlendShapes();
	}

	private void CopyBlendShapes(SkinnedMeshRenderer target, SkinnedMeshRenderer source)
	{
		if (!target || !source || !target.sharedMesh || !source.sharedMesh)
		{
			return;
		}
		Mesh sharedMesh = target.sharedMesh;
		Mesh sharedMesh2 = source.sharedMesh;
		for (int i = 0; i < sharedMesh.blendShapeCount; i++)
		{
			int blendShapeIndex = sharedMesh2.GetBlendShapeIndex(sharedMesh.GetBlendShapeName(i));
			if (blendShapeIndex >= 0)
			{
				target.SetBlendShapeWeight(i, source.GetBlendShapeWeight(blendShapeIndex));
			}
		}
	}

	public List<SkinnedMeshRenderer> GetActiveAccessories()
	{
		return _selectedAccessories;
	}

	private void UpdateOutfit()
	{
		System.Random steamGovernmentAssignedHyena = GetSteamGovernmentAssignedHyena();
		PlayerSpecies playerSpecies = (PlayerSpecies)Math.Min((int)Species, 1);
		_headSkin = ((steamGovernmentAssignedHyena.Next(0, 100) > 80) ? steamGovernmentAssignedHyena.Next(0, headRenderers[playerSpecies].Count) : 0);
		_tailSkin = steamGovernmentAssignedHyena.Next(0, tailRenderers[playerSpecies].Count);
		float num = 0.8f + (float)steamGovernmentAssignedHyena.Next(0, 5) * 0.1f;
		float num2 = 0.94f + (float)steamGovernmentAssignedHyena.Next(0, 5) * 0.01f;
		double num3 = steamGovernmentAssignedHyena.NextDouble();
		byte b = (byte)steamGovernmentAssignedHyena.Next(0, 3);
		byte b2 = (byte)steamGovernmentAssignedHyena.Next(0, 101);
		byte b3 = (byte)steamGovernmentAssignedHyena.Next(0, 3);
		byte b4 = (byte)steamGovernmentAssignedHyena.Next(0, 101);
		byte b5 = (byte)steamGovernmentAssignedHyena.Next(0, 5);
		byte b6 = (byte)steamGovernmentAssignedHyena.Next(30, 81);
		steamGovernmentAssignedHyena.Next(0, 101);
		steamGovernmentAssignedHyena.Next(0, 100);
		steamGovernmentAssignedHyena.Next(86, 101);
		byte value = (byte)(HasMustache ? 100 : 0);
		byte value2 = (byte)(HasMustache ? 100 : 0);
		List<Material> list = skinMaterials[playerSpecies];
		byte value3 = (byte)((list != null && list.Count > 0) ? ((byte)steamGovernmentAssignedHyena.Next(0, list.Count)) : 0);
		int num4 = jumpsuitMaterials.Count - 1;
		_suitSkin = ((num4 > 0) ? (HasCustomAccessories ? (SuitSkin % num4) : steamGovernmentAssignedHyena.Next(0, num4)) : 0);
		tail.localScale = Vector3.one * num;
		model.localScale = Vector3.one * num2;
		foreach (KeyValuePair<PlayerSpecies, List<SkinnedMeshRenderer>> headRenderer in headRenderers)
		{
			List<SkinnedMeshRenderer> value4 = headRenderer.Value;
			for (int i = 0; i < value4.Count; i++)
			{
				if ((bool)value4[i])
				{
					value4[i].gameObject.SetActive((_renderPlayerHead || !base.IsOwner) && playerSpecies == headRenderer.Key && i == _headSkin);
				}
			}
		}
		foreach (KeyValuePair<PlayerSpecies, List<SkinnedMeshRenderer>> tailRenderer in tailRenderers)
		{
			List<SkinnedMeshRenderer> value5 = tailRenderer.Value;
			for (int j = 0; j < value5.Count; j++)
			{
				if ((bool)value5[j])
				{
					value5[j].gameObject.SetActive(playerSpecies == tailRenderer.Key && j == _tailSkin);
				}
			}
		}
		SkinnedMeshRenderer skinRenderer = GetSkinRenderer(PlayerRenderer.BODY);
		if ((bool)skinRenderer)
		{
			skinRenderer.SetBlendShapeWeight(0, HasBobs ? 80 : 0);
			Material[] materials = skinRenderer.materials;
			materials[0].SetFloat(Wear, (float)num3);
			skinRenderer.materials = materials;
		}
		SkinnedMeshRenderer skinRenderer2 = GetSkinRenderer(PlayerRenderer.SKIRT);
		if ((bool)skinRenderer2)
		{
			skinRenderer2.gameObject.SetActive(HasSkirt);
		}
		bool flag = ApplyAccessories(steamGovernmentAssignedHyena);
		PlayerBlendShape playerBlendShape = PlayerBlendShape.FACE_A;
		while ((int)playerBlendShape <= 8)
		{
			SetPlayerShape(playerBlendShape, (byte)(((uint)playerBlendShape == (byte)(6 + b)) ? b2 : 0));
			playerBlendShape++;
		}
		PlayerBlendShape playerBlendShape2 = PlayerBlendShape.EARS_A;
		while ((int)playerBlendShape2 <= 4)
		{
			SetPlayerShape(playerBlendShape2, (byte)(((uint)playerBlendShape2 == (byte)(2 + b3)) ? b4 : 0));
			playerBlendShape2++;
		}
		if (!flag)
		{
			SetPlayerShape(PlayerBlendShape.MANE_A, 100);
			SetPlayerShape(PlayerBlendShape.MANE_B, 100);
			SetPlayerShape(PlayerBlendShape.MANE_C, 100);
			SetPlayerShape(PlayerBlendShape.MANE_D, 0);
			SetPlayerShape(PlayerBlendShape.MANE_E, 100);
		}
		else
		{
			PlayerBlendShape playerBlendShape3 = PlayerBlendShape.MANE_A;
			while ((int)playerBlendShape3 <= 14)
			{
				SetPlayerShape(playerBlendShape3, (byte)(((uint)playerBlendShape3 == (byte)(10 + b5)) ? b6 : 0));
				playerBlendShape3++;
			}
		}
		SetPlayerShape(PlayerBlendShape.SIDEBURNS, value);
		SetPlayerShape(PlayerBlendShape.MOUSTACHE, value2);
		if (list != null && list.Count > 0)
		{
			SetPlayerShape(PlayerBlendShape.SKIN, value3);
		}
		List<PLAYER_JUMPSUITS> list2 = jumpsuitMaterials;
		if (list2 != null && list2.Count > 0)
		{
			SetPlayerShape(PlayerBlendShape.SUIT, (byte)_suitSkin);
		}
		MonoController<RagdollController>.Instance?.UpdateRagdollSkin(GetPlayerID());
	}

	[Client]
	private void SetPlayerShape(PlayerBlendShape shape, byte value)
	{
		switch (shape)
		{
		case PlayerBlendShape.SUIT:
			SetSuit(value);
			return;
		case PlayerBlendShape.SKIN:
			SetSkin(value);
			return;
		}
		SkinnedMeshRenderer headRenderer = GetHeadRenderer();
		SkinnedMeshRenderer itemRenderer = GetItemRenderer(PlayerItemRenderer.MASK);
		if (!headRenderer || !itemRenderer)
		{
			throw new UnityException("Invalid entity_player, missing head or mask renderer");
		}
		headRenderer.SetBlendShapeWeight((int)shape, (int)value);
		if ((int)shape >= 6 && (int)shape <= 8)
		{
			int index = (int)(1 + (shape - 6));
			itemRenderer.SetBlendShapeWeight(index, (int)value);
		}
		SetupAccessoryBlendShapes();
	}

	public float GetCommsVoiceIntensity()
	{
		return _micIntensity;
	}

	private void OnVoiceKeyEnd(InputAction.CallbackContext obj)
	{
		if ((bool)MonoController<SettingsController>.Instance && MonoController<SettingsController>.Instance.CurrentSettings.microphoneMode == VoiceChatMode.PUSH_TO_TALK)
		{
			_voiceChat.isInputMuted.Value = true;
		}
	}

	private void OnVoiceKeyStart(InputAction.CallbackContext obj)
	{
		if ((bool)MonoController<SettingsController>.Instance && MonoController<SettingsController>.Instance.CurrentSettings.microphoneMode == VoiceChatMode.PUSH_TO_TALK)
		{
			_voiceChat.isInputMuted.Value = false;
		}
	}

	private void PlayerSpeaking()
	{
		if (!_voiceChat)
		{
			return;
		}
		SkinnedMeshRenderer headRenderer = GetHeadRenderer();
		if (!headRenderer)
		{
			throw new UnityException("Invalid entity_player, missing head renderer");
		}
		SkinnedMeshRenderer itemRenderer = GetItemRenderer(PlayerItemRenderer.MASK);
		if (!itemRenderer)
		{
			throw new UnityException("Invalid entity_player, missing mask renderer");
		}
		if ((bool)_voiceChat.isSpeaking)
		{
			_micIntensity = Mathf.Max(Mathf.Clamp(Mathf.Pow(_voiceChatFilter.amplitude, 0.175f), 0.25f, 1f), _micIntensity - Time.unscaledDeltaTime);
		}
		else if (_micIntensity > 0f)
		{
			_micIntensity -= Time.unscaledDeltaTime * 1.5f;
		}
		float value = Math.Clamp(_micIntensity * 150f, 0f, 100f);
		headRenderer.SetBlendShapeWeight(0, value);
		itemRenderer.SetBlendShapeWeight(0, value);
		foreach (SkinnedMeshRenderer selectedAccessory in _selectedAccessories)
		{
			if ((bool)selectedAccessory && selectedAccessory.gameObject.activeSelf && (bool)selectedAccessory.sharedMesh)
			{
				int blendShapeIndex = selectedAccessory.sharedMesh.GetBlendShapeIndex("TALK");
				if (blendShapeIndex >= 0)
				{
					selectedAccessory.SetBlendShapeWeight(blendShapeIndex, value);
				}
			}
		}
	}

	[Client]
	private void OnAnimationEvent(string eventName)
	{
		if (!string.IsNullOrEmpty(eventName) && string.Equals(eventName, "OnWoop") && (bool)NetController<IngameController>.Instance && NetController<IngameController>.Instance.Status() == INGAME_STATUS.PLAYING)
		{
			SetRenderersLayer("WallHack", render: true);
			_wallhackTimer?.Stop();
			_wallhackTimer = util_timer.Simple(2f, delegate
			{
				SetRenderersLayer("WallHack", render: false);
			});
		}
	}

	[Server]
	public void SetFreeze(bool freeze)
	{
		if (!base.IsServer)
		{
			throw new UnityException("Not server");
		}
		_freeze.SetSpawnValue(freeze);
	}

	public bool IsFrozen()
	{
		return _freeze.Value;
	}

	[Client]
	public void SetRenderersLayer(string layerID, bool render)
	{
		if (_renderers.Length == 0)
		{
			return;
		}
		uint mask = RenderingLayerMask.GetMask(layerID);
		Renderer[] renderers = _renderers;
		foreach (Renderer renderer in renderers)
		{
			if ((bool)renderer)
			{
				if (!render)
				{
					renderer.renderingLayerMask &= ~mask;
				}
				else
				{
					renderer.renderingLayerMask |= mask;
				}
			}
		}
	}

	[Server]
	public void SetPlayerID(byte id)
	{
		_plyID.SetSpawnValue(id);
	}

	public byte GetPlayerID()
	{
		return _plyID.Value;
	}

	[Server]
	public ulong GetConnectionID()
	{
		if (base.IsServer)
		{
			return _connectionID;
		}
		throw new UnityException("GetConnectionID can only be called on server");
	}

	[Server]
	public void SetConnectionID(ulong id)
	{
		_connectionID = id;
	}

	[Server]
	public void SetSteamID(ulong id)
	{
		_plySteamID.SetSpawnValue(id);
	}

	public ulong GetSteamID()
	{
		return _plySteamID.Value;
	}

	[Server]
	public void SetPlayerName(string plyName)
	{
		_playerName.SetSpawnValue(plyName);
	}

	public string GetPlayerName()
	{
		return _playerName.Value.ToString();
	}

	public System.Random GetSteamGovernmentAssignedHyena()
	{
		return new System.Random((int)((_plySteamID.Value + PlayerController.STEAM_ID_SKIN_VERSION) & 0x7FFFFFFF));
	}

	public Texture2D GetPlayerAvatar()
	{
		return _playerAvatar;
	}

	private async UniTaskVoid LoadPlayerAvatarTask()
	{
		await UniTask.DelayFrame(GetPlayerID() + 1);
		if (!this || !base.IsSpawned || !_badge || !SteamworksController.IsSteamRunning)
		{
			return;
		}
		CSteamID steamIDFriend = new CSteamID(_plySteamID.Value);
		if (!steamIDFriend.IsValid())
		{
			return;
		}
		int mediumFriendAvatar = SteamFriends.GetMediumFriendAvatar(steamIDFriend);
		if (mediumFriendAvatar <= 0 || !SteamUtils.GetImageSize(mediumFriendAvatar, out var width, out var height) || width == 0 || height == 0)
		{
			return;
		}
		byte[] imageData = new byte[width * height * 4];
		if (!SteamUtils.GetImageRGBA(mediumFriendAvatar, imageData, imageData.Length))
		{
			return;
		}
		await UniTask.Yield(PlayerLoopTiming.PreLateUpdate);
		if ((bool)this && base.IsSpawned)
		{
			if ((bool)_playerAvatar)
			{
				UnityEngine.Object.Destroy(_playerAvatar);
			}
			_playerAvatar = new Texture2D((int)width, (int)height, TextureFormat.RGBA32, mipChain: false);
			_playerAvatar.LoadRawTextureData(imageData);
			_playerAvatar.Apply(updateMipmaps: false, makeNoLongerReadable: true);
			_badge.SetPlayerIcon(_playerAvatar);
		}
	}

	public int GetPlayerDeaths()
	{
		return _statsDeaths.Value;
	}

	public int GetPlayerDeliveries()
	{
		return _statsDeliveries.Value;
	}

	public int GetPlayerScrap()
	{
		return _statsScraps.Value;
	}

	public int GetBadgeData()
	{
		return _statsBadges.Value;
	}

	[Client]
	public void RenderPlayerHead(bool render)
	{
		if (!base.IsClient)
		{
			throw new UnityException("RenderPlayerHead can only be called on the owner");
		}
		if (!render && !base.IsOwner)
		{
			return;
		}
		SkinnedMeshRenderer skinRenderer = GetSkinRenderer(PlayerRenderer.BODY);
		if (!skinRenderer)
		{
			return;
		}
		SkinnedMeshRenderer headRenderer = GetHeadRenderer();
		if (!headRenderer)
		{
			return;
		}
		headRenderer.gameObject.SetActive(render);
		skinRenderer.SetBlendShapeWeight(2, (!render) ? 100 : 0);
		foreach (SkinnedMeshRenderer ownerOnlyAccessory in _ownerOnlyAccessories)
		{
			if ((bool)ownerOnlyAccessory)
			{
				ownerOnlyAccessory.enabled = render;
			}
		}
		_renderPlayerHead = render;
	}

	[Rpc(SendTo.Owner)]
	public void TakeHealthRPC(byte amount, DamageType type = DamageType.GENERIC)
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
			FastBufferWriter bufferWriter = __beginSendRpc(2188505251u, rpcParams, attributeParams, SendTo.Owner, RpcDelivery.Reliable);
			bufferWriter.WriteValueSafe(in amount, default(FastBufferWriter.ForPrimitives));
			bufferWriter.WriteValueSafe(in type, default(FastBufferWriter.ForEnums));
			__endSendRpc(ref bufferWriter, 2188505251u, rpcParams, attributeParams, SendTo.Owner, RpcDelivery.Reliable);
		}
		if (__rpc_exec_stage == __RpcExecStage.Execute)
		{
			__rpc_exec_stage = __RpcExecStage.Send;
			if (!IsDead())
			{
				TakeHealth(amount, type);
			}
		}
	}

	[Client]
	public void TakeHealth(byte amount, DamageType type = DamageType.GENERIC)
	{
		if (!base.IsOwner)
		{
			throw new UnityException("Can only be called on owner! Use TakeHealthRPC instead");
		}
		bool num = amount == byte.MaxValue;
		if (!num)
		{
			entity_player_inventory inventory = _inventory;
			if ((object)inventory != null && inventory.HasItem("item_shield"))
			{
				amount = (byte)Mathf.Clamp(amount - Mathf.RoundToInt((float)(int)amount * 0.5f), 1, MAX_HEALTH);
			}
		}
		byte b = (byte)((!num) ? ((byte)Math.Clamp(_health.Value.health - amount, 0, MAX_HEALTH)) : 0);
		if (b != _health.Value.health)
		{
			MonoController<DamageController>.Instance?.Damage(type);
			SetHealth(b, type);
		}
	}

	[Rpc(SendTo.Owner)]
	public void AddHealthRPC(byte amount)
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
			FastBufferWriter bufferWriter = __beginSendRpc(2492987220u, rpcParams, attributeParams, SendTo.Owner, RpcDelivery.Reliable);
			bufferWriter.WriteValueSafe(in amount, default(FastBufferWriter.ForPrimitives));
			__endSendRpc(ref bufferWriter, 2492987220u, rpcParams, attributeParams, SendTo.Owner, RpcDelivery.Reliable);
		}
		if (__rpc_exec_stage == __RpcExecStage.Execute)
		{
			__rpc_exec_stage = __RpcExecStage.Send;
			if (amount != 0 && GetHealth() < MAX_HEALTH)
			{
				AddHealth(amount);
			}
		}
	}

	[Rpc(SendTo.Owner)]
	public void SetHealthRPC(byte amount)
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
			FastBufferWriter bufferWriter = __beginSendRpc(80031680u, rpcParams, attributeParams, SendTo.Owner, RpcDelivery.Reliable);
			bufferWriter.WriteValueSafe(in amount, default(FastBufferWriter.ForPrimitives));
			__endSendRpc(ref bufferWriter, 80031680u, rpcParams, attributeParams, SendTo.Owner, RpcDelivery.Reliable);
		}
		if (__rpc_exec_stage == __RpcExecStage.Execute)
		{
			__rpc_exec_stage = __RpcExecStage.Send;
			if (amount != _health.Value.health)
			{
				SetHealth(amount, DamageType.GENERIC);
			}
		}
	}

	[Client]
	public void AddHealth(byte amount)
	{
		if (!base.IsOwner)
		{
			throw new UnityException("Can only be called on owner! Use AddHealthRPC instead");
		}
		if (amount != 0)
		{
			byte newValue = (byte)Math.Clamp(_health.Value.health + amount, 0, MAX_HEALTH);
			SetHealth(newValue, DamageType.GENERIC);
		}
	}

	[Server]
	public bool Revive()
	{
		if (!base.IsServer)
		{
			throw new UnityException("Revive can only be called on server");
		}
		if (!base.IsSpawned || !IsDead())
		{
			return false;
		}
		SetHealthRPC(MAX_HEALTH);
		return true;
	}

	[Server]
	public bool Kill(DamageType type)
	{
		if (!base.IsServer)
		{
			throw new UnityException("Kill can only be called on server");
		}
		if (!base.IsSpawned || IsDead())
		{
			return false;
		}
		TakeHealthRPC(byte.MaxValue, type);
		return true;
	}

	[Client]
	public void SetHealth(byte newValue, DamageType type)
	{
		if (!base.IsSpawned)
		{
			return;
		}
		if (!MonoController<PlayerController>.Instance)
		{
			throw new UnityException("PlayerController instance not found");
		}
		if (!base.IsOwner)
		{
			throw new UnityException("Can only be called on owner! Use SetHealthRPC instead");
		}
		newValue = Math.Clamp(newValue, (byte)0, MAX_HEALTH);
		if (newValue != _health.Value.health)
		{
			if (newValue == 0 && CheckDSafeItem())
			{
				newValue = (byte)UnityEngine.Random.Range(2, 5);
				type = DamageType.CUT;
			}
			_health.SetSpawnValue(new HEALTH
			{
				health = newValue,
				damage = type,
				damageLocation = base.transform.position
			});
		}
	}

	private bool CheckDSafeItem()
	{
		IngameController instance = NetController<IngameController>.Instance;
		if ((object)instance == null || instance.Status() != INGAME_STATUS.PLAYING)
		{
			return false;
		}
		entity_player_inventory inventory = GetInventory();
		if (!inventory)
		{
			throw new UnityException("Failed to get owner inventory");
		}
		entity_item_pickable entity_item_pickable2 = inventory.FindItemByID("item_dsafe");
		if (!entity_item_pickable2 || !(entity_item_pickable2 is entity_item_dsafe entity_item_dsafe2))
		{
			return false;
		}
		Transform playerSpawn = MonoController<PlayerController>.Instance.GetPlayerSpawn(GetPlayerID());
		if (!playerSpawn)
		{
			throw new UnityException("Failed to get player spawn");
		}
		SetPosition(playerSpawn.transform.position, playerSpawn.transform.rotation);
		NetController<SoundController>.Instance?.PlaySound($"Ingame/Items/D-SAFE/death_{UnityEngine.Random.Range(0, 3)}.ogg", new AudioData
		{
			volume = 0.3f,
			pitch = UnityEngine.Random.Range(0.9f, 1.1f)
		});
		MonoController<UIController>.Instance?.SetFade(fadeIn: false, 1.8f);
		entity_item_dsafe2.OnUseItemRPC();
		return true;
	}

	public bool IsDead()
	{
		return GetHealth() <= 0;
	}

	public int GetHealth()
	{
		return _health.Value.health;
	}

	[Client]
	public void SetPosition(Vector3 pos, Quaternion angle = default(Quaternion), bool resetPlayer = true)
	{
		if (!base.IsOwner)
		{
			throw new UnityException("SetPosition can only be called on the owner, use SetPositionRPC instead");
		}
		angle = Quaternion.Normalize(angle);
		if ((bool)_networkTransform)
		{
			_networkTransform.SetState(pos, angle, base.transform.localScale, teleportDisabled: false);
		}
		_volume?.ResetVolume();
		_movement.ResetMovementState();
		_movement.SetVelocity(Vector3.zero);
		_movement.SetRotation(angle);
		_movement.SetPosition(pos, updateGround: true);
		if (resetPlayer)
		{
			ResetPlayer();
		}
		Physics.SyncTransforms();
	}

	[Rpc(SendTo.Owner, InvokePermission = RpcInvokePermission.Server)]
	public void SetPositionRPC(Vector3 pos, Quaternion angle)
	{
		NetworkManager networkManager = base.NetworkManager;
		if ((object)networkManager == null || !networkManager.IsListening)
		{
			Debug.LogError("Rpc methods can only be invoked after starting the NetworkManager!");
			return;
		}
		if (__rpc_exec_stage != __RpcExecStage.Execute)
		{
			RpcAttribute.RpcAttributeParams attributeParams = new RpcAttribute.RpcAttributeParams
			{
				InvokePermission = RpcInvokePermission.Server
			};
			RpcParams rpcParams = default(RpcParams);
			FastBufferWriter bufferWriter = __beginSendRpc(4271906622u, rpcParams, attributeParams, SendTo.Owner, RpcDelivery.Reliable);
			bufferWriter.WriteValueSafe(in pos);
			bufferWriter.WriteValueSafe(in angle);
			__endSendRpc(ref bufferWriter, 4271906622u, rpcParams, attributeParams, SendTo.Owner, RpcDelivery.Reliable);
		}
		if (__rpc_exec_stage == __RpcExecStage.Execute)
		{
			__rpc_exec_stage = __RpcExecStage.Send;
			if (base.IsOwner)
			{
				SetPosition(pos, angle);
			}
		}
	}

	[Rpc(SendTo.Owner, InvokePermission = RpcInvokePermission.Server)]
	public void SetPositionRPC(Vector3 pos)
	{
		NetworkManager networkManager = base.NetworkManager;
		if ((object)networkManager == null || !networkManager.IsListening)
		{
			Debug.LogError("Rpc methods can only be invoked after starting the NetworkManager!");
			return;
		}
		if (__rpc_exec_stage != __RpcExecStage.Execute)
		{
			RpcAttribute.RpcAttributeParams attributeParams = new RpcAttribute.RpcAttributeParams
			{
				InvokePermission = RpcInvokePermission.Server
			};
			RpcParams rpcParams = default(RpcParams);
			FastBufferWriter bufferWriter = __beginSendRpc(1668219908u, rpcParams, attributeParams, SendTo.Owner, RpcDelivery.Reliable);
			bufferWriter.WriteValueSafe(in pos);
			__endSendRpc(ref bufferWriter, 1668219908u, rpcParams, attributeParams, SendTo.Owner, RpcDelivery.Reliable);
		}
		if (__rpc_exec_stage == __RpcExecStage.Execute)
		{
			__rpc_exec_stage = __RpcExecStage.Send;
			if (base.IsOwner)
			{
				SetPosition(pos, base.transform.rotation);
			}
		}
	}

	[Client]
	public void SetLookingAtArmItem(bool look)
	{
		if (base.IsOwner)
		{
			_lookingAtItemArm = look;
		}
	}

	[Client]
	private void SetupControls()
	{
		if (base.IsOwner)
		{
			grabAction.action.performed += OnGrab;
			useAction.action.performed += OnUseStart;
			useAction.action.canceled += OnUseEnd;
			voiceAction.action.performed += OnVoiceKeyStart;
			voiceAction.action.canceled += OnVoiceKeyEnd;
			woopAction.action.performed += OnWoopPerformed;
			questionAction.action.performed += OnQuestionPerformed;
			danceAction.action.performed += OnDancePerformed;
			jumpAction.action.performed += OnJump;
			crouchAction.action.performed += OnCrouch;
			moveAction.action.performed += OnMove;
			_inventory.SetupControls();
		}
	}

	private void OnMove(InputAction.CallbackContext obj)
	{
		if (!IsDead())
		{
			PlayerAction playerAction = PlayerAction.NONE;
			Vector2 vector = obj.ReadValue<Vector2>();
			if (vector.y < 0f)
			{
				playerAction = PlayerAction.BACKWARD;
			}
			else if (vector.y > 0f)
			{
				playerAction = PlayerAction.FORWARD;
			}
			if (vector.x > 0f)
			{
				playerAction = PlayerAction.RIGHT;
			}
			else if (vector.x < 0f)
			{
				playerAction = PlayerAction.LEFT;
			}
			OnPlayerAction.Invoke(playerAction, param2: false);
			OnPlayerActionRPC(playerAction);
		}
	}

	private void OnCrouch(InputAction.CallbackContext obj)
	{
		if (!IsDead())
		{
			OnPlayerAction.Invoke(PlayerAction.CROUCH, param2: false);
			OnPlayerActionRPC(PlayerAction.CROUCH);
		}
	}

	private void OnJump(InputAction.CallbackContext obj)
	{
		if (!IsDead())
		{
			OnPlayerAction.Invoke(PlayerAction.JUMP, param2: false);
			OnPlayerActionRPC(PlayerAction.JUMP);
		}
	}

	[Rpc(SendTo.Server, Delivery = RpcDelivery.Unreliable)]
	private void OnPlayerActionRPC(PlayerAction action)
	{
		NetworkManager networkManager = base.NetworkManager;
		if ((object)networkManager == null || !networkManager.IsListening)
		{
			Debug.LogError("Rpc methods can only be invoked after starting the NetworkManager!");
			return;
		}
		if (__rpc_exec_stage != __RpcExecStage.Execute)
		{
			RpcAttribute.RpcAttributeParams attributeParams = new RpcAttribute.RpcAttributeParams
			{
				Delivery = RpcDelivery.Unreliable
			};
			RpcParams rpcParams = default(RpcParams);
			FastBufferWriter bufferWriter = __beginSendRpc(4089489482u, rpcParams, attributeParams, SendTo.Server, RpcDelivery.Unreliable);
			bufferWriter.WriteValueSafe(in action, default(FastBufferWriter.ForEnums));
			__endSendRpc(ref bufferWriter, 4089489482u, rpcParams, attributeParams, SendTo.Server, RpcDelivery.Unreliable);
		}
		if (__rpc_exec_stage == __RpcExecStage.Execute)
		{
			__rpc_exec_stage = __RpcExecStage.Send;
			if (!IsDead())
			{
				OnPlayerAction.Invoke(action, param2: true);
			}
		}
	}

	[Client]
	public bool IsPressingAnyKey()
	{
		if (!base.IsClient)
		{
			throw new UnityException("IsPressingAnyKey only available on the client");
		}
		if (IsDead())
		{
			return false;
		}
		if (!jumpAction.action.IsPressed() && !crouchAction.action.IsPressed())
		{
			return moveAction.action.IsPressed();
		}
		return true;
	}

	[Client]
	private void HandleAnimation()
	{
		if ((bool)_animator && !IsDead())
		{
			_animator.SetBool(Grabbing, (bool)_physgun.GetGrabbingObject() || _vacuum.IsVacuuming());
			_animator.SetBool(LookAtArm, _lookingAtItemArm);
			if (_tauntTimer == null && _movement.GetVelocity().magnitude > 0.05f)
			{
				CancelTaunt();
			}
		}
	}

	private void HandleVacuumAnimation()
	{
		if ((bool)_animator && (bool)leftShoulder && !IsDead())
		{
			float b = (_animator.GetBool(LookAtArm) ? 1f : 0f);
			_lookingAtItemArmSmooth = Mathf.Lerp(_lookingAtItemArmSmooth, b, Time.deltaTime * 10f);
			float num = view.transform.localEulerAngles.x;
			if (num > 180f)
			{
				num -= 360f;
			}
			float b2 = -110f + num * 0.8f;
			float a = Mathf.Lerp(-77.301f, b2, _lookingAtItemArmSmooth);
			leftShoulder.localRotation = Quaternion.Euler(Mathf.Max(a, -140f), 2.881f, 89.974f);
		}
	}

	[Client]
	public void ForcePrisonSkin()
	{
		if (!base.IsClient)
		{
			throw new UnityException("SetPrisonSkin only available on the client");
		}
		SetSuit((byte)(jumpsuitMaterials.Count - 1));
	}

	[Client]
	private void SetSkin(byte skinIndex)
	{
		if (!base.IsClient)
		{
			throw new UnityException("SetSkin only available on the client");
		}
		SkinnedMeshRenderer headRenderer = GetHeadRenderer();
		SkinnedMeshRenderer tailRenderer = GetTailRenderer();
		SkinnedMeshRenderer skinRenderer = GetSkinRenderer(PlayerRenderer.BODY);
		if (!skinRenderer || !headRenderer || !tailRenderer)
		{
			throw new UnityException("Invalid entity_player, missing body, head or tail renderer");
		}
		Material material = GetSkinMaterials(PlayerSkinType.SKIN, skinIndex);
		if (!material)
		{
			throw new UnityException($"Invalid entity_player skin {skinIndex}");
		}
		Material[] materials = skinRenderer.materials;
		materials[2] = material;
		skinRenderer.materials = materials;
		Material[] materials2 = headRenderer.materials;
		materials2[0] = material;
		headRenderer.materials = materials2;
		if (Species != 1)
		{
			Material[] materials3 = tailRenderer.materials;
			materials3[0] = material;
			tailRenderer.materials = materials3;
		}
	}

	[Client]
	private void SetSuit(byte suit)
	{
		if (!base.IsClient)
		{
			throw new UnityException("SetSuit only available on the client");
		}
		SkinnedMeshRenderer skinRenderer = GetSkinRenderer(PlayerRenderer.BODY);
		if ((bool)skinRenderer)
		{
			Material[] materials = skinRenderer.materials;
			materials[0] = GetSkinMaterials(PlayerSkinType.JUMPSUIT, suit);
			skinRenderer.materials = materials;
		}
	}

	private void ResetPlayer()
	{
		if (base.IsOwner)
		{
			_lookingAtItemArm = false;
			_lookingAtItemArmSmooth = 0f;
			_volume?.ResetVolume();
			_camera?.ResetCamera();
			_movement?.ResetMovementState();
			NetController<ShakeController>.Instance?.StopAllControllerVibration();
			MonoController<PostProcessController>.Instance?.ResetDepthOfField();
			MonoController<PostProcessController>.Instance?.ResetSaturation();
			MonoController<UIController>.Instance?.HideHUD(hidden: false);
			if (_inOutfitMode)
			{
				SetInOutfitMode(set: false);
			}
			ResetUse();
			CancelGrabbing();
		}
	}

	public Vector3 GetDeathLocation()
	{
		return _health.Value.damageLocation;
	}

	private void HealthStatusUpdate(bool dead)
	{
		if (!MonoController<RagdollController>.Instance)
		{
			throw new UnityException("Missing RagdollController instance");
		}
		if (!MonoController<BloodController>.Instance)
		{
			throw new UnityException("Missing BloodController instance");
		}
		if (!MonoController<SpectateController>.Instance)
		{
			throw new UnityException("Missing SpectateController instance");
		}
		if (dead)
		{
			if (base.IsServer)
			{
				SetVehicle(null);
				SetFreeze(freeze: true);
				ClearCurses();
				if (_health.Value.damage == DamageType.ELECTRIC_ASHES)
				{
					NetController<IngameController>.Instance.SpawnPlayerAshes(base.transform.position);
				}
				_inventory.DropAllItems();
				if (_health.Value.damage == DamageType.ABYSS)
				{
					NetController<StatsController>.Instance.UnlockAchievementSV(STEAM_ACHIEVEMENTS.ACHIEVEMENT_OUT_OF_BOUNDS, GetConnectionID());
				}
				SetPositionRPC(new Vector3(0f, 5000f, 0f), Quaternion.identity);
				MonoController<PlayerController>.Instance?.OnDeath(this, server: true);
			}
			bool num = _health.Value.damage != DamageType.PIT && _health.Value.damage != DamageType.INSTANT && _health.Value.damage != DamageType.ELECTRIC_ASHES;
			DamageType damage = _health.Value.damage;
			bool instant = damage == DamageType.INSTANT || damage == DamageType.PIT;
			if (num)
			{
				_ragdoll = MonoController<RagdollController>.Instance.SpawnRagdoll(this, _health.Value.damageLocation);
				if ((bool)_ragdoll)
				{
					MonoController<BloodController>.Instance.SpawnBlood(_ragdoll.transform.position, new Vector2(3f, 3f));
				}
			}
			if (base.IsOwner)
			{
				MonoController<UIController>.Instance?.SetFade(fadeIn: false, 1f);
				ResetPlayer();
				MonoController<DeathController>.Instance.Death(_health.Value.damage);
				MonoController<SpectateController>.Instance.StartSpectating(instant);
			}
			_voiceChatOutput.transform.SetParent(networkCamera, worldPositionStays: false);
			PlayDeathSound(_health.Value.damage);
			SetRenderers(state: false);
			MonoController<PlayerController>.Instance?.OnDeath(this, server: false);
			return;
		}
		MonoController<RagdollController>.Instance.RemoveRagdoll(GetPlayerID());
		_ragdoll = null;
		if (base.IsOwner)
		{
			if (!MonoController<PlayerController>.Instance)
			{
				throw new UnityException("PlayerController not found");
			}
			Transform playerSpawn = MonoController<PlayerController>.Instance.GetPlayerSpawn(GetPlayerID());
			MonoController<UIController>.Instance?.SetFade(fadeIn: false);
			SetPosition(playerSpawn.position, playerSpawn.rotation);
			SetRenderers(state: true);
			MonoController<SpectateController>.Instance.StopSpectating();
			if (reviveSnd.Count > 0)
			{
				AudioClip clip = reviveSnd[UnityEngine.Random.Range(0, reviveSnd.Count)];
				NetController<SoundController>.Instance.PlaySound(clip, new AudioData
				{
					pitch = UnityEngine.Random.Range(0.75f, 1f),
					volume = 0.6f
				});
			}
		}
		if (base.IsServer)
		{
			SetFreeze(freeze: false);
			MonoController<PlayerController>.Instance?.OnRevive(this, server: true);
		}
		if (base.IsOwner)
		{
			MonoController<PlayerController>.Instance?.OnRevive(this, server: false);
			return;
		}
		_voiceChatOutput.transform.SetParent(_voiceChat.transform, worldPositionStays: false);
		util_timer.Simple(0.15f, delegate
		{
			SetRenderers(state: true);
		});
		MonoController<PlayerController>.Instance?.OnRevive(this, server: false);
	}

	private void PlayDeathSound(DamageType type)
	{
		string text = null;
		switch (type)
		{
		case DamageType.ABYSS:
			text = $"Ingame/Monsters/Abyss/abyss_gore-{UnityEngine.Random.Range(0, 2)}.ogg";
			break;
		case DamageType.FALL:
			text = $"Ingame/Player/Death/FALL/fall_{UnityEngine.Random.Range(0, 2)}.ogg";
			break;
		case DamageType.PIT:
			text = ((UnityEngine.Random.Range(0, 100) <= 10) ? "Ingame/Player/Death/FALL/fall_2.ogg" : $"Ingame/Player/Death/FALL/fall_{UnityEngine.Random.Range(0, 2)}.ogg");
			break;
		case DamageType.NECK_SNAP:
			text = $"Ingame/Player/Death/NECK/neck_snap_{UnityEngine.Random.Range(0, 2)}.ogg";
			break;
		case DamageType.ELECTRIC_ASHES:
			text = $"Ingame/Player/Death/POOF/death_poof_{UnityEngine.Random.Range(0, 2)}.ogg";
			break;
		case DamageType.INSTANT:
			text = "SILENCE";
			break;
		}
		if (text == null)
		{
			AudioClip clip = deathSnd[UnityEngine.Random.Range(0, deathSnd.Count)];
			if (base.IsOwner)
			{
				NetController<SoundController>.Instance.PlaySound(clip, new AudioData
				{
					volume = 0.25f
				});
			}
			else
			{
				NetController<SoundController>.Instance.Play3DSound(clip, base.transform.position, new AudioData
				{
					distance = 5f
				});
			}
		}
		else if (!string.Equals(text, "SILENCE", StringComparison.InvariantCultureIgnoreCase))
		{
			if (base.IsOwner)
			{
				NetController<SoundController>.Instance.PlaySound(text, new AudioData
				{
					pitch = UnityEngine.Random.Range(0.8f, 1.2f),
					volume = 0.25f
				});
			}
			else
			{
				NetController<SoundController>.Instance.Play3DSound(text, base.transform.position, new AudioData
				{
					pitch = UnityEngine.Random.Range(0.8f, 1.2f),
					distance = 5f
				});
			}
		}
	}

	[Client]
	private void OnOptionsToggle(bool active)
	{
		if (!base.IsOwner)
		{
			throw new UnityException("Owner only");
		}
		if (IsDead())
		{
			return;
		}
		IngameController instance = NetController<IngameController>.Instance;
		if ((object)instance == null || instance.Status() != INGAME_STATUS.ROUND_END)
		{
			if (active)
			{
				PlayTaunt(PlayerTauntAnim.THONK, -1f);
			}
			else
			{
				CancelTaunt();
			}
		}
	}

	[Client]
	private void OnQuestionPerformed(InputAction.CallbackContext obj)
	{
		if (!base.IsOwner)
		{
			throw new UnityException("Owner only");
		}
		if (!IsDead() && PlayTaunt(PlayerTauntAnim.QUESTION, 1.2f))
		{
			PlayerSpecies species = GetSpecies();
			if (species == PlayerSpecies.YEEN || species != PlayerSpecies.RAT)
			{
				NetController<SoundController>.Instance.Play3DSound($"Ingame/Player/Taunts/question_{UnityEngine.Random.Range(0, 5)}.ogg", base.transform.position, new AudioData
				{
					volume = 0.8f,
					distance = 5f,
					pitch = UnityEngine.Random.Range(0.8f, 1.2f)
				}, broadcast: true);
			}
			else
			{
				NetController<SoundController>.Instance.Play3DSound($"Ingame/Player/Taunts/RAT/question_{UnityEngine.Random.Range(1, 4)}.ogg", base.transform.position, new AudioData
				{
					volume = 0.8f,
					distance = 5f,
					pitch = UnityEngine.Random.Range(0.8f, 1.2f)
				}, broadcast: true);
			}
		}
	}

	[Client]
	private void OnWoopPerformed(InputAction.CallbackContext obj)
	{
		if (!base.IsOwner)
		{
			throw new UnityException("Owner only");
		}
		if (IsDead())
		{
			return;
		}
		if (!_volume)
		{
			throw new UnityException("Invalid entity_player, missing entity_volume_affector object");
		}
		if (ShouldRenderMask(_volume.GetCurrentVolumeType(), _volume.GetCurrentImmersion()))
		{
			return;
		}
		_woopTimer?.Stop();
		_woopTimer = util_timer.Simple(0.5f, delegate
		{
			PlayerSpecies species = GetSpecies();
			if (species == PlayerSpecies.YEEN || species != PlayerSpecies.RAT)
			{
				NetController<SoundController>.Instance?.Play3DSound($"Ingame/Player/Taunts/woop_{UnityEngine.Random.Range(1, 3)}.ogg", base.transform.position, new AudioData
				{
					pitch = UnityEngine.Random.Range(0.8f, 1.2f),
					distance = 5f
				}, broadcast: true);
			}
			else
			{
				NetController<SoundController>.Instance?.Play3DSound($"Ingame/Player/Taunts/RAT/woop_{UnityEngine.Random.Range(1, 3)}.ogg", base.transform.position, new AudioData
				{
					pitch = UnityEngine.Random.Range(0.8f, 1.2f),
					distance = 5f
				}, broadcast: true);
			}
		});
		PlayTaunt(PlayerTauntAnim.WOOP);
	}

	[Client]
	private void OnDancePerformed(InputAction.CallbackContext obj)
	{
		if (!base.IsOwner)
		{
			throw new UnityException("Owner only");
		}
		if (!IsDead())
		{
			PlayTaunt(_movement.IsSwimming() ? PlayerTauntAnim.DANCE_UNDERWATER : PlayerTauntAnim.DANCE, -1f);
		}
	}

	[Client]
	public bool PlayTaunt(PlayerTauntAnim anim, float resetTime = 1f, float offset = 0f)
	{
		if (IsDead() || _currentTaunt != 0)
		{
			return false;
		}
		_tauntTimer?.Stop();
		if (resetTime > 0f)
		{
			_tauntTimer = util_timer.Simple(resetTime, CancelTaunt);
		}
		entity_animator_random_offset.playedState = 0;
		_currentTaunt = anim;
		_animator.SetInteger(TauntID, (int)anim);
		_animator.SetFloat(TauntOffset, offset);
		return true;
	}

	[Client]
	public void CancelTaunt()
	{
		if (_currentTaunt != 0)
		{
			_currentTaunt = PlayerTauntAnim.NONE;
			_animator.SetInteger(TauntID, 0);
			_tauntTimer?.Stop();
			_tauntTimer = null;
		}
	}

	[Client]
	public void CancelGrabbing()
	{
		if (base.IsOwner && base.IsSpawned)
		{
			if ((bool)_camera)
			{
				_camera.LockCamera(locked: false);
			}
			if ((bool)_physgun)
			{
				_physgun.SetGrabbingObject(null);
			}
		}
	}

	[Client]
	private void OnGrab(InputAction.CallbackContext obj)
	{
		if (!SDK.MainCamera || !base.IsOwner || IsDead() || Time.time < _pressDebounce)
		{
			return;
		}
		_pressDebounce = Time.time + 0.15f;
		if (!_physgun.OnUse(_aimingHit) && Physics.Raycast(SDK.MainCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f)), out var hitInfo, _maxInteractionDistance, _playerMask, QueryTriggerInteraction.Ignore) && !(hitInfo.distance > shoveDistance) && !(_shoveCooldown > Time.time) && hitInfo.collider.TryGetComponent<entity_player>(out var component) && (bool)component && !(component == this) && !component.IsDead())
		{
			_shoveCooldown = Time.time + shoveCooldown;
			PlayTaunt(PlayerTauntAnim.SHOVE, 0.5f);
			component.ShoveRPC(view.forward, 8f);
			if (shoveSnd.Count > 0)
			{
				NetController<SoundController>.Instance.PlaySound(shoveSnd[UnityEngine.Random.Range(0, shoveSnd.Count)], new AudioData
				{
					pitch = UnityEngine.Random.Range(0.8f, 1.2f),
					volume = 0.3f
				});
			}
		}
	}

	[Client]
	private void ResetUse()
	{
		if (base.IsOwner)
		{
			_aimingHit = null;
			if ((bool)_usingObject)
			{
				OnUse(_usingObject, down: false);
				_usingObject = null;
			}
		}
	}

	private void OnUseStart(InputAction.CallbackContext obj)
	{
		_holdingUse = true;
	}

	private void OnUseEnd(InputAction.CallbackContext obj)
	{
		_holdingUse = false;
	}

	[Client]
	private void CheckUsable()
	{
		if (!SDK.MainCamera || !base.IsOwner)
		{
			return;
		}
		_interaction = null;
		if (IsDead())
		{
			ResetUse();
			return;
		}
		if ((bool)_physgun.GetGrabbingObject())
		{
			ResetUse();
			return;
		}
		Ray ray = SDK.MainCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
		if (Physics.Raycast(ray, out var hitInfo, _maxInteractionDistance * 0.8f, _playerMask | _groundMask, QueryTriggerInteraction.Ignore) && hitInfo.collider.CompareTag("Player"))
		{
			_interaction = new InteractionData(Interaction.INTERACT_YEEN, new Bounds[1]
			{
				new Bounds(hitInfo.collider.transform.position + Vector3.up * 0.85f, new Vector3(0.5f, 1.5f, 0.5f))
			}, hitInfo.collider.name);
		}
		if (!Physics.Raycast(ray, out var hitInfo2, _maxInteractionDistance, _interactMask))
		{
			ResetUse();
			return;
		}
		if (Physics.Linecast(ray.origin, hitInfo2.point, out var _, _groundMask, QueryTriggerInteraction.Ignore))
		{
			ResetUse();
			return;
		}
		_aimingHit = hitInfo2;
		entity_usable component;
		entity_client_usable component2;
		entity_phys component3;
		if (!_aimingHit.HasValue || !hitInfo2.collider || hitInfo2.collider.gameObject.layer == _groundLayer)
		{
			ResetUse();
		}
		else if (hitInfo2.collider.TryGetComponent<entity_usable>(out component) && component.IsSpawned)
		{
			if (hitInfo2.distance <= component.clickDistance)
			{
				_interaction = component.InteractionSelector(hitInfo2.collider);
				if (_pressedUse != _holdingUse)
				{
					OnUse(component, _holdingUse);
					_pressedUse = _holdingUse;
					_usingObject = component;
				}
			}
			else
			{
				ResetUse();
			}
		}
		else if (hitInfo2.collider.TryGetComponentInChildren<entity_client_usable>(out component2))
		{
			if (hitInfo2.distance <= component2.clickDistance)
			{
				_interaction = component2.InteractionSelector(hitInfo2.collider);
				if (_pressedUse != _holdingUse)
				{
					if (_holdingUse)
					{
						component2.OnPlayerUse(this);
					}
					_pressedUse = _holdingUse;
					_usingObject = null;
				}
			}
			else
			{
				ResetUse();
			}
		}
		else if (hitInfo2.collider.TryGetComponent<entity_phys>(out component3, 2) && component3.IsSpawned)
		{
			if (hitInfo2.distance <= _interactionDistance)
			{
				_interaction = component3.InteractionSelector(hitInfo2.collider);
				if (_pressedUse == _holdingUse)
				{
					return;
				}
				if (_holdingUse)
				{
					if (!(component3 is entity_item_pickable entity_item_pickable2))
					{
						if (component3 is entity_phys_usable entity_phys_usable2)
						{
							entity_phys_usable2.OnUse(this);
						}
					}
					else if (!entity_item_pickable2.IsItemOwner())
					{
						byte b = (_inventory.IsInventorySlotEmpty() ? _inventory.GetInventorySlot() : _inventory.GetAvailableSlot());
						if (b != byte.MaxValue)
						{
							if (!_inventory.HasItem(entity_item_pickable2) && entity_item_pickable2.CanPickUp(_inventory))
							{
								_inventory.PickupItem(b, entity_item_pickable2);
							}
							else
							{
								NetController<NotificationController>.Instance.CreateNotification(new NotificationData
								{
									duration = 4f,
									id = "inventory_item_owned",
									text = "ingame.ui.notification.inventory.already-owned",
									soundEffect = "Ingame/Entities/Terminal/142608__autistic-lucario__error.ogg",
									soundVolume = 0.05f
								});
							}
						}
						else
						{
							NetController<NotificationController>.Instance.CreateNotification(new NotificationData
							{
								duration = 4f,
								id = "inventory_full",
								text = "ingame.ui.notification.inventory.full",
								soundEffect = "Ingame/Entities/Terminal/142608__autistic-lucario__error.ogg",
								soundVolume = 0.05f
							});
						}
					}
				}
				_pressedUse = _holdingUse;
			}
			else
			{
				ResetUse();
			}
		}
		else
		{
			ResetUse();
		}
	}

	[Client]
	private void OnUse(entity_usable ent, bool down)
	{
		if ((bool)ent && ent.IsSpawned && (down ? ent.OnUseDown(this, server: false) : ent.OnUseUP(this, server: false)))
		{
			OnUseRPC(ent, down);
		}
	}

	[Rpc(SendTo.Server)]
	private void OnUseRPC(NetworkBehaviourReference entObj, bool down)
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
			FastBufferWriter bufferWriter = __beginSendRpc(2378342402u, rpcParams, attributeParams, SendTo.Server, RpcDelivery.Reliable);
			bufferWriter.WriteValueSafe(in entObj, default(FastBufferWriter.ForNetworkSerializable));
			bufferWriter.WriteValueSafe(in down, default(FastBufferWriter.ForPrimitives));
			__endSendRpc(ref bufferWriter, 2378342402u, rpcParams, attributeParams, SendTo.Server, RpcDelivery.Reliable);
		}
		if (__rpc_exec_stage != __RpcExecStage.Execute)
		{
			return;
		}
		__rpc_exec_stage = __RpcExecStage.Send;
		entity_usable entity_usable2 = NETController.Get<entity_usable>(entObj);
		if ((bool)entity_usable2 && entity_usable2.IsSpawned)
		{
			if (down)
			{
				entity_usable2.OnUseDown(this, server: true);
			}
			else
			{
				entity_usable2.OnUseUP(this, server: true);
			}
		}
	}

	[Rpc(SendTo.Owner, InvokePermission = RpcInvokePermission.Everyone)]
	public void ShoveRPC(Vector3 dir, float force)
	{
		NetworkManager networkManager = base.NetworkManager;
		if ((object)networkManager == null || !networkManager.IsListening)
		{
			Debug.LogError("Rpc methods can only be invoked after starting the NetworkManager!");
			return;
		}
		if (__rpc_exec_stage != __RpcExecStage.Execute)
		{
			RpcAttribute.RpcAttributeParams attributeParams = new RpcAttribute.RpcAttributeParams
			{
				InvokePermission = RpcInvokePermission.Everyone
			};
			RpcParams rpcParams = default(RpcParams);
			FastBufferWriter bufferWriter = __beginSendRpc(3680886454u, rpcParams, attributeParams, SendTo.Owner, RpcDelivery.Reliable);
			bufferWriter.WriteValueSafe(in dir);
			bufferWriter.WriteValueSafe(in force, default(FastBufferWriter.ForPrimitives));
			__endSendRpc(ref bufferWriter, 3680886454u, rpcParams, attributeParams, SendTo.Owner, RpcDelivery.Reliable);
		}
		if (__rpc_exec_stage == __RpcExecStage.Execute)
		{
			__rpc_exec_stage = __RpcExecStage.Send;
			Shove(dir, force);
		}
	}

	public void Shove(Vector3 dir, float force)
	{
		if (base.IsClient && base.IsOwner)
		{
			if (!_movement)
			{
				throw new UnityException($"Invalid caller for shove -> {base.IsClient}");
			}
			_movement?.Shove(dir, force);
		}
	}

	protected override void __initializeVariables()
	{
		if (_plyID == null)
		{
			throw new Exception("entity_player._plyID cannot be null. All NetworkVariableBase instances must be initialized.");
		}
		_plyID.Initialize(this);
		__nameNetworkVariable(_plyID, "_plyID");
		NetworkVariableFields.Add(_plyID);
		if (_plySteamID == null)
		{
			throw new Exception("entity_player._plySteamID cannot be null. All NetworkVariableBase instances must be initialized.");
		}
		_plySteamID.Initialize(this);
		__nameNetworkVariable(_plySteamID, "_plySteamID");
		NetworkVariableFields.Add(_plySteamID);
		if (_playerName == null)
		{
			throw new Exception("entity_player._playerName cannot be null. All NetworkVariableBase instances must be initialized.");
		}
		_playerName.Initialize(this);
		__nameNetworkVariable(_playerName, "_playerName");
		NetworkVariableFields.Add(_playerName);
		if (_freeze == null)
		{
			throw new Exception("entity_player._freeze cannot be null. All NetworkVariableBase instances must be initialized.");
		}
		_freeze.Initialize(this);
		__nameNetworkVariable(_freeze, "_freeze");
		NetworkVariableFields.Add(_freeze);
		if (_flashActive == null)
		{
			throw new Exception("entity_player._flashActive cannot be null. All NetworkVariableBase instances must be initialized.");
		}
		_flashActive.Initialize(this);
		__nameNetworkVariable(_flashActive, "_flashActive");
		NetworkVariableFields.Add(_flashActive);
		if (_playerCurses == null)
		{
			throw new Exception("entity_player._playerCurses cannot be null. All NetworkVariableBase instances must be initialized.");
		}
		_playerCurses.Initialize(this);
		__nameNetworkVariable(_playerCurses, "_playerCurses");
		NetworkVariableFields.Add(_playerCurses);
		if (_inVehicle == null)
		{
			throw new Exception("entity_player._inVehicle cannot be null. All NetworkVariableBase instances must be initialized.");
		}
		_inVehicle.Initialize(this);
		__nameNetworkVariable(_inVehicle, "_inVehicle");
		NetworkVariableFields.Add(_inVehicle);
		if (_statsDeaths == null)
		{
			throw new Exception("entity_player._statsDeaths cannot be null. All NetworkVariableBase instances must be initialized.");
		}
		_statsDeaths.Initialize(this);
		__nameNetworkVariable(_statsDeaths, "_statsDeaths");
		NetworkVariableFields.Add(_statsDeaths);
		if (_statsScraps == null)
		{
			throw new Exception("entity_player._statsScraps cannot be null. All NetworkVariableBase instances must be initialized.");
		}
		_statsScraps.Initialize(this);
		__nameNetworkVariable(_statsScraps, "_statsScraps");
		NetworkVariableFields.Add(_statsScraps);
		if (_statsDeliveries == null)
		{
			throw new Exception("entity_player._statsDeliveries cannot be null. All NetworkVariableBase instances must be initialized.");
		}
		_statsDeliveries.Initialize(this);
		__nameNetworkVariable(_statsDeliveries, "_statsDeliveries");
		NetworkVariableFields.Add(_statsDeliveries);
		if (_statsBadges == null)
		{
			throw new Exception("entity_player._statsBadges cannot be null. All NetworkVariableBase instances must be initialized.");
		}
		_statsBadges.Initialize(this);
		__nameNetworkVariable(_statsBadges, "_statsBadges");
		NetworkVariableFields.Add(_statsBadges);
		if (_outfit == null)
		{
			throw new Exception("entity_player._outfit cannot be null. All NetworkVariableBase instances must be initialized.");
		}
		_outfit.Initialize(this);
		__nameNetworkVariable(_outfit, "_outfit");
		NetworkVariableFields.Add(_outfit);
		if (_health == null)
		{
			throw new Exception("entity_player._health cannot be null. All NetworkVariableBase instances must be initialized.");
		}
		_health.Initialize(this);
		__nameNetworkVariable(_health, "_health");
		NetworkVariableFields.Add(_health);
		base.__initializeVariables();
	}

	protected override void __initializeRpcs()
	{
		__registerRpc(482732586u, __rpc_handler_482732586, "SetOutfitFlagsRPC", RpcInvokePermission.Server);
		__registerRpc(2188505251u, __rpc_handler_2188505251, "TakeHealthRPC", RpcInvokePermission.Everyone);
		__registerRpc(2492987220u, __rpc_handler_2492987220, "AddHealthRPC", RpcInvokePermission.Everyone);
		__registerRpc(80031680u, __rpc_handler_80031680, "SetHealthRPC", RpcInvokePermission.Everyone);
		__registerRpc(4271906622u, __rpc_handler_4271906622, "SetPositionRPC", RpcInvokePermission.Server);
		__registerRpc(1668219908u, __rpc_handler_1668219908, "SetPositionRPC", RpcInvokePermission.Server);
		__registerRpc(4089489482u, __rpc_handler_4089489482, "OnPlayerActionRPC", RpcInvokePermission.Everyone);
		__registerRpc(2378342402u, __rpc_handler_2378342402, "OnUseRPC", RpcInvokePermission.Everyone);
		__registerRpc(3680886454u, __rpc_handler_3680886454, "ShoveRPC", RpcInvokePermission.Everyone);
		base.__initializeRpcs();
	}

	private static void __rpc_handler_482732586(NetworkBehaviour target, FastBufferReader reader, __RpcParams rpcParams)
	{
		NetworkManager networkManager = target.NetworkManager;
		if ((object)networkManager != null && networkManager.IsListening)
		{
			ByteUnpacker.ReadValueBitPacked(reader, out ulong value);
			target.__rpc_exec_stage = __RpcExecStage.Execute;
			((entity_player)target).SetOutfitFlagsRPC(value);
			target.__rpc_exec_stage = __RpcExecStage.Send;
		}
	}

	private static void __rpc_handler_2188505251(NetworkBehaviour target, FastBufferReader reader, __RpcParams rpcParams)
	{
		NetworkManager networkManager = target.NetworkManager;
		if ((object)networkManager != null && networkManager.IsListening)
		{
			reader.ReadValueSafe(out byte value, default(FastBufferWriter.ForPrimitives));
			reader.ReadValueSafe(out DamageType value2, default(FastBufferWriter.ForEnums));
			target.__rpc_exec_stage = __RpcExecStage.Execute;
			((entity_player)target).TakeHealthRPC(value, value2);
			target.__rpc_exec_stage = __RpcExecStage.Send;
		}
	}

	private static void __rpc_handler_2492987220(NetworkBehaviour target, FastBufferReader reader, __RpcParams rpcParams)
	{
		NetworkManager networkManager = target.NetworkManager;
		if ((object)networkManager != null && networkManager.IsListening)
		{
			reader.ReadValueSafe(out byte value, default(FastBufferWriter.ForPrimitives));
			target.__rpc_exec_stage = __RpcExecStage.Execute;
			((entity_player)target).AddHealthRPC(value);
			target.__rpc_exec_stage = __RpcExecStage.Send;
		}
	}

	private static void __rpc_handler_80031680(NetworkBehaviour target, FastBufferReader reader, __RpcParams rpcParams)
	{
		NetworkManager networkManager = target.NetworkManager;
		if ((object)networkManager != null && networkManager.IsListening)
		{
			reader.ReadValueSafe(out byte value, default(FastBufferWriter.ForPrimitives));
			target.__rpc_exec_stage = __RpcExecStage.Execute;
			((entity_player)target).SetHealthRPC(value);
			target.__rpc_exec_stage = __RpcExecStage.Send;
		}
	}

	private static void __rpc_handler_4271906622(NetworkBehaviour target, FastBufferReader reader, __RpcParams rpcParams)
	{
		NetworkManager networkManager = target.NetworkManager;
		if ((object)networkManager != null && networkManager.IsListening)
		{
			reader.ReadValueSafe(out Vector3 value);
			reader.ReadValueSafe(out Quaternion value2);
			target.__rpc_exec_stage = __RpcExecStage.Execute;
			((entity_player)target).SetPositionRPC(value, value2);
			target.__rpc_exec_stage = __RpcExecStage.Send;
		}
	}

	private static void __rpc_handler_1668219908(NetworkBehaviour target, FastBufferReader reader, __RpcParams rpcParams)
	{
		NetworkManager networkManager = target.NetworkManager;
		if ((object)networkManager != null && networkManager.IsListening)
		{
			reader.ReadValueSafe(out Vector3 value);
			target.__rpc_exec_stage = __RpcExecStage.Execute;
			((entity_player)target).SetPositionRPC(value);
			target.__rpc_exec_stage = __RpcExecStage.Send;
		}
	}

	private static void __rpc_handler_4089489482(NetworkBehaviour target, FastBufferReader reader, __RpcParams rpcParams)
	{
		NetworkManager networkManager = target.NetworkManager;
		if ((object)networkManager != null && networkManager.IsListening)
		{
			reader.ReadValueSafe(out PlayerAction value, default(FastBufferWriter.ForEnums));
			target.__rpc_exec_stage = __RpcExecStage.Execute;
			((entity_player)target).OnPlayerActionRPC(value);
			target.__rpc_exec_stage = __RpcExecStage.Send;
		}
	}

	private static void __rpc_handler_2378342402(NetworkBehaviour target, FastBufferReader reader, __RpcParams rpcParams)
	{
		NetworkManager networkManager = target.NetworkManager;
		if ((object)networkManager != null && networkManager.IsListening)
		{
			reader.ReadValueSafe(out NetworkBehaviourReference value, default(FastBufferWriter.ForNetworkSerializable));
			reader.ReadValueSafe(out bool value2, default(FastBufferWriter.ForPrimitives));
			target.__rpc_exec_stage = __RpcExecStage.Execute;
			((entity_player)target).OnUseRPC(value, value2);
			target.__rpc_exec_stage = __RpcExecStage.Send;
		}
	}

	private static void __rpc_handler_3680886454(NetworkBehaviour target, FastBufferReader reader, __RpcParams rpcParams)
	{
		NetworkManager networkManager = target.NetworkManager;
		if ((object)networkManager != null && networkManager.IsListening)
		{
			reader.ReadValueSafe(out Vector3 value);
			reader.ReadValueSafe(out float value2, default(FastBufferWriter.ForPrimitives));
			target.__rpc_exec_stage = __RpcExecStage.Execute;
			((entity_player)target).ShoveRPC(value, value2);
			target.__rpc_exec_stage = __RpcExecStage.Send;
		}
	}

	protected internal override string __getTypeName()
	{
		return "entity_player";
	}
}
