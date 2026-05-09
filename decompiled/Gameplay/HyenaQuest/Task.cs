using System;
using Unity.Netcode;

namespace HyenaQuest;

[Serializable]
public struct Task : INetworkSerializable, IEquatable<Task>
{
	public int ID;

	public int Reward;

	public byte DeliveryPrefabIndex;

	public int Address;

	public int ScrapRequired;

	public bool HasDeliveryItem;

	public override bool Equals(object obj)
	{
		if (obj == null)
		{
			return false;
		}
		if (obj.GetType() == GetType())
		{
			return Equals((Task)obj);
		}
		return false;
	}

	public override int GetHashCode()
	{
		return (ID, HasDeliveryItem).GetHashCode();
	}

	public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
	{
		if (serializer.IsReader)
		{
			FastBufferReader fastBufferReader = serializer.GetFastBufferReader();
			fastBufferReader.ReadValueSafe(out ID, default(FastBufferWriter.ForPrimitives));
			fastBufferReader.ReadValueSafe(out Reward, default(FastBufferWriter.ForPrimitives));
			fastBufferReader.ReadValueSafe(out DeliveryPrefabIndex, default(FastBufferWriter.ForPrimitives));
			fastBufferReader.ReadValueSafe(out Address, default(FastBufferWriter.ForPrimitives));
			fastBufferReader.ReadValueSafe(out ScrapRequired, default(FastBufferWriter.ForPrimitives));
			fastBufferReader.ReadValueSafe(out HasDeliveryItem, default(FastBufferWriter.ForPrimitives));
		}
		else
		{
			FastBufferWriter fastBufferWriter = serializer.GetFastBufferWriter();
			fastBufferWriter.WriteValueSafe(in ID, default(FastBufferWriter.ForPrimitives));
			fastBufferWriter.WriteValueSafe(in Reward, default(FastBufferWriter.ForPrimitives));
			fastBufferWriter.WriteValueSafe(in DeliveryPrefabIndex, default(FastBufferWriter.ForPrimitives));
			fastBufferWriter.WriteValueSafe(in Address, default(FastBufferWriter.ForPrimitives));
			fastBufferWriter.WriteValueSafe(in ScrapRequired, default(FastBufferWriter.ForPrimitives));
			fastBufferWriter.WriteValueSafe(in HasDeliveryItem, default(FastBufferWriter.ForPrimitives));
		}
	}

	public static bool operator ==(Task a, Task b)
	{
		return a.Equals(b);
	}

	public static bool operator !=(Task a, Task b)
	{
		return !(a == b);
	}

	public bool Equals(Task other)
	{
		if (ID == other.ID)
		{
			return HasDeliveryItem == other.HasDeliveryItem;
		}
		return false;
	}
}
