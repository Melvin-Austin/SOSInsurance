using System;
using Unity.Netcode;
using UnityEngine;

namespace HyenaQuest;

public class entity_prop_delivery_easter_chance : entity_prop_delivery
{
	private readonly NetVar<byte> _easter = new NetVar<byte>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

	protected override void OnNetworkPostSpawn()
	{
		base.OnNetworkPostSpawn();
		if (base.IsClient)
		{
			_easter.RegisterOnValueChanged(delegate(byte _, byte newValue)
			{
				OnEaster(newValue);
			});
		}
	}

	public override void OnNetworkPreDespawn()
	{
		base.OnNetworkPreDespawn();
		if (base.IsClient)
		{
			_easter.OnValueChanged = null;
		}
	}

	protected virtual void OnEaster(byte indx)
	{
	}

	protected virtual float GetEasterHitChance()
	{
		return 0.5f;
	}

	protected override void OnDamage(byte newHealth)
	{
		base.OnDamage(newHealth);
		if (base.IsOwner && UnityEngine.Random.value > GetEasterHitChance())
		{
			_easter.Value = (byte)Mathf.Repeat(_easter.Value + 1, 255f);
		}
	}

	protected override void __initializeVariables()
	{
		if (_easter == null)
		{
			throw new Exception("entity_prop_delivery_easter_chance._easter cannot be null. All NetworkVariableBase instances must be initialized.");
		}
		_easter.Initialize(this);
		__nameNetworkVariable(_easter, "_easter");
		NetworkVariableFields.Add(_easter);
		base.__initializeVariables();
	}

	protected override void __initializeRpcs()
	{
		base.__initializeRpcs();
	}

	protected internal override string __getTypeName()
	{
		return "entity_prop_delivery_easter_chance";
	}
}
