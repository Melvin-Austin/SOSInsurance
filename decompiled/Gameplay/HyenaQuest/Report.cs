using System;
using Unity.Collections;
using Unity.Netcode;

namespace HyenaQuest;

[Serializable]
public class Report : INetworkSerializable, IEquatable<Report>
{
	public byte playerID;

	public FixedString128Bytes title;

	public override bool Equals(object obj)
	{
		if (!(obj is Report report))
		{
			return false;
		}
		return playerID == report.playerID;
	}

	public bool Equals(Report other)
	{
		if ((object)this == other)
		{
			return true;
		}
		if ((object)other == null)
		{
			return false;
		}
		if (playerID == other.playerID)
		{
			return title.Equals(other.title);
		}
		return false;
	}

	public override int GetHashCode()
	{
		return (playerID, title).GetHashCode();
	}

	public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
	{
		if (serializer.IsReader)
		{
			FastBufferReader fastBufferReader = serializer.GetFastBufferReader();
			fastBufferReader.ReadValueSafe(out playerID, default(FastBufferWriter.ForPrimitives));
			fastBufferReader.ReadValueSafe(out title, default(FastBufferWriter.ForFixedStrings));
		}
		else
		{
			FastBufferWriter fastBufferWriter = serializer.GetFastBufferWriter();
			fastBufferWriter.WriteValueSafe(in playerID, default(FastBufferWriter.ForPrimitives));
			fastBufferWriter.WriteValueSafe(in title, default(FastBufferWriter.ForFixedStrings));
		}
	}

	public static bool operator ==(Report a, Report b)
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

	public static bool operator !=(Report a, Report b)
	{
		return !(a == b);
	}
}
