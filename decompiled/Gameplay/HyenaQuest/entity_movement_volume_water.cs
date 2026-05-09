using UnityEngine;

namespace HyenaQuest;

[RequireComponent(typeof(Collider))]
public class entity_movement_volume_water : entity_movement_volume
{
	public bool canSwim = true;

	public new void Awake()
	{
		base.Awake();
		friction = 4f;
		maxFallSpeed = 15f;
		waterVolume = true;
		priority = 10;
	}

	public override VolumeType GetVolumeType()
	{
		return VolumeType.WATER;
	}

	protected override void OnVolumeInternalUpdate(Collider other, bool enter)
	{
	}
}
