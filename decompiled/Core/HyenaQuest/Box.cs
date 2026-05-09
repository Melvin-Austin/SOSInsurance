using UnityEngine;

namespace HyenaQuest;

public struct Box
{
	public Vector3 localFrontTopLeft { get; private set; }

	public Vector3 localFrontTopRight { get; private set; }

	public Vector3 localFrontBottomLeft { get; private set; }

	public Vector3 localFrontBottomRight { get; private set; }

	public Vector3 localBackTopLeft => -localFrontBottomRight;

	public Vector3 localBackTopRight => -localFrontBottomLeft;

	public Vector3 localBackBottomLeft => -localFrontTopRight;

	public Vector3 localBackBottomRight => -localFrontTopLeft;

	public Vector3 frontTopLeft => localFrontTopLeft + origin;

	public Vector3 frontTopRight => localFrontTopRight + origin;

	public Vector3 frontBottomLeft => localFrontBottomLeft + origin;

	public Vector3 frontBottomRight => localFrontBottomRight + origin;

	public Vector3 backTopLeft => localBackTopLeft + origin;

	public Vector3 backTopRight => localBackTopRight + origin;

	public Vector3 backBottomLeft => localBackBottomLeft + origin;

	public Vector3 backBottomRight => localBackBottomRight + origin;

	public Vector3 origin { get; }

	public Box(Vector3 origin, Vector3 halfExtents, Quaternion orientation)
		: this(origin, halfExtents)
	{
		Rotate(orientation);
	}

	public Box(Vector3 origin, Vector3 halfExtents)
	{
		localFrontTopLeft = new Vector3(0f - halfExtents.x, halfExtents.y, 0f - halfExtents.z);
		localFrontTopRight = new Vector3(halfExtents.x, halfExtents.y, 0f - halfExtents.z);
		localFrontBottomLeft = new Vector3(0f - halfExtents.x, 0f - halfExtents.y, 0f - halfExtents.z);
		localFrontBottomRight = new Vector3(halfExtents.x, 0f - halfExtents.y, 0f - halfExtents.z);
		this.origin = origin;
	}

	public void Rotate(Quaternion orientation)
	{
		localFrontTopLeft = RotatePointAroundPivot(localFrontTopLeft, Vector3.zero, orientation);
		localFrontTopRight = RotatePointAroundPivot(localFrontTopRight, Vector3.zero, orientation);
		localFrontBottomLeft = RotatePointAroundPivot(localFrontBottomLeft, Vector3.zero, orientation);
		localFrontBottomRight = RotatePointAroundPivot(localFrontBottomRight, Vector3.zero, orientation);
	}

	private static Vector3 RotatePointAroundPivot(Vector3 point, Vector3 pivot, Quaternion rotation)
	{
		Vector3 vector = point - pivot;
		return pivot + rotation * vector;
	}

	public bool Contains(Vector3 point)
	{
		Vector3 lhs = point - origin;
		Vector3 normalized = (localFrontTopRight - localFrontTopLeft).normalized;
		Vector3 normalized2 = (localFrontTopLeft - localFrontBottomLeft).normalized;
		Vector3 normalized3 = (localBackTopLeft - localFrontTopLeft).normalized;
		float num = Vector3.Distance(localFrontTopLeft, localFrontTopRight) * 0.5f;
		float num2 = Vector3.Distance(localFrontTopLeft, localFrontBottomLeft) * 0.5f;
		float num3 = Vector3.Distance(localFrontTopLeft, localBackTopLeft) * 0.5f;
		float f = Vector3.Dot(lhs, normalized);
		float f2 = Vector3.Dot(lhs, normalized2);
		float f3 = Vector3.Dot(lhs, normalized3);
		if (Mathf.Abs(f) <= num && Mathf.Abs(f2) <= num2)
		{
			return Mathf.Abs(f3) <= num3;
		}
		return false;
	}
}
