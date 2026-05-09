using System;
using Unity.Netcode;
using UnityEngine;

namespace HyenaQuest;

[RequireComponent(typeof(NetworkObject))]
public class entity_round_controller : NetworkBehaviour
{
	private static readonly float ROUND_IDLE_PULL_TIME = 1.25f;

	private static readonly float ROUND_PLAYING_PULL_TIME = 0.8f;

	private entity_led_switcher _switch;

	private entity_shake _shake;

	private entity_curved_text _hint;

	public void Awake()
	{
		_switch = GetComponent<entity_led_switcher>();
		if (!_switch)
		{
			throw new UnityException("entity_floor_controller requires entity_led_switcher component");
		}
		_shake = GetComponentInChildren<entity_shake>(includeInactive: true);
		if (!_shake)
		{
			throw new UnityException("entity_floor_controller requires entity_shake component");
		}
		_hint = GetComponentInChildren<entity_curved_text>(includeInactive: true);
		if (!_hint)
		{
			throw new UnityException("entity_floor_controller requires entity_curved_text component");
		}
	}

	public override void OnNetworkSpawn()
	{
		base.OnNetworkSpawn();
		CoreController.WaitFor(delegate(IngameController ingameCtrl)
		{
			ingameCtrl.OnStatusUpdated += new Action<INGAME_STATUS, bool>(OnIngameStatusUpdated);
			OnIngameStatusUpdated(ingameCtrl.Status(), base.IsServer);
			_switch.OnTick += new Action<byte, bool>(OnTick);
			if (base.IsServer)
			{
				_switch.OnComplete += new Action(ingameCtrl.RequestNewRound);
			}
		});
	}

	public override void OnNetworkDespawn()
	{
		base.OnNetworkDespawn();
		if ((bool)NetController<IngameController>.Instance)
		{
			NetController<IngameController>.Instance.OnStatusUpdated -= new Action<INGAME_STATUS, bool>(OnIngameStatusUpdated);
		}
		if (base.IsClient)
		{
			if ((bool)_switch)
			{
				_switch.OnTick -= new Action<byte, bool>(OnTick);
			}
			if (!base.IsServer)
			{
				return;
			}
		}
		if ((bool)_switch && (bool)NetController<IngameController>.Instance)
		{
			_switch.OnComplete -= new Action(NetController<IngameController>.Instance.RequestNewRound);
		}
	}

	private void OnTick(byte tick, bool server)
	{
		if (!server && (bool)_shake)
		{
			_shake.SetIntensity(0.005f * (float)(int)tick);
			_shake.SetActive(act: true, 2f);
		}
	}

	[Shared]
	private void OnIngameStatusUpdated(INGAME_STATUS newStatus, bool server)
	{
		if (server && (bool)_switch)
		{
			_switch.timePerLED = ((newStatus == INGAME_STATUS.PLAYING) ? ROUND_PLAYING_PULL_TIME : ROUND_IDLE_PULL_TIME);
		}
		if (newStatus == INGAME_STATUS.IDLE || newStatus == INGAME_STATUS.PLAYING)
		{
			if (server && (bool)_switch)
			{
				_switch.SetLocked(locked: false);
			}
			if ((bool)_hint)
			{
				_hint.SetText("⬇ ⬇ ⬇ ⬇", new Color(0.376f, 0.69f, 0.796f));
			}
		}
		else
		{
			if (server && (bool)_switch)
			{
				_switch.SetLocked(locked: true);
			}
			if ((bool)_hint)
			{
				_hint.SetText("<b>X X X X X</b>", new Color(0.975f, 0.15f, 0f));
			}
		}
	}

	protected override void __initializeVariables()
	{
		base.__initializeVariables();
	}

	protected override void __initializeRpcs()
	{
		base.__initializeRpcs();
	}

	protected internal override string __getTypeName()
	{
		return "entity_round_controller";
	}
}
