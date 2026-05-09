using System;
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
public class ScrapController : NetController<ScrapController>
{
	public static readonly float DEFAULT_TRANSFER_TIME = 3f;

	private static readonly int Status = Animator.StringToHash("STATUS");

	public entity_led activeLED;

	public entity_lever activateLever;

	public entity_split_flap_display scrapDisplay;

	public entity_prop_phys_item_place vacuumPlace;

	public NetworkAnimator pipeAnimator;

	public GameObject transferCanvas;

	public TextMeshPro transferText;

	public GameObject transferGauge;

	public GameObject idleCanvas;

	public TextMeshPro idleText;

	public Animator idleAnimator;

	public GameEvent<int, bool> OnWorldScrapUpdate = new GameEvent<int, bool>();

	public GameEvent<int, bool> OnShipScrapUpdate = new GameEvent<int, bool>();

	private entity_item_vacuum _currentVacuumItem;

	private util_timer _pipeTransferTimer;

	private util_fade_timer _clientCountTimer;

	private readonly NetVar<int> _claimedScrap = new NetVar<int>(0);

	private readonly NetVar<int> _worldScrap = new NetVar<int>(0);

	private readonly NetVar<int> _maxContainerScrap = new NetVar<int>(200);

	private readonly NetVar<bool> _vacuumUpgrade = new NetVar<bool>(value: false);

	private readonly NetVar<float> _transferTime = new NetVar<float>(DEFAULT_TRANSFER_TIME);

	private readonly NetVar<TRANSFER_STATUS> _transferStatus = new NetVar<TRANSFER_STATUS>(TRANSFER_STATUS.NONE);

	public new void Awake()
	{
		base.Awake();
		if (!activeLED)
		{
			throw new UnityException("activeLED is not set");
		}
		if (!activateLever)
		{
			throw new UnityException("activateLever is not set");
		}
		if (!scrapDisplay)
		{
			throw new UnityException("scrapDisplay is not set");
		}
		if (!vacuumPlace)
		{
			throw new UnityException("vacuumPlace is not set");
		}
		if (!transferText)
		{
			throw new UnityException("transferText is not set");
		}
		if (!transferCanvas)
		{
			throw new UnityException("transferCanvas is not set");
		}
		transferCanvas.SetActive(value: false);
		if (!idleCanvas)
		{
			throw new UnityException("idleCanvas is not set");
		}
		idleCanvas.SetActive(value: true);
		if (!pipeAnimator)
		{
			throw new UnityException("pipeAnimator is not set");
		}
		if (!transferGauge)
		{
			throw new UnityException("transferGauge is not set");
		}
	}

	public override void OnNetworkSpawn()
	{
		base.OnNetworkSpawn();
		if (base.IsServer)
		{
			CoreController.WaitFor(delegate(IngameController ingameCtrl)
			{
				ingameCtrl.OnStatusUpdated += new Action<INGAME_STATUS, bool>(OnIngameStatusUpdated);
			});
			CoreController.WaitFor(delegate(MapController mapCtrl)
			{
				mapCtrl.OnMapGenerated += new Action<bool>(OnMapGenerated);
			});
			activateLever.OnUSE += new Action<entity_player, bool>(OnScrapTransferLever);
		}
		vacuumPlace.OnItemUpdate += new Action<entity_item, bool, bool>(OnItemUpdate);
	}

	public override void OnNetworkDespawn()
	{
		base.OnNetworkDespawn();
		MonoController<LocalizationController>.Instance?.Cleanup("ScrapController.status.text");
		if (base.IsServer)
		{
			if ((bool)NetController<IngameController>.Instance)
			{
				NetController<IngameController>.Instance.OnStatusUpdated -= new Action<INGAME_STATUS, bool>(OnIngameStatusUpdated);
			}
			if ((bool)NetController<MapController>.Instance)
			{
				NetController<MapController>.Instance.OnMapGenerated -= new Action<bool>(OnMapGenerated);
			}
			if (!activateLever)
			{
				activateLever.OnUSE -= new Action<entity_player, bool>(OnScrapTransferLever);
			}
			_pipeTransferTimer?.Stop();
		}
		if ((bool)vacuumPlace)
		{
			vacuumPlace.OnItemUpdate -= new Action<entity_item, bool, bool>(OnItemUpdate);
		}
		_clientCountTimer?.Stop();
	}

