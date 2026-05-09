using System;
using System.Collections.Generic;
using FailCake;
using Gameplay.Tutorial;
using Steamworks;
using Unity.Netcode;
using UnityEngine;

namespace HyenaQuest;

[DisallowMultipleComponent]
[RequireComponent(typeof(NetworkObject))]
[DefaultExecutionOrder(-70)]
public class TutorialController : NetController<TutorialController>
{
	public entity_led_switcher tutorialStartSwitcher;

	public entity_shake tutorialStartShake;

	public entity_item_vacuum container;

	public entity_split_flap_display scrapDisplay;

	public entity_prop_phys_item_place containerPlace;

	public entity_lever containerLever;

	public entity_split_flap_display debtText;

	public entity_split_flap_display scrapDeliveryText;

	public entity_delivery_spot deliverySpot;

	public entity_trigger winTrigger;

	public entity_button leaveButton;

	private util_timer _reviveTimer;

	private entity_tutorial_section _currentTutorial;

	private bool _hasWon;

	private readonly NetVar<int> _debt = new NetVar<int>(10);

	private readonly NetVar<int> _scrap = new NetVar<int>(0);

	public new void Awake()
	{
		base.Awake();
		if (!container)
		{
			throw new UnityException("Missing entity_item_vacuum");
		}
		if (!scrapDisplay)
		{
			throw new UnityException("Missing scrapDisplay");
		}
		if (!containerPlace)
		{
			throw new UnityException("Missing containerPlace");
		}
		if (!containerLever)
		{
			throw new UnityException("Missing containerLever");
		}
		if (!debtText)
		{
			throw new UnityException("Missing debtText");
		}
		if (!scrapDeliveryText)
		{
			throw new UnityException("Missing scrapDeliveryText");
		}
		if (!deliverySpot)
		{
			throw new UnityException("Missing deliverySpot");
		}
		if (!winTrigger)
		{
			throw new UnityException("Missing winTrigger");
		}
		if (!leaveButton)
		{
			throw new UnityException("Missing leaveButton");
		}
		if (!tutorialStartSwitcher)
		{
			throw new UnityException("Missing tutorialStartSwitcher");
		}
		tutorialStartSwitcher.SetLocked(locked: false);
		if (!tutorialStartShake)
		{
			throw new UnityException("Missing tutorialStartShake");
		}
	}

	private void OnScrapAdded(int scrap, bool server)
	{
		if (server && (bool)_currentTutorial && scrap >= 25 && string.Equals(_currentTutorial.GetSection().id, "scrap", StringComparison.InvariantCultureIgnoreCase))
		{
			if ((bool)container)
			{
				container.OnScrapAdded -= new Action<int, bool>(OnScrapAdded);
			}
			MarkSectionCompleted();
		}
	}

	protected override void OnNetworkPostSpawn()
	{
		base.OnNetworkPostSpawn();
		if (!base.IsClient)
		{
			return;
		}
		_scrap.RegisterOnValueChanged(delegate(int _, int newValue)
		{
			if ((bool)scrapDisplay && (bool)scrapDeliveryText)
			{
				scrapDisplay.SetText(SplitFlapMode.SHUFFLE, Mathf.Abs(newValue).ToString().PadLeft(6, ' '));
				scrapDeliveryText.SetText(SplitFlapMode.SHUFFLE, Mathf.Abs(newValue).ToString().PadLeft(6, ' '));
			}
		});
		_debt.RegisterOnValueChanged(delegate(int _, int newValue)
		{
			if ((bool)debtText)
			{
				debtText.SetText(SplitFlapMode.SHUFFLE, Mathf.Abs(newValue).ToString().PadLeft(6, ' '));
			}
		});
	}

	public override void OnNetworkPreDespawn()
	{
		base.OnNetworkPreDespawn();
		if (base.IsClient)
		{
			_scrap.OnValueChanged = null;
			_debt.OnValueChanged = null;
		}
	}

