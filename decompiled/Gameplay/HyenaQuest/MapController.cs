using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using FailCake;
using FailCake.VIS;
using Pathfinding;
using SaintsField;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;
using ZLinq;

namespace HyenaQuest;

[DisallowMultipleComponent]
[DefaultExecutionOrder(-70)]
[RequireComponent(typeof(NetworkObject))]
public class MapController : NetController<MapController>
{
	private static readonly float ROOM_NET_COOLDOWN = 0.6f;

	private static readonly int ROOM_NET_PER_ROOM_COOLDOWN = 5;

	public AudioSource heistPlayer;

	public Material defaultSky;

	public LayerMask roomMeshLayer;

	public entity_vis_portal_2d roomVISPortal;

	public Transform mapGeneratorParent;

	public SaintsDictionary<ContractModifiers, Texture2D> modifierTextures = new SaintsDictionary<ContractModifiers, Texture2D>();

	public GameEvent<bool> OnMapGenerated = new GameEvent<bool>();

	public GameEvent<bool> OnMapCleared = new GameEvent<bool>();

	private readonly Dictionary<string, WorldSettings> _worlds = new Dictionary<string, WorldSettings>();

	private readonly HashSet<entity_room> _spawnedRooms = new HashSet<entity_room>();

	private readonly List<entity_room_interior> _spawnedInteriors = new List<entity_room_interior>();

	private readonly HashSet<NetworkObject> _spawnedClosers = new HashSet<NetworkObject>();

	private readonly HashSet<NetworkObject> _networkToSend = new HashSet<NetworkObject>();

	private readonly Dictionary<string, int> _biomeCount = new Dictionary<string, int>();

	private readonly Dictionary<string, int> _interiorNameCount = new Dictionary<string, int>();

	private readonly Dictionary<string, int> _roomUsageCount = new Dictionary<string, int>();

	private readonly Dictionary<string, float> _penaltyCache = new Dictionary<string, float>();

	private readonly Dictionary<entity_room_base, OrientedBounds> _spawnedBounds = new Dictionary<entity_room_base, OrientedBounds>();

	private readonly Dictionary<entity_room_base, OrientedBounds> _spawnedInteriorBounds = new Dictionary<entity_room_base, OrientedBounds>();

	private OrientedBounds _fitTesterBounds;

	private readonly Dictionary<string, string> _templateNameCache = new Dictionary<string, string>();

	private byte _spawnedClientRooms;

	private byte _spawnedInteriorExits;

	private List<GameObject> _roomTemplates;

	private bool _isGenerated;

	private util_fade_timer _heistMusicFade;

	private entity_room _startRoom;

	private entity_room_exit _currentRoute;

	private readonly NetVar<FixedString128Bytes> _seed = new NetVar<FixedString128Bytes>();

	private readonly NetVar<FixedString128Bytes> _selectedWorld = new NetVar<FixedString128Bytes>();

	private readonly NetVar<byte> _expectedClientRooms = new NetVar<byte>(0);

	public void RegisterWorld(WorldSettings worldSettings)
	{
		if (!worldSettings)
		{
			throw new UnityException("WorldSettings cannot be null");
		}
		if (string.IsNullOrEmpty(worldSettings.name))
		{
			throw new UnityException("Invalid world, uniqueName cannot be empty");
		}
		if (!_worlds.TryAdd(worldSettings.name.ToLowerInvariant(), worldSettings))
		{
			throw new UnityException("World already registered");
		}
	}

	public override void OnNetworkSpawn()
	{
		base.OnNetworkSpawn();
		if (!mapGeneratorParent)
		{
			throw new UnityException("Missing mapGeneratorParent");
		}
		if (!roomVISPortal)
		{
			throw new UnityException("Missing roomVISPortal");
		}
		if ((int)roomMeshLayer == 0)
		{
			throw new UnityException("Invalid room mesh layer");
		}
		WorldSettings[] array = Resources.LoadAll<WorldSettings>("World");
		if (array.Length == 0)
		{
			Debug.LogWarning("No WorldSettings found in Resources/Worlds folder");
		}
		WorldSettings[] array2 = array;
		foreach (WorldSettings worldSettings in array2)
		{
			if ((bool)worldSettings)
			{
				RegisterWorld(worldSettings);
			}
		}
		if (_worlds.Count == 0)
		{
			throw new UnityException("No world templates found");
		}
		if (!heistPlayer)
		{
			throw new UnityException("No heist player found");
		}
		heistPlayer.loop = true;
		if ((bool)roomVISPortal.GetRoomA())
		{
			throw new UnityException("RoomA should not be assigned on the main room vis");
		}
		SDK.GetSeed = GetSeed;
		SDK.OnRoomSpawn = delegate(entity_room_base room)
		{
			StartCoroutine(OnClientRoomSpawned(room));
		};
		CoreController.WaitFor<SDKController>(delegate
		{
			RegisterCustomMaps();
		});
		if (base.IsServer)
		{
			ResetFitTester();
		}
	}

	private async void RegisterCustomMaps()
	{
		if (!MonoController<SDKController>.Instance)
		{
			throw new UnityException("Missing SteamworksController instance");
		}
		await UniTask.WaitUntil(() => SteamworksController.CustomMapsLoaded, PlayerLoopTiming.Update, base.destroyCancellationToken);
		IReadOnlyDictionary<string, CustomMap> customMaps = MonoController<SteamworksController>.Instance.GetCustomMaps();
		if (customMaps.Count == 0)
		{
			return;
		}
		foreach (KeyValuePair<string, CustomMap> item in customMaps)
		{
			Debug.Log("Registering " + item.Key + " map");
			NetController<MapController>.Instance.RegisterWorld(item.Value.settings);
		}
	}

	public new void OnDestroy()
	{
		_heistMusicFade?.Stop();
		SetSkybox(set: false);
		SDK.GetSeed = null;
		SDK.OnRoomSpawn = null;
		base.OnDestroy();
	}

	public WorldSettings GetWorld(string uniqueName)
	{
		if (string.IsNullOrEmpty(uniqueName))
		{
			return null;
		}
		if (!string.IsNullOrEmpty(uniqueName))
		{
			return _worlds.GetValueOrDefault(uniqueName.ToLowerInvariant());
		}
		return null;
	}

	public bool HasWorld(string uniqueName)
	{
		if (!string.IsNullOrEmpty(uniqueName))
		{
			return _worlds.ContainsKey(uniqueName.ToLowerInvariant());
		}
		return false;
	}

	public List<WorldSettings> GetWorlds(int rounds = -1)
	{
		return (from world in _worlds.Values.AsValueEnumerable()
			where rounds < 0 || world.minRounds <= rounds
			select world).ToList();
	}

	public entity_room GetStartRoom()
	{
		return _startRoom;
	}

	public List<entity_room_interior> GetSpawnedInteriors()
	{
		return _spawnedInteriors;
	}

	public HashSet<entity_room> GetSpawnedRooms()
	{
		return _spawnedRooms;
	}

