using Unity.Netcode;
using UnityEngine;

namespace HyenaQuest;

[RequireComponent(typeof(NetworkObject))]
public class entity_item : entity_phys
{
	[Client]
	public virtual void OnUse(entity_player ply, Collider obj, bool pressing)
	{
	}

	[Client]
	public override InteractionData InteractionSelector(Collider obj)
	{
		if (IsLocked())
		{
			return new InteractionData(Interaction.INTERACT_LOCKED, _renderers);
		}
		return new InteractionData(Interaction.INTERACT, _renderers, "ingame.ui.hints.use");
	}

	public virtual string GetID()
	{
		throw new UnityException("GetID() not implemented");
	}

	[Server]
	public override SaveDataItems? LoadData(SaveData saveData, string id = null)
	{
		if (!base.IsServer)
		{
			throw new UnityException("Server only");
		}
		return base.LoadData(saveData, GetID()) ?? ((SaveDataItems?)null);
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
		return "entity_item";
	}
}
