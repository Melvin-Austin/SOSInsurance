using System;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Scripting;

namespace HyenaQuest;

[Serializable]
[Preserve]
public struct EntryRenderSettings : INetworkSerializable, IEquatable<EntryRenderSettings>
{
	public bool exterior;

	public Vector3 doorOpenAngle;

	public Vector3 shipOffset;

	public SunSettings sun;

	public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
	{
		if (serializer.IsReader)
		{
			FastBufferReader fastBufferReader = serializer.GetFastBufferReader();
			fastBufferReader.ReadValueSafe(out exterior, default(FastBufferWriter.ForPrimitives));
			fastBufferReader.ReadValueSafe(out doorOpenAngle);
			fastBufferReader.ReadValueSafe(out shipOffset);
			fastBufferReader.ReadValueSafe(out sun, default(FastBufferWriter.ForNetworkSerializable));
		}
		else
		{
			FastBufferWriter fastBufferWriter = serializer.GetFastBufferWriter();
			fastBufferWriter.WriteValueSafe(in exterior, default(FastBufferWriter.ForPrimitives));
			fastBufferWriter.WriteValueSafe(in doorOpenAngle);
			fastBufferWriter.WriteValueSafe(in shipOffset);
			fastBufferWriter.WriteValueSafe(in sun, default(FastBufferWriter.ForNetworkSerializable));
		}
	}

	public bool Equals(EntryRenderSettings other)
	{
		if (doorOpenAngle == other.doorOpenAngle && shipOffset == other.shipOffset && exterior == other.exterior)
		{
			return sun == other.sun;
		}
		return false;
	}

	public static bool operator ==(EntryRenderSettings left, EntryRenderSettings right)
	{
		return left.Equals(right);
	}

	public static bool operator !=(EntryRenderSettings left, EntryRenderSettings right)
	{
		return !left.Equals(right);
	}

	public override bool Equals(object obj)
	{
		if (obj is EntryRenderSettings other)
		{
			return Equals(other);
		}
		return false;
	}

	public override int GetHashCode()
	{
		return HashCode.Combine(doorOpenAngle, shipOffset, exterior, sun);
	}
}
