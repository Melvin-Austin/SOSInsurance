using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

namespace HyenaQuest;

[DisallowMultipleComponent]
[DefaultExecutionOrder(-70)]
[RequireComponent(typeof(NetworkObject))]
public class HelperController : NetController<HelperController>
{
	public static readonly int PRICE_PER_ROUND = 10;

	public static readonly int START_RESPAWN_COST = 25;

	public static readonly int START_POLICE_DELAY_COST = 25;

	public static readonly int POLICE_EXTRA_TIME = 60;

	public entity_button respawnButton;

	public entity_split_flap_display respawnCostDisplay;

	public entity_split_flap_display deadDisplay;

	public entity_split_flap_display zebraCostDisplay;

	private bool _boughtZebraThisRound;

	private readonly NetVar<int> _respawnCost = new NetVar<int>(START_RESPAWN_COST);

	private readonly NetVar<int> _policeDelayCost = new NetVar<int>(START_POLICE_DELAY_COST);

	public new void Awake()
	{
		base.Awake();
		if (!respawnButton)
		{
			throw new UnityException("Respawn button not assigned in HelperController");
		}
		if (!respawnCostDisplay)
		{
			throw new UnityException("Respawn cost display not assigned in HelperController");
		}
		if (!deadDisplay)
		{
			throw new UnityException("Dead display not assigned in HelperController");
		}
		if (!zebraCostDisplay)
		{
			throw new UnityException("Zebra cost display not assigned in HelperController");
		}
	}

	public override void OnNetworkSpawn()
	{
		base.OnNetworkSpawn();
		CoreController.WaitFor(delegate(PlayerController plyCtrl)
		{
			plyCtrl.OnPlayerRemoved += new Action<entity_player, bool>(UpdateDeadCount);
			plyCtrl.OnPlayerDeath += new Action<entity_player, bool>(UpdateDeadCount);
			plyCtrl.OnPlayerRevive += new Action<entity_player, bool>(UpdateDeadCount);
			UpdateDeadCount(null, base.IsServer);
		});
		if (base.IsServer)
		{
			CoreController.WaitFor(delegate(IngameController ingameCtrl)
			{
				ingameCtrl.OnStatusUpdated += new Action<INGAME_STATUS, bool>(OnStatusUpdated);
				ingameCtrl.OnRoundUpdate += new Action<byte, bool>(OnRoundUpdate);
			});
			CoreController.WaitFor(delegate(PhoneController phoneCtrl)
			{
				phoneCtrl.Register("93272", BuyPoliceTime);
			});
			respawnButton.OnUSE += new Action<entity_player>(OnRespawnButtonUse);
			respawnButton.SetLocked(newVal: true);
		}
	}

	private void UpdateDeadCount(entity_player ply, bool server)
	{
		if ((bool)MonoController<PlayerController>.Instance && (bool)deadDisplay)
		{
			List<entity_player> list = MonoController<PlayerController>.Instance?.GetDeadPlayers() ?? new List<entity_player>();
			deadDisplay.SetText(SplitFlapMode.NORMAL, list.Count.ToString().PadLeft(2, ' '));
			if (server)
			{
				UpdateRespawnButton();
			}
		}
	}

