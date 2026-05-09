using System;
using UnityEngine;

namespace HyenaQuest;

public class entity_prop_delivery_flashlight : entity_prop_delivery
{
	public GameObject flashlight;

	private readonly NetVar<bool> _flash = new NetVar<bool>(value: false);

	protected override void OnNetworkPostSpawn()
	{
		base.OnNetworkPostSpawn();
		if (!base.IsClient)
		{
			return;
		}
		_flash.RegisterOnValueChanged(delegate(bool _, bool newValue)
		{
			if ((bool)flashlight)
			{
				flashlight.SetActive(newValue);
			}
		});
	}

	public override void OnNetworkPreDespawn()
	{
		base.OnNetworkPreDespawn();
		if (base.IsClient)
		{
			_flash.OnValueChanged = null;
		}
	}

	protected override void Init()
	{
		base.Init();
		if (!flashlight)
		{
			throw new UnityException("Missing flashlight");
		}
	}

	protected override void OnDamage(byte newHealth)
	{
		base.OnDamage(newHealth);
		if (base.IsServer)
		{
			_flash.Value = newHealth >= 1;
		}
	}

	protected override void __initializeVariables()
	{
		if (_flash == null)
		{
			throw new Exception("entity_prop_delivery_flashlight._flash cannot be null. All NetworkVariableBase instances must be initialized.");
		}
		_flash.Initialize(this);
		__nameNetworkVariable(_flash, "_flash");
		NetworkVariableFields.Add(_flash);
		base.__initializeVariables();
	}

	protected override void __initializeRpcs()
	{
		base.__initializeRpcs();
	}

	protected internal override string __getTypeName()
	{
		return "entity_prop_delivery_flashlight";
	}
}
