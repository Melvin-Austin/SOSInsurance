using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Steamworks;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;
using ZLinq;

namespace HyenaQuest;

[DefaultExecutionOrder(-96)]
[DisallowMultipleComponent]
public class PlayerController : MonoController<PlayerController>
{
	public static entity_player LOCAL;

	public static readonly ulong STEAM_ID_SKIN_VERSION = 4uL;

	public static GameEvent OnLocalPlayerSet = new GameEvent();

	public GameEvent<entity_player, bool> OnPlayerCreated = new GameEvent<entity_player, bool>();

	public GameEvent<entity_player, bool> OnPlayerRemoved = new GameEvent<entity_player, bool>();

	public GameEvent<entity_player, bool> OnPlayerDeath = new GameEvent<entity_player, bool>();

	public GameEvent<entity_player, bool> OnPlayerRevive = new GameEvent<entity_player, bool>();

	public GameObject playerTemplate;

	public GameObject playerSpawns;

	public Dictionary<byte, Player> players = new Dictionary<byte, Player>();

	public Dictionary<ACCESSORY_TYPE, List<PlayerAccessory>> accessories = new Dictionary<ACCESSORY_TYPE, List<PlayerAccessory>>();

	public new void Awake()
	{
		base.Awake();
		if (!playerTemplate)
		{
			throw new UnityException("Missing playerTemplate");
		}
		if (!playerSpawns)
		{
			throw new UnityException("Missing playerSpawns");
		}
		if (!NetworkManager.Singleton)
		{
			throw new UnityException("Missing NetworkManager");
		}
		if (NetworkManager.Singleton.IsServer)
		{
			NetworkManager.Singleton.OnClientDisconnectCallback += RemovePlayer;
			NetworkManager.Singleton.SceneManager.OnLoadComplete += OnPlayerLoadComplete;
		}
		PlayerAccessory[] array = Resources.LoadAll<PlayerAccessory>("Accessories");
		if (array.Length == 0)
		{
			Debug.LogWarning("No PlayerAccessory found in Resources/Accessories folder");
		}
		PlayerAccessory[] array2 = array;
		foreach (PlayerAccessory playerAccessory in array2)
		{
			if ((bool)playerAccessory && (bool)playerAccessory.obj && (bool)playerAccessory.preview)
			{
				RegisterAccessory(playerAccessory);
			}
		}
		if (accessories.Count == 0)
		{
			throw new UnityException("No accessories templates found");
		}
		if (playerSpawns.transform.childCount <= 0)
		{
			throw new UnityException("Missing at least 1 player spawn");
		}
		UnityEngine.Random.InitState((int)DateTime.UtcNow.Ticks);
	}

	public void RegisterAccessory(PlayerAccessory acc)
	{
		if (!accessories.ContainsKey(acc.type))
		{
			accessories[acc.type] = new List<PlayerAccessory>();
		}
		accessories[acc.type].Add(acc);
	}

	public new void OnDestroy()
	{
		LOCAL = null;
		OnLocalPlayerSet = new GameEvent();
		if ((bool)NetworkManager.Singleton && NetworkManager.Singleton.IsServer)
		{
			NetworkManager.Singleton.OnClientDisconnectCallback -= RemovePlayer;
			NetworkManager.Singleton.SceneManager.OnLoadComplete -= OnPlayerLoadComplete;
		}
		base.OnDestroy();
	}

	[Client]
	public static void SetLocalPlayer(entity_player ply)
	{
		if (!ply)
		{
			throw new UnityException("SetLocalPlayer called with null player");
		}
		if ((bool)LOCAL)
		{
			throw new UnityException("SetLocalPlayer called with existing local player");
		}
		LOCAL = ply;
		OnLocalPlayerSet?.Invoke();
	}

