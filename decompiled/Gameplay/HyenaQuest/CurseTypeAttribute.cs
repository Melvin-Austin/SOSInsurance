using System;

namespace HyenaQuest;

[AttributeUsage(AttributeTargets.Class)]
public class CurseTypeAttribute : Attribute
{
	public CURSE_TYPE Type { get; }

	public CurseTypeAttribute(CURSE_TYPE type)
	{
		Type = type;
	}
}
