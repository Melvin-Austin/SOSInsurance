using System;
using Unity.Collections;
using Unity.Netcode;

namespace HyenaQuest;

[Serializable]
public struct Contract : INetworkSerializable, IEquatable<Contract>
{
	public FixedString128Bytes name;

	public ContractModifiers modifiers;

	public override bool Equals(object obj)
	{
		if (!(obj is Contract contract))
		{
			return false;
		}
		if (name == contract.name)
		{
			return modifiers == contract.modifiers;
		}
		return false;
	}

	public bool Equals(Contract other)
	{
		if (name == other.name)
		{
			return modifiers == other.modifiers;
		}
		return false;
	}

	public override int GetHashCode()
	{
		return (name, modifiers).GetHashCode();
	}

	public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
	{
		if (serializer.IsReader)
		{
			FastBufferReader fastBufferReader = serializer.GetFastBufferReader();
			fastBufferReader.ReadValueSafe(out name, default(FastBufferWriter.ForFixedStrings));
			fastBufferReader.ReadValueSafe(out modifiers, default(FastBufferWriter.ForEnums));
		}
		else
		{
			FastBufferWriter fastBufferWriter = serializer.GetFastBufferWriter();
			fastBufferWriter.WriteValueSafe(in name, default(FastBufferWriter.ForFixedStrings));
			fastBufferWriter.WriteValueSafe(in modifiers, default(FastBufferWriter.ForEnums));
		}
	}

	public static bool operator ==(Contract a, Contract b)
	{
		return a.Equals(b);
	}

	public static bool operator !=(Contract a, Contract b)
	{
		return !(a == b);
	}
}
