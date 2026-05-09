using UnityEngine;

namespace HyenaQuest;

public class entity_ship_upgrade_time : entity_ship_upgrade
{
	private int _extraTime;

	public override void OnUpgradeBought(bool isLoad)
	{
		if (!NetController<IngameController>.Instance)
		{
			throw new UnityException("IngameController not found");
		}
		_extraTime += 60;
		NetController<IngameController>.Instance.SetPermaWorldTime(_extraTime);
	}

	public override void ResetUpgrade()
	{
		_extraTime = 0;
	}

	public override bool CanBuyAgain()
	{
		return false;
	}

	public override string GetID()
	{
		return "ship_upgrade_time";
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
		return "entity_ship_upgrade_time";
	}
}
