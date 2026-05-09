using System;

namespace HyenaQuest;

[Serializable]
public enum PlayerAction : byte
{
	NONE,
	FORWARD,
	BACKWARD,
	LEFT,
	RIGHT,
	CROUCH,
	JUMP
}
