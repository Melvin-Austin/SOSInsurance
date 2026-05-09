using System;
using System.Collections.Generic;
using System.IO;
using Cysharp.Threading.Tasks;
using Steamworks;
using Unity.Netcode;
using UnityEngine;
using ZLinq;

namespace HyenaQuest;

[DefaultExecutionOrder(-122)]
[DisallowMultipleComponent]
public class SteamworksController : MonoController<SteamworksController>
{
	public static bool IsSteamRunning;

	public static bool IsOverlayOpen;

	public static bool CustomMapsLoaded;

	private readonly Dictionary<string, CustomMap> _customMaps = new Dictionary<string, CustomMap>();

	private static readonly string PLATFORM_FOLDER = "linux";

	private bool _searching;

	private bool _cancelRequest;

	private CallResult<LobbyMatchList_t> _lobbyListCallback;

	private Callback<GameOverlayActivated_t> _GameOverlayActivated;

	private readonly List<WorkshopItem> _workshopItems = new List<WorkshopItem>();

	private CallResult<SteamUGCQueryCompleted_t> _ugcQueryCallback;

	public new void Awake()
	{
		base.Awake();
		_GameOverlayActivated = new Callback<GameOverlayActivated_t>(OnGameOverlayActivated);
		UnityEngine.Object.DontDestroyOnLoad(base.gameObject);
	}

	public IReadOnlyDictionary<string, CustomMap> GetCustomMaps()
	{
		return _customMaps;
	}

	public bool Init()
	{
		if (SteamAPI.InitEx(out var OutSteamErrMsg) != 0)
		{
			Debug.LogError("Failed to initialize steamworks! ERROR: " + OutSteamErrMsg);
			return false;
		}
		SteamInput.Init(bExplicitlyCallRunFrame: false);
		SteamNetworkingUtils.InitRelayNetworkAccess();
		IsSteamRunning = true;
		Load().Forget();
		return true;
	}

	public new void OnDestroy()
	{
		foreach (KeyValuePair<string, CustomMap> customMap in _customMaps)
		{
			if ((bool)customMap.Value.shaderBundle)
			{
				customMap.Value.shaderBundle.UnloadAsync(unloadAllLoadedObjects: true);
			}
			if ((bool)customMap.Value.bundle)
			{
				customMap.Value.bundle.UnloadAsync(unloadAllLoadedObjects: true);
			}
		}
		_customMaps.Clear();
		_lobbyListCallback?.Dispose();
		_GameOverlayActivated?.Dispose();
		_ugcQueryCallback?.Dispose();
		if (IsSteamRunning)
		{
			SteamAPI.Shutdown();
			IsSteamRunning = false;
		}
		base.OnDestroy();
	}

