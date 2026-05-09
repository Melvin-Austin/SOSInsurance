using System;
using Unity.Netcode;
using UnityEngine;

namespace HyenaQuest;

[Serializable]
public struct GLASS_HEALTH : INetworkSerializable, IEquatable<GLASS_HEALTH>
{
	public GLASS_STATUS status;

	public Vector3 hitPosition;

	public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
	{
		if (serializer.IsReader)
		{
			FastBufferReader fastBufferReader = serializer.GetFastBufferReader();
			fastBufferReader.ReadValueSafe(out status, default(FastBufferWriter.ForEnums));
			fastBufferReader.ReadValueSafe(out hitPosition);
		}
		else
		{
			FastBufferWriter fastBufferWriter = serializer.GetFastBufferWriter();
			fastBufferWriter.WriteValueSafe(in status, default(FastBufferWriter.ForEnums));
			fastBufferWriter.WriteValueSafe(in hitPosition);
		}
	}

	public bool Equals(GLASS_HEALTH other)
	{
		if (status == other.status)
		{
			return hitPosition == other.hitPosition;
		}
		return false;
	}

	public static bool operator ==(GLASS_HEALTH left, GLASS_HEALTH right)
	{
		return left.Equals(right);
	}

	public static bool operator !=(GLASS_HEALTH left, GLASS_HEALTH right)
	{
		return !left.Equals(right);
	}

	public override bool Equals(object obj)
	{
		if (obj is GLASS_HEALTH other)
		{
			return Equals(other);
		}
		return false;
	}

	public override int GetHashCode()
	{
		return HashCode.Combine(status, hitPosition);
	}
}
