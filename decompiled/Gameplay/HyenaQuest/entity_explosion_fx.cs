namespace HyenaQuest;

public class entity_explosion_fx : entity_particle_effect
{
	public new void Awake()
	{
		base.Awake();
		base.name = "entity_explosion_fx";
	}
}