	private async UniTask Load()
	{
		if (IsSteamRunning)
		{
			Debug.Log("Loading workshop mods");
			await LoadWorkshop();
		}
		Debug.Log("Loading custom maps at 'maps/' folder");
		string fullPath = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "maps"));
		if (Directory.Exists(fullPath))
		{
			await LoadMapBundlesFromFolder(fullPath);
		}
		CustomMapsLoaded = true;
	}

	private async UniTask LoadMapBundlesFromFolder(string folder)
	{
		string[] files = Directory.GetFiles(folder, "*.bundle", SearchOption.AllDirectories);
		foreach (string text in files)
		{
			if (!text.EndsWith("_shaders.bundle", StringComparison.OrdinalIgnoreCase))
			{
				string fileName = Path.GetFileName(Path.GetDirectoryName(text) ?? "");
				if (!(fileName == "windows") && !(fileName == "linux"))
				{
					await LoadMapBundle(text);
				}
			}
		}
	}

	private async UniTask LoadMapBundle(string path)
	{
		string bundleName = Path.GetFileNameWithoutExtension(path);
		if (_customMaps.ContainsKey(bundleName))
		{
			Debug.LogWarning("Duplicate bundle '" + bundleName + "', skipping: " + path);
			return;
		}
		AssetBundle shaderBundle = null;
		string directoryName = Path.GetDirectoryName(path);
		string shaderPath = Path.Combine(directoryName, PLATFORM_FOLDER, bundleName + "_shaders.bundle");
		if (File.Exists(shaderPath))
		{
			shaderBundle = await AssetBundle.LoadFromFileAsync(shaderPath);
			if ((bool)shaderBundle)
			{
				Debug.Log("Loaded shader bundle for '" + bundleName + "' (" + PLATFORM_FOLDER + ")");
			}
			else
			{
				Debug.LogWarning("Failed to load shader bundle: " + shaderPath);
			}
		}
		else
		{
			Debug.LogWarning("No shader bundle found, maps should have at least one");
		}
		AssetBundle bundle = await AssetBundle.LoadFromFileAsync(path);
		if (!bundle)
		{
			Debug.LogError("Failed to load bundle: " + path);
			if ((bool)shaderBundle)
			{
				await shaderBundle.UnloadAsync(unloadAllLoadedObjects: true).ToUniTask();
			}
			return;
		}
		AssetBundleRequest request = bundle.LoadAllAssetsAsync<WorldSettings>();
		await request.ToUniTask();
		UnityEngine.Object[] allAssets = request.allAssets;
		WorldSettings worldSettings = ((allAssets != null && allAssets.Length != 0) ? (request.allAssets[0] as WorldSettings) : null);
		if (!worldSettings)
		{
			Debug.LogError("No WorldSettings in bundle: " + bundleName + "! Invalid map");
			await bundle.UnloadAsync(unloadAllLoadedObjects: true).ToUniTask();
			if ((bool)shaderBundle)
			{
				await shaderBundle.UnloadAsync(unloadAllLoadedObjects: true).ToUniTask();
			}
			return;
		}
		RegisterNetworkPrefabs(worldSettings);
		Debug.Log("Loaded custom map '" + worldSettings.name + "' on bundle '" + bundleName + "'");
		_customMaps[bundleName] = new CustomMap
		{
			bundle = bundle,
			shaderBundle = shaderBundle,
			settings = worldSettings
		};
	}

	private void RegisterNetworkPrefabs(WorldSettings settings)
	{
		if (!NETController.Instance)
		{
			throw new UnityException("Missing NETController instance");
		}
		HashSet<int> registered = new HashSet<int>();
		TryRegisterList(settings.rooms);
		TryRegisterList(settings.traversal);
		TryRegisterList(settings.deadEnds);
		TryRegisterList(settings.closers);
		TryRegisterList(settings.interiorClosers);
		TryRegisterList(settings.interiors);
		TryRegisterList(settings.extraNetworkObjects);
		if (settings.entry != null)
		{
			foreach (EntrySettings item in settings.entry)
			{
				TryRegister(item.template);
			}
		}
		if (settings.monsters == null)
		{
			return;
		}
		foreach (MonsterSpawn monster in settings.monsters)
		{
			TryRegisterList(monster.variants);
		}
		void TryRegister(GameObject prefab)
		{
			if ((bool)prefab && registered.Add(prefab.GetInstanceID()) && (bool)prefab.GetComponent<NetworkObject>())
			{
				NETController.Instance.AddNetworkPrefab(prefab);
			}
		}
		void TryRegisterList(List<GameObject> list)
		{
			if (list == null || list.Count <= 0)
			{
				return;
			}
			foreach (GameObject item2 in list)
			{
				TryRegister(item2);
			}
		}
	}

	public bool IsModded(params string[] tags)
	{
		if (tags == null || tags.Length == 0)
		{
			return _workshopItems.Count > 0;
		}
		return _workshopItems.AsValueEnumerable().Any((WorkshopItem item) => tags.AsValueEnumerable().Any((string subTag) => item.tags.Contains(subTag)));
	}

	public List<WorkshopItem> GetWorkshopItems(params string[] tags)
	{
		if (tags == null || tags.Length == 0)
		{
			return _workshopItems;
		}
		List<WorkshopItem> list = new List<WorkshopItem>();
		foreach (WorkshopItem workshopItem in _workshopItems)
		{
			foreach (string item in tags)
			{
				if (workshopItem.tags.Contains(item))
				{
					list.Add(workshopItem);
					break;
				}
			}
		}
		return list;
	}

	public List<ulong> GetWorkshopItemIDs(params string[] tags)
	{
		List<ulong> list = new List<ulong>();
		foreach (WorkshopItem workshopItem in GetWorkshopItems(tags))
		{
			list.Add(workshopItem.fileId.m_PublishedFileId);
		}
		return list;
	}

	public uint GetWorkshopCRC(List<ulong> workshopIds)
	{
		if (workshopIds == null || workshopIds.Count == 0)
		{
			return 0u;
		}
		List<ulong> list = new List<ulong>(workshopIds);
		list.Sort();
		uint num = uint.MaxValue;
		foreach (ulong item in list)
		{
			byte[] bytes = BitConverter.GetBytes(item);
			foreach (byte b in bytes)
			{
				num ^= b;
				for (int j = 0; j < 8; j++)
				{
					num = (((num & 1) == 0) ? (num >> 1) : ((num >> 1) ^ 0xEDB88320u));
				}
			}
		}
		return ~num;
	}

	private async UniTask LoadWorkshop()
	{
		_workshopItems.Clear();
		uint numSubscribedItems = SteamUGC.GetNumSubscribedItems();
		if (numSubscribedItems == 0)
		{
			return;
		}
		PublishedFileId_t[] pvecPublishedFileID = new PublishedFileId_t[numSubscribedItems];
		uint subscribedItems = SteamUGC.GetSubscribedItems(pvecPublishedFileID, numSubscribedItems);
		if (subscribedItems == 0)
		{
			return;
		}
		UGCQueryHandle_t uGCQueryHandle_t = SteamUGC.CreateQueryUGCDetailsRequest(pvecPublishedFileID, subscribedItems);
		if (uGCQueryHandle_t == UGCQueryHandle_t.Invalid)
		{
			return;
		}
		UniTaskCompletionSource<SteamUGCQueryCompleted_t> tcs = new UniTaskCompletionSource<SteamUGCQueryCompleted_t>();
		if (_ugcQueryCallback == null)
		{
			_ugcQueryCallback = CallResult<SteamUGCQueryCompleted_t>.Create();
		}
		SteamAPICall_t hAPICall = SteamUGC.SendQueryUGCRequest(uGCQueryHandle_t);
		_ugcQueryCallback.Set(hAPICall, delegate(SteamUGCQueryCompleted_t result, bool bIOFailure)
		{
			if (bIOFailure || result.m_eResult != EResult.k_EResultOK)
			{
				SteamUGC.ReleaseQueryUGCRequest(result.m_handle);
				tcs.TrySetResult(default(SteamUGCQueryCompleted_t));
			}
			else
			{
				tcs.TrySetResult(result);
			}
		});
		SteamUGCQueryCompleted_t queryResult = await tcs.Task;
		if (queryResult.m_handle == UGCQueryHandle_t.Invalid)
		{
			return;
		}
		uint i = 0u;
		while (i < queryResult.m_unNumResultsReturned)
		{
			uint punTimeStamp;
			if (SteamUGC.GetQueryUGCResult(queryResult.m_handle, i, out var pDetails))
			{
				WorkshopItem workshopItem = default(WorkshopItem);
				workshopItem.fileId = pDetails.m_nPublishedFileId;
				workshopItem.title = pDetails.m_rgchTitle;
				workshopItem.description = pDetails.m_rgchDescription;
				workshopItem.tags = new HashSet<string>();
				workshopItem.creator = new CSteamID(pDetails.m_ulSteamIDOwner);
				workshopItem.status = (EItemState)SteamUGC.GetItemState(pDetails.m_nPublishedFileId);
				WorkshopItem item = workshopItem;
				if (SteamUGC.GetItemInstallInfo(pDetails.m_nPublishedFileId, out var _, out var pchFolder, 1024u, out punTimeStamp))
				{
					item.installPath = pchFolder;
				}
				uint queryUGCNumTags = SteamUGC.GetQueryUGCNumTags(queryResult.m_handle, i);
				for (uint num = 0u; num < queryUGCNumTags; num++)
				{
					SteamUGC.GetQueryUGCTag(queryResult.m_handle, i, num, out var pchValue, 256u);
					if (!string.IsNullOrEmpty(pchValue))
					{
						item.tags.Add(pchValue);
					}
				}
				_workshopItems.Add(item);
				if (item.tags.Contains("MAP") && (item.status & EItemState.k_EItemStateInstalled) != 0 && !string.IsNullOrEmpty(item.installPath))
				{
					await LoadMapBundlesFromFolder(item.installPath);
				}
			}
			punTimeStamp = i++;
		}
		SteamUGC.ReleaseQueryUGCRequest(queryResult.m_handle);
	}

	public void CancelServerSearch()
	{
		if (_searching)
		{
			_cancelRequest = true;
		}
	}

	public void SearchLobbies(Action<List<SteamLobby>> onComplete)
	{
		if (_searching)
		{
			return;
		}
		List<SteamLobby> lobbyList = new List<SteamLobby>();
		if (!IsSteamRunning)
		{
			onComplete?.Invoke(lobbyList);
			return;
		}
		_searching = true;
		_cancelRequest = false;
		SteamMatchmaking.AddRequestLobbyListDistanceFilter(ELobbyDistanceFilter.k_ELobbyDistanceFilterWorldwide);
		SteamMatchmaking.AddRequestLobbyListResultCountFilter(200);
		SteamMatchmaking.AddRequestLobbyListFilterSlotsAvailable(1);
		SteamMatchmaking.AddRequestLobbyListStringFilter("Version", SteamApps.GetAppBuildId().ToString(), ELobbyComparison.k_ELobbyComparisonEqual);
		if (_lobbyListCallback == null)
		{
			_lobbyListCallback = CallResult<LobbyMatchList_t>.Create();
		}
		SteamAPICall_t hAPICall = SteamMatchmaking.RequestLobbyList();
		_lobbyListCallback.Set(hAPICall, delegate(LobbyMatchList_t result, bool bIOFailure)
		{
			if (_cancelRequest)
			{
				_searching = false;
				_cancelRequest = false;
				onComplete?.Invoke(lobbyList);
			}
			else
			{
				_searching = false;
				for (int i = 0; i < result.m_nLobbiesMatching; i++)
				{
					CSteamID lobbyByIndex = SteamMatchmaking.GetLobbyByIndex(i);
					int numLobbyMembers = SteamMatchmaking.GetNumLobbyMembers(lobbyByIndex);
					int lobbyMemberLimit = SteamMatchmaking.GetLobbyMemberLimit(lobbyByIndex);
					int.TryParse(SteamMatchmaking.GetLobbyData(lobbyByIndex, "round"), out var result2);
					string lobbyData = SteamMatchmaking.GetLobbyData(lobbyByIndex, "name");
					lobbyList.Add(new SteamLobby
					{
						id = lobbyByIndex,
						name = lobbyData,
						players = numLobbyMembers,
						maxPlayers = lobbyMemberLimit,
						round = result2,
						isFull = (numLobbyMembers >= lobbyMemberLimit),
						isModded = string.Equals(SteamMatchmaking.GetLobbyData(lobbyByIndex, "mods"), "1", StringComparison.InvariantCultureIgnoreCase),
						isCheating = (string.Equals(SteamMatchmaking.GetLobbyData(lobbyByIndex, "cheats"), "1", StringComparison.InvariantCultureIgnoreCase) || lobbyMemberLimit > NETController.DEFAULT_MAX_PLAYERS)
					});
				}
				onComplete(lobbyList);
			}
		});
	}

	public void Update()
	{
		if (IsSteamRunning)
		{
			SteamAPI.RunCallbacks();
		}
	}

	private void OnGameOverlayActivated(GameOverlayActivated_t param)
	{
		if (IsSteamRunning)
		{
			IsOverlayOpen = param.m_bActive == 1;
			MonoController<StartupController>.Instance?.OnCursorRequestUpdate();
		}
	}
}
