using UnityEngine;

namespace HyenaQuest;

public class entity_prop_delivery_achievement : entity_prop_delivery
{
	public STEAM_ACHIEVEMENTS achievement;

	[Server]
	protected override void OnDelivery()
	{
		if (!base.IsServer)
		{
			throw new UnityException("OnDelivery can only be called on the server");
		}
		if (health.Value == _maxHealth)
		{
			if (!NetController<StatsController>.Instance)
			{
				throw new UnityException("StatsController instance not found");
			}
			NetController<StatsController>.Instance.UnlockAchievementSV(achievement, ulong.MaxValue);
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
		return "entity_prop_delivery_achievement";
	}
}
