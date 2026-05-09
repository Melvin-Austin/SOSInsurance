using System;
using UnityEngine;

namespace HyenaQuest;

public struct AffectorData : IEquatable<AffectorData>
{
	public Collider collider;

	public entity_volume_affector affector;

	public bool Equals(AffectorData other)
	{
		if (object.Equals(collider, other.collider))
		{
			return object.Equals(affector, other.affector);
		}
		return false;
	}

	public override bool Equals(object obj)
	{
		if (obj is AffectorData other)
		{
			return Equals(other);
		}
		return false;
	}

	public override int GetHashCode()
	{
		return HashCode.Combine(collider, affector);
	}
}
