using UnityEngine;

namespace HyenaQuest;

public class entity_ship_upgrade_scrap : entity_ship_upgrade
{
	private int _boughtCount;

	public override void OnUpgradeBought(bool isLoad)
	{
		if (!NetController<ScrapController>.Instance)
		{
			throw new UnityException("ScrapController not found");
		}
		int maxContainerScrap = NetController<ScrapController>.Instance.GetMaxContainerScrap();
		NetController<ScrapController>.Instance.SetMaxContainerScrap(maxContainerScrap + 100);
		_boughtCount++;
	}

	public override bool CanBuyAgain()
	{
		return _boughtCount < 2;
	}

	public override string GetID()
	{
		return "ship_upgrade_scrap";
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
		return "entity_ship_upgrade_scrap";
	}
}
