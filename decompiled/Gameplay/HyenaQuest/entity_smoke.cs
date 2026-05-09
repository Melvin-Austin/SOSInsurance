namespace HyenaQuest;

public class entity_smoke : entity_particle_effect
{
	public bool big;

	public new void Awake()
	{
		base.Awake();
		base.name = "entity_smoke";
	}

	public override EffectType GetEffectType()
	{
		if (!big)
		{
			return EffectType.SMOKE;
		}
		return EffectType.SMOKE_LARGE;
	}
}
