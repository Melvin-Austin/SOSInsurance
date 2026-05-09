using System;
using System.Collections.Generic;
using FailCake;
using Pathfinding;
using UnityEngine;

namespace HyenaQuest;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(Collider))]
[DisallowMultipleComponent]
public class entity_door_phys : entity_phys_breakable
{
	private static readonly int LOCKED_CHANCE = 25;

	public List<GameObject> layers = new List<GameObject>();

	public GameObject trap;

	public GameEvent<bool> onDoorDetached = new GameEvent<bool>();

	private NavmeshCut _navmeshCut;

	private util_timer _trapTimer;

	private readonly NetVar<byte> _layer = new NetVar<byte>(byte.MaxValue);

	private readonly NetVar<bool> _trapped = new NetVar<bool>(value: false);

	protected override void OnNetworkPostSpawn()
	{
		base.OnNetworkPostSpawn();
		if (!base.IsClient)
		{
			return;
		}
		_locked.RegisterOnValueChanged(delegate(LOCK_TYPE oldValue, LOCK_TYPE newValue)
		{
			if (oldValue != newValue && oldValue != LOCK_TYPE.SOFT_FROZEN && newValue == LOCK_TYPE.NONE)
			{
				NetController<SoundController>.Instance.Play3DSound("Ingame/Props/Wood/wood_panel_impact_hard1.ogg", base.transform.position, new AudioData
				{
					distance = 4f,
					pitch = UnityEngine.Random.Range(0.8f, 1.2f),
					volume = 0.4f
				});
				onDoorDetached.Invoke(param1: false);
			}
		});
		_layer.RegisterOnValueChanged(delegate(byte oldValue, byte newValue)
		{
			if (oldValue != newValue)
			{
				ProcessLayer();
			}
		});
		if (!trap)
		{
			return;
		}
		_trapped.RegisterOnValueChanged(delegate(bool oldValue, bool newValue)
		{
			if ((bool)trap && oldValue != newValue)
			{
				trap.SetActive(newValue);
			}
		});
	}

	public override void OnNetworkPreDespawn()
	{
		base.OnNetworkPreDespawn();
		if (base.IsClient)
		{
			_locked.OnValueChanged = null;
			_layer.OnValueChanged = null;
			_trapped.OnValueChanged = null;
		}
	}

	public override void OnNetworkSpawn()
	{
		base.OnNetworkSpawn();
		_navmeshCut = GetComponent<NavmeshCut>();
		if (!base.IsServer)
		{
			return;
		}
		if (!NetController<ContractController>.Instance)
		{
			health.SetSpawnValue((byte)UnityEngine.Random.Range(2, 4));
			_maxHealth = health.Value;
		}
		else if ((bool)NetController<IngameController>.Instance && NetController<IngameController>.Instance?.GetCurrentRound() >= 2)
		{
			Contract pickedContract = NetController<ContractController>.Instance.GetPickedContract();
			if (UnityEngine.Random.Range(0, 100) < LOCKED_CHANCE || pickedContract.modifiers.HasFlag(ContractModifiers.LOCKED_DOORS))
			{
				health.SetSpawnValue((byte)UnityEngine.Random.Range(2, 4));
				_maxHealth = health.Value;
			}
		}
		else
		{
			health.SetSpawnValue(0);
		}
		if ((bool)trap)
		{
			if (!NetController<IngameController>.Instance)
			{
				throw new UnityException("IngameController not found");
			}
			_trapped.SetSpawnValue(NetController<IngameController>.Instance.GetCurrentRound() > 2 && UnityEngine.Random.value < 0.3f);
		}
		SetLocked((health.Value <= 0) ? LOCK_TYPE.SOFT : LOCK_TYPE.LOCKED);
		_layer.SetSpawnValue((byte)UnityEngine.Random.Range(0, layers.Count));
	}