	[Server]
	private void AddAdjacentRoom(entity_room room1, entity_room room2)
	{
		if ((bool)room1 && (bool)room2)
		{
			room1.AddAdjacentRoom(room2);
			room2.AddAdjacentRoom(room1);
		}
	}

	private void CleanupNetworkObjects(HashSet<NetworkObject> objects)
	{
		if (objects == null)
		{
			return;
		}
		foreach (NetworkObject @object in objects)
		{
			if ((object)@object != null && @object.IsSpawned)
			{
				@object.Despawn();
			}
		}
	}

	[Server]
	public Transform GetFurthestRoomFromPlayers()
	{
		if (!IsGenerated())
		{
			return null;
		}
		List<entity_player> alivePlayers = MonoController<PlayerController>.Instance.GetAlivePlayers();
		if (alivePlayers == null || alivePlayers.Count <= 0)
		{
			throw new UnityException("No players found");
		}
		entity_room entity_room2 = null;
		float num = float.MinValue;
		foreach (entity_room spawnedRoom in _spawnedRooms)
		{
			float num2 = 0f;
			foreach (entity_player item in alivePlayers)
			{
				num2 += Vector3.Distance(spawnedRoom.transform.position, item.transform.position);
			}
			if (num2 > num)
			{
				num = num2;
				entity_room2 = spawnedRoom;
			}
		}
		return entity_room2?.transform ?? throw new UnityException("No rooms found");
	}

	[Server]
	public Transform GetFurthestRoomFromPlayer(entity_player player)
	{
		if (!IsGenerated())
		{
			return null;
		}
		if (_spawnedRooms.Count == 0)
		{
			throw new UnityException("No rooms found");
		}
		if (!player)
		{
			throw new UnityException("Invalid player");
		}
		entity_room entity_room2 = null;
		float num = float.MinValue;
		foreach (entity_room spawnedRoom in _spawnedRooms)
		{
			float num2 = Vector3.Distance(spawnedRoom.transform.position, player.transform.position);
			if (num2 > num)
			{
				num = num2;
				entity_room2 = spawnedRoom;
			}
		}
		return entity_room2?.transform ?? throw new UnityException("Failed to find furthest room");
	}

	[Server]
	public Transform GetRandomRoomSpawnPointAwayFromPlayers(List<Transform> usedSpawnPoints = null)
	{
		if (!IsGenerated())
		{
			return null;
		}
		List<Transform> randomRoomSpawnPointsAwayFromPlayers = GetRandomRoomSpawnPointsAwayFromPlayers(usedSpawnPoints);
		if (randomRoomSpawnPointsAwayFromPlayers != null && randomRoomSpawnPointsAwayFromPlayers.Count > 0)
		{
			return randomRoomSpawnPointsAwayFromPlayers[UnityEngine.Random.Range(0, randomRoomSpawnPointsAwayFromPlayers.Count)];
		}
		return GetFurthestRoomFromPlayers();
	}

	[Server]
	public List<Transform> GetRandomRoomSpawnPointsAwayFromPlayers(List<Transform> usedSpawnPoints = null)
	{
		if (!IsGenerated())
		{
			return null;
		}
		List<Transform> allSpawnPoints = GetAllSpawnPoints();
		if (allSpawnPoints == null || allSpawnPoints.Count == 0)
		{
			Debug.LogError("No room spawn points available");
			return null;
		}
		List<Transform> list = (from sp in allSpawnPoints.AsValueEnumerable()
			where usedSpawnPoints == null || !usedSpawnPoints.Contains(sp)
			select sp).ToList();
		if (list.Count == 0)
		{
			list = allSpawnPoints;
		}
		List<entity_player> players = MonoController<PlayerController>.Instance.GetAlivePlayers();
		return (from sp in list.AsValueEnumerable()
			where !players.AsValueEnumerable().Any(delegate(entity_player p)
			{
				Vector3 vector = p.GetChestTransform().transform.position - (sp.position + Vector3.up);
				RaycastHit hitInfo;
				return !Physics.Raycast(sp.position + Vector3.up, vector.normalized, out hitInfo, vector.magnitude, roomMeshLayer);
			})
			select sp).ToList();
	}

	[Server]
	public List<Transform> GetAllSpawnPoints()
	{
		return (from spawn in UnityEngine.Object.FindObjectsByType<entity_room_monster_spawn>(FindObjectsInactive.Exclude).AsValueEnumerable()
			select spawn.transform).ToList();
	}

	[Server]
	public bool IsGenerated()
	{
		return _isGenerated;
	}

	public WorldSettings GetGeneratedWorld()
	{
		return GetWorld(_selectedWorld.Value.ToString());
	}

	public string GetGeneratedWorldID()
	{
		return _selectedWorld.Value.ToString().ToLowerInvariant();
	}

	[Server]
	private void ResetFitTester()
	{
		_fitTesterBounds = new OrientedBounds(Vector3.zero, new Vector3(0.001f, 0.001f, 0.001f), Quaternion.identity);
	}

	[Server]
	private void SetupFitTester(Bounds roomBounds, Vector3 position, Quaternion rotation, Vector3 scale)
	{
		Vector3 vector = Vector3.Scale(roomBounds.center, scale);
		Vector3 center = position + rotation * vector;
		Vector3 size = Vector3.Scale(roomBounds.size, new Vector3(Mathf.Abs(scale.x), Mathf.Abs(scale.y), Mathf.Abs(scale.z)));
		_fitTesterBounds = new OrientedBounds(center, size, rotation);
	}

	[Server]
	private bool FitRoomBounds(entity_room roomTemplate, entity_room_exit entrance, entity_room_exit exit)
	{
		ResetFitTester();
		Bounds bounds = roomTemplate.GetBounds();
		Vector3 position = entrance.transform.position;
		if (!GetGeneratedWorld().collisionChecks)
		{
			return true;
		}
		Quaternion rotation = entrance.transform.rotation;
		for (int i = 0; i < 4; i++)
		{
			Quaternion quaternion = Quaternion.Euler(0f, i * 90, 0f);
			Vector3 vector = quaternion * exit.transform.localPosition;
			Vector3 position2 = position - vector;
			SetupFitTester(bounds, position2, quaternion, Vector3.one);
			if (CheckFitBounds(_spawnedBounds))
			{
				entrance.transform.rotation = quaternion;
				return true;
			}
		}
		entrance.transform.rotation = rotation;
		return false;
	}

