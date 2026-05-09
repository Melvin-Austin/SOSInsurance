using System;

namespace HyenaQuest;

[Serializable]
public enum LOCK_TYPE : byte
{
	NONE,
	SOFT,
	SOFT_FROZEN,
	LOCKED
}
