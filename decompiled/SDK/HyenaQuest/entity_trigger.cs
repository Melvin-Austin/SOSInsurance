using UnityEngine;

namespace HyenaQuest;

[RequireComponent(typeof(Collider))]
public class entity_trigger : MonoBehaviour
{
	public LayerMask filters;

	public bool triggerOnce;

	public LayerMask LODMask = 0;

	public GameEvent<Collider> OnEnter = new GameEvent<Collider>();

	public GameEvent<Collider> OnStay = new GameEvent<Collider>();

	public GameEvent<Collider> OnExit = new GameEvent<Collider>();

	protected Collider _trigger;

	public void Awake()
	{
		_trigger = GetComponent<Collider>();
		if (!_trigger)
		{
			_trigger = GetComponentInChildren<Collider>(includeInactive: true);
		}
		if (!_trigger)
		{
			throw new UnityException("entity_trigger requires Collider component");
		}
		SetFilters(filters);
	}

	public void OnTriggerEnter(Collider col)
	{
		if (OnEnter != null && base.isActiveAndEnabled && CheckLOD(col))
		{
			if (triggerOnce)
			{
				base.enabled = false;
			}
			OnEnter.Invoke(col);
		}
	}

	public void OnTriggerStay(Collider col)
	{
		if (OnEnter != null && base.isActiveAndEnabled && CheckLOD(col))
		{
			OnStay.Invoke(col);
		}
	}

	public void OnTriggerExit(Collider col)
	{
		if (OnExit != null && base.isActiveAndEnabled && CheckLOD(col))
		{
			OnExit.Invoke(col);
		}
	}

	public Bounds GetBounds()
	{
		if (!_trigger)
		{
			return default(Bounds);
		}
		Bounds bounds = _trigger.bounds;
		if (_trigger is SphereCollider sphereCollider)
		{
			bounds.size = Vector3.one * sphereCollider.radius * 2f;
		}
		return bounds;
	}

	private void SetFilters(LayerMask filter)
	{
		_trigger.includeLayers = -1;
		_trigger.excludeLayers = ~filter.value;
		_trigger.isTrigger = true;
		filters = filter;
	}

	private bool CheckLOD(Collider col)
	{
		if ((int)LODMask == 0)
		{
			return true;
		}
		Vector3 position = _trigger.transform.position;
		Vector3 vector = col.ClosestPoint(position) - position;
		float magnitude = vector.magnitude;
		vector.Normalize();
		Vector3 halfExtents = new Vector3(0.1f, 0.1f, 0.1f);
		if (Physics.BoxCast(position, halfExtents, vector, out var hitInfo, Quaternion.LookRotation(vector), magnitude, LODMask, QueryTriggerInteraction.Ignore))
		{
			return hitInfo.collider == col;
		}
		return true;
	}
}
