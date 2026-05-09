using System;
using Unity.Netcode;
using UnityEngine;

namespace HyenaQuest;

[Serializable]
public struct HEALTH : INetworkSerializable, IEquatable<HEALTH>
{
	public byte health;

	public DamageType damage;

	public Vector3 damageLocation;

	public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
	{
		if (serializer.IsReader)
		{
			FastBufferReader fastBufferReader = serializer.GetFastBufferReader();
			fastBufferReader.ReadValueSafe(out health, default(FastBufferWriter.ForPrimitives));
			fastBufferReader.ReadValueSafe(out damage, default(FastBufferWriter.ForEnums));
			fastBufferReader.ReadValueSafe(out damageLocation);
		}
		else
		{
			FastBufferWriter fastBufferWriter = serializer.GetFastBufferWriter();
			fastBufferWriter.WriteValueSafe(in health, default(FastBufferWriter.ForPrimitives));
			fastBufferWriter.WriteValueSafe(in damage, default(FastBufferWriter.ForEnums));
			fastBufferWriter.WriteValueSafe(in damageLocation);
		}
	}

	public bool Equals(HEALTH other)
	{
		if (health == other.health)
		{
			return damage == other.damage;
		}
		return false;
	}

	public static bool operator ==(HEALTH left, HEALTH right)
	{
		return left.Equals(right);
	}

	public static bool operator !=(HEALTH left, HEALTH right)
	{
		return !left.Equals(right);
	}

	public override bool Equals(object obj)
	{
		if (obj is HEALTH other)
		{
			return Equals(other);
		}
		return false;
	}

	public override int GetHashCode()
	{
		return HashCode.Combine(health, damage, damageLocation);
	}

	public override string ToString()
	{
		return $"HEALTH: {health}, Last Damage: {damage}, Position: {damageLocation}";
	}
}
