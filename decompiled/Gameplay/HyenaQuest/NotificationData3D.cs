using System;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

namespace HyenaQuest;

[Serializable]
public class NotificationData3D : INetworkSerializable
{
	public FixedString128Bytes message = "";

	public Vector3 position;

	public float fadeSpeed = 5f;

	public float scale = 1f;

	public Color startColor = Color.white;

	public Color endColor = Color.white;

	public override bool Equals(object obj)
	{
		if (!(obj is NotificationData3D notificationData3D))
		{
			return false;
		}
		return message == notificationData3D.message;
	}

	public override int GetHashCode()
	{
		return message.GetHashCode();
	}

	public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
	{
		if (serializer.IsReader)
		{
			FastBufferReader fastBufferReader = serializer.GetFastBufferReader();
			fastBufferReader.ReadValueSafe(out message, default(FastBufferWriter.ForFixedStrings));
			fastBufferReader.ReadValueSafe(out position);
			fastBufferReader.ReadValueSafe(out fadeSpeed, default(FastBufferWriter.ForPrimitives));
			fastBufferReader.ReadValueSafe(out scale, default(FastBufferWriter.ForPrimitives));
			fastBufferReader.ReadValueSafe(out startColor);
			fastBufferReader.ReadValueSafe(out endColor);
		}
		else
		{
			FastBufferWriter fastBufferWriter = serializer.GetFastBufferWriter();
			fastBufferWriter.WriteValueSafe(in message, default(FastBufferWriter.ForFixedStrings));
			fastBufferWriter.WriteValueSafe(in position);
			fastBufferWriter.WriteValueSafe(in fadeSpeed, default(FastBufferWriter.ForPrimitives));
			fastBufferWriter.WriteValueSafe(in scale, default(FastBufferWriter.ForPrimitives));
			fastBufferWriter.WriteValueSafe(in startColor);
			fastBufferWriter.WriteValueSafe(in endColor);
		}
	}

	public static bool operator ==(NotificationData3D a, NotificationData3D b)
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

	public static bool operator !=(NotificationData3D a, NotificationData3D b)
	{
		return !(a == b);
	}
}
