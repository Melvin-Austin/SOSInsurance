using System;
using Steamworks;
using Unity.Netcode;
using UnityEngine;

namespace HyenaQuest;

[Serializable]
public class Player
{
	public entity_player player;

	public CSteamID? steamID;

	public ulong connectionID;

	public byte clientID;

	public Player(entity_player player, ulong connectionID, byte clientID)
	{
		this.player = player;
		this.clientID = clientID;
		this.connectionID = connectionID;
	}

	public CSteamID GetSteamID()
	{
		if (!NetworkManager.Singleton)
		{
			throw new UnityException("Missing NetworkManager");
		}
		if (steamID.HasValue)
		{
			return steamID.Value;
		}
		steamID = (NetworkManager.Singleton as NETController)?.GetSteamID(connectionID);
		return steamID ?? new CSteamID(0uL);
	}

	public byte GetID()
	{
		return clientID;
	}
}