	[Server]
	private IEnumerator CreatePlayer(ulong connectionID)
	{
		Player playerByConnection = GetPlayerByConnection(connectionID);
		if (playerByConnection != null)
		{
			throw new UnityException($"Player {connectionID} already exists");
		}
		byte playerID = GeneratePlayerID();
		ReservePlayerID(playerID);
		Transform spawnAllocation = GetPlayerSpawn(playerID);
		if (!spawnAllocation)
		{
			ReleasePlayerID(playerID);
			throw new UnityException("Failed to find empty spawn point");
		}
		AsyncInstantiateOperation<GameObject> op = UnityEngine.Object.InstantiateAsync(playerTemplate, spawnAllocation.transform.position, spawnAllocation.transform.rotation);
		yield return op;
		GameObject gameObject = op.Result[0];
		if (!gameObject)
		{
			ReleasePlayerID(playerID);
			throw new UnityException("Failed to instantiate player object");
		}
		entity_player playerEnt = gameObject.GetComponent<entity_player>();
		if (!playerEnt)
		{
			UnityEngine.Object.Destroy(gameObject);
			ReleasePlayerID(playerID);
			throw new UnityException("Failed to get entity_player component");
		}
		yield return null;
		playerByConnection = ClaimReservedPlayer(playerEnt, connectionID, playerID);
		playerEnt.SetPlayerID(playerID);
		playerEnt.SetConnectionID(connectionID);
		playerEnt.SetSteamID(playerByConnection.GetSteamID().m_SteamID);
		string playerName = $"Player {playerID}";
		if (SteamworksController.IsSteamRunning)
		{
			playerName = SteamFriends.GetFriendPersonaName(playerByConnection.GetSteamID());
		}
		playerEnt.SetPlayerName(playerName);
		yield return null;
		playerEnt.NetworkObject.SpawnAsPlayerObject(connectionID, destroyWithScene: true);
		yield return null;
		playerEnt.SetPositionRPC(spawnAllocation.position, spawnAllocation.rotation);
		MonoController<PlayerController>.Instance.OnPlayerCreate(playerEnt, server: true);
	}

	public void OnPlayerRemove(entity_player ply, bool server)
	{
		OnPlayerRemoved?.Invoke(ply, server);
	}

	public void OnPlayerCreate(entity_player ply, bool server)
	{
		OnPlayerCreated?.Invoke(ply, server);
	}

	public Player GetPlayer(byte plyID)
	{
		if (!NetworkManager.Singleton)
		{
			throw new UnityException("Missing NetworkManager");
		}
		if (NetworkManager.Singleton.IsServer)
		{
			return players.GetValueOrDefault(plyID);
		}
		entity_player[] array = UnityEngine.Object.FindObjectsByType<entity_player>();
		if (array == null || array.Length == 0)
		{
			return null;
		}
		if (plyID < array.Length)
		{
			return new Player(array[plyID], ulong.MaxValue, plyID);
		}
		return null;
	}

	[Server]
	public Player GetPlayerByConnection(ulong connectionID)
	{
		foreach (KeyValuePair<byte, Player> player in players)
		{
			if (player.Value != null && player.Value.connectionID == connectionID)
			{
				return player.Value;
			}
		}
		return null;
	}

	[Server]
	public HashSet<Player> GetPlayersByDistance(Vector3 pos, float distance)
	{
		HashSet<Player> hashSet = new HashSet<Player>();
		foreach (KeyValuePair<byte, Player> player in players)
		{
			if ((bool)player.Value?.player && !(Vector3.Distance(player.Value.player.transform.position, pos) > distance))
			{
				hashSet.Add(player.Value);
			}
		}
		return hashSet;
	}

	[Server]
	public HashSet<entity_player> GetPlayerEntitiesByDistance(Vector3 pos, float distance)
	{
		HashSet<entity_player> hashSet = new HashSet<entity_player>();
		entity_player[] array = UnityEngine.Object.FindObjectsByType<entity_player>();
		foreach (entity_player entity_player2 in array)
		{
			if ((bool)entity_player2 && !(Vector3.Distance(entity_player2.transform.position, pos) > distance))
			{
				hashSet.Add(entity_player2);
			}
		}
		return hashSet;
	}

	public entity_player FindNearbyPlayer(Vector3 pos, float searchRadius)
	{
		List<entity_player> alivePlayers = GetAlivePlayers();
		if (alivePlayers == null || alivePlayers.Count == 0)
		{
			return null;
		}
		entity_player result = null;
		float num = searchRadius;
		foreach (entity_player item in alivePlayers)
		{
			float num2 = Vector3.Distance(pos, item.transform.position);
			if (num2 < num)
			{
				num = num2;
				result = item;
			}
		}
		return result;
	}

