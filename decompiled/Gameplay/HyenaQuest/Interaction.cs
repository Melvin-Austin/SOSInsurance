using System;

namespace HyenaQuest;

[Serializable]
public enum Interaction
{
	INTERACT,
	INTERACT_LOCKED,
	INTERACT_ACCEPT,
	INTERACT_YEEN,
	INTERACT_DENY,
	COUNT
}
