using System;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Scripting;

namespace HyenaQuest;

[Serializable]
[Preserve]
public struct SunSettings : INetworkSerializable, IEquatable<SunSettings>
{
	public float intensity;

	public Vector3 angle;

	public Color color;

	public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
	{
		if (serializer.IsReader)
		{
			FastBufferReader fastBufferReader = serializer.GetFastBufferReader();
			fastBufferReader.ReadValueSafe(out intensity, default(FastBufferWriter.ForPrimitives));
			fastBufferReader.ReadValueSafe(out angle);
			fastBufferReader.ReadValueSafe(out color);
		}
		else
		{
			FastBufferWriter fastBufferWriter = serializer.GetFastBufferWriter();
			fastBufferWriter.WriteValueSafe(in intensity, default(FastBufferWriter.ForPrimitives));
			fastBufferWriter.WriteValueSafe(in angle);
			fastBufferWriter.WriteValueSafe(in color);
		}
	}

	public bool Equals(SunSettings other)
	{
		if (Mathf.Approximately(intensity, other.intensity) && angle == other.angle)
		{
			return color == other.color;
		}
		return false;
	}

	public static bool operator ==(SunSettings left, SunSettings right)
	{
		return left.Equals(right);
	}

	public static bool operator !=(SunSettings left, SunSettings right)
	{
		return !left.Equals(right);
	}

	public override bool Equals(object obj)
	{
		if (obj is SunSettings other)
		{
			return Equals(other);
		}
		return false;
	}

	public override int GetHashCode()
	{
		return HashCode.Combine(intensity, angle, color);
	}
}
