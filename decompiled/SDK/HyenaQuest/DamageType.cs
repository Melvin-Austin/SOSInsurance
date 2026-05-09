using System;

namespace HyenaQuest;

[Serializable]
[Flags]
public enum DamageType
{
	GENERIC = 1,
	CUT = 2,
	PIT = 4,
	FALL = 8,
	ELECTRIC = 0x10,
	NECK_SNAP = 0x20,
	BURN = 0x40,
	CURSE = 0x80,
	INSTANT = 0x400,
	ABYSS = 0x800,
	ELECTRIC_ASHES = 0x1000
}
