namespace HyenaQuest;

public class entity_spark : entity_particle_effect
{
	public new void Awake()
	{
		base.Awake();
		base.name = "entity_spark";
	}

	public override EffectType GetEffectType()
	{
		return EffectType.SPARKS;
	}
}