	protected override void OnNetworkPostSpawn()
	{
		base.OnNetworkPostSpawn();
		if (!base.IsClient)
		{
			return;
		}
		_claimedScrap.RegisterOnValueChanged(delegate(int oldValue, int newValue)
		{
			if (oldValue != newValue)
			{
				UpdateShipScrapInfo();
			}
		});
		_transferStatus.RegisterOnValueChanged(delegate(TRANSFER_STATUS oldValue, TRANSFER_STATUS newValue)
		{
			if (oldValue != newValue)
			{
				OnTransferStatusUpdate(newValue);
			}
		});
		_worldScrap.RegisterOnValueChanged(delegate(int oldValue, int newValue)
		{
			if (oldValue != newValue)
			{
				OnWorldScrapUpdate?.Invoke(newValue, param2: false);
			}
		});
		UpdateShipScrapInfo();
	}

	public override void OnNetworkPreDespawn()
	{
		base.OnNetworkPreDespawn();
		if (base.IsClient)
		{
			_claimedScrap.OnValueChanged = null;
			_transferStatus.OnValueChanged = null;
			_worldScrap.OnValueChanged = null;
		}
	}

	[Server]
	public bool Pay(int scrap)
	{
		if (!base.IsServer)
		{
			throw new UnityException("Pay can only be called on the server.");
		}
		if (scrap <= 0)
		{
			return false;
		}
		if (_claimedScrap.Value - scrap < 0)
		{
			return false;
		}
		_claimedScrap.Value = Math.Max(0, _claimedScrap.Value - scrap);
		OnShipScrapUpdate?.Invoke(_claimedScrap.Value, param2: true);
		return true;
	}

	[Server]
	public void Add(int scrap)
	{
		if (!base.IsServer)
		{
			throw new UnityException("Add can only be called on the server.");
		}
		if (scrap > 0)
		{
			_claimedScrap.Value += scrap;
			OnShipScrapUpdate?.Invoke(_claimedScrap.Value, param2: true);
		}
	}

	[Server]
	public void RemoveWorldScrap(int scrap)
	{
		if (!base.IsServer)
		{
			throw new UnityException("Claim can only be called on the server.");
		}
		_worldScrap.Value = Math.Max(0, _worldScrap.Value - scrap);
	}

	[Server]
	public void SetMaxContainerScrap(int scrap)
	{
		if (base.IsSpawned && !base.IsServer)
		{
			throw new UnityException("SetMaxContainerScrap can only be called on the server.");
		}
		if (scrap > 0)
		{
			_maxContainerScrap.SetSpawnValue(scrap);
		}
	}

	public int GetMaxContainerScrap()
	{
		return _maxContainerScrap.Value;
	}

	[Server]
	public void SetVacuumUpgrade(bool set)
	{
		if (!base.IsServer)
		{
			throw new UnityException("Server only");
		}
		_vacuumUpgrade.SetSpawnValue(set);
	}

	public bool HasVacuumUpgrade()
	{
		return _vacuumUpgrade.Value;
	}

	[Server]
	private void OnScrapTransferLever(entity_player caller, bool start)
	{
		if (!base.IsServer)
		{
			throw new UnityException("OnScrapTransferLever can only be called on the server.");
		}
		if (!activateLever)
		{
			throw new UnityException("activateLever is not set");
		}
		if ((bool)caller && !caller.IsDead() && start)
		{
			if (!_currentVacuumItem)
			{
				Debug.LogWarning("No vacuum item found to transfer scrap.");
				activateLever.SetLocked(newVal: true);
			}
			else if (!_currentVacuumItem.IsSoftLocked())
			{
				Debug.LogWarning("Unordered package detected. Scrap should be soft locked on transfer spot");
			}
			else
			{
				_currentVacuumItem.SetLocked(LOCK_TYPE.LOCKED);
				_transferStatus.Value = TRANSFER_STATUS.COUNTING;
			}
		}
	}

