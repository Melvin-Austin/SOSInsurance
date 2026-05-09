using System.Collections;
using UnityEngine;

namespace HyenaQuest;

public class entity_prop_pizza_pepperoni : entity_prop_pizza_teleport
{
	protected override IEnumerator Teleport()
	{
		yield return new WaitForSecondsRealtime(1f);
		if (entity_prop_pizza.PEPPERONI_POSITION.HasValue)
		{
			_networkTransform?.SetState(entity_prop_pizza.PEPPERONI_POSITION.Value, base.transform.rotation, base.transform.localScale, teleportDisabled: false);
			entity_prop_pizza.PEPPERONI_POSITION = null;
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
		return "entity_prop_pizza_pepperoni";
	}
}
