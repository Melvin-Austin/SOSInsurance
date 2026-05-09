using System;
using System.Collections.Generic;
using UnityEngine;

namespace HyenaQuest;

[Serializable]
public struct SaveDataItems : IEquatable<SaveDataItems>
{
	public string id;

	public Vector3 position;

	public Quaternion rotation;

	public Dictionary<string, string> data;

	public bool Equals(SaveDataItems other)
	{
		if (id == other.id && position.Equals(other.position) && rotation.Equals(other.rotation))
		{
			return object.Equals(data, other.data);
		}
		return false;
	}

	public override bool Equals(object obj)
	{
		if (obj is SaveDataItems other)
		{
			return Equals(other);
		}
		return false;
	}

	public override int GetHashCode()
	{
		return HashCode.Combine(id, position, rotation, data);
	}

	public static bool operator ==(SaveDataItems left, SaveDataItems right)
	{
		return left.Equals(right);
	}

	public static bool operator !=(SaveDataItems left, SaveDataItems right)
	{
		return !left.Equals(right);
	}
}
