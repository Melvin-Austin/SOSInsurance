using System.Collections.Generic;
using UnityEngine;

namespace HyenaQuest;

public class entity_room_world : MonoBehaviour
{
	public List<GameObject> backgrounds;

	public Vector3 velocity;

	public MeshFilter boundsFilter;

	public void Awake()
	{
		List<GameObject> list = backgrounds;
		if (list == null || list.Count <= 0)
		{
			throw new UnityException("Missing backgrounds");
		}
		if (!boundsFilter)
		{
			throw new UnityException("Missing boundsFilter");
		}
		if (backgrounds.Count < 2)
		{
			throw new UnityException("Not enough backgrounds assigned to entity_world_background, need at least 2");
		}
	}

	public void Update()
	{
		if (!boundsFilter || backgrounds == null || backgrounds.Count == 0 || velocity == Vector3.zero)
		{
			return;
		}
		Bounds bounds = boundsFilter.sharedMesh.bounds;
		foreach (GameObject background in backgrounds)
		{
			if ((bool)background)
			{
				Vector3 position = background.transform.position;
				position += velocity * Time.deltaTime;
				if (position.x <= base.transform.position.x - bounds.size.x)
				{
					position.x += bounds.size.x * (float)backgrounds.Count;
				}
				if (position.y <= base.transform.position.y - bounds.size.y)
				{
					position.y += bounds.size.y * (float)backgrounds.Count;
				}
				if (position.z <= base.transform.position.z - bounds.size.z)
				{
					position.z += bounds.size.z * (float)backgrounds.Count;
				}
				background.transform.position = position;
			}
		}
	}
}