	public void MarkSectionCompleted()
	{
		if ((bool)_currentTutorial && !_currentTutorial.IsCompleted())
		{
			_currentTutorial.MarkCompleted();
			NetController<NotificationController>.Instance?.RemoveNotification(_currentTutorial?.GetSection().id);
			NetController<NotificationController>.Instance?.CreateNotification(new NotificationData
			{
				id = "yataaaa",
				duration = 3f,
				text = MonoController<LocalizationController>.Instance.Get("training.tutorial.completed"),
				soundEffect = "Ingame/Notifications/success-0.ogg",
				soundVolume = 0.4f
			});
		}
	}

	public void ActivateSection(entity_tutorial_section section)
	{
		if (!NetController<NotificationController>.Instance)
		{
			throw new UnityException("Missing NotificationController");
		}
		if (!MonoController<LocalizationController>.Instance)
		{
			throw new UnityException("Missing LocalizationController");
		}
		MarkSectionCompleted();
		_currentTutorial = section;
		TutorialSection section2 = _currentTutorial.GetSection();
		NetController<NotificationController>.Instance?.CreateNotification(new NotificationData
		{
			id = section2.id,
			duration = -1f,
			text = MonoController<LocalizationController>.Instance.Get(section2.notification),
			soundEffect = "Ingame/Notifications/success-1.ogg",
			soundVolume = 0.4f
		});
	}

	public override void OnNetworkSpawn()
	{
		base.OnNetworkSpawn();
		if (!base.IsServer)
		{
			return;
		}
		if (!NETController.Instance)
		{
			throw new UnityException("Missing NETController");
		}
		NETController.ValidatePlayerJoin = (NetworkManager.ConnectionApprovalRequest _) => ("Cannot join player on tutorial level", false);
		CoreController.WaitFor(delegate(PlayerController plyCtrl)
		{
			plyCtrl.OnPlayerDeath += new Action<entity_player, bool>(OnPlayerDeath);
		});
		container.OnScrapAdded += new Action<int, bool>(OnScrapAdded);
		containerPlace.OnItemUpdate += new Action<entity_item, bool, bool>(OnContainerPlaced);
		containerLever.OnUSE += new Action<entity_player, bool>(OnUSE);
		winTrigger.OnEnter += new Action<Collider>(WinTheGame);
		leaveButton.OnUSE += new Action<entity_player>(OnLeaveTutorial);
		tutorialStartSwitcher.OnTick += new Action<byte, bool>(OnStartSwitchTick);
		tutorialStartSwitcher.OnComplete += new Action(OnStartSwitchComplete);
		CoreController.WaitFor(delegate(PhoneController phoneCtrl)
		{
			phoneCtrl.Register("2312", delegate
			{
				_scrap.Value = Mathf.Clamp(_scrap.Value - 10, 0, 10000);
				NetController<DeliveryController>.Instance.CreateDelivery(new Task
				{
					Address = 2312,
					DeliveryPrefabIndex = (byte)UnityEngine.Random.Range(0, NetController<DeliveryController>.Instance.propPrefabs.Count),
					Reward = UnityEngine.Random.Range(100, 200),
					ScrapRequired = 10,
					ID = 0
				});
				return new List<string> { MonoController<LocalizationController>.Instance.Get("ingame.world.delivery-maker.creating") };
			});
		});
		deliverySpot.SetDeliveryAddress(2312);
	}

	private void OnStartSwitchComplete()
	{
		if ((bool)tutorialStartSwitcher)
		{
			tutorialStartSwitcher.SetLocked(locked: true);
			MarkSectionCompleted();
		}
	}

	private void OnStartSwitchTick(byte tick, bool server)
	{
		if (!server && (bool)tutorialStartShake)
		{
			tutorialStartShake.SetIntensity(0.005f * (float)(int)tick);
			tutorialStartShake.SetActive(act: true, 2f);
		}
	}

	private void OnLeaveTutorial(entity_player obj)
	{
		if ((bool)obj)
		{
			NETController.Instance.Disconnect();
		}
	}

