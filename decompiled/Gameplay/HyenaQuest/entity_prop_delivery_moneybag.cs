using UnityEngine;
using UnityEngine.VFX;

namespace HyenaQuest;

public class entity_prop_delivery_moneybag : entity_prop_delivery
{
	private VisualEffect _moneyVFX;

	protected override void Init()
	{
		base.Init();
		_moneyVFX = GetComponentInChildren<VisualEffect>(includeInactive: true);
		if (!_moneyVFX)
		{
			throw new UnityException("Missing VisualEffect");
		}
	}

	protected override void OnDamage(byte newHealth)
	{
		base.OnDamage(newHealth);
		_moneyVFX?.Play();
		if (base.IsServer)
		{
			NetController<CurrencyController>.Instance.Pay(Random.Range(2, 20));
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
		return "entity_prop_delivery_moneybag";
	}
}
