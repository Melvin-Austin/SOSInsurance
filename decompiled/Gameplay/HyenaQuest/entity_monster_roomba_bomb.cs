using System;
using FailCake;
using UnityEngine;

namespace HyenaQuest;

public class entity_monster_roomba_bomb : entity_monster_roomba
{
	[Range(0f, 1000f)]
	public int damage = 1000;

	[Range(0f, 100f)]
	public float range = 4f;

	private entity_trigger _triggerArea;

	private util_timer _explosion;

	public new void Awake()
	{
		base.Awake();
		_triggerArea = GetComponentInChildren<entity_trigger>(includeInactive: true);
		if (!_triggerArea)
		{
			throw new UnityException("entity_monster_roomba_bomb requires entity_trigger component");
		}
	}

	public override void OnNetworkSpawn()
	{
		base.OnNetworkSpawn();
		if (base.IsServer)
		{
			_triggerArea.OnEnter += new Action<Collider>(OnEnter);
		}
	}

	public override void OnNetworkDespawn()
	{
		base.OnNetworkDespawn();
		if (base.IsServer)
		{
			_explosion?.Stop();
			_triggerArea.OnEnter -= new Action<Collider>(OnEnter);
		}
	}

	public override void TakeHealth(byte _)
	{
		Explode();
	}

	[Server]
	private void OnEnter(Collider obj)
	{
		Explode();
	}

	[Server]
	private void Explode()
	{
		if (!base.IsServer || _explosion != null)
		{
			return;
		}
		ResetPath();
		_behavior.StopBehavior();
		NetController<SoundController>.Instance.Play3DSound("Ingame/Monsters/Roomba/tripmine.ogg", base.transform.position, new AudioData
		{
			distance = 5f
		}, broadcast: true);
		_explosion?.Stop();
		_explosion = util_timer.Simple(0.5f, delegate
		{
			NetController<ExplosionController>.Instance.Explode(base.transform.position, range, damage);
			if (base.IsSpawned)
			{
				base.NetworkObject.Despawn();
			}
		});
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
		return "entity_monster_roomba_bomb";
	}
}