	[Server]
	private void OnServerFinishedCounting()
	{
		if (!base.IsServer)
		{
			throw new UnityException("OnScrapTransferLever can only be called on the server.");
		}
		_pipeTransferTimer?.Stop();
		_pipeTransferTimer = util_timer.Simple(0.5f, delegate
		{
			pipeAnimator.SetTrigger("TRANSFER");
			_transferStatus.Value = TRANSFER_STATUS.TRANSFERRING;
			NetController<SoundController>.Instance.Play3DSound("Ingame/Player/Scrap/tube_suck.ogg", base.transform.position, new AudioData
			{
				volume = 0.3f
			}, broadcast: true);
		});
	}

	[Server]
	private void OnServerFinishedTransfer()
	{
		if (!base.IsServer)
		{
			throw new UnityException("OnServerFinishedTransfer can only be called on the server.");
		}
		if ((bool)_currentVacuumItem)
		{
			Add(_currentVacuumItem.GetTotalScrap());
			_pipeTransferTimer = util_timer.Simple(2f, delegate
			{
				ResetStatus();
				vacuumPlace.Eject(base.transform.right * UnityEngine.Random.Range(75, 90));
			});
		}
	}

	[Server]
	private void ResetStatus()
	{
		if (!base.IsServer)
		{
			throw new UnityException("ResetStatus can only be called on the server.");
		}
		_transferStatus.Value = TRANSFER_STATUS.NONE;
		activateLever.SetActive(active: false);
		_currentVacuumItem?.Clear();
		_currentVacuumItem = null;
	}

	[Server]
	private void UpdateLeverStatus()
	{
		if (!activateLever)
		{
			throw new UnityException("activateLever is not set");
		}
		bool num = _transferStatus.Value != TRANSFER_STATUS.NONE;
		int num2 = _currentVacuumItem?.GetTotalScrap() ?? 0;
		IngameController instance = NetController<IngameController>.Instance;
		bool flag = (object)instance == null || instance.Status() != INGAME_STATUS.PLAYING;
		bool locked = num || num2 <= 0 || flag;
		activateLever.SetLocked(locked);
	}

	private void OnItemUpdate(entity_item item, bool set, bool server)
	{
		if (set)
		{
			if (!(item is entity_item_vacuum currentVacuumItem))
			{
				return;
			}
			_currentVacuumItem = currentVacuumItem;
		}
		else
		{
			_currentVacuumItem = null;
		}
		if (server)
		{
			UpdateLeverStatus();
		}
		UpdateInfo();
	}

	[Client]
	private void UpdateInfo(int scrap = 0)
	{
		if (!MonoController<LocalizationController>.Instance)
		{
			throw new UnityException("Missing LocalizationController");
		}
		if (!idleText)
		{
			throw new UnityException("statusText is not set");
		}
		if (!transferText)
		{
			throw new UnityException("infoText is not set");
		}
		if (!idleAnimator)
		{
			throw new UnityException("statusAnimator is not set");
		}
		if (!_currentVacuumItem)
		{
			transferGauge.transform.eulerAngles = new Vector3(0f, 0f, 0f);
			idleAnimator.SetInteger(Status, 0);
			MonoController<LocalizationController>.Instance.Get("ScrapController.status.text", "ingame.world.scrapper.status.idle", delegate(string v)
			{
				if ((bool)idleText)
				{
					idleText.text = v;
				}
			});
			return;
		}
		int scrapPercentage = _currentVacuumItem.GetScrapPercentage();
		transferGauge.transform.eulerAngles = new Vector3(0f, 0f, Mathf.Clamp((float)scrapPercentage * 2.65f, 0f, 265f));
		if (scrapPercentage <= 0)
		{
			idleAnimator.SetInteger(Status, 1);
			MonoController<LocalizationController>.Instance.Get("ScrapController.status.text", "ingame.world.scrapper.status.empty", delegate(string v)
			{
				if ((bool)idleText)
				{
					idleText.text = v;
				}
			});
		}
		else if (_transferStatus.Value == TRANSFER_STATUS.NONE)
		{
			idleAnimator.SetInteger(Status, 2);
			MonoController<LocalizationController>.Instance.Get("ScrapController.status.text", "ingame.world.scrapper.status.ready", delegate(string v)
			{
				if ((bool)idleText)
				{
					idleText.text = v;
				}
			});
		}
		else
		{
			transferText.text = $"<indent=5%><size={200f + (float)scrap / (float)GetMaxContainerScrap() * 200f:F0}%><align=center>+{Mathf.FloorToInt(scrap)}";
		}
	}

