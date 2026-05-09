using Steamworks;

namespace HyenaQuest;

public struct SteamLobby
{
	public CSteamID id;

	public string name;

	public int maxPlayers;

	public int players;

	public int round;

	public bool isModded;

	public bool isCheating;

	public bool isFull;
}