	[Server]
	private bool FitInteriorBounds(entity_room_interior template, entity_interior_exit entrance, out bool useMirroring)
	{
		useMirroring = false;
		if (!entrance.transform.parent)
		{
			throw new UnityException("Invalid exit, parent room is null");
		}
		ResetFitTester();
		WorldSettings generatedWorld = GetGeneratedWorld();
		if (!generatedWorld.collisionChecks)
		{
			return true;
		}
		template.UpdateBounds();
		Bounds bounds = template.GetBounds();
		Vector3 position = entrance.transform.position;
		Quaternion rotation = Quaternion.Euler(entrance.direction.x, entrance.transform.rotation.eulerAngles.y + entrance.direction.y, entrance.direction.z);
		entity_room_base componentInParent = entrance.GetComponentInParent<entity_room_base>();
		bool flag = generatedWorld.interiorMirroring && UnityEngine.Random.value > 0.5f;
		for (int i = 0; i < 2; i++)
		{
			bool flag2 = ((i == 0) ? flag : (!flag));
			SetupFitTester(bounds, position, rotation, flag2 ? new Vector3(-1f, 1f, 1f) : Vector3.one);
			if (CheckFitBounds(_spawnedBounds, componentInParent) && CheckFitBounds(_spawnedInteriorBounds))
			{
				useMirroring = flag2;
				return true;
			}
		}
		return false;
	}

	[Server]
	private bool CheckFitBounds(Dictionary<entity_room_base, OrientedBounds> boundsToCheck, entity_room_base exclude = null)
	{
		foreach (KeyValuePair<entity_room_base, OrientedBounds> item in boundsToCheck)
		{
			if ((!exclude || !(item.Key == exclude)) && _fitTesterBounds.Intersects(item.Value))
			{
				return false;
			}
		}
		return true;
	}

	[Server]
	private void AddRoomBounds(entity_room room)
	{
		if ((bool)room)
		{
			Bounds bounds = room.GetBounds();
			_spawnedBounds.Add(room, new OrientedBounds(bounds, room.transform.position, room.transform.rotation));
		}
	}

	[Server]
	private void AddInteriorBounds(entity_room_interior interior)
	{
		if ((bool)interior)
		{
			Bounds bounds = interior.GetBounds();
			Vector3 localScale = interior.transform.localScale;
			Vector3 vector = Vector3.Scale(bounds.center, localScale);
			Vector3 center = interior.transform.position + interior.transform.rotation * vector;
			Vector3 size = Vector3.Scale(bounds.size, new Vector3(Mathf.Abs(localScale.x), Mathf.Abs(localScale.y), Mathf.Abs(localScale.z)));
			_spawnedInteriorBounds.Add(interior, new OrientedBounds(center, size, interior.transform.rotation));
		}
	}

	[Server]
	private void CleanupPartialGeneration()
	{
		foreach (NetworkObject item in _networkToSend)
		{
			if ((bool)item && (bool)item.gameObject)
			{
				UnityEngine.Object.Destroy(item.gameObject);
			}
		}
		_networkToSend.Clear();
		_spawnedRooms.Clear();
		_spawnedInteriors.Clear();
		_spawnedBounds.Clear();
		_spawnedInteriorBounds.Clear();
		_biomeCount.Clear();
		_interiorNameCount.Clear();
		_templateNameCache.Clear();
		_roomTemplates?.Clear();
		_roomUsageCount.Clear();
		_penaltyCache.Clear();
		_spawnedInteriorExits = 0;
		_currentRoute = null;
		_startRoom = null;
		ResetFitTester();
	}

	[Server]
	public IEnumerator Cleanup()
	{
		if (!_isGenerated)
		{
			Debug.LogWarning("[MapController] Map not generated, nothing to cleanup");
			yield break;
		}
		CleanupNetworkObjects(_networkToSend);
		CleanupNetworkObjects(_spawnedClosers);
		_spawnedInteriorExits = 0;
		_spawnedClientRooms = 0;
		_expectedClientRooms.Value = 0;
		_spawnedClosers.Clear();
		_networkToSend.Clear();
		_spawnedRooms.Clear();
		_spawnedInteriors.Clear();
		_biomeCount.Clear();
		_interiorNameCount.Clear();
		_roomTemplates?.Clear();
		_templateNameCache.Clear();
		_roomUsageCount.Clear();
		_penaltyCache.Clear();
		_currentRoute = null;
		_startRoom = null;
		_selectedWorld.SetSpawnValue("");
		_seed.SetSpawnValue("");
		_spawnedBounds.Clear();
		_spawnedInteriorBounds.Clear();
		ResetFitTester();
		Debug.Log("[MapController] Cleaned up");
		MapClearedBroadcastRPC();
		_isGenerated = false;
		OnMapCleared.Invoke(param1: true);
		yield return null;
	}

	public int GetSeed()
	{
		if (!string.IsNullOrEmpty(_seed.Value.ToString()))
		{
			return int.Parse(_seed.Value.ToString());
		}
		return 0;
	}

	[Server]
	public IEnumerator CleanupAndGenerate(string worldID, string seed = null, Action<EntryRenderSettings> preGen = null, Action callback = null)
	{
		yield return Cleanup();
		yield return new WaitForSeconds(1f);
		yield return Generate(worldID, seed, preGen, callback);
	}

	[Server]
	public IEnumerator Generate(string worldID, string seed = null, Action<EntryRenderSettings> preGen = null, Action onComplete = null)
	{
		WorldSettings world = GetWorld(worldID.ToLowerInvariant());
		if (!world)
		{
			throw new UnityException("Invalid world " + worldID);
		}
		return Generate(world, seed, preGen, onComplete);
	}

	[Server]
	public IEnumerator Generate(WorldSettings world, string seed = null, Action<EntryRenderSettings> preGen = null, Action onComplete = null)
	{
		if (_isGenerated)
		{
			Debug.LogWarning("[MapController] Map generated, please Cleanup first");
			yield break;
		}
		int worldSeed;
		if (!string.IsNullOrEmpty(seed))
		{
			worldSeed = int.Parse(seed);
			Debug.Log($"[MapController] Using override seed: {worldSeed}");
		}
		else
		{
			worldSeed = DateTime.UtcNow.GetHashCode();
			Debug.Log($"[MapController] Generated new seed: {worldSeed}");
		}
		_seed.SetSpawnValue(worldSeed.ToString());
		UnityEngine.Random.InitState(worldSeed);
		util_list.SetSeed(worldSeed);
		_selectedWorld.SetSpawnValue(world.name);
		if (world.entry == null || world.entry.Count == 0)
		{
			throw new UnityException("No main rooms found");
		}
		EntrySettings entrySettings = world.entry[UnityEngine.Random.Range(0, world.entry.Count)];
		if (!entrySettings.template)
		{
			throw new UnityException("Failed to get main room template");
		}
		preGen?.Invoke(entrySettings.settings);
		int maxRetries = 3;
		int attempt = 0;
		bool layoutSuccess = false;
		HashSet<entity_room_exit> roomExits = new HashSet<entity_room_exit>();
		List<entity_interior_exit> interiorExits = new List<entity_interior_exit>();
		for (; attempt < maxRetries; attempt++)
		{
			if (attempt > 0)
			{
				CleanupPartialGeneration();
				int num = worldSeed + attempt;
				UnityEngine.Random.InitState(num);
				util_list.SetSeed(num);
				_seed.SetSpawnValue(num.ToString());
				Debug.Log($"[MapController] Retry {attempt}/{maxRetries} with seed {num}");
			}
			entity_room entity_room2 = SpawnStartRoom(entrySettings);
			if (!entity_room2)
			{
				throw new UnityException("Failed to generate start room");
			}
			_startRoom = entity_room2;
			yield return GenerateMap(delegate((HashSet<entity_room_exit>, List<entity_interior_exit>) result)
			{
				(roomExits, interiorExits) = result;
			});
			if (_spawnedInteriorExits >= world.minInteriorRooms)
			{
				layoutSuccess = true;
				break;
			}
			Debug.LogWarning($"[MapController] Layout attempt {attempt + 1} failed: {_spawnedInteriorExits}/{world.minInteriorRooms} interior exits");
		}
		if (!layoutSuccess)
		{
			Debug.LogWarning($"[MapController] All {maxRetries} retries exhausted, proceeding with {_spawnedInteriorExits}/{world.minInteriorRooms} interior exits");
		}
		CloseRooms(roomExits, interiorExits);
		interiorExits.AddRange(_startRoom.GetInteriorExits());
		HashSet<entity_interior_exit> interiorExits2 = GenerateInteriors(interiorExits);
		CloseInteriors(interiorExits2);
		ResetFitTester();
		UnityEngine.Random.InitState((int)DateTime.UtcNow.Ticks);
		util_list.SetSeed(0);
		yield return SendMap();
		yield return GenerateAIPath();
		Debug.Log($"Done generating, created '{_spawnedRooms.Count} rooms'");
		_isGenerated = true;
		OnMapGenerated.Invoke(param1: true);
		onComplete?.Invoke();
	}

