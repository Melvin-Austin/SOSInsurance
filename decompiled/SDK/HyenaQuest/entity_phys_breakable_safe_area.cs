using System.Collections.Generic;
using UnityEngine;

namespace HyenaQuest;

[RequireComponent(typeof(Collider))]
public class entity_phys_breakable_safe_area : MonoBehaviour
{
	public static HashSet<entity_phys_breakable_safe_area> NO_BREAK_AREAS = new HashSet<entity_phys_breakable_safe_area>();

	private Collider _collider;

	public void Awake()
	{
		if (!_collider)
		{
			_collider = base.gameObject.GetComponent<Collider>();
		}
		if (!_collider)
		{
			_collider = base.gameObject.GetComponentInChildren<Collider>();
		}
		if (!_collider)
		{
			throw new UnityException("Missing collider");
		}
		NO_BREAK_AREAS.Add(this);
	}

	public void OnDestroy()
	{
		NO_BREAK_AREAS.Remove(this);
	}

	public bool IsInside(Vector3 pos, Bounds bounds)
	{
		if (!_collider.bounds.Contains(pos))
		{
			return _collider.bounds.Intersects(bounds);
		}
		return true;
	}
}
