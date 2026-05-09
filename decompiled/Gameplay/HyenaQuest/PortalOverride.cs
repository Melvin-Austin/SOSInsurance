using System;
using Unity.Netcode;
using UnityEngine;

namespace HyenaQuest;

[Serializable]
public class PortalOverride : INetworkSerializable, IEquatable<PortalOverride>
{
	public Vector3 pos;

	public Vector3 angle;

	public PortalOverride()
	{
		pos = Vector3.zero;
		angle = Vector3.zero;
	}

	public PortalOverride(Vector3 pos, Vector3 angle)
	{
		this.pos = pos;
		this.angle = angle;
	}

	public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
	{
		if (serializer.IsReader)
		{
			FastBufferReader fastBufferReader = serializer.GetFastBufferReader();
			fastBufferReader.ReadValueSafe(out pos);
			fastBufferReader.ReadValueSafe(out angle);
		}
		else
		{
			FastBufferWriter fastBufferWriter = serializer.GetFastBufferWriter();
			fastBufferWriter.WriteValueSafe(in pos);
			fastBufferWriter.WriteValueSafe(in angle);
		}
	}

	public bool Equals(PortalOverride other)
	{
		if (other == null)
		{
			return false;
		}
		if (pos.Equals(other.pos))
		{
			return angle.Equals(other.angle);
		}
		return false;
	}

	public override bool Equals(object obj)
	{
		if (obj is PortalOverride other)
		{
			return Equals(other);
		}
		return false;
	}

	public override int GetHashCode()
	{
		return HashCode.Combine(pos, angle);
	}
}
