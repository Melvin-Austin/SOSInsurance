using UnityEngine;

namespace HyenaQuest;

public class entity_phys_usable : entity_phys
{
	[Client]
	public virtual void OnUse(entity_player ply)
	{
	}

	[Client]
	public override InteractionData InteractionSelector(Collider obj)
	{
		if (!IsLocked())
		{
			return new InteractionData(Interaction.INTERACT, _renderers, "ingame.ui.hints.open");
		}
		return new InteractionData(Interaction.INTERACT_LOCKED, _renderers);
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
		return "entity_phys_usable";
	}
}
