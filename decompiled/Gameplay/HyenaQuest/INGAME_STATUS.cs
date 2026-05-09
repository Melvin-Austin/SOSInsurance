using System;

namespace HyenaQuest;

[Serializable]
public enum INGAME_STATUS : byte
{
	IDLE,
	GENERATE,
	WAITING_PLAY_CONFIRMATION,
	PLAYING,
	ROUND_END,
	GAMEOVER
}
