using System;
using Unity.Netcode;

namespace HyenaQuest;

[Serializable]
public struct CurseNetwork : INetworkSerializable, IEquatable<CurseNetwork>
{
	public CURSE_TYPE curseType;

	public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
	{
		if (serializer.IsReader)
		{
			serializer.GetFastBufferReader().ReadValueSafe(out curseType, default(FastBufferWriter.ForEnums));
		}
		else
		{
			serializer.GetFastBufferWriter().WriteValueSafe(in curseType, default(FastBufferWriter.ForEnums));
		}
	}

	public bool Equals(CurseNetwork other)
	{
		return curseType.Equals(other.curseType);
	}

	public override bool Equals(object obj)
	{
		if (obj is CurseNetwork other)
		{
			return Equals(other);
		}
		return false;
	}

	public override int GetHashCode()
	{
		return HashCode.Combine(curseType);
	}
}
