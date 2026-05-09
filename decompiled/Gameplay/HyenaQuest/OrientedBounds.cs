using UnityEngine;

namespace HyenaQuest;

public readonly struct OrientedBounds
{
	public readonly Vector3 Center;

	public readonly Vector3 Extents;

	public readonly Quaternion Rotation;

	public OrientedBounds(Vector3 center, Vector3 size, Quaternion rotation)
	{
		Center = center;
		Extents = size * 0.5f;
		Rotation = rotation;
	}

	public OrientedBounds(Bounds bounds, Vector3 position, Quaternion rotation)
	{
		Center = position + rotation * bounds.center;
		Extents = bounds.extents;
		Rotation = rotation;
	}

	public bool Intersects(in OrientedBounds other, float shrinkFactor = 0.98f)
	{
		Vector3 extentsA = Extents * shrinkFactor;
		Vector3 extentsB = other.Extents * shrinkFactor;
		Vector3[] array = new Vector3[3]
		{
			Rotation * Vector3.right,
			Rotation * Vector3.up,
			Rotation * Vector3.forward
		};
		Vector3[] array2 = new Vector3[3]
		{
			other.Rotation * Vector3.right,
			other.Rotation * Vector3.up,
			other.Rotation * Vector3.forward
		};
		Vector3 translation = other.Center - Center;
		for (int i = 0; i < 3; i++)
		{
			if (IsSeparatingAxis(array[i], translation, array, array2, extentsA, extentsB))
			{
				return false;
			}
		}
		for (int j = 0; j < 3; j++)
		{
			if (IsSeparatingAxis(array2[j], translation, array, array2, extentsA, extentsB))
			{
				return false;
			}
		}
		for (int k = 0; k < 3; k++)
		{
			for (int l = 0; l < 3; l++)
			{
				Vector3 vector = Vector3.Cross(array[k], array2[l]);
				if (vector.sqrMagnitude > 0.0001f && IsSeparatingAxis(vector.normalized, translation, array, array2, extentsA, extentsB))
				{
					return false;
				}
			}
		}
		return true;
	}

	private bool IsSeparatingAxis(Vector3 axis, Vector3 translation, Vector3[] axesA, Vector3[] axesB, Vector3 extentsA, Vector3 extentsB)
	{
		float num = Mathf.Abs(Vector3.Dot(axesA[0], axis)) * extentsA.x + Mathf.Abs(Vector3.Dot(axesA[1], axis)) * extentsA.y + Mathf.Abs(Vector3.Dot(axesA[2], axis)) * extentsA.z;
		float num2 = Mathf.Abs(Vector3.Dot(axesB[0], axis)) * extentsB.x + Mathf.Abs(Vector3.Dot(axesB[1], axis)) * extentsB.y + Mathf.Abs(Vector3.Dot(axesB[2], axis)) * extentsB.z;
		return Mathf.Abs(Vector3.Dot(translation, axis)) > num + num2;
	}
}
