using UnityEngine;

namespace HyenaQuest;

[RequireComponent(typeof(BoxCollider))]
public class entity_movement_volume_static : entity_movement_volume
{
	public new void Awake()
	{
		base.Awake();
		maxFallSpeed = 100f;
		friction = 0f;
		priority = 1;
		waterVolume = false;
	}

	public override VolumeType GetVolumeType()
	{
		return VolumeType.WORLD_STATIC;
	}
}
