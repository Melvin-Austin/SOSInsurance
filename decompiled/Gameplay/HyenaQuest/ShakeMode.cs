using System;

namespace HyenaQuest;

[Serializable]
public enum ShakeMode : byte
{
	SHAKE_UP,
	SHAKE_DOWN,
	SHAKE_LEFT,
	SHAKE_RIGHT,
	SHAKE_ALL
}