	private void WinTheGame(Collider obj)
	{
		if (_hasWon)
		{
			return;
		}
		_hasWon = true;
		NetController<NotificationController>.Instance?.ClearNotifications();
		NetController<NotificationController>.Instance?.CreateNotification(new NotificationData
		{
			id = "finished",
			duration = -1f,
			text = MonoController<LocalizationController>.Instance.Get("training.tutorial.finished"),
			soundEffect = "Ingame/Notifications/success-1.ogg",
			soundVolume = 0.4f
		});
		if (SteamworksController.IsSteamRunning)
		{
			if (!SteamUserStats.SetAchievement("ACHIEVEMENT_PRACTICE"))
			{
				Debug.LogError("Failed to set achievement ACHIEVEMENT_PRACTICE");
			}
			if (!SteamUserStats.StoreStats())
			{
				Debug.LogError("Failed to store stats for local player.");
			}
		}
	}

	private void OnUSE(entity_player caller, bool use)
	{
		if ((bool)container && use)
		{
			container.SetLocked(LOCK_TYPE.LOCKED);
			_scrap.Value += container.GetTotalScrap();
			container.SetScrap(0);
			MarkSectionCompleted();
		}
	}

	public override void OnNetworkDespawn()
	{
		base.OnNetworkDespawn();
		if (base.IsServer)
		{
			NETController.ValidatePlayerJoin = null;
			_reviveTimer?.Stop();
			if ((bool)MonoController<PlayerController>.Instance)
			{
				MonoController<PlayerController>.Instance.OnPlayerDeath -= new Action<entity_player, bool>(OnPlayerDeath);
			}
			if ((bool)container)
			{
				container.OnScrapAdded -= new Action<int, bool>(OnScrapAdded);
			}
			if ((bool)containerPlace)
			{
				containerPlace.OnItemUpdate -= new Action<entity_item, bool, bool>(OnContainerPlaced);
			}
			if ((bool)containerLever)
			{
				containerLever.OnUSE -= new Action<entity_player, bool>(OnUSE);
			}
			if ((bool)winTrigger)
			{
				winTrigger.OnEnter -= new Action<Collider>(WinTheGame);
			}
			if ((bool)leaveButton)
			{
				leaveButton.OnUSE -= new Action<entity_player>(OnLeaveTutorial);
			}
			if ((bool)NetController<PhoneController>.Instance)
			{
				NetController<PhoneController>.Instance.Unregister("2312");
			}
		}
	}

	[Server]
	public void OnDeliveryCompleted()
	{
		if (!base.IsServer)
		{
			throw new UnityException("Server only");
		}
		_debt.Value = Mathf.Clamp(_debt.Value - 120, 0, 10000);
		MarkSectionCompleted();
	}

	private void OnPlayerDeath(entity_player ply, bool server)
	{
		if (server)
		{
			_reviveTimer?.Stop();
			_reviveTimer = util_timer.Simple(2f, delegate
			{
				ply.Revive();
			});
		}
	}

	private void OnContainerPlaced(entity_item item, bool placed, bool server)
	{
		if (server && (bool)containerLever)
		{
			containerLever.SetLocked(!placed);
		}
	}

	protected override void __initializeVariables()
	{
		if (_debt == null)
		{
			throw new Exception("TutorialController._debt cannot be null. All NetworkVariableBase instances must be initialized.");
		}
		_debt.Initialize(this);
		__nameNetworkVariable(_debt, "_debt");
		NetworkVariableFields.Add(_debt);
		if (_scrap == null)
		{
			throw new Exception("TutorialController._scrap cannot be null. All NetworkVariableBase instances must be initialized.");
		}
		_scrap.Initialize(this);
		__nameNetworkVariable(_scrap, "_scrap");
		NetworkVariableFields.Add(_scrap);
		base.__initializeVariables();
	}

	protected override void __initializeRpcs()
	{
		base.__initializeRpcs();
	}

	protected internal override string __getTypeName()
	{
		return "TutorialController";
	}
}