	public override void OnNetworkDespawn()
	{
		base.OnNetworkDespawn();
		if ((bool)MonoController<PlayerController>.Instance)
		{
			MonoController<PlayerController>.Instance.OnPlayerRemoved -= new Action<entity_player, bool>(UpdateDeadCount);
			MonoController<PlayerController>.Instance.OnPlayerDeath -= new Action<entity_player, bool>(UpdateDeadCount);
			MonoController<PlayerController>.Instance.OnPlayerRevive -= new Action<entity_player, bool>(UpdateDeadCount);
		}
		if (base.IsServer)
		{
			if ((bool)NetController<IngameController>.Instance)
			{
				NetController<IngameController>.Instance.OnStatusUpdated -= new Action<INGAME_STATUS, bool>(OnStatusUpdated);
				NetController<IngameController>.Instance.OnRoundUpdate -= new Action<byte, bool>(OnRoundUpdate);
			}
			if ((bool)respawnButton)
			{
				respawnButton.OnUSE -= new Action<entity_player>(OnRespawnButtonUse);
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
		_respawnCost.RegisterOnValueChanged(delegate
		{
			if ((bool)respawnCostDisplay)
			{
				UpdateRespawnDisplay();
			}
		});
		_policeDelayCost.RegisterOnValueChanged(delegate
		{
			if ((bool)zebraCostDisplay)
			{
				UpdateZebraDisplay();
			}
		});
		UpdateZebraDisplay();
		UpdateRespawnDisplay();
	}

	[Server]
	public List<string> BuyPoliceTime(entity_player caller)
	{
		if (!base.IsServer)
		{
			throw new UnityException("BuyPoliceTime is server only");
		}
		if (!NetController<IngameController>.Instance)
		{
			throw new UnityException("IngameController not found");
		}
		if (NetController<IngameController>.Instance.Status() != INGAME_STATUS.PLAYING)
		{
			return new List<string> { "ingame.phone.status.invalid-location" };
		}
		if (_boughtZebraThisRound)
		{
			return new List<string> { "ingame.phone.status.blocked" };
		}
		if (!MonoController<LocalizationController>.Instance)
		{
			throw new UnityException("LocalizationController not found");
		}
		if (!NetController<CurrencyController>.Instance)
		{
			throw new UnityException("CurrencyController not found");
		}
		if (!NetController<CurrencyController>.Instance.Pay(_policeDelayCost.Value))
		{
			return new List<string> { "ingame.phone.upgrade.no-money" };
		}
		NetController<IngameController>.Instance.AddTemporaryWorldTime(POLICE_EXTRA_TIME);
		_boughtZebraThisRound = true;
		return new List<string> { "ingame.phone.police.intro", "ingame.phone.police.pranks", "ingame.phone.police.outro" };
	}

	public override void OnNetworkPreDespawn()
	{
		base.OnNetworkPreDespawn();
		if (base.IsClient)
		{
			_respawnCost.OnValueChanged = null;
			_policeDelayCost.OnValueChanged = null;
		}
	}

	[Client]
	private void UpdateRespawnDisplay()
	{
		if (base.IsClient && (bool)respawnCostDisplay)
		{
			respawnCostDisplay.SetText(SplitFlapMode.SHUFFLE, Mathf.Abs(_respawnCost.Value).ToString().PadLeft(4, ' '));
		}
	}

	[Client]
	private void UpdateZebraDisplay()
	{
		if (base.IsClient && (bool)zebraCostDisplay)
		{
			zebraCostDisplay.SetText(SplitFlapMode.SHUFFLE, Mathf.Abs(_policeDelayCost.Value).ToString().PadLeft(4, ' '));
		}
	}

	[Server]
	private void OnRoundUpdate(byte round, bool server)
	{
		if (server)
		{
			int b = Mathf.RoundToInt(START_RESPAWN_COST + (round - 1) * PRICE_PER_ROUND);
			int b2 = Mathf.RoundToInt(START_POLICE_DELAY_COST + (round - 1) * PRICE_PER_ROUND);
			_respawnCost.Value = Mathf.Min(500, b);
			_policeDelayCost.Value = Mathf.Min(500, b2);
		}
	}

	[Server]
	private void OnStatusUpdated(INGAME_STATUS status, bool server)
	{
		if (server && (bool)respawnButton)
		{
			_boughtZebraThisRound = false;
			UpdateRespawnButton();
		}
	}

	[Server]
	private void UpdateRespawnButton()
	{
		if (!base.IsServer)
		{
			throw new UnityException("UpdateRespawnButton is server only");
		}
		respawnButton.SetLocked(NetController<IngameController>.Instance.Status() != INGAME_STATUS.PLAYING || !(MonoController<PlayerController>.Instance?.AnyPlayerDead() ?? false));
	}

	[Server]
	private void OnRespawnButtonUse(entity_player ply)
	{
		if (!base.IsServer)
		{
			throw new UnityException("OnRespawnButtonUse is server only");
		}
		if (!MonoController<PlayerController>.Instance)
		{
			throw new UnityException("PlayerController not found");
		}
		if (!NetController<CurrencyController>.Instance)
		{
			throw new UnityException("CurrencyController not found");
		}
		List<entity_player> deadPlayers = MonoController<PlayerController>.Instance.GetDeadPlayers();
		if (deadPlayers.Count == 0)
		{
			NetController<SoundController>.Instance?.Play3DSound("Ingame/Entities/Terminal/142608__autistic-lucario__error.ogg", respawnButton.transform, new AudioData
			{
				pitch = UnityEngine.Random.Range(0.85f, 1.2f),
				distance = 6f
			}, broadcast: true);
			return;
		}
		entity_player entity_player2 = deadPlayers[UnityEngine.Random.Range(0, deadPlayers.Count)];
		if (!entity_player2 || !entity_player2.IsDead())
		{
			NetController<SoundController>.Instance?.Play3DSound("Ingame/Entities/Terminal/142608__autistic-lucario__error.ogg", respawnButton.transform, new AudioData
			{
				pitch = UnityEngine.Random.Range(0.85f, 1.2f),
				distance = 6f
			}, broadcast: true);
		}
		else if (!NetController<CurrencyController>.Instance.CanPay(_respawnCost.Value))
		{
			NetController<SoundController>.Instance?.Play3DSound("Ingame/Entities/Terminal/142608__autistic-lucario__error.ogg", respawnButton.transform, new AudioData
			{
				pitch = UnityEngine.Random.Range(0.85f, 1.2f),
				distance = 6f
			}, broadcast: true);
		}
		else if (!entity_player2.Revive())
		{
			NetController<SoundController>.Instance?.Play3DSound("Ingame/Entities/Terminal/142608__autistic-lucario__error.ogg", respawnButton.transform, new AudioData
			{
				pitch = UnityEngine.Random.Range(0.85f, 1.2f),
				distance = 6f
			}, broadcast: true);
		}
		else
		{
			NetController<CurrencyController>.Instance.Pay(_respawnCost.Value);
			NetController<SoundController>.Instance?.Play3DSound($"Ingame/Store/buy_{UnityEngine.Random.Range(0, 2)}.ogg", respawnButton.transform, new AudioData
			{
				pitch = UnityEngine.Random.Range(0.85f, 1.2f),
				distance = 6f
			}, broadcast: true);
		}
	}

	protected override void __initializeVariables()
	{
		if (_respawnCost == null)
		{
			throw new Exception("HelperController._respawnCost cannot be null. All NetworkVariableBase instances must be initialized.");
		}
		_respawnCost.Initialize(this);
		__nameNetworkVariable(_respawnCost, "_respawnCost");
		NetworkVariableFields.Add(_respawnCost);
		if (_policeDelayCost == null)
		{
			throw new Exception("HelperController._policeDelayCost cannot be null. All NetworkVariableBase instances must be initialized.");
		}
		_policeDelayCost.Initialize(this);
		__nameNetworkVariable(_policeDelayCost, "_policeDelayCost");
		NetworkVariableFields.Add(_policeDelayCost);
		base.__initializeVariables();
	}

	protected override void __initializeRpcs()
	{
		base.__initializeRpcs();
	}

	protected internal override string __getTypeName()
	{
		return "HelperController";
	}
}
