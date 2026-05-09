using UnityEngine;

namespace HyenaQuest;

public class MathUtils
{
	public static float ClampAngle(float a, float min, float max)
	{
		while (max < min)
		{
			max += 360f;
		}
		while (a > max)
		{
			a -= 360f;
		}
		while (a < min)
		{
			a += 360f;
		}
		if (!(a > max))
		{
			return a;
		}
		if (!(a - (max + min) * 0.5f < 180f))
		{
			return min;
		}
		return max;
	}

	public static Vector3 Floor(Vector3 v, int decimals)
	{
		float num = Mathf.Pow(10f, decimals);
		return new Vector3(Mathf.Floor(v.x * num) / num, Mathf.Floor(v.y * num) / num, Mathf.Floor(v.z * num) / num);
	}

	public static int SnapAngle(float angle)
	{
		return Mathf.RoundToInt(angle / 90f) * 90;
	}
}
