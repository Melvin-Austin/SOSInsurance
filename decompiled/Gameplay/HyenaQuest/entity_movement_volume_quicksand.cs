using UnityEngine;

namespace HyenaQuest;

[RequireComponent(typeof(BoxCollider))]
public class entity_movement_volume_quicksand : entity_movement_volume
{
	public new void Awake()
	{
		base.Awake();
		maxFallSpeed = 1E-05f;
		friction = 200f;
		waterVolume = true;
		priority = 5;
	}

	public override VolumeType GetVolumeType()
	{
		return VolumeType.QUICKSAND;
	}
}