	[Client]
	private void OnTransferStatusUpdate(TRANSFER_STATUS status)
	{
		if (!base.IsClient)
		{
			throw new UnityException("OnTransferStatusUpdate can only be called on the client.");
		}
		activeLED.SetActive(status != TRANSFER_STATUS.NONE);
		transferCanvas.SetActive(status != TRANSFER_STATUS.NONE);
		idleCanvas.SetActive(status == TRANSFER_STATUS.NONE);
		_clientCountTimer?.Stop();
		UpdateInfo();
		if ((bool)_currentVacuumItem)
		{
			switch (status)
			{
			case TRANSFER_STATUS.COUNTING:
				OnCountingStatus();
				break;
			case TRANSFER_STATUS.TRANSFERRING:
				OnTransferringStatus();
				break;
			}
		}
	}

	[Client]
	private void OnTransferringStatus()
	{
		if (!base.IsClient)
		{
			throw new UnityException("OnTransferringStatus can only be called on the client.");
		}
		int totalScrap = _currentVacuumItem.GetTotalScrap();
		_clientCountTimer?.Stop();
		_clientCountTimer = util_fade_timer.Fade(0.65f, totalScrap, 0f, delegate(float f)
		{
			UpdateInfo(Mathf.FloorToInt(f));
		}, delegate
		{
			if (base.IsServer)
			{
				OnServerFinishedTransfer();
			}
		});
	}

	[Server]
	public void SetTransferTime(float time)
	{
		if (!base.IsServer)
		{
			throw new UnityException("Server only");
		}
		_transferTime.SetSpawnValue(time);
	}

	[Client]
	private void OnCountingStatus()
	{
		if (!base.IsClient)
		{
			throw new UnityException("OnCountingStatus can only be called on the client.");
		}
		int totalScrap = _currentVacuumItem.GetTotalScrap();
		_clientCountTimer?.Stop();
		_clientCountTimer = util_fade_timer.Fade(1f / _transferTime.Value, 0f, totalScrap, delegate(float f)
		{
			UpdateInfo(Mathf.FloorToInt(f));
		}, delegate
		{
			if (base.IsServer)
			{
				OnServerFinishedCounting();
			}
		});
	}

	[Server]
	private void SetScrap(int scrap)
	{
		if (!base.IsServer)
		{
			throw new UnityException("SetScrap can only be called on the server.");
		}
		_claimedScrap.SetSpawnValue(scrap);
		OnShipScrapUpdate?.Invoke(_claimedScrap.Value, param2: true);
	}

	[Server]
	private void SetWorldScrap(int scrap)
	{
		if (!base.IsServer)
		{
			throw new UnityException("SetTotalWorldScrap can only be called on the server.");
		}
		_worldScrap.Value = Math.Max(0, scrap);
		OnWorldScrapUpdate?.Invoke(_worldScrap.Value, param2: true);
	}

	public int GetWorldScrap(bool prev = false)
	{
		if (!prev)
		{
			return _worldScrap.Value;
		}
		return _worldScrap.PrevValue;
	}

