using UnityEngine;

namespace HyenaQuest;

public class entity_prop_delivery_ancient : entity_prop_delivery
{
	private entity_force_look _look;

	protected override void Init()
	{
		base.Init();
		_look = GetComponent<entity_force_look>();
		if (!_look)
		{
			throw new UnityException("Missing entity_force_look");
		}
	}

	protected override void OnDamage(byte newHealth)
	{
		base.OnDamage(newHealth);
		if ((bool)_look && newHealth != 0)
		{
			_look.forceLookSpeed = Mathf.Lerp(1.2f, 0.65f, (float)(int)newHealth / (float)(int)_maxHealth);
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
		return "entity_prop_delivery_ancient";
	}
}
