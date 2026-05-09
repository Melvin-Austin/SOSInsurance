using UnityEngine;

namespace HyenaQuest;

public class entity_ship_upgrade_vacuum : entity_ship_upgrade
{
	public override void OnUpgradeBought(bool isLoad)
	{
		if (!NetController<ScrapController>.Instance)
		{
			throw new UnityException("ScrapController not found");
		}
		NetController<ScrapController>.Instance.SetVacuumUpgrade(set: true);
	}

	public override bool CanBuyAgain()
	{
		return false;
	}

	public override string GetID()
	{
		return "entity_ship_upgrade_vacuum";
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
		return "entity_ship_upgrade_vacuum";
	}
}
