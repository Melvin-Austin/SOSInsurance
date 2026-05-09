using UnityEngine;

namespace HyenaQuest;

public class entity_item_link : entity_phys_usable
{
	public string url;

	[Client]
	public override void OnUse(entity_player ply)
	{
		if ((bool)ply && !IsLocked())
		{
			Application.OpenURL(url);
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
		return "entity_item_link";
	}
}
