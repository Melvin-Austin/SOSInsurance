using System;

namespace HyenaQuest;

[Serializable]
[Flags]
public enum ContractModifiers
{
	NONE = 1,
	LOCKED_DOORS = 2,
	DELIVERY_MALFUNCTION = 4,
	ICE_WORLD = 0x400,
	TOXIC_GAS_WORLD = 0x800,
	DARKNESS_WORLD = 0x1000
}
