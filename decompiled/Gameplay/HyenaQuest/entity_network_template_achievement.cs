using System;
using Unity.Netcode;
using UnityEngine;

namespace HyenaQuest;

public class entity_network_template_achievement : entity_network_template_base
{
	public STEAM_ACHIEVEMENTS achievement;

	public new void Awake()
	{
		base.Awake();
		if (!NETController.Instance || !NETController.Instance.IsServer)
		{
			UnityEngine.Object.Destroy(base.gameObject);
			return;
		}
		if (!NetController<StatsController>.Instance)
		{
			throw new UnityException("No StatsController found in scene!");
		}
		NetController<StatsController>.Instance.OnAchievementUpdate += new Action<STEAM_ACHIEVEMENTS, bool>(OnAchievementUpdate);
	}

	public new void OnDestroy()
	{
		if ((bool)NetController<StatsController>.Instance)
		{
			NetController<StatsController>.Instance.OnAchievementUpdate -= new Action<STEAM_ACHIEVEMENTS, bool>(OnAchievementUpdate);
		}
		base.OnDestroy();
	}

	public override bool CanSpawn()
	{
		return true;
	}

	private void OnAchievementUpdate(STEAM_ACHIEVEMENTS id, bool unlocked)
	{
		if (achievement == id && unlocked && !_spawnedGameObject)
		{
			(GameObject, NetworkObject) tuple = NetworkSpawn();
			if (!tuple.Item1 || !tuple.Item2)
			{
				throw new UnityException("Failed to spawn achievement object!");
			}
			tuple.Item2.Spawn();
		}
	}
}