	[Server]
	private entity_room SpawnStartRoom(EntrySettings settings)
	{
		if (!settings.template)
		{
			throw new UnityException("Invalid start room template");
		}
		entity_room_exit[] exits = settings.template.GetComponent<entity_room>().GetExits();
		if (exits == null || exits.Length == 0)
		{
			throw new UnityException("No exits found on main room template");
		}
		GameObject obj = UnityEngine.Object.Instantiate(settings.template, mapGeneratorParent, worldPositionStays: false);
		if (!obj)
		{
			throw new UnityException("Failed to instantiate room");
		}
		obj.transform.SetLocalPositionAndRotation(settings.settings.shipOffset, Quaternion.identity);
		entity_room component = obj.GetComponent<entity_room>();
		if (!component || !component.NetworkObject)
		{
			throw new UnityException("Failed to get entity_room component from room template");
		}
		_networkToSend.Add(component.NetworkObject);
		byte spawnedInteriorExits = _spawnedInteriorExits;
		entity_interior_exit[] interiorExits = component.GetInteriorExits();
		_spawnedInteriorExits = (byte)(spawnedInteriorExits + (byte)((interiorExits != null) ? interiorExits.Length : 0));
		AddRoomBounds(component);
		return component;
	}

	[Server]
	private IEnumerator GenerateMap(Action<(HashSet<entity_room_exit>, List<entity_interior_exit>)> result)
	{
		WorldSettings world = GetGeneratedWorld();
		if (!world)
		{
			throw new UnityException("No world selected");
		}
		_roomTemplates = world.rooms.ShuffleWithNew() as List<GameObject>;
		List<GameObject> roomTemplates = _roomTemplates;
		if (roomTemplates == null || roomTemplates.Count <= 0)
		{
			throw new UnityException("No room templates found");
		}
		HashSet<entity_room_exit> roomEnds = new HashSet<entity_room_exit>();
		List<entity_interior_exit> roomInteriors = new List<entity_interior_exit>();
		List<entity_room_exit> openRoutes = new List<entity_room_exit>(_startRoom.GetExits());
		if (openRoutes == null || openRoutes.Count == 0)
		{
			throw new UnityException("No open routes found from start room");
		}
		byte currentRound = NetController<IngameController>.Instance.GetCurrentRound();
		int mapSize = world.CalculateMapSize(currentRound);
		int maxAttempts = 100;
		int failedAttempts = 0;
		entity_room_exit setTemplate = null;
		while (openRoutes.Count > 0 && (_spawnedRooms.Count < mapSize || _spawnedInteriorExits < world.minInteriorRooms))
		{
			if (failedAttempts >= maxAttempts)
			{
				Debug.LogWarning($"[MapController] Generation bailed after {maxAttempts} attempts. Interiors: {_spawnedInteriorExits}/{world.minInteriorRooms}, Rooms: {_spawnedRooms.Count}/{mapSize}");
				break;
			}
			_currentRoute = openRoutes[0];
			if (setTemplate != _currentRoute)
			{
				setTemplate = _currentRoute;
				_roomTemplates = GatherRoomTemplates();
			}
			if (_roomTemplates.Count == 0)
			{
				roomEnds.Add(_currentRoute);
				openRoutes.RemoveAt(0);
				_currentRoute = null;
				failedAttempts++;
				continue;
			}
			GameObject template = _roomTemplates[0];
			entity_room entity_room2 = CreateRoom(template, _currentRoute);
			if (!entity_room2)
			{
				_roomTemplates.RemoveAt(0);
				failedAttempts++;
				continue;
			}
			failedAttempts = 0;
			openRoutes.RemoveAt(0);
			entity_room_exit[] exits = entity_room2.GetExits();
			if (world.exitShuffle)
			{
				exits.Shuffle();
			}
			openRoutes.AddRange(exits);
			roomInteriors.AddRange(entity_room2.GetInteriorExits());
			if (_spawnedRooms.Count % 3 == 0)
			{
				yield return null;
			}
		}
		roomEnds.UnionWith(openRoutes);
		result((roomEnds, roomInteriors));
	}

	[Server]
	private bool IsStraightCorridor(entity_room room)
	{
		if (!room)
		{
			return false;
		}
		entity_room_exit[] exits = room.GetExits();
		if (exits == null || exits.Length != 2)
		{
			return false;
		}
		Vector3 normalized = exits[0].transform.localPosition.normalized;
		Vector3 normalized2 = exits[1].transform.localPosition.normalized;
		return Vector3.Dot(normalized, normalized2) < -0.8f;
	}

