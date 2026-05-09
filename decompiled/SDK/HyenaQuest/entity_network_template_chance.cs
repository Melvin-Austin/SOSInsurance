using UnityEngine;

namespace HyenaQuest;

public class entity_network_template_chance : entity_network_template_base
{
	[Range(0f, 1f)]
	public float chance;

	public override bool CanSpawn()
	{
		if (!Mathf.Approximately(chance, 1f))
		{
			return Random.value < Mathf.Clamp01(chance);
		}
		return true;
	}
}
