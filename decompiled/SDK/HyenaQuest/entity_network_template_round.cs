using UnityEngine;

namespace HyenaQuest;

public class entity_network_template_round : entity_network_template_chance
{
	[Range(1f, 255f)]
	public byte minRounds;

	public override bool CanSpawn()
	{
		if ((SDK.GetCurrentRound?.Invoke() ?? 1) < minRounds)
		{
			return false;
		}
		return Random.Range(0f, 1f) < chance;
	}
}
