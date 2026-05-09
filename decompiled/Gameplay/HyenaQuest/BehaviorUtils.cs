using UnityEngine;

namespace HyenaQuest;

public class BehaviorUtils
{
	private static int _wallMask = -1;

	public static bool IsPlayerLookingAtMonster(entity_player player, Vector3 monsterPosition, Collider monsterCollider, float maxDistance = 15f, float FOV = 95f, int layerMask = -1, float visibilityThreshold = 0.05f)
	{
		if (!player || !player.view)
		{
			return false;
		}
		if (_wallMask == -1)
		{
			_wallMask = LayerMask.GetMask("entity_ground");
		}
		if (layerMask == -1)
		{
			layerMask = _wallMask;
		}
		Transform view = player.view;
		Vector3 position = view.position;
		Vector3 vector = monsterPosition - position;
		float magnitude = vector.magnitude;
		if (magnitude > maxDistance)
		{
			return false;
		}
		Camera component = view.GetComponent<Camera>();
		float num = (component ? component.fieldOfView : FOV);
		if (Vector3.Angle(view.forward, vector.normalized) > num * 0.5f)
		{
			return false;
		}
		float num2 = (component ? component.aspect : 1.7777778f);
		float num3 = num * num2;
		Vector3 vector2 = Vector3.ProjectOnPlane(vector, view.up);
		if (Vector3.Angle(view.forward, vector2.normalized) > num3 * 0.5f)
		{
			return false;
		}
		if (!Physics.Raycast(position, vector.normalized, magnitude, layerMask))
		{
			return true;
		}
		if (!monsterCollider || !monsterCollider.enabled)
		{
			return false;
		}
		Bounds bounds = monsterCollider.bounds;
		Vector3[] array = new Vector3[14]
		{
			bounds.center + new Vector3(0f, bounds.extents.y, 0f),
			bounds.center - new Vector3(0f, bounds.extents.y, 0f),
			bounds.center + new Vector3(bounds.extents.x, 0f, 0f),
			bounds.center - new Vector3(bounds.extents.x, 0f, 0f),
			bounds.center + new Vector3(0f, 0f, bounds.extents.z),
			bounds.center - new Vector3(0f, 0f, bounds.extents.z),
			bounds.center + new Vector3(bounds.extents.x, bounds.extents.y, bounds.extents.z),
			bounds.center + new Vector3(bounds.extents.x, bounds.extents.y, 0f - bounds.extents.z),
			bounds.center + new Vector3(bounds.extents.x, 0f - bounds.extents.y, bounds.extents.z),
			bounds.center + new Vector3(bounds.extents.x, 0f - bounds.extents.y, 0f - bounds.extents.z),
			bounds.center + new Vector3(0f - bounds.extents.x, bounds.extents.y, bounds.extents.z),
			bounds.center + new Vector3(0f - bounds.extents.x, bounds.extents.y, 0f - bounds.extents.z),
			bounds.center + new Vector3(0f - bounds.extents.x, 0f - bounds.extents.y, bounds.extents.z),
			bounds.center + new Vector3(0f - bounds.extents.x, 0f - bounds.extents.y, 0f - bounds.extents.z)
		};
		int num4 = 0;
		Vector3[] array2 = array;
		for (int i = 0; i < array2.Length; i++)
		{
			Vector3 vector3 = array2[i] - position;
			if (Vector3.Angle(view.forward, vector3.normalized) > num * 0.5f)
			{
				continue;
			}
			Vector3 vector4 = Vector3.ProjectOnPlane(vector3, view.up);
			if (!(Vector3.Angle(view.forward, vector4.normalized) > num3 * 0.5f))
			{
				if (!Physics.Raycast(position, vector3.normalized, vector3.magnitude, layerMask))
				{
					num4++;
				}
				if ((float)num4 / (float)array.Length >= visibilityThreshold)
				{
					return true;
				}
			}
		}
		return (float)num4 / (float)array.Length >= visibilityThreshold;
	}
}
