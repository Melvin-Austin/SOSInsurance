using System;
using UnityEngine;

namespace HyenaQuest;

[Serializable]
public struct Point : IEquatable<Point>
{
	public Vector3 pos;

	public bool smoothPos;

	public Vector3 angle;

	public bool smoothAngle;

	public Vector2 delay;

	[Range(0f, 2f)]
	public float speedModifier;

	public bool Equals(Point other)
	{
		if (pos.Equals(other.pos) && smoothPos == other.smoothPos && angle.Equals(other.angle) && smoothAngle == other.smoothAngle && delay.Equals(other.delay))
		{
			return speedModifier.Equals(other.speedModifier);
		}
		return false;
	}

	public override bool Equals(object obj)
	{
		if (obj is Point other)
		{
			return Equals(other);
		}
		return false;
	}

	public static bool operator ==(Point a, Point b)
	{
		return a.Equals(b);
	}

	public static bool operator !=(Point a, Point b)
	{
		return !(a == b);
	}

	public override int GetHashCode()
	{
		return HashCode.Combine(pos, smoothPos, angle, smoothAngle, delay, speedModifier);
	}
}
