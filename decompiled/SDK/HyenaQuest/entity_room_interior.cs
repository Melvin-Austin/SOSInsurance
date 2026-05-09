namespace HyenaQuest;

public class entity_room_interior : entity_room_base
{
	private bool _isFlipped;

	public void SetFlip(bool flip)
	{
		_isFlipped = flip;
	}

	public bool IsRoomFlipped()
	{
		return _isFlipped;
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
		return "entity_room_interior";
	}
}
