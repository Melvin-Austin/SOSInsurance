using UnityEngine;

namespace HyenaQuest;

public class entity_confetti : entity_particle_effect
{
	[Header("Confetti Settings")]
	public bool sphere;

	public new void Awake()
	{
		base.Awake();
		base.name = "entity_confetti";
	}

	public override EffectType GetEffectType()
	{
		if (!sphere)
		{
			return EffectType.CONFETTI;
		}
		return EffectType.CONFETTI_SPHERE;
	}
}