	public int GetClaimedScrap()
	{
		return _claimedScrap.Value;
	}

	private void OnIngameStatusUpdated(INGAME_STATUS status, bool server)
	{
		if (server)
		{
			if (status == INGAME_STATUS.IDLE || status == INGAME_STATUS.ROUND_END)
			{
				ResetStatus();
				SetWorldScrap(0);
				SetScrap(0);
			}
			UpdateLeverStatus();
		}
	}

	[Server]
	private void OnMapGenerated(bool server)
	{
		if (server)
		{
			ValueEnumerable<ArrayWhere<entity_phys_prop_scrap>, entity_phys_prop_scrap> source = from s in UnityEngine.Object.FindObjectsByType<entity_phys_prop_scrap>(FindObjectsInactive.Exclude, FindObjectsSortMode.None).AsValueEnumerable()
				where (bool)s && !(s is entity_prop_debt_receipt)
				select s;
			SetWorldScrap(source.Sum((entity_phys_prop_scrap scrap) => scrap.GetReward()));
		}
	}

	[Client]
	private void UpdateShipScrapInfo()
	{
		if (!base.IsClient)
		{
			throw new UnityException("UpdateShipScrapInfo can only be called on the client.");
		}
		if (!scrapDisplay)
		{
			throw new UnityException("scrapDisplay is not set");
		}
		scrapDisplay.SetText(SplitFlapMode.SHUFFLE, Mathf.Abs(_claimedScrap.Value).ToString().PadLeft(5, ' '));
		OnShipScrapUpdate?.Invoke(_claimedScrap.Value, param2: false);
	}

	protected override void __initializeVariables()
	{
		if (_claimedScrap == null)
		{
			throw new Exception("ScrapController._claimedScrap cannot be null. All NetworkVariableBase instances must be initialized.");
		}
		_claimedScrap.Initialize(this);
		__nameNetworkVariable(_claimedScrap, "_claimedScrap");
		NetworkVariableFields.Add(_claimedScrap);
		if (_worldScrap == null)
		{
			throw new Exception("ScrapController._worldScrap cannot be null. All NetworkVariableBase instances must be initialized.");
		}
		_worldScrap.Initialize(this);
		__nameNetworkVariable(_worldScrap, "_worldScrap");
		NetworkVariableFields.Add(_worldScrap);
		if (_maxContainerScrap == null)
		{
			throw new Exception("ScrapController._maxContainerScrap cannot be null. All NetworkVariableBase instances must be initialized.");
		}
		_maxContainerScrap.Initialize(this);
		__nameNetworkVariable(_maxContainerScrap, "_maxContainerScrap");
		NetworkVariableFields.Add(_maxContainerScrap);
		if (_vacuumUpgrade == null)
		{
			throw new Exception("ScrapController._vacuumUpgrade cannot be null. All NetworkVariableBase instances must be initialized.");
		}
		_vacuumUpgrade.Initialize(this);
		__nameNetworkVariable(_vacuumUpgrade, "_vacuumUpgrade");
		NetworkVariableFields.Add(_vacuumUpgrade);
		if (_transferTime == null)
		{
			throw new Exception("ScrapController._transferTime cannot be null. All NetworkVariableBase instances must be initialized.");
		}
		_transferTime.Initialize(this);
		__nameNetworkVariable(_transferTime, "_transferTime");
		NetworkVariableFields.Add(_transferTime);
		if (_transferStatus == null)
		{
			throw new Exception("ScrapController._transferStatus cannot be null. All NetworkVariableBase instances must be initialized.");
		}
		_transferStatus.Initialize(this);
		__nameNetworkVariable(_transferStatus, "_transferStatus");
		NetworkVariableFields.Add(_transferStatus);
		base.__initializeVariables();
	}

	protected override void __initializeRpcs()
	{
		base.__initializeRpcs();
	}

	protected internal override string __getTypeName()
	{
		return "ScrapController";
	}
}