	private void ProcessLayer()
	{
		List<GameObject> list = layers;
		if (list == null || list.Count <= 0)
		{
			return;
		}
		for (int i = 0; i < layers.Count; i++)
		{
			if ((bool)layers[i])
			{
				if (i == _layer.Value)
				{
					layers[i].SetActive(value: true);
				}
				else
				{
					UnityEngine.Object.Destroy(layers[i]);
				}
			}
		}
		Renderer[] componentsInChildren = GetComponentsInChildren<MeshRenderer>(includeInactive: false);
		_renderers = componentsInChildren;
	}

	protected override bool CanTakeDamage()
	{
		LOCK_TYPE value = _locked.Value;
		return value == LOCK_TYPE.SOFT || value == LOCK_TYPE.LOCKED;
	}

	protected override bool IsBreakDamage(float impactForce)
	{
		return impactForce > breakForce;
	}

	[Server]
	protected override void OnBreak()
	{
		if (!base.IsServer)
		{
			throw new UnityException("OnBreak can only be called on the server!");
		}
		SetLocked(LOCK_TYPE.NONE);
		Vector3 vector = base.transform.position + Vector3.up * 1f;
		Vector3 forward = base.transform.forward;
		if ((bool)(UnityEngine.Object)(object)_navmeshCut)
		{
			UnityEngine.Object.Destroy((UnityEngine.Object)(object)_navmeshCut);
		}
		_rigidbody.AddForce(vector + forward * 30f, ForceMode.Impulse);
	}

	[Server]
	public override void SetLocked(LOCK_TYPE locked)
	{
		if (!base.IsServer)
		{
			throw new UnityException("Server only");
		}
		base.SetLocked(locked);
		if (_locked.Value == LOCK_TYPE.SOFT_FROZEN || locked != 0)
		{
			return;
		}
		if ((bool)trap && _trapped.Value)
		{
			NetController<SoundController>.Instance?.Play3DSound("Ingame/DoorTrap/trip_start.ogg", trap.transform.position, new AudioData
			{
				distance = 4f,
				parent = this
			}, broadcast: true);
			_trapTimer?.Stop();
			_trapTimer = util_timer.Create(2, 0.4f, delegate(int i)
			{
				if ((bool)trap)
				{
					NetController<SoundController>.Instance?.Play3DSound("Ingame/DoorTrap/tick.ogg", trap.transform.position, new AudioData
					{
						distance = 4f,
						parent = this,
						pitch = 0.6f * (float)i + 1f
					}, broadcast: true);
				}
			}, delegate
			{
				if ((bool)trap)
				{
					NetController<SoundController>.Instance?.Play3DSound($"Ingame/DoorTrap/goodbye_{UnityEngine.Random.Range(0, 2)}.ogg", trap.transform.position, new AudioData
					{
						distance = 4f,
						volume = 0.8f,
						parent = this
					}, broadcast: true);
					_trapTimer?.Stop();
					_trapTimer = util_timer.Simple(0.7f, delegate
					{
						NetController<ExplosionController>.Instance?.Explode(trap.transform.position, 5f, 80);
						_trapped.Value = false;
					});
				}
			});
		}
		onDoorDetached.Invoke(param1: true);
	}

	protected override void __initializeVariables()
	{
		if (_layer == null)
		{
			throw new Exception("entity_door_phys._layer cannot be null. All NetworkVariableBase instances must be initialized.");
		}
		_layer.Initialize(this);
		__nameNetworkVariable(_layer, "_layer");
		NetworkVariableFields.Add(_layer);
		if (_trapped == null)
		{
			throw new Exception("entity_door_phys._trapped cannot be null. All NetworkVariableBase instances must be initialized.");
		}
		_trapped.Initialize(this);
		__nameNetworkVariable(_trapped, "_trapped");
		NetworkVariableFields.Add(_trapped);
		base.__initializeVariables();
	}

	protected override void __initializeRpcs()
	{
		base.__initializeRpcs();
	}

	protected internal override string __getTypeName()
	{
		return "entity_door_phys";
	}
}