	public entity_player GetPlayerEntityByID(byte playerID)
	{
		if (playerID == byte.MaxValue)
		{
			return null;
		}
		entity_player[] array = UnityEngine.Object.FindObjectsByType<entity_player>();
		if (array == null || array.Length == 0)
		{
			return null;
		}
		entity_player[] array2 = array;
		foreach (entity_player entity_player2 in array2)
		{
			if ((bool)entity_player2 && entity_player2.GetPlayerID() == playerID)
			{
				return entity_player2;
			}
		}
		return null;
	}

	[Server]
	public Player GetHost()
	{
		return players.GetValueOrDefault<byte, Player>(0);
	}

	public List<entity_player> GetAllPlayers()
	{
		return UnityEngine.Object.FindObjectsByType<entity_player>()?.ToList();
	}

	public List<entity_player> GetDeadPlayers(entity_player filter = null)
	{
		entity_player[] array = UnityEngine.Object.FindObjectsByType<entity_player>();
		if (array == null || array.Length == 0)
		{
			return null;
		}
		List<entity_player> list = new List<entity_player>();
		entity_player[] array2 = array;
		foreach (entity_player entity_player2 in array2)
		{
			if (entity_player2.IsDead() && entity_player2 != filter)
			{
				list.Add(entity_player2);
			}
		}
		return list;
	}

	public bool AnyPlayerDead()
	{
		entity_player[] array = UnityEngine.Object.FindObjectsByType<entity_player>();
		if (array == null || array.Length == 0)
		{
			return false;
		}
		return array.AsValueEnumerable().Any((entity_player p) => p.IsDead());
	}

	public List<entity_player> GetAlivePlayers(entity_player[] filter = null)
	{
		entity_player[] array = UnityEngine.Object.FindObjectsByType<entity_player>();
		if (array == null || array.Length == 0)
		{
			return null;
		}
		List<entity_player> list = new List<entity_player>();
		entity_player[] array2 = array;
		foreach (entity_player entity_player2 in array2)
		{
			if (!entity_player2.IsDead() && (filter == null || !filter.Contains(entity_player2)))
			{
				list.Add(entity_player2);
			}
		}
		return list;
	}

	public void OnDeath(entity_player ply, bool server)
	{
		OnPlayerDeath?.Invoke(ply, server);
	}

	public void OnRevive(entity_player ply, bool server)
	{
		OnPlayerRevive?.Invoke(ply, server);
	}

	private void OnPlayerLoadComplete(ulong clientId, string scene, LoadSceneMode mode)
	{
		if (!string.Equals(scene, "INGAME", StringComparison.InvariantCultureIgnoreCase) && !string.Equals(scene, "TRAINING", StringComparison.InvariantCultureIgnoreCase))
		{
			NETController.Instance.DisconnectClientWithReason(clientId, "Server shutdown");
		}
		else
		{
			StartCoroutine(CreatePlayer(clientId));
		}
	}

	[Server]
	private void RemovePlayer(ulong connectionID)
	{
		Player playerByConnection = GetPlayerByConnection(connectionID);
		if (playerByConnection != null)
		{
			players.Remove(playerByConnection.GetID());
		}
		OnPlayerRemove(playerByConnection?.player, server: true);
	}

	[Server]
	private byte GeneratePlayerID()
	{
		for (byte b = 0; b < NETController.MAX_PLAYERS; b++)
		{
			if (!players.ContainsKey(b))
			{
				return b;
			}
		}
		throw new UnityException("Failed to generate player ID");
	}

	[Server]
	private void ReservePlayerID(byte index)
	{
		if (!players.TryAdd(index, null))
		{
			throw new UnityException($"Player ID {index} already reserved");
		}
	}

	[Server]
	private void ReleasePlayerID(byte index)
	{
		if (players.TryGetValue(index, out var value) && value != null)
		{
			throw new UnityException($"Cannot release claimed player ID {index}");
		}
		players.Remove(index);
	}

	[Server]
	private Player ClaimReservedPlayer(entity_player playerEnt, ulong connectionID, byte index)
	{
		if (!players.ContainsKey(index))
		{
			throw new UnityException($"Player ID {index} was not reserved");
		}
		Player player = new Player(playerEnt, connectionID, index);
		players[index] = player;
		return player;
	}

	public Transform GetPlayerSpawn(byte playerID)
	{
		return playerSpawns.transform.GetChild(playerID % playerSpawns.transform.childCount);
	}
}
