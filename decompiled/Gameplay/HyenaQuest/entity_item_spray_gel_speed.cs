namespace HyenaQuest;

public class entity_item_spray_gel_speed : entity_item_spray_gel
{
	public override string GetID()
	{
		return "item_spray_gel_speed";
	}

	protected override int GetMaxDecals()
	{
		return 60;
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
		return "entity_item_spray_gel_speed";
	}
}