	[Server]
	private List<GameObject> GatherRoomTemplates()
	{
		WorldSettings world = GetGeneratedWorld();
		if (!world)
		{
			throw new UnityException("No world selected");
		}
		List<GameObject> list = new List<GameObject>(world.rooms);
		byte currentRound = NetController<IngameController>.Instance.GetCurrentRound();
		int mapSize = world.CalculateMapSize(currentRound);
		if (!_currentRoute)
		{
			list.AddRange(world.traversal);
			return list.ShuffleWithNew().ToList();
		}
		string biomeID = _currentRoute.biomeID;
		if (!string.IsNullOrEmpty(biomeID) && world.biomeLimit.TryGetValue(biomeID, out var value))
		{
			int num = value;
			if (num <= 0)
			{
				if (num == 0)
				{
					list.Shuffle();
					return list;
				}
			}
			else if (_biomeCount.GetValueOrDefault(biomeID, 0) >= value)
			{
				return world.traversal.ShuffleWithNew().ToList();
			}
		}
		list.AddRange(world.traversal);
		list.Shuffle();
		entity_room owner = _currentRoute.GetOwner();
		_penaltyCache.Clear();
		foreach (entity_room_base adjacentRoom in owner.GetAdjacentRooms())
		{
			if (!adjacentRoom)
			{
				continue;
			}
			_penaltyCache[GetTemplateBaseName(adjacentRoom.name)] = 1500f;
			foreach (entity_room_base adjacentRoom2 in adjacentRoom.GetAdjacentRooms())
			{
				if ((bool)adjacentRoom2 && !(adjacentRoom2 == owner))
				{
					_penaltyCache.TryAdd(GetTemplateBaseName(adjacentRoom2.name), 750f);
				}
			}
		}
		bool hasAvailable = list.AsValueEnumerable().Any(delegate(GameObject t)
		{
			string templateBaseName = GetTemplateBaseName(t.name);
			int valueOrDefault = _roomUsageCount.GetValueOrDefault(templateBaseName, 0);
			int value2;
			return !world.templateLimit.TryGetValue(templateBaseName, out value2) || value2 <= 0 || valueOrDefault < value2;
		});
		string currentTemplateName = GetTemplateBaseName(owner.name);
		return (from x in list.AsValueEnumerable().Select(delegate(GameObject template)
			{
				entity_room component = template.GetComponent<entity_room>();
				if (!component)
				{
					return (template: template, score: float.MaxValue);
				}
				string templateBaseName2 = GetTemplateBaseName(template.name);
				if (world.duplicateChecks && string.Equals(currentTemplateName, templateBaseName2, StringComparison.InvariantCultureIgnoreCase))
				{
					return (template: template, score: float.MaxValue);
				}
				entity_interior_exit[] interiorExits = component.GetInteriorExits();
				bool num2 = interiorExits != null && interiorExits.Length != 0;
				bool num3 = IsStraightCorridor(component);
				float valueOrDefault2 = _penaltyCache.GetValueOrDefault(templateBaseName2, 0f);
				int valueOrDefault3 = _roomUsageCount.GetValueOrDefault(templateBaseName2, 0);
				valueOrDefault2 += (float)(valueOrDefault3 * valueOrDefault3) * 50f;
				if (num3)
				{
					valueOrDefault2 += 500f;
				}
				if (num2)
				{
					float num4 = (float)_spawnedRooms.Count / (float)mapSize;
					if (_spawnedInteriorExits < world.minInteriorRooms)
					{
						if (num4 < 0.4f)
						{
							valueOrDefault2 += 1000f;
						}
						else
						{
							float t2 = Mathf.InverseLerp(0.4f, 0.85f, num4);
							valueOrDefault2 -= Mathf.Lerp(200f, 1500f, t2);
						}
					}
					else
					{
						valueOrDefault2 += 400f;
					}
				}
				if (!hasAvailable || !world.templateLimit.TryGetValue(templateBaseName2, out var value3))
				{
					return (template: template, score: valueOrDefault2 + UnityEngine.Random.Range(0f, 100f));
				}
				return (value3 > 0 && valueOrDefault3 >= value3) ? (template: template, score: float.MaxValue) : (template: template, score: valueOrDefault2 + UnityEngine.Random.Range(0f, 100f));
			})
			orderby x.score
			select x.template).ToList();
	}

	private string GetTemplateBaseName(string fullName)
	{
		if (_templateNameCache.TryGetValue(fullName, out var value))
		{
			return value;
		}
		string text = fullName;
		while (text.Length > 0)
		{
			string text2 = text;
			if (text2[text2.Length - 1] != ')')
			{
				break;
			}
			int num = text.LastIndexOf('(');
			if (num <= 0)
			{
				break;
			}
			text = text.Substring(0, num).TrimEnd();
		}
		_templateNameCache[fullName] = text;
		return text;
	}

	[Server]
	private entity_room CreateRoom(GameObject template, entity_room_exit oldRoomExit)
	{
		WorldSettings generatedWorld = GetGeneratedWorld();
		if (!generatedWorld)
		{
			throw new UnityException("No world selected");
		}
		if (!oldRoomExit)
		{
			throw new UnityException("Invalid exit, cannot be null");
		}
		if (!template)
		{
			throw new UnityException("Invalid room template, cannot be null");
		}
		entity_room component = template.GetComponent<entity_room>();
		if (!component)
		{
			throw new UnityException("Invalid room " + template.name + ", missing entity_room");
		}
		if (NetController<IngameController>.Instance.GetCurrentRound() < component.minSpawnRounds)
		{
			return null;
		}
		entity_room_exit[] exits = component.GetExits();
		if (exits == null || exits.Length <= 0)
		{
			throw new UnityException("Invalid room '" + template.name + "', missing entity_room_exit");
		}
		bool flag = generatedWorld.traversal.Contains(template);
		List<(entity_room_exit, int)> list = (from pair in exits.Select((entity_room_exit exit, int index) => (exit: exit, index: index))
			where (bool)pair.exit && string.Equals(pair.exit.biomeID, oldRoomExit.biomeID, StringComparison.OrdinalIgnoreCase)
			select pair).ToList();
		if (generatedWorld.exitShuffle)
		{
			list.Shuffle();
		}
		foreach (var (entity_room_exit2, num) in list)
		{
			if (FitRoomBounds(component, oldRoomExit, entity_room_exit2))
			{
				GameObject obj = UnityEngine.Object.Instantiate(template, mapGeneratorParent, worldPositionStays: true);
				entity_room componentInChildren = obj.GetComponentInChildren<entity_room>();
				if (!componentInChildren || !componentInChildren.NetworkObject)
				{
					throw new UnityException("Invalid room, missing entity_room");
				}
				Quaternion rotation = oldRoomExit.transform.rotation;
				Vector3 vector = rotation * entity_room_exit2.transform.localPosition;
				Vector3 position = oldRoomExit.transform.position - vector;
				obj.transform.SetPositionAndRotation(position, rotation);
				obj.name = template.name;
				entity_room_exit[] exits2 = componentInChildren.GetExits();
				if (exits2 != null)
				{
					exits2[num]?.gameObject.SetActive(value: false);
				}
				byte spawnedInteriorExits = _spawnedInteriorExits;
				entity_interior_exit[] interiorExits = componentInChildren.GetInteriorExits();
				_spawnedInteriorExits = (byte)(spawnedInteriorExits + (byte)((interiorExits != null) ? interiorExits.Length : 0));
				_spawnedRooms.Add(componentInChildren);
				string templateBaseName = GetTemplateBaseName(template.name);
				_roomUsageCount[templateBaseName] = _roomUsageCount.GetValueOrDefault(templateBaseName, 0) + 1;
				_networkToSend.Add(componentInChildren.NetworkObject);
				AddRoomBounds(componentInChildren);
				entity_room componentInParent = oldRoomExit.GetComponentInParent<entity_room>();
				AddAdjacentRoom(componentInParent, componentInChildren);
				string biomeID = oldRoomExit.biomeID;
				if (!flag && !string.IsNullOrEmpty(biomeID))
				{
					_biomeCount[biomeID] = _biomeCount.GetValueOrDefault(biomeID, 0) + 1;
				}
				else if (!string.IsNullOrEmpty(biomeID))
				{
					_biomeCount.Remove(biomeID);
				}
				return componentInChildren;
			}
		}
		return null;
	}

