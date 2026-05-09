using System;
using Unity.Netcode;
using UnityEngine;

namespace HyenaQuest;

[Serializable]
public struct EffectSettings : INetworkSerializable
{
	public int count;

	public float delay;

	public float chance;

	public bool playSound;

	public float volume;

	public EffectSettings(int count, float delay, float chance, bool playSound, float volume)
	{
		this.count = count;
		this.delay = delay;
		this.chance = chance;
		this.playSound = playSound;
		this.volume = volume;
	}

	public EffectSettings(int count, bool playSound)
	{
		this.count = count;
		delay = 0f;
		chance = 0f;
		volume = 1f;
		this.playSound = playSound;
	}

	public override bool Equals(object obj)
	{
		if (!(obj is EffectSettings effectSettings))
		{
			return false;
		}
		if (count == effectSettings.count && Mathf.Approximately(delay, effectSettings.delay) && Mathf.Approximately(chance, effectSettings.chance) && playSound == effectSettings.playSound)
		{
			return Mathf.Approximately(volume, effectSettings.volume);
		}
		return false;
	}

	public override int GetHashCode()
	{
		return (count, delay, chance, playSound, volume).GetHashCode();
	}

	public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
	{
		if (serializer.IsReader)
		{
			FastBufferReader fastBufferReader = serializer.GetFastBufferReader();
			fastBufferReader.ReadValueSafe(out count, default(FastBufferWriter.ForPrimitives));
			fastBufferReader.ReadValueSafe(out delay, default(FastBufferWriter.ForPrimitives));
			fastBufferReader.ReadValueSafe(out chance, default(FastBufferWriter.ForPrimitives));
			fastBufferReader.ReadValueSafe(out playSound, default(FastBufferWriter.ForPrimitives));
			fastBufferReader.ReadValueSafe(out volume, default(FastBufferWriter.ForPrimitives));
		}
		else
		{
			FastBufferWriter fastBufferWriter = serializer.GetFastBufferWriter();
			fastBufferWriter.WriteValueSafe(in count, default(FastBufferWriter.ForPrimitives));
			fastBufferWriter.WriteValueSafe(in delay, default(FastBufferWriter.ForPrimitives));
			fastBufferWriter.WriteValueSafe(in chance, default(FastBufferWriter.ForPrimitives));
			fastBufferWriter.WriteValueSafe(in playSound, default(FastBufferWriter.ForPrimitives));
			fastBufferWriter.WriteValueSafe(in volume, default(FastBufferWriter.ForPrimitives));
		}
	}

	public static bool operator ==(EffectSettings a, EffectSettings b)
	{
		if (a.count == b.count && Mathf.Approximately(a.delay, b.delay) && Mathf.Approximately(a.chance, b.chance) && a.playSound == b.playSound)
		{
			return Mathf.Approximately(a.volume, b.volume);
		}
		return false;
	}

	public static bool operator !=(EffectSettings a, EffectSettings b)
	{
		return !(a == b);
	}
}
