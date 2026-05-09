using UnityEngine;

namespace HyenaQuest;

public class entity_ship_upgrade_systems : entity_ship_upgrade
{
	public override void OnUpgradeBought(bool isLoad)
	{
		if (!NetController<ScrapController>.Instance)
		{
			throw new UnityException("ScrapController not found");
		}
		if (!NetController<DeliveryController>.Instance)
		{
			throw new UnityException("DeliveryController not found");
		}
		if (!NetController<PhoneController>.Instance)
		{
			throw new UnityException("PhoneController not found");
		}
		NetController<ScrapController>.Instance.SetTransferTime(1f);
		NetController<DeliveryController>.Instance.SetDeliverySpeed(1f);
		NetController<PhoneController>.Instance.SetFastCallUpgrade(set: true);
	}

	public override bool CanBuyAgain()
	{
		return false;
	}

	public override string GetID()
	{
		return "ship_upgrade_systems";
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
		return "entity_ship_upgrade_systems";
	}
}
