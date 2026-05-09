using UnityEngine;

namespace HyenaQuest;

public class entity_attractor : MonoBehaviour
{
	[Range(-1000f, 1000f)]
	public float force;

	[Range(0f, 100f)]
	public float distance;

	public LayerMask layers;

	public LayerMask LODMask = 0;

	public BoxCollider shape;

	public Vector3 direction;

	public bool forceItemDrop;

	private readonly Collider[] _hitColliders = new Collider[512];

	private bool HasShape => shape;

	private bool HasPhysLayer => ((int)layers & LayerMask.GetMask("entity_phys", "entity_phys_item")) != 0;

	public void FixedUpdate()
	{
		if (base.isActiveAndEnabled)
		{
			Push();
		}
	}

	public void Push()
	{
		if (force == 0f)
		{
			return;
		}
		int num = ((!shape) ? Physics.OverlapSphereNonAlloc(base.transform.position, distance, _hitColliders, layers) : Physics.OverlapBoxNonAlloc(shape.transform.TransformPoint(shape.center), Vector3.Scale(shape.size * 0.5f, shape.transform.lossyScale), orientation: shape.transform.rotation, results: _hitColliders, mask: layers));
		for (int i = 0; i < num; i++)
		{
			Collider collider = _hitColliders[i];
			if (!collider)
			{
				continue;
			}
			if ((int)LODMask != 0)
			{
				Vector3 vector = (collider.attachedRigidbody ? collider.attachedRigidbody.position : collider.transform.position) - base.transform.position;
				if (Physics.Raycast(base.transform.position, vector.normalized, vector.magnitude, LODMask))
				{
					continue;
				}
			}
			if (collider.TryGetComponent<entity_player>(out var component))
			{
				entity_player_movement movement = component.GetMovement();
				if ((bool)movement)
				{
					if ((bool)shape)
					{
						Vector3 vector2 = base.transform.TransformDirection(direction.normalized);
						movement.AddForce(vector2 * force);
					}
					else if (force > 0f)
					{
						movement.AddExplosionForce(force, base.transform.position, distance, 0.5f, ForceMode.VelocityChange);
					}
					else
					{
						Vector3 normalized = (component.transform.position - base.transform.position).normalized;
						movement.AddForce(normalized * force);
					}
				}
				continue;
			}
			Rigidbody attachedRigidbody = collider.attachedRigidbody;
			if ((bool)attachedRigidbody)
			{
				if (forceItemDrop && collider.TryGetComponent<entity_phys>(out var component2) && component2.IsPhysOwner(PlayerController.LOCAL))
				{
					PlayerController.LOCAL.CancelGrabbing();
				}
				if ((bool)shape)
				{
					Vector3 vector3 = base.transform.TransformDirection(direction.normalized);
					attachedRigidbody.AddForce(vector3 * (force * attachedRigidbody.mass), ForceMode.VelocityChange);
				}
				else if (force > 0f)
				{
					attachedRigidbody.AddExplosionForce(force, base.transform.position, distance, 0f, ForceMode.VelocityChange);
				}
				else
				{
					Vector3 normalized2 = (attachedRigidbody.position - base.transform.position).normalized;
					attachedRigidbody.AddForce(normalized2 * force);
				}
			}
		}
	}
}
