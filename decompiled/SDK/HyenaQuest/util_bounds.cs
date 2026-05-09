using UnityEngine;
using UnityEngine.Scripting;

namespace HyenaQuest;

[Preserve]
public static class util_bounds
{
	public static Bounds GetWorldBounds(Bounds bounds, Transform world)
	{
		if (!world)
		{
			return bounds;
		}
		Vector3[] localCorners = GetLocalCorners(bounds);
		Vector3 vector = Vector3.positiveInfinity;
		Vector3 vector2 = Vector3.negativeInfinity;
		Vector3[] array = localCorners;
		foreach (Vector3 position in array)
		{
			Vector3 rhs = world.TransformPoint(position);
			vector = Vector3.Min(vector, rhs);
			vector2 = Vector3.Max(vector2, rhs);
		}
		return new Bounds((vector + vector2) * 0.5f, vector2 - vector);
	}

	private static Vector3[] GetLocalCorners(Bounds bounds)
	{
		Vector3 center = bounds.center;
		Vector3 extents = bounds.extents;
		return new Vector3[8]
		{
			center + new Vector3(0f - extents.x, 0f - extents.y, 0f - extents.z),
			center + new Vector3(extents.x, 0f - extents.y, 0f - extents.z),
			center + new Vector3(0f - extents.x, extents.y, 0f - extents.z),
			center + new Vector3(extents.x, extents.y, 0f - extents.z),
			center + new Vector3(0f - extents.x, 0f - extents.y, extents.z),
			center + new Vector3(extents.x, 0f - extents.y, extents.z),
			center + new Vector3(0f - extents.x, extents.y, extents.z),
			center + new Vector3(extents.x, extents.y, extents.z)
		};
	}
}
