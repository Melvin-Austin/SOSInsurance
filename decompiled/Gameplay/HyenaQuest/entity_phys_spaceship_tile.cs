using System;
using System.Collections.Generic;
using UnityEngine;

namespace HyenaQuest;

public class entity_phys_spaceship_tile : entity_phys_breakable
{
	private bool _disconnected;

	protected override void OnNetworkPostSpawn()
	{
		base.OnNetworkPostSpawn();
		if (!base.IsClient)
		{
			return;
		}
		_locked.RegisterOnValueChanged(delegate(LOCK_TYPE oldValue, LOCK_TYPE newValue)
		{
			if (oldValue != newValue && newValue == LOCK_TYPE.NONE && !_disconnected)
			{
				NetController<SoundController>.Instance.Play3DSound("Ingame/Props/Metal/metal_break.ogg", base.transform.position, new AudioData
				{
					distance = 4f,
					pitch = UnityEngine.Random.Range(0.8f, 1.2f),
					volume = 0.4f
				});
				_disconnected = true;
			}
		});
	}

	public override void OnNetworkPreDespawn()
	{
		base.OnNetworkPreDespawn();
		if (base.IsClient)
		{
			_locked.OnValueChanged = null;
		}
	}

	public override void OnNetworkSpawn()
	{
		base.OnNetworkSpawn();
		if (base.IsServer)
		{
			SetLocked(LOCK_TYPE.LOCKED);
		}
	}

	protected override void OnDamage(byte newHealth)
	{
	}

	[Client]
	public override InteractionData InteractionSelector(Collider obj)
	{
		if (_locked.Value != LOCK_TYPE.LOCKED)
		{
			return base.InteractionSelector(obj);
		}
		return null;
	}

	[Server]
	public override Dictionary<string, string> Save()
	{
		return new Dictionary<string, string> { 
		{
			"frozen",
			IsLocked() ? "1" : "0"
		} };
	}

	[Server]
	public override void Load(Dictionary<string, string> data)
	{
		if (data.TryGetValue("frozen", out var value) && string.Equals(value, "0", StringComparison.InvariantCultureIgnoreCase))
		{
			SetLocked(LOCK_TYPE.NONE);
		}
	}

	protected override bool CanTakeDamage()
	{
		return _locked.Value == LOCK_TYPE.LOCKED;
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
		return "entity_phys_spaceship_tile";
	}
}
