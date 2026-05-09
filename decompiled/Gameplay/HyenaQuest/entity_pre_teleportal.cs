using UnityEngine;

namespace HyenaQuest;

public class entity_pre_teleportal : MonoBehaviour
{
	public entity_pre_teleportal linkedPortal;

	protected Collider _area;

	public void Awake()
	{
		_area = GetComponent<Collider>();
		if (!_area)
		{
			throw new UnityException("Area collider missing!");
		}
	}

	public void FixedUpdate()
	{
		if ((bool)linkedPortal && util_teleportal.ShouldTeleport(_area.bounds))
		{
			util_teleportal.TeleportLocalClient(base.transform, linkedPortal.transform);
		}
	}
}
