using System;
using Unity.Netcode;
using UnityEngine;

namespace HyenaQuest;

[Serializable]
public class AudioData : INetworkSerializable
{
	public float pitch = 1f;

	public float volume = 1f;

	public float distance = 5f;

	public NetworkBehaviourReference parent;

	public SoundMixer mixer = SoundMixer.SFX;

	public override bool Equals(object obj)
	{
		if (!(obj is AudioData))
		{
			return false;
		}
		AudioData audioData = (AudioData)obj;
		if (Mathf.Approximately(pitch, audioData.pitch) && Mathf.Approximately(volume, audioData.volume) && Mathf.Approximately(distance, audioData.distance) && mixer == audioData.mixer)
		{
			return parent.Equals(audioData.parent);
		}
		return false;
	}

	public override int GetHashCode()
	{
		return HashCode.Combine(pitch, volume, distance, parent, (int)mixer);
	}

	public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
	{
		if (serializer.IsReader)
		{
			FastBufferReader fastBufferReader = serializer.GetFastBufferReader();
			fastBufferReader.ReadValueSafe(out pitch, default(FastBufferWriter.ForPrimitives));
			fastBufferReader.ReadValueSafe(out volume, default(FastBufferWriter.ForPrimitives));
			fastBufferReader.ReadValueSafe(out distance, default(FastBufferWriter.ForPrimitives));
			fastBufferReader.ReadValueSafe(out mixer, default(FastBufferWriter.ForEnums));
			fastBufferReader.ReadValueSafe(out parent, default(FastBufferWriter.ForNetworkSerializable));
		}
		else
		{
			FastBufferWriter fastBufferWriter = serializer.GetFastBufferWriter();
			fastBufferWriter.WriteValueSafe(in pitch, default(FastBufferWriter.ForPrimitives));
			fastBufferWriter.WriteValueSafe(in volume, default(FastBufferWriter.ForPrimitives));
			fastBufferWriter.WriteValueSafe(in distance, default(FastBufferWriter.ForPrimitives));
			fastBufferWriter.WriteValueSafe(in mixer, default(FastBufferWriter.ForEnums));
			fastBufferWriter.WriteValueSafe(in parent, default(FastBufferWriter.ForNetworkSerializable));
		}
	}

	public static bool operator ==(AudioData a, AudioData b)
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

	public static bool operator !=(AudioData a, AudioData b)
	{
		return !(a == b);
	}
}