	[Server]
	private HashSet<entity_interior_exit> GenerateInteriors(List<entity_interior_exit> interiorExits)
	{
		WorldSettings world = GetGeneratedWorld();
		if (!world)
		{
			throw new UnityException("No world selected");
		}
		HashSet<entity_interior_exit> hashSet = new HashSet<entity_interior_exit>();
		if (interiorExits == null || interiorExits.Count <= 0)
		{
			Debug.LogWarning("[MapController] No interior exits to generate");
			return hashSet;
		}
		List<entity_interior_exit> list = interiorExits.AsValueEnumerable().Where(delegate(entity_interior_exit t)
		{
			string templateBaseName = GetTemplateBaseName(t.name);
			int valueOrDefault = _roomUsageCount.GetValueOrDefault(templateBaseName, 0);
			int value;
			return !world.templateLimit.TryGetValue(templateBaseName, out value) || value <= 0 || valueOrDefault < value;
		}).ToList();
		if (list.Count <= 0)
		{
			list = interiorExits;
		}
		list.Shuffle();
		interiorExits.Clear();
		interiorExits.AddRange(list);
		while (interiorExits.Count > 0)
		{
			entity_interior_exit entity_interior_exit2 = interiorExits[0];
			if (!entity_interior_exit2)
			{
				throw new UnityException("Invalid interior exit");
			}
			interiorExits.RemoveAt(0);
			List<GameObject> interiors = world.interiors;
			if (interiors == null || interiors.Count <= 0)
			{
				Debug.LogWarning("[MapController] No interior templates found, skipping");
				continue;
			}
			IList<GameObject> list2 = world.interiors.AsValueEnumerable().Where(delegate(GameObject t)
			{
				string templateBaseName2 = GetTemplateBaseName(t.name);
				int valueOrDefault2 = _interiorNameCount.GetValueOrDefault(templateBaseName2, 0);
				int value2;
				return !world.templateLimit.TryGetValue(templateBaseName2, out value2) || value2 <= 0 || valueOrDefault2 < value2;
			}).ToList()
				.ShuffleWithNew();
			if (list2 == null || list2.Count <= 0)
			{
				Debug.LogWarning("[MapController] No interior templates found (or all at limit), skipping");
				continue;
			}
			bool flag = false;
			while (list2.Count > 0)
			{
				if ((bool)CreateInterior(list2[0], entity_interior_exit2))
				{
					string templateBaseName3 = GetTemplateBaseName(list2[0].name);
					_interiorNameCount[templateBaseName3] = _interiorNameCount.GetValueOrDefault(templateBaseName3, 0) + 1;
					flag = true;
					break;
				}
				list2.RemoveAt(0);
			}
			if (!flag)
			{
				hashSet.Add(entity_interior_exit2);
			}
		}
		return hashSet;
	}

	[Server]
	private entity_room_interior CreateInterior(GameObject template, entity_interior_exit oldExit)
	{
		WorldSettings generatedWorld = GetGeneratedWorld();
		if (!generatedWorld)
		{
			throw new UnityException("No world selected");
		}
		if (!oldExit || !template)
		{
			return null;
		}
		entity_room_interior component = template.GetComponent<entity_room_interior>();
		if (!component)
		{
			throw new UnityException("Invalid interior, missing entity_interior on template");
		}
		if (!FitInteriorBounds(component, oldExit, out var useMirroring))
		{
			return null;
		}
		bool flag = useMirroring && generatedWorld.interiorMirroring;
		GameObject gameObject = UnityEngine.Object.Instantiate(template, mapGeneratorParent, worldPositionStays: false);
		if (!gameObject)
		{
			throw new UnityException("Failed to instantiate room");
		}
		gameObject.transform.rotation = oldExit.transform.rotation * Quaternion.Euler(oldExit.direction);
		gameObject.transform.position = oldExit.transform.position;
		if (flag)
		{
			gameObject.transform.localScale = new Vector3(-1f, 1f, 1f);
		}
		gameObject.name = template.name;
		entity_room_interior component2 = gameObject.GetComponent<entity_room_interior>();
		if (!(component2?.NetworkObject))
		{
			throw new UnityException("Invalid interior, missing entity_interior");
		}
		component2.SetFlip(flag);
		_networkToSend.Add(component2.NetworkObject);
		_spawnedInteriors.Add(component2);
		AddInteriorBounds(component2);
		return component2;
	}

	[Server]
	private void CloseRooms(HashSet<entity_room_exit> roomExits, List<entity_interior_exit> interiors)
	{
		if (roomExits == null || roomExits.Count <= 0)
		{
			return;
		}
		foreach (entity_room_exit roomExit in roomExits)
		{
			CreateDeadEnd(roomExit, interiors);
		}
	}

	[Server]
	private void CloseInteriors(HashSet<entity_interior_exit> interiorExits)
	{
		if (interiorExits == null || interiorExits.Count <= 0)
		{
			return;
		}
		foreach (entity_interior_exit interiorExit in interiorExits)
		{
			CreateInteriorClosure(interiorExit);
		}
	}

	[Server]
	private void CreateDeadEnd(entity_room_exit exit, List<entity_interior_exit> interiors)
	{
		WorldSettings generatedWorld = GetGeneratedWorld();
		if (!generatedWorld)
		{
			throw new UnityException("No world selected");
		}
		List<GameObject> closers = generatedWorld.closers;
		if (closers == null || closers.Count <= 0)
		{
			closers = generatedWorld.deadEnds;
			if (closers == null || closers.Count <= 0)
			{
				return;
			}
		}
		if (!exit)
		{
			throw new UnityException("Invalid exit");
		}
		if (_spawnedInteriorExits < generatedWorld.minInteriorRooms)
		{
			List<GameObject> list = generatedWorld.rooms.AsValueEnumerable().Where(delegate(GameObject t)
			{
				entity_room component = t.GetComponent<entity_room>();
				return (object)component != null && component.GetInteriorExits()?.Length > 0;
			}).ToList();
			list.Shuffle();
			foreach (GameObject item in list)
			{
				entity_room entity_room2 = CreateRoom(item, exit);
				if (!entity_room2)
				{
					continue;
				}
				entity_room_exit[] exits = entity_room2.GetExits();
				if (exits != null)
				{
					entity_room_exit[] array = exits;
					foreach (entity_room_exit entity_room_exit2 in array)
					{
						if ((bool)entity_room_exit2 && entity_room_exit2.gameObject.activeSelf)
						{
							CreateClosure(entity_room_exit2);
						}
					}
				}
				interiors.AddRange(entity_room2.GetInteriorExits());
				return;
			}
		}
		closers = generatedWorld.deadEnds;
		if (closers != null && closers.Count > 0)
		{
			List<GameObject> list2 = new List<GameObject>(generatedWorld.deadEnds);
			list2.Shuffle();
			foreach (GameObject item2 in list2)
			{
				if ((bool)CreateRoom(item2, exit))
				{
					return;
				}
			}
		}
		CreateClosure(exit);
	}

