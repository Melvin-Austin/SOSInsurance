using UnityEngine;

namespace HyenaQuest;

public class entity_ship_upgrade_inventory : entity_ship_upgrade
{
	private byte _slots = IngameController.START_INVENTORY_SLOTS;

	public override void OnUpgradeBought(bool isLoad)
	{
		if (!NetController<IngameController>.Instance)
		{
			throw new UnityException("IngameController not found");
		}
		_slots++;
		NetController<IngameController>.Instance.SetMaxInventorySlots(_slots);
	}

	public override void ResetUpgrade()
	{
		_slots = 1;
	}

	public override bool CanBuyAgain()
	{
		return NetController<IngameController>.Instance.GetMaxInventorySlots() < IngameController.MAX_INVENTORY_SLOTS;
	}

	public override string GetID()
	{
		return "ship_upgrade_inventory";
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
		return "entity_ship_upgrade_inventory";
	}
}
