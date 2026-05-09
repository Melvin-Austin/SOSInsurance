using UnityEngine;

namespace HyenaQuest;

public class entity_ship_upgrade_health : entity_ship_upgrade
{
	public override void OnUpgradeBought(bool isLoad)
	{
		if (!isLoad)
		{
			if (!NetController<IngameController>.Instance)
			{
				throw new UnityException("IngameController not found");
			}
			NetController<IngameController>.Instance.AddHealth(1);
		}
	}

	public override bool CanBuyAgain()
	{
		return false;
	}

	public override string GetID()
	{
		return "ship_upgrade_health";
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
		return "entity_ship_upgrade_health";
	}
}