	private void CreateClosure(entity_room_exit exit)
	{
		WorldSettings generatedWorld = GetGeneratedWorld();
		if (!generatedWorld)
		{
			throw new UnityException("No world selected");
		}
		List<GameObject> list = generatedWorld.closers.AsValueEnumerable().Where(delegate(GameObject closure)
		{
			entity_room_closure component = closure.GetComponent<entity_room_closure>();
			return (bool)component && string.Equals(component.biomeID, exit.biomeID, StringComparison.OrdinalIgnoreCase);
		}).ToList();
		if (list.Count == 0)
		{
			Debug.LogError("[MapController] No valid closure templates found for the exit biome");
			return;
		}
		GameObject obj = UnityEngine.Object.Instantiate(list[UnityEngine.Random.Range(0, list.Count)], exit.transform.position, exit.transform.parent.rotation * Quaternion.Euler(exit.direction));
		if (!obj)
		{
			throw new UnityException("Failed to instantiate dead-end");
		}
		NetworkObject component2 = obj.GetComponent<NetworkObject>();
		if (!component2)
		{
			throw new UnityException("Invalid dead end, missing NetworkObject");
		}
		component2.Spawn();
		_spawnedClosers.Add(component2);
	}

	[Server]
	private void CreateInteriorClosure(entity_interior_exit exit)
	{
		WorldSettings generatedWorld = GetGeneratedWorld();
		if (!generatedWorld)
		{
			throw new UnityException("No world selected");
		}
		List<GameObject> interiorClosers = generatedWorld.interiorClosers;
		if (interiorClosers != null && interiorClosers.Count > 0)
		{
			if (!exit)
			{
				throw new UnityException("Invalid interior exit");
			}
			GameObject obj = UnityEngine.Object.Instantiate(generatedWorld.interiorClosers[UnityEngine.Random.Range(0, generatedWorld.interiorClosers.Count)], exit.transform.position, exit.transform.parent.rotation * Quaternion.Euler(exit.direction));
			if (!obj)
			{
				throw new UnityException("Failed to instantiate interior dead-end");
			}
			NetworkObject component = obj.GetComponent<NetworkObject>();
			if (!component)
			{
				throw new UnityException("Invalid interior dead end, missing NetworkObject");
			}
			component.Spawn();
			_spawnedClosers.Add(component);
		}
	}

	public void SetSkybox(bool set)
	{
		WorldSettings generatedWorld = GetGeneratedWorld();
		if ((bool)generatedWorld)
		{
			RenderSettings.skybox = ((set && (bool)generatedWorld.skyMaterial) ? generatedWorld.skyMaterial : defaultSky);
		}
	}

	public void SetAmbientMusic(bool play)
	{
		WorldSettings generatedWorld = GetGeneratedWorld();
		if (!generatedWorld)
		{
			return;
		}
		if (!heistPlayer)
		{
			throw new UnityException("No heist player found");
		}
		_heistMusicFade?.Stop();
		if (play)
		{
			List<AudioClip> heistMusic = generatedWorld.heistMusic;
			if (heistMusic != null && heistMusic.Count > 0)
			{
				heistPlayer.clip = generatedWorld.heistMusic[UnityEngine.Random.Range(0, generatedWorld.heistMusic.Count)];
				heistPlayer.volume = generatedWorld.musicVolume;
				heistPlayer.pitch = 1f;
				heistPlayer.Play();
			}
		}
		else
		{
			if (!heistPlayer.isPlaying)
			{
				return;
			}
			_heistMusicFade = util_fade_timer.Fade(0.25f, 1f, 0f, delegate(float f)
			{
				if ((bool)heistPlayer)
				{
					heistPlayer.pitch = f;
				}
			}, delegate
			{
				if ((bool)heistPlayer)
				{
					heistPlayer.Stop();
				}
			});
		}
	}

	public void StopAmbientMusic()
	{
		if (!heistPlayer)
		{
			throw new UnityException("No heist player found");
		}
		heistPlayer.Stop();
	}

	public void SwitchAmbientToHorror()
	{
		if (!heistPlayer)
		{
			throw new UnityException("No heist player found");
		}
		AudioClip clip = NetController<SoundController>.Instance.GetClip($"Ingame/Ambient/HAPPY/happy_music_{UnityEngine.Random.Range(0, 3)}.ogg");
		if (!clip)
		{
			throw new UnityException("Horror sound not found");
		}
		heistPlayer.clip = clip;
		heistPlayer.pitch = UnityEngine.Random.Range(0.9f, 1.1f);
		heistPlayer.volume = 0.15f;
		heistPlayer.Play();
	}

	[Server]
	private IEnumerator SendMap()
	{
		if (_networkToSend.Count == 0)
		{
			throw new UnityException("No rooms to send, was the generation successful?");
		}
		_expectedClientRooms.SetSpawnValue((byte)_networkToSend.Count);
		yield return new WaitForSecondsRealtime(1f);
		int counter = 0;
		int batchSize = Mathf.Max(1, ROOM_NET_PER_ROOM_COOLDOWN * 2);
		foreach (NetworkObject item in _networkToSend)
		{
			if ((bool)item && (bool)item.gameObject && !item.IsSpawned)
			{
				item.Spawn(destroyWithScene: true);
				counter++;
				if (counter % batchSize == 0)
				{
					yield return new WaitForSeconds(ROOM_NET_COOLDOWN);
				}
			}
		}
		Debug.Log($"[MapController] Map sent to all clients - {counter} objects spawned");
		yield return null;
	}

	[Client]
	public IEnumerator OnClientRoomSpawned(entity_room_base room)
	{
		yield return new WaitUntil(() => _expectedClientRooms.Value > 0 && !string.IsNullOrEmpty(_selectedWorld.Value.ToString()));
		_spawnedClientRooms++;
		if (_expectedClientRooms.Value > 0 && _spawnedClientRooms >= _expectedClientRooms.Value)
		{
			OnClientReceiveAllRooms();
		}
		WorldSettings generatedWorld = GetGeneratedWorld();
		if (!generatedWorld)
		{
			throw new UnityException("No generated world found on client");
		}
		Contract? contract = NetController<ContractController>.Instance?.GetPickedContract();
		bool flag = generatedWorld.modifiers.HasFlag(ContractModifiers.ICE_WORLD) && contract.HasValue && contract.GetValueOrDefault().modifiers.HasFlag(ContractModifiers.ICE_WORLD);
		bool flag2 = generatedWorld.modifiers.HasFlag(ContractModifiers.DARKNESS_WORLD) && contract.HasValue && contract.GetValueOrDefault().modifiers.HasFlag(ContractModifiers.DARKNESS_WORLD);
		if (flag)
		{
			room.SetModifierTexture(modifierTextures[ContractModifiers.ICE_WORLD], 0.15f);
			room.SetWindowColor(new Color(0f, 0.663f, 1f), 10f);
			room.SetSkyboxColor(new Color(0f, 0.663f, 1f) * 5f);
		}
		room.SetWindowStatus(_seed.Value.ToString(), flag || flag2);
		Debug.Log($"Room {room.name} done spawning on client side ({_spawnedClientRooms} / {_expectedClientRooms.Value})");
	}

