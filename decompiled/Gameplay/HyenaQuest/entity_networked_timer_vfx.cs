using System.Collections.Generic;
using UnityEngine;
using UnityEngine.VFX;

namespace HyenaQuest;

public class entity_networked_timer_vfx : entity_networked_timer
{
	public List<VisualEffect> vfx = new List<VisualEffect>();

	public List<ParticleSystem> particles = new List<ParticleSystem>();

	public override void OnUpdate(bool active)
	{
		foreach (VisualEffect item in vfx)
		{
			if (active)
			{
				item?.Play();
			}
			else
			{
				item?.Stop();
			}
		}
		foreach (ParticleSystem particle in particles)
		{
			if (active)
			{
				particle?.Play(withChildren: true);
			}
			else
			{
				particle?.Stop(withChildren: true);
			}
		}
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
		return "entity_networked_timer_vfx";
	}
}
