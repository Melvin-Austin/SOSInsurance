using System;
using Unity.Collections;
using Unity.Netcode;

namespace HyenaQuest;

[Serializable]
public class NotificationData : INetworkSerializable
{
	public FixedString128Bytes id = "";

	public FixedString512Bytes text = "";

	public float duration;

	public FixedString128Bytes soundEffect = "";

	public float soundVolume = 0.25f;

	public float soundPitch = 1f;

	public byte detailedIndex = byte.MaxValue;

	public override bool Equals(object obj)
	{
		if (!(obj is NotificationData notificationData))
		{
			return false;
		}
		return id == notificationData.id;
	}

	public override int GetHashCode()
	{
		return id.GetHashCode();
	}

	public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
	{
		if (serializer.IsReader)
		{
			FastBufferReader fastBufferReader = serializer.GetFastBufferReader();
			fastBufferReader.ReadValueSafe(out id, default(FastBufferWriter.ForFixedStrings));
			fastBufferReader.ReadValueSafe(out text, default(FastBufferWriter.ForFixedStrings));
			fastBufferReader.ReadValueSafe(out duration, default(FastBufferWriter.ForPrimitives));
			fastBufferReader.ReadValueSafe(out soundEffect, default(FastBufferWriter.ForFixedStrings));
			fastBufferReader.ReadValueSafe(out soundVolume, default(FastBufferWriter.ForPrimitives));
			fastBufferReader.ReadValueSafe(out soundPitch, default(FastBufferWriter.ForPrimitives));
			fastBufferReader.ReadValueSafe(out detailedIndex, default(FastBufferWriter.ForPrimitives));
		}
		else
		{
			FastBufferWriter fastBufferWriter = serializer.GetFastBufferWriter();
			fastBufferWriter.WriteValueSafe(in id, default(FastBufferWriter.ForFixedStrings));
			fastBufferWriter.WriteValueSafe(in text, default(FastBufferWriter.ForFixedStrings));
			fastBufferWriter.WriteValueSafe(in duration, default(FastBufferWriter.ForPrimitives));
			fastBufferWriter.WriteValueSafe(in soundEffect, default(FastBufferWriter.ForFixedStrings));
			fastBufferWriter.WriteValueSafe(in soundVolume, default(FastBufferWriter.ForPrimitives));
			fastBufferWriter.WriteValueSafe(in soundPitch, default(FastBufferWriter.ForPrimitives));
			fastBufferWriter.WriteValueSafe(in detailedIndex, default(FastBufferWriter.ForPrimitives));
		}
	}

	public static bool operator ==(NotificationData a, NotificationData b)
	{
		if ((object)a == b)
		{
			return true;
		}
		if ((object)a == null || (object)b == null)
		{
			return false;
		}
		return a.Equals(b);
	}

	public static bool operator !=(NotificationData a, NotificationData b)
	{
		return !(a == b);
	}

	public override string ToString()
	{
		return $"{id}\n{text.ToString()}\n{duration}\n{detailedIndex}";
	}
}
