using UnityEngine;

namespace HyenaQuest;

[RequireComponent(typeof(BoxCollider))]
public class entity_teleportal_trapper : MonoBehaviour
{
	[Header("Settings")]
	public LayerMask mask;

	private BoxCollider _trapArea;

	private readonly Collider[] _players = new Collider[NETController.MAX_PLAYERS];

	public void Awake()
	{
		_trapArea = GetComponent<BoxCollider>();
		if (!_trapArea)
		{
			throw new UnityException("Trap area collider missing!");
		}
	}

	public void FixedUpdate()
	{
		if (!_trapArea || Physics.OverlapBoxNonAlloc(_trapArea.bounds.center, _trapArea.bounds.extents, _players, Quaternion.identity, mask) == 0)
		{
			return;
		}
		Vector3 center = _trapArea.bounds.center;
		Vector3 size = _trapArea.bounds.size;
		Collider[] players = _players;
		foreach (Collider collider in players)
		{
			if ((bool)collider)
			{
				Vector3 position = collider.transform.position;
				bool flag = false;
				if (position.x > center.x + size.x / 2f)
				{
					position.x = center.x - size.x / 2f;
					flag = true;
				}
				else if (position.x < center.x - size.x / 2f)
				{
					position.x = center.x + size.x / 2f;
					flag = true;
				}
				if (position.z > center.z + size.z / 2f)
				{
					position.z = center.z - size.z / 2f;
					flag = true;
				}
				else if (position.z < center.z - size.z / 2f)
				{
					position.z = center.z + size.z / 2f;
					flag = true;
				}
				if (flag)
				{
					collider.transform.position = position;
					Physics.SyncTransforms();
				}
			}
		}
	}
}
