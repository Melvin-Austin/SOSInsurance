namespace HyenaQuest;

public class entity_item_shield : entity_item_pickable
{
	public override string GetID()
	{
		return "item_shield";
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
		return "entity_item_shield";
	}
}
