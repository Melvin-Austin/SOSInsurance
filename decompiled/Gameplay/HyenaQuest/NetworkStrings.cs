using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Netcode;

namespace HyenaQuest;

[Serializable]
[GenerateSerializationForGenericParameter(0)]
public struct NetworkStrings : INetworkSerializable
{
	public FixedString512Bytes[] data;

	public NetworkStrings(FixedString512Bytes[] data)
	{
		this.data = data;
	}

	public NetworkStrings(List<string> data)
	{
		this.data = new FixedString512Bytes[data.Count];
		for (int i = 0; i < data.Count; i++)
		{
			this.data[i] = data[i];
		}
	}

	public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
	{
		if (serializer.IsWriter)
		{
			serializer.GetFastBufferWriter().WriteValueSafe<int>(data.Length, default(FastBufferWriter.ForPrimitives));
			FixedString512Bytes[] array = data;
			for (int i = 0; i < array.Length; i++)
			{
				FixedString512Bytes value = array[i];
				serializer.GetFastBufferWriter().WriteValueSafe(in value, default(FastBufferWriter.ForFixedStrings));
			}
		}
		else
		{
			serializer.GetFastBufferReader().ReadValueSafe(out int value2, default(FastBufferWriter.ForPrimitives));
			data = new FixedString512Bytes[value2];
			for (int j = 0; j < value2; j++)
			{
				serializer.GetFastBufferReader().ReadValueSafe(out data[j], default(FastBufferWriter.ForFixedStrings));
			}
		}
	}
}
