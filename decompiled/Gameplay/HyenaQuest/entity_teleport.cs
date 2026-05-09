using UnityEngine;

namespace HyenaQuest;

public class entity_teleport : MonoBehaviour
{
	public LayerMask mask;

	public BoxCollider start;

	public BoxCollider end;

	private readonly Collider[] _results = new Collider[10];

	public void Awake()
	{
		if (!start)
		{
			throw new UnityException("Missing start");
		}
		if (!end)
		{
			throw new UnityException("Missing end");
		}
	}

	public void Teleport()
	{
		if (!start || !end)
		{
			throw new UnityException("Start or End collider not assigned");
		}
		if (Physics.OverlapBoxNonAlloc(base.transform.position, start.size, _results, Quaternion.identity, mask) <= 0)
		{
			return;
		}
		Collider[] results = _results;
		foreach (Collider collider in results)
		{
			Vector3 vector = start.transform.InverseTransformPoint(collider.transform.position);
			Vector3 vector2 = new Vector3((vector.x - start.bounds.min.x) / start.bounds.size.x, (vector.y - start.bounds.min.y) / start.bounds.size.y, (vector.z - start.bounds.min.z) / start.bounds.size.z);
			Vector3 position = new Vector3(end.bounds.min.x + vector2.x * end.bounds.size.x, end.bounds.min.y + vector2.y * end.bounds.size.y, end.bounds.min.z + vector2.z * end.bounds.size.z);
			Vector3 vector3 = end.transform.TransformPoint(position);
			if (collider.TryGetComponent<entity_player>(out var component))
			{
				component.SetPositionRPC(vector3);
				continue;
			}
			collider.transform.position = vector3;
			Physics.SyncTransforms();
		}
	}
}
