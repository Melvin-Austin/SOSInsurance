namespace HyenaQuest;

public class entity_room : entity_room_base
{
	private int _roomDistance;

	protected override int TextureLayerSeed()
	{
		return SDK.GetSeed?.Invoke() ?? (-1);
	}

	public entity_interior_exit[] GetInteriorExits()
	{
		return GetComponentsInChildren<entity_interior_exit>(includeInactive: true);
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
		return "entity_room";
	}
}
