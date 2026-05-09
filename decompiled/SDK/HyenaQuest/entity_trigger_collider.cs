using System.Collections.Generic;
using UnityEngine;

namespace HyenaQuest;

[RequireComponent(typeof(Collider))]
[RequireComponent(typeof(Rigidbody))]
public class entity_trigger_collider : MonoBehaviour
{
	public LayerMask filters;

	public bool isEnabled = true;

	public GameEvent<Collision> OnEnter = new GameEvent<Collision>();

	public GameEvent<Collision> OnExit = new GameEvent<Collision>();

	private readonly List<Collision> _colliders = new List<Collision>();

	private Collider _collision;

	private Rigidbody _body;

	public void Awake()
	{
		_body = GetComponent<Rigidbody>();
		if (!_body)
		{
			throw new UnityException("entity_trigger_collider requires Rigidbody component");
		}
		_collision = GetComponent<Collider>();
		if (!_collision)
		{
			throw new UnityException("entity_trigger_collider requires Collider component");
		}
		SetFilters(filters);
	}

	public void SetEnabled(bool enable)
	{
		isEnabled = enable;
	}

	public void SetFilters(LayerMask filter)
	{
		if ((bool)_collision && (bool)_body)
		{
			_collision.includeLayers = -1;
			_collision.excludeLayers = ~filter.value;
			_collision.isTrigger = false;
			_body.includeLayers = -1;
			_body.excludeLayers = ~filter.value;
			_body.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
			_body.isKinematic = true;
			_body.useGravity = false;
			_body.constraints = RigidbodyConstraints.FreezeRotation;
			filters = filter;
		}
	}

	public void OnCollisionStay(Collision col)
	{
		if (isEnabled && !_colliders.Contains(col))
		{
			_colliders.Add(col);
			OnEnter?.Invoke(col);
		}
	}

	public void OnCollisionExit(Collision col)
	{
		if (_colliders.Contains(col))
		{
			_colliders?.Remove(col);
			OnExit?.Invoke(col);
		}
	}

	public List<Collision> GetColliders()
	{
		return _colliders;
	}
}