	[Client]
	private void OnClientReceiveAllRooms()
	{
		if (!base.IsClient)
		{
			throw new UnityException("Not a client");
		}
		ResetExits();
		ProcessPortals();
		if (NetController<IngameController>.Instance.Status() == INGAME_STATUS.PLAYING)
		{
			SetAmbientMusic(play: true);
		}
		SetSkybox(set: true);
		Debug.Log("Map generated, seed: " + _seed.Value.ToString());
		OnMapGenerated.Invoke(param1: false);
	}

	[Client]
	private void ResetExits()
	{
		if (!base.IsClient)
		{
			throw new UnityException("Not client");
		}
		entity_room_exit[] array = UnityEngine.Object.FindObjectsByType<entity_room_exit>(FindObjectsInactive.Include);
		foreach (entity_room_exit obj in array)
		{
			obj.transform.localEulerAngles = Vector3.zero;
			obj.gameObject.SetActive(value: true);
		}
	}

	[Client]
	private void ProcessPortals()
	{
		WorldSettings generatedWorld = GetGeneratedWorld();
		if (!generatedWorld)
		{
			throw new UnityException("Invalid generated world");
		}
		if (!base.IsClient)
		{
			throw new UnityException("Not client");
		}
		entity_room_base[] source = UnityEngine.Object.FindObjectsByType<entity_room_base>(FindObjectsInactive.Exclude);
		List<entity_vis_portal_2d> list = source.AsValueEnumerable().SelectMany((entity_room_base room) => room.GetComponentsInChildren<entity_vis_portal_2d>(includeInactive: true)).ToList();
		if (list.Count <= 1)
		{
			return;
		}
		HashSet<entity_vis_portal_2d> processed = new HashSet<entity_vis_portal_2d>();
		for (int i = 0; i < list.Count; i++)
		{
			entity_vis_portal_2d port = list[i];
			if (!processed.Add(port) || !port.transform.parent || !port.transform.parent)
			{
				continue;
			}
			List<entity_vis_portal_2d> list2 = (from p in list.AsValueEnumerable()
				where p != port && !processed.Contains(p) && Vector3.Distance(p.transform.parent.position, port.transform.parent.position) < 1f
				select p).ToList();
			if (list2.Count > 0)
			{
				foreach (entity_vis_portal_2d item in list2)
				{
					entity_vis_room roomB = item.GetRoomB();
					if ((bool)roomB)
					{
						if (!port.GetRoomA())
						{
							port.SetRoomA(roomB);
						}
						else if (!port.GetRoomB())
						{
							port.SetRoomB(roomB);
						}
						processed.Add(item);
						UnityEngine.Object.Destroy(item.gameObject);
					}
				}
			}
			if (generatedWorld.visCleanup && (!port.GetRoomA() || !port.GetRoomB()))
			{
				UnityEngine.Object.Destroy(port.gameObject);
			}
		}
		entity_room_base entity_room_base2 = (from r in source.AsValueEnumerable()
			where r.name.StartsWith("entry", StringComparison.InvariantCultureIgnoreCase)
			select r).FirstOrDefault();
		roomVISPortal.SetRoomA(entity_room_base2?.GetComponent<entity_vis_room>());
	}

	[Rpc(SendTo.ClientsAndHost)]
	private void MapClearedBroadcastRPC()
	{
		NetworkManager networkManager = base.NetworkManager;
		if ((object)networkManager == null || !networkManager.IsListening)
		{
			Debug.LogError("Rpc methods can only be invoked after starting the NetworkManager!");
			return;
		}
		if (__rpc_exec_stage != __RpcExecStage.Execute)
		{
			RpcAttribute.RpcAttributeParams attributeParams = default(RpcAttribute.RpcAttributeParams);
			RpcParams rpcParams = default(RpcParams);
			FastBufferWriter bufferWriter = __beginSendRpc(1404326529u, rpcParams, attributeParams, SendTo.ClientsAndHost, RpcDelivery.Reliable);
			__endSendRpc(ref bufferWriter, 1404326529u, rpcParams, attributeParams, SendTo.ClientsAndHost, RpcDelivery.Reliable);
		}
		if (__rpc_exec_stage == __RpcExecStage.Execute)
		{
			__rpc_exec_stage = __RpcExecStage.Send;
			roomVISPortal.SetRoomA(null);
			_spawnedClientRooms = 0;
			SetSkybox(set: false);
			OnMapCleared.Invoke(param1: false);
		}
	}

	[Client]
	private IEnumerator GenerateAIPath()
	{
		foreach (Pathfinding.Progress item in AstarPath.active.ScanAsync())
		{
			_ = item;
			yield return null;
		}
		yield return new WaitForFixedUpdate();
	}

	protected override void __initializeVariables()
	{
		if (_seed == null)
		{
			throw new Exception("MapController._seed cannot be null. All NetworkVariableBase instances must be initialized.");
		}
		_seed.Initialize(this);
		__nameNetworkVariable(_seed, "_seed");
		NetworkVariableFields.Add(_seed);
		if (_selectedWorld == null)
		{
			throw new Exception("MapController._selectedWorld cannot be null. All NetworkVariableBase instances must be initialized.");
		}
		_selectedWorld.Initialize(this);
		__nameNetworkVariable(_selectedWorld, "_selectedWorld");
		NetworkVariableFields.Add(_selectedWorld);
		if (_expectedClientRooms == null)
		{
			throw new Exception("MapController._expectedClientRooms cannot be null. All NetworkVariableBase instances must be initialized.");
		}
		_expectedClientRooms.Initialize(this);
		__nameNetworkVariable(_expectedClientRooms, "_expectedClientRooms");
		NetworkVariableFields.Add(_expectedClientRooms);
		base.__initializeVariables();
	}

	protected override void __initializeRpcs()
	{
		__registerRpc(1404326529u, __rpc_handler_1404326529, "MapClearedBroadcastRPC", RpcInvokePermission.Everyone);
		base.__initializeRpcs();
	}

	private static void __rpc_handler_1404326529(NetworkBehaviour target, FastBufferReader reader, __RpcParams rpcParams)
	{
		NetworkManager networkManager = target.NetworkManager;
		if ((object)networkManager != null && networkManager.IsListening)
		{
			target.__rpc_exec_stage = __RpcExecStage.Execute;
			((MapController)target).MapClearedBroadcastRPC();
			target.__rpc_exec_stage = __RpcExecStage.Send;
		}
	}

	protected internal override string __getTypeName()
	{
		return "MapController";
	}
}
