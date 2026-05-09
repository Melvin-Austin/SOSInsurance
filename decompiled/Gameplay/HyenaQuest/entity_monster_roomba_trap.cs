using System;
using FailCake;
using Opsive.GraphDesigner.Runtime.Variables;
using UnityEngine;

namespace HyenaQuest;

public class entity_monster_roomba_trap : entity_monster_roomba
{
	[Range(0f, 20f)]
	public float trapTime = 3f;

	public float resetTime = 2f;

	[Header("Damage")]
	[Range(0f, 100f)]
	public byte damage = 25;

	private static readonly int Trap = Animator.StringToHash("Trap");

	private SharedVariable<bool> _runAway;

	private entity_trigger _triggerArea;

	private entity_vehicle_seat _vehicleSeat;

	private util_timer _snapTimer;

	private util_timer _resetTimer;

	private entity_player _trapPlayer;

	private readonly NetVar<bool> _hasTarget = new NetVar<bool>(value: false);

	public new void Awake()
	{
		base.Awake();
		_triggerArea = GetComponentInChildren<entity_trigger>(includeInactive: true);
		if (!_triggerArea)
		{
			throw new UnityException("entity_monster_roomba_trap requires entity_trigger component");
		}
		_vehicleSeat = GetComponent<entity_vehicle_seat>();
		if (!_vehicleSeat)
		{
			throw new UnityException("entity_monster_roomba_trap requires entity_vehicle_seat component");
		}
	}

	public override void OnNetworkSpawn()
	{
		base.OnNetworkSpawn();
		if (base.IsServer)
		{
			_runAway = _behavior.GetVariable<bool>("RUN_AWAY");
			_triggerArea.OnEnter += new Action<Collider>(OnEnter);
		}
	}

	public override void OnNetworkDespawn()
	{
		base.OnNetworkDespawn();
		if (base.IsServer)
		{
			if (_snapTimer != null)
			{
				_snapTimer.Stop();
			}
			if (_resetTimer != null)
			{
				_resetTimer.Stop();
			}
			if ((bool)_trapPlayer)
			{
				_trapPlayer.SetVehicle(null);
				_trapPlayer = null;
			}
			_triggerArea.OnEnter -= new Action<Collider>(OnEnter);
		}
	}

	public new void LateUpdate()
	{
		base.LateUpdate();
		if (!base.IsClient)
		{
			if ((bool)_trapPlayer && _trapPlayer.IsDead())
			{
				_trapPlayer = null;
			}
		}
		else if ((bool)_animator)
		{
			_animator.SetBool(Trap, _hasTarget.Value);
		}
	}

	private void OnEnter(Collider obj)
	{
		if (!base.IsServer || !obj || !obj.CompareTag("Player") || (bool)_trapPlayer)
		{
			return;
		}
		entity_player component = obj.GetComponent<entity_player>();
		if (!component)
		{
			return;
		}
		_trapPlayer = component;
		_triggerArea.enabled = false;
		ResetPath();
		_hasTarget.Value = true;
		_runAway.Value = true;
		_trapPlayer.SetVehicle(_vehicleSeat);
		_trapPlayer.TakeHealthRPC(damage, DamageType.CUT);
		AudioData data = new AudioData
		{
			pitch = UnityEngine.Random.Range(0.8f, 1.2f),
			distance = 4f
		};
		NetController<SoundController>.Instance.Play3DSound("Ingame/Monsters/Roomba/bear-trap.ogg", base.transform.position, data, broadcast: true);
		if (!_trapPlayer || _trapPlayer.IsDead())
		{
			_triggerArea.enabled = true;
			ResetTarget();
			return;
		}
		_snapTimer?.Stop();
		_snapTimer = util_timer.Simple(0.1f, delegate
		{
			Transform furthestRoomFromPlayer = NetController<MapController>.Instance.GetFurthestRoomFromPlayer(_trapPlayer);
			if ((bool)furthestRoomFromPlayer)
			{
				SetPath(furthestRoomFromPlayer.transform.position);
			}
			_snapTimer?.Stop();
			_snapTimer = util_timer.Simple(trapTime, delegate
			{
				ResetTarget();
				NetController<SoundController>.Instance.Play3DSound("Ingame/Monsters/Roomba/bear-trap-open.ogg", base.transform.position, data, broadcast: true);
				_resetTimer?.Stop();
				_resetTimer = util_timer.Simple(resetTime, delegate
				{
					_triggerArea.enabled = true;
				});
			});
		});
	}

	private void ResetTarget()
	{
		_resetTimer?.Stop();
		_hasTarget.Value = false;
		_runAway.Value = false;
		_trapPlayer?.SetVehicle(null);
		_trapPlayer = null;
	}

	protected override void __initializeVariables()
	{
		if (_hasTarget == null)
		{
			throw new Exception("entity_monster_roomba_trap._hasTarget cannot be null. All NetworkVariableBase instances must be initialized.");
		}
		_hasTarget.Initialize(this);
		__nameNetworkVariable(_hasTarget, "_hasTarget");
		NetworkVariableFields.Add(_hasTarget);
		base.__initializeVariables();
	}

	protected override void __initializeRpcs()
	{
		base.__initializeRpcs();
	}

	protected internal override string __getTypeName()
	{
		return "entity_monster_roomba_trap";
	}
}
