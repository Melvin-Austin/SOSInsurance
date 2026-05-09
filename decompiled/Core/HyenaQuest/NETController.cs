using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using Netcode.Transports;
using Steamworks;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;
using UnityEngine.SceneManagement;
using ZLinq;

namespace HyenaQuest;

[DisallowMultipleComponent]
[DefaultExecutionOrder(-119)]
public class NETController : NetworkManager
{
	public static readonly int DEFAULT_MAX_PLAYERS = 5;

	public static int MAX_PLAYERS = DEFAULT_MAX_PLAYERS;

	public static readonly int MAX_CHEAT_PLAYERS = 15;

	public static ulong? LOBBY_CONNECT_ID;

	public static ELobbyType LOBBY_VISIBILITY = ELobbyType.k_ELobbyTypePublic;

	public static string LOBBY_NAME;

	public static string LAST_NETWORK_ERROR;

	public static Func<ConnectionApprovalRequest, (string, bool)> ValidatePlayerJoin;

	private SteamNetworkingSocketsTransport _steamworksTransport;

	private UnityTransport _unityTransport;

	private bool _clientDisconnect;

	protected byte[] _RGUB_TICKET = new byte[1024];

	protected uint _CUB_TICKET;

	protected Callback<LobbyCreated_t> _lobbyCreated;

	protected Callback<LobbyEnter_t> _lobbyJoined;

	protected Callback<GameLobbyJoinRequested_t> _lobbyJoinRequest;

	protected Callback<GameRichPresenceJoinRequested_t> _richPresenceJoinRequested;

	protected bool _requestedTicket;

	public GameEvent<bool> OnCheatsUpdate = new GameEvent<bool>();

	public static bool SV_CHEATS { get; private set; }

	public static NETController Instance => (NETController)NetworkManager.Singleton;

	public static T Get<T>(NetworkBehaviourReference refObj) where T : NetworkBehaviour
	{
		if (!NetworkManager.Singleton || !refObj.TryGet(out T networkBehaviour, (NetworkManager)null))
		{
			return null;
		}
		return networkBehaviour;
	}

	public void Awake()
	{
		MethodInfo method = typeof(NetworkManager).GetMethod("Awake", BindingFlags.Instance | BindingFlags.NonPublic);
		if (method != null)
		{
			method.Invoke(this, null);
		}
		_unityTransport = GetComponent<UnityTransport>();
		if (!_unityTransport)
		{
			throw new UnityException("Missing UnityTransport component");
		}
		_steamworksTransport = GetComponent<SteamNetworkingSocketsTransport>();
		if (!_steamworksTransport)
		{
			throw new UnityException("Missing SteamNetworkingSocketsTransport component");
		}
		NetworkConfig.ConnectionApproval = true;
		base.OnServerStarted += OnStarted;
		base.OnClientStopped += OnDisconnect;
		base.OnTransportFailure += TransportFailure;
		base.ConnectionApprovalCallback = (Action<ConnectionApprovalRequest, ConnectionApprovalResponse>)Delegate.Combine(base.ConnectionApprovalCallback, new Action<ConnectionApprovalRequest, ConnectionApprovalResponse>(OnConnectionApprovalCallback));
		_lobbyCreated = Callback<LobbyCreated_t>.Create(OnLobbyCreated);
		_lobbyJoined = Callback<LobbyEnter_t>.Create(OnLobbyEntered);
		_lobbyJoinRequest = Callback<GameLobbyJoinRequested_t>.Create(OnLobbyJoinRequest);
		_richPresenceJoinRequested = Callback<GameRichPresenceJoinRequested_t>.Create(OnRichPresenceJoinRequested);
	}

	public void Init()
	{
		GCHandle gCHandle = GCHandle.Alloc(new int[1] { 5242880 }, GCHandleType.Pinned);
		SteamNetworkingUtils.SetConfigValue(ESteamNetworkingConfigValue.k_ESteamNetworkingConfig_SendBufferSize, ESteamNetworkingConfigScope.k_ESteamNetworkingConfig_Global, IntPtr.Zero, ESteamNetworkingConfigDataType.k_ESteamNetworkingConfig_Int32, gCHandle.AddrOfPinnedObject());
		gCHandle.Free();
		GCHandle gCHandle2 = GCHandle.Alloc(new int[1] { 1 }, GCHandleType.Pinned);
		SteamNetworkingUtils.SetConfigValue(ESteamNetworkingConfigValue.k_ESteamNetworkingConfig_P2P_Transport_ICE_Enable, ESteamNetworkingConfigScope.k_ESteamNetworkingConfig_Global, IntPtr.Zero, ESteamNetworkingConfigDataType.k_ESteamNetworkingConfig_Int32, gCHandle2.AddrOfPinnedObject());
		gCHandle2.Free();
		GCHandle gCHandle3 = GCHandle.Alloc(new int[1] { 60000 }, GCHandleType.Pinned);
		SteamNetworkingUtils.SetConfigValue(ESteamNetworkingConfigValue.k_ESteamNetworkingConfig_TimeoutInitial, ESteamNetworkingConfigScope.k_ESteamNetworkingConfig_Global, IntPtr.Zero, ESteamNetworkingConfigDataType.k_ESteamNetworkingConfig_Int32, gCHandle3.AddrOfPinnedObject());
		gCHandle3.Free();
		GCHandle gCHandle4 = GCHandle.Alloc(new int[1] { 60000 }, GCHandleType.Pinned);
		SteamNetworkingUtils.SetConfigValue(ESteamNetworkingConfigValue.k_ESteamNetworkingConfig_TimeoutConnected, ESteamNetworkingConfigScope.k_ESteamNetworkingConfig_Global, IntPtr.Zero, ESteamNetworkingConfigDataType.k_ESteamNetworkingConfig_Int32, gCHandle4.AddrOfPinnedObject());
		gCHandle4.Free();
		if ((bool)MonoController<DiscordController>.Instance)
		{
			MonoController<DiscordController>.Instance.OnActivityJoin += new Action<ulong>(OnDiscordActivityJoinCallback);
		}
		SetRichPresence("On MainMenu");
	}

	public void DisconnectClientWithReason(ulong connectionID, string reason)
	{
		if (!base.IsServer)
		{
			throw new UnityException("Server only");
		}
		if (NetworkConfig.NetworkTransport == _steamworksTransport)
		{
			_steamworksTransport?.SetPendingDisconnectReason(GetSteamID(connectionID), reason);
		}
		DisconnectClient(connectionID, reason);
	}

	[Server]
	public void SetCheats(bool enable)
	{
		if (SV_CHEATS != enable)
		{
			SV_CHEATS = enable;
			if (enable)
			{
				Debug.LogWarning("\n\n==============================================================\nCHEATS ENABLED, ACHIEVEMENTS / STATS DISABLED!\n==============================================================\n\n");
			}
			if (base.IsServer && SteamworksController.IsSteamRunning && LOBBY_CONNECT_ID.HasValue)
			{
				SteamMatchmaking.SetLobbyData(new CSteamID(LOBBY_CONNECT_ID.Value), "cheats", enable ? "1" : "0");
			}
			OnCheatsUpdate?.Invoke(enable);
		}
	}

	private void OnConnectionApprovalCallback(ConnectionApprovalRequest request, ConnectionApprovalResponse response)
	{
		if (request.ClientNetworkId == 0L)
		{
			response.Approved = true;
			response.Reason = "";
			return;
		}
		CSteamID steamID = GetSteamID(request.ClientNetworkId);
		bool flag = NetworkConfig.NetworkTransport == _steamworksTransport;
		if (base.ConnectedClientsIds.Count >= MAX_PLAYERS)
		{
			response.Approved = false;
			response.Reason = "ingame.ui.disconnected.reason.server-full";
			if (flag)
			{
				_steamworksTransport?.SetPendingDisconnectReason(steamID, response.Reason);
			}
			Debug.LogWarning($"Connection {request.ClientNetworkId} declined -> {response.Reason}");
			return;
		}
		if (request.Payload == null || request.Payload.Length < 8)
		{
			response.Approved = false;
			response.Reason = "ingame.ui.disconnected.reason.generic-fail";
			if (flag)
			{
				_steamworksTransport?.SetPendingDisconnectReason(steamID, response.Reason);
			}
			Debug.LogWarning($"Connection {request.ClientNetworkId} declined -> {response.Reason}");
			return;
		}
		int num = BitConverter.ToInt32(request.Payload, 0);
		if (request.Payload.Length < 4 + num + 4 + 4)
		{
			response.Approved = false;
			response.Reason = "ingame.ui.disconnected.reason.generic-fail";
			if (flag)
			{
				_steamworksTransport?.SetPendingDisconnectReason(steamID, response.Reason);
			}
			Debug.LogWarning($"Connection {request.ClientNetworkId} declined -> {response.Reason}");
			return;
		}
		byte[] array = new byte[num];
		Buffer.BlockCopy(request.Payload, 4, array, 0, num);
		uint num2 = BitConverter.ToUInt32(request.Payload, 4 + num);
		if (num2 != (uint)SteamApps.GetAppBuildId())
		{
			response.Approved = false;
			response.Reason = "ingame.ui.disconnected.reason.version-mismatch";
			if (flag)
			{
				_steamworksTransport?.SetPendingDisconnectReason(steamID, response.Reason);
			}
			Debug.LogWarning($"Connection {request.ClientNetworkId} declined -> {response.Reason} (Client: {num2}, Server: {SteamApps.GetAppBuildId()})");
			return;
		}
		int num3 = 4 + num + 4;
		if (request.Payload.Length < num3 + 4)
		{
			response.Approved = false;
			response.Reason = "ingame.ui.disconnected.reason.generic-fail";
			if (flag)
			{
				_steamworksTransport?.SetPendingDisconnectReason(steamID, response.Reason);
			}
			Debug.LogWarning($"Connection {request.ClientNetworkId} declined -> {response.Reason} (Missing workshop data)");
			return;
		}
		int num4 = BitConverter.ToInt32(request.Payload, num3);
		if (request.Payload.Length < num3 + 4 + num4 * 8)
		{
			response.Approved = false;
			response.Reason = "ingame.ui.disconnected.reason.generic-fail";
			if (flag)
			{
				_steamworksTransport?.SetPendingDisconnectReason(steamID, response.Reason);
			}
			Debug.LogWarning($"Connection {request.ClientNetworkId} declined -> {response.Reason} (Invalid workshop data)");
			return;
		}
		HashSet<ulong> hashSet = new HashSet<ulong>();
		int num5 = num3 + 4;
		for (int i = 0; i < num4; i++)
		{
			ulong item = BitConverter.ToUInt64(request.Payload, num5 + i * 8);
			hashSet.Add(item);
		}
		HashSet<ulong> hashSet2 = new HashSet<ulong>();
		if (MonoController<SteamworksController>.Instance != null)
		{
			hashSet2 = new HashSet<ulong>(MonoController<SteamworksController>.Instance.GetWorkshopItemIDs("SHARED"));
		}
		if (hashSet.Count != hashSet2.Count)
		{
			List<ulong> list = new List<ulong>();
			foreach (ulong item2 in hashSet2)
			{
				if (!hashSet.Contains(item2))
				{
					list.Add(item2);
				}
			}
			if (list.Count > 0)
			{
				response.Approved = false;
				response.Reason = "ingame.ui.disconnected.reason.mods-mismatch||" + string.Join(", ", list);
				if (flag)
				{
					_steamworksTransport?.SetPendingDisconnectReason(steamID, response.Reason);
				}
				Debug.LogWarning($"Connection {request.ClientNetworkId} declined -> {response.Reason}");
				return;
			}
		}
		if (!steamID.IsValid())
		{
			response.Approved = false;
			response.Reason = "ingame.ui.disconnected.reason.invalid-steamid";
			if (flag)
			{
				_steamworksTransport?.SetPendingDisconnectReason(steamID, response.Reason);
			}
			Debug.LogWarning($"Connection {request.ClientNetworkId} declined -> {response.Reason}");
			return;
		}
		if (SteamUser.BeginAuthSession(array, num, steamID) != 0)
		{
			response.Approved = false;
			response.Reason = "ingame.ui.disconnected.reason.invalid-auth";
			if (flag)
			{
				_steamworksTransport?.SetPendingDisconnectReason(steamID, response.Reason);
			}
			SteamUser.EndAuthSession(steamID);
			Debug.LogWarning($"Connection {request.ClientNetworkId} declined -> {response.Reason}");
			return;
		}
		SteamUser.EndAuthSession(steamID);
		Dictionary<ulong, string> dictionary = MonoController<SettingsController>.Instance?.GetBanList();
		if (dictionary != null && dictionary.ContainsKey(steamID.m_SteamID))
		{
			response.Approved = false;
			response.Reason = "ingame.ui.disconnected.reason.banned";
			if (flag)
			{
				_steamworksTransport?.SetPendingDisconnectReason(steamID, response.Reason);
			}
			Debug.LogWarning($"Connection {request.ClientNetworkId} declined -> {response.Reason}");
			return;
		}
		if (ValidatePlayerJoin != null)
		{
			(string, bool) tuple = ValidatePlayerJoin(request);
			response.Approved = tuple.Item2;
			(response.Reason, _) = tuple;
			if (!response.Approved)
			{
				if (flag)
				{
					_steamworksTransport?.SetPendingDisconnectReason(steamID, response.Reason);
				}
				Debug.LogWarning($"Connection {request.ClientNetworkId} declined -> {response.Reason}");
				return;
			}
		}
		response.Approved = true;
		response.Reason = "";
	}

	public void Disconnect(string internalReason = null)
	{
		if (!base.ShutdownInProgress)
		{
			Shutdown();
			CleanupConnection();
			_clientDisconnect = true;
			LAST_NETWORK_ERROR = internalReason;
		}
	}

	private void CleanupConnection()
	{
		try
		{
			if (SteamworksController.IsSteamRunning)
			{
				if (LOBBY_CONNECT_ID.HasValue && LOBBY_CONNECT_ID != 1337)
				{
					SteamMatchmaking.LeaveLobby(new CSteamID(LOBBY_CONNECT_ID.Value));
				}
				SteamFriends.ClearRichPresence();
			}
		}
		catch (Exception ex)
		{
			Debug.LogWarning("CleanupConnection error: " + ex.Message);
		}
		MonoController<DiscordController>.Instance?.ResetStatus();
		ValidatePlayerJoin = null;
		LOBBY_CONNECT_ID = null;
	}

	public void OnDestroy()
	{
		if (!base.ShutdownInProgress && base.IsListening)
		{
			Shutdown();
		}
		CleanupConnection();
		base.OnServerStarted -= OnStarted;
		base.OnTransportFailure -= TransportFailure;
		base.OnClientStopped -= OnDisconnect;
		base.ConnectionApprovalCallback = (Action<ConnectionApprovalRequest, ConnectionApprovalResponse>)Delegate.Remove(base.ConnectionApprovalCallback, new Action<ConnectionApprovalRequest, ConnectionApprovalResponse>(OnConnectionApprovalCallback));
		ValidatePlayerJoin = null;
		if (_lobbyCreated != null)
		{
			_lobbyCreated.Dispose();
		}
		if (_lobbyJoined != null)
		{
			_lobbyJoined.Dispose();
		}
		if (_lobbyJoinRequest != null)
		{
			_lobbyJoinRequest.Dispose();
		}
		if (_richPresenceJoinRequested != null)
		{
			_richPresenceJoinRequested.Dispose();
		}
		if ((bool)MonoController<DiscordController>.Instance)
		{
			MonoController<DiscordController>.Instance.OnActivityJoin -= new Action<ulong>(OnDiscordActivityJoinCallback);
		}
	}

	[Server]
	public CSteamID GetSteamID(ulong connectionID)
	{
		if (connectionID == 0L)
		{
			return SteamUser.GetSteamID();
		}
		Dictionary<ulong, ulong> connectedSteamIDs = GetConnectedSteamIDs();
		if (connectedSteamIDs == null)
		{
			return default(CSteamID);
		}
		connectedSteamIDs.TryGetValue(connectionID, out var value);
		return new CSteamID(value);
	}

	public IEnumerator StartNetwork()
	{
		ValidatePlayerJoin = null;
		if (LOBBY_CONNECT_ID == 1337)
		{
			Debug.Log("Creating training room...");
			NetworkConfig.NetworkTransport = _unityTransport;
			NetworkManager.Singleton.StartHost();
			yield break;
		}
		NetworkConfig.NetworkTransport = _steamworksTransport;
		if (!LOBBY_CONNECT_ID.HasValue)
		{
			Debug.Log("Starting host...");
			SteamMatchmaking.CreateLobby(LOBBY_VISIBILITY, MAX_PLAYERS);
		}
		else
		{
			Debug.Log("Joining host...");
			SteamMatchmaking.JoinLobby(new CSteamID(LOBBY_CONNECT_ID.Value));
		}
	}

	[Server]
	public bool IsConnected(ulong id)
	{
		return base.ConnectedClientsIds.Contains(id);
	}

	public IEnumerator ConnectToServer(ulong lobbyID)
	{
		if (base.IsHost || base.IsConnectedClient)
		{
			Disconnect();
			yield return new WaitForSecondsRealtime(0.25f);
		}
		LOBBY_CONNECT_ID = lobbyID;
		UnityEngine.SceneManagement.SceneManager.LoadScene("LOADING");
	}

	private void OnDiscordActivityJoinCallback(ulong lobbyID)
	{
		Debug.Log("Joining discord lobby...");
		if (!new CSteamID(lobbyID).IsLobby())
		{
			Debug.LogError($"Invalid discord invite! {lobbyID} is not a valid steam lobby id");
		}
		else
		{
			StartCoroutine(ConnectToServer(lobbyID));
		}
	}

	private void OnRichPresenceJoinRequested(GameRichPresenceJoinRequested_t param)
	{
		if (!string.IsNullOrEmpty(param.m_rgchConnect) && ulong.TryParse(param.m_rgchConnect, out var result))
		{
			StartCoroutine(ConnectToServer(result));
		}
	}

	private void OnLobbyJoinRequest(GameLobbyJoinRequested_t param)
	{
		if (!param.m_steamIDLobby.IsLobby())
		{
			Debug.LogWarning("Invalid lobby id");
		}
		else
		{
			StartCoroutine(ConnectToServer((ulong)param.m_steamIDLobby));
		}
	}

	private bool RequestAndSetSteamAuth(CSteamID steamID)
	{
		if (!steamID.IsValid())
		{
			return false;
		}
		byte[] array = new byte[1024];
		SteamNetworkingIdentity pSteamNetworkingIdentity = default(SteamNetworkingIdentity);
		pSteamNetworkingIdentity.SetSteamID(steamID);
		uint pcbTicket;
		HAuthTicket authSessionTicket = SteamUser.GetAuthSessionTicket(array, 1024, out pcbTicket, ref pSteamNetworkingIdentity);
		if (authSessionTicket == HAuthTicket.Invalid)
		{
			SteamUser.CancelAuthTicket(authSessionTicket);
			return false;
		}
		byte[] bytes = BitConverter.GetBytes(pcbTicket);
		byte[] bytes2 = BitConverter.GetBytes((uint)SteamApps.GetAppBuildId());
		List<ulong> list = MonoController<SteamworksController>.Instance?.GetWorkshopItemIDs("SHARED") ?? new List<ulong>();
		byte[] bytes3 = BitConverter.GetBytes(list.Count);
		int num = list.Count * 8;
		byte[] array2 = new byte[4 + pcbTicket + 4 + 4 + num];
		Buffer.BlockCopy(bytes, 0, array2, 0, 4);
		Buffer.BlockCopy(array, 0, array2, 4, (int)pcbTicket);
		Buffer.BlockCopy(bytes2, 0, array2, (int)(4 + pcbTicket), 4);
		Buffer.BlockCopy(bytes3, 0, array2, (int)(4 + pcbTicket + 4), 4);
		int num2 = (int)(4 + pcbTicket + 4 + 4);
		for (int i = 0; i < list.Count; i++)
		{
			Buffer.BlockCopy(BitConverter.GetBytes(list[i]), 0, array2, num2 + i * 8, 8);
		}
		NetworkConfig.ConnectionData = array2;
		return true;
	}

	private void OnLobbyEntered(LobbyEnter_t param)
	{
		if (NetworkManager.Singleton.IsHost)
		{
			return;
		}
		if (NetworkConfig.NetworkTransport != _steamworksTransport)
		{
			OnConnectionEnd("Invalid transport used for steam lobby");
			return;
		}
		CSteamID steamIDLobby = new CSteamID(param.m_ulSteamIDLobby);
		if (!steamIDLobby.IsLobby())
		{
			OnConnectionEnd("ingame.ui.disconnected.reason.invalid-host");
			return;
		}
		CSteamID lobbyOwner = SteamMatchmaking.GetLobbyOwner(steamIDLobby);
		if (lobbyOwner.IsValid())
		{
			_steamworksTransport.ConnectToSteamID = lobbyOwner.m_SteamID;
			if (!RequestAndSetSteamAuth(lobbyOwner))
			{
				OnConnectionEnd("ingame.ui.disconnected.reason.invalid-auth");
				return;
			}
			if (!NetworkManager.Singleton.StartClient())
			{
				OnConnectionEnd("ingame.ui.disconnected.reason.generic-fail");
				return;
			}
			string text = steamIDLobby.ToString();
			int numLobbyMembers = SteamMatchmaking.GetNumLobbyMembers(steamIDLobby);
			int lobbyMemberLimit = SteamMatchmaking.GetLobbyMemberLimit(steamIDLobby);
			SteamFriends.SetRichPresence("connect", text);
			SteamFriends.SetRichPresence("steam_player_group", text);
			SteamFriends.SetRichPresence("steam_player_group_size", numLobbyMembers.ToString());
			bool privateParty = SteamMatchmaking.GetLobbyData(steamIDLobby, "type") == 1.ToString() || LOBBY_VISIBILITY != ELobbyType.k_ELobbyTypePublic;
			MonoController<DiscordController>.Instance?.JoinParty(text, numLobbyMembers, lobbyMemberLimit, privateParty);
		}
		else
		{
			OnConnectionEnd("ingame.ui.disconnected.reason.invalid-host");
		}
	}

	private void OnLobbyCreated(LobbyCreated_t param)
	{
		if (param.m_eResult != EResult.k_EResultOK)
		{
			OnConnectionEnd("ingame.ui.disconnected.reason.lobby-failure");
			return;
		}
		CSteamID cSteamID = new CSteamID(param.m_ulSteamIDLobby);
		LOBBY_CONNECT_ID = (ulong)cSteamID;
		CSteamID cSteamID2 = cSteamID;
		Debug.Log("Lobby created: " + cSteamID2.ToString());
		CSteamID steamID = SteamUser.GetSteamID();
		if (!steamID.IsValid())
		{
			OnConnectionEnd("ingame.ui.disconnected.reason.invalid-host");
			return;
		}
		string text = cSteamID.ToString();
		SteamMatchmaking.SetLobbyData(cSteamID, "Name", string.IsNullOrEmpty(LOBBY_NAME) ? (SteamFriends.GetPersonaName() + "'s server") : LOBBY_NAME);
		SteamMatchmaking.SetLobbyData(cSteamID, "HostAddress", steamID.ToString());
		SteamMatchmaking.SetLobbyData(cSteamID, "Version", SteamApps.GetAppBuildId().ToString());
		CSteamID steamIDLobby = cSteamID;
		SteamworksController instance = MonoController<SteamworksController>.Instance;
		SteamMatchmaking.SetLobbyData(steamIDLobby, "mods", ((object)instance != null && instance.IsModded("SERVER", "SHARED")) ? "1" : "0");
		SteamMatchmaking.SetLobbyData(cSteamID, "cheats", SV_CHEATS ? "1" : "0");
		SteamMatchmaking.SetLobbyMemberLimit(cSteamID, MAX_PLAYERS);
		SteamMatchmaking.SetLobbyJoinable(cSteamID, bLobbyJoinable: true);
		SteamMatchmaking.SetLobbyOwner(cSteamID, steamID);
		if (!NetworkManager.Singleton.StartHost())
		{
			OnConnectionEnd("Failed to start host");
			return;
		}
		SteamFriends.SetRichPresence("connect", text);
		SteamFriends.SetRichPresence("steam_player_group", text);
		SteamFriends.SetRichPresence("steam_player_group_size", "1");
		MonoController<DiscordController>.Instance?.SetParty(text, MAX_PLAYERS, LOBBY_VISIBILITY != ELobbyType.k_ELobbyTypePublic);
	}

	[Server]
	public Dictionary<ulong, ulong> GetConnectedSteamIDs()
	{
		if (NetworkConfig.NetworkTransport == _steamworksTransport)
		{
			object obj = typeof(NetworkManager).GetField("ConnectionManager", BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(this);
			return obj?.GetType().GetField("ClientIdToTransportIdMap", BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(obj) as Dictionary<ulong, ulong>;
		}
		return base.ConnectedClients.AsValueEnumerable().ToDictionary((KeyValuePair<ulong, NetworkClient> val) => val.Key, (KeyValuePair<ulong, NetworkClient> val) => val.Value.ClientId);
	}

	public static void SetTimelineStatus(bool playing)
	{
		if (SteamworksController.IsSteamRunning)
		{
			SteamTimeline.SetTimelineGameMode(playing ? ETimelineGameMode.k_ETimelineGameMode_Playing : ETimelineGameMode.k_ETimelineGameMode_Staging);
		}
	}

	public static void AddTimelineEvent(string title, string description, TIMELINE_EVENT_ICON icon, uint priority, float start, float end, bool featured)
	{
		if (SteamworksController.IsSteamRunning)
		{
			SteamTimeline.AddRangeTimelineEvent(title, description, icon.ToString().ToLower(), priority, start, end, featured ? ETimelineEventClipPriority.k_ETimelineEventClipPriority_Featured : ETimelineEventClipPriority.k_ETimelineEventClipPriority_Standard);
		}
	}

	public static void AddInstantTimelineEvent(string title, string description, TIMELINE_EVENT_ICON icon, uint priority, bool featured)
	{
		if (SteamworksController.IsSteamRunning)
		{
			SteamTimeline.AddInstantaneousTimelineEvent(title, description, icon.ToString().ToLower(), priority, 0f, featured ? ETimelineEventClipPriority.k_ETimelineEventClipPriority_Featured : ETimelineEventClipPriority.k_ETimelineEventClipPriority_Standard);
		}
	}

	public static void SetRichPresence(string presence)
	{
		if (SteamworksController.IsSteamRunning)
		{
			if (!SteamFriends.SetRichPresence("gamestatus", presence) || !SteamFriends.SetRichPresence("steam_display", "#IngameStatus"))
			{
				Debug.LogWarning("Failed to set rich presence to " + presence);
			}
			MonoController<DiscordController>.Instance?.SetStatus(presence);
		}
	}

	public static void SetCurrentRound(byte round)
	{
		if (SteamworksController.IsSteamRunning && LOBBY_CONNECT_ID.HasValue)
		{
			SteamMatchmaking.SetLobbyData(new CSteamID(LOBBY_CONNECT_ID.Value), "round", round.ToString());
		}
	}

	public static void UpdateRichPresenceCount(byte total)
	{
		if (SteamworksController.IsSteamRunning)
		{
			SteamFriends.SetRichPresence("steam_player_group_size", total.ToString());
			MonoController<DiscordController>.Instance?.UpdatePartySize(total);
		}
	}

	private void OnStarted()
	{
		if (base.IsServer)
		{
			if (base.SceneManager == null)
			{
				throw new UnityException("Missing SceneManager");
			}
			if (base.SceneManager.LoadScene((LOBBY_CONNECT_ID == 1337) ? "TRAINING" : "INGAME", LoadSceneMode.Single) != SceneEventProgressStatus.Started)
			{
				throw new UnityException("Failed to load scene");
			}
		}
	}

	private void OnDisconnect(bool server)
	{
		if (NetworkConfig.NetworkTransport == _steamworksTransport)
		{
			OnConnectionEnd(_steamworksTransport?.LastDisconnectReason);
		}
		else
		{
			OnConnectionEnd(base.DisconnectReason);
		}
	}

	private void TransportFailure()
	{
		Debug.LogWarning("TransportFailure");
		OnDisconnect(server: false);
	}

	private void OnConnectionEnd(string errorMessage = null)
	{
		CleanupConnection();
		if (!base.IsServer)
		{
			string text = errorMessage ?? base.DisconnectReason ?? "Unknown connection error";
			if (text.StartsWith("ingame.ui.disconnected.reason"))
			{
				text = MonoController<LocalizationController>.Instance?.Get(text) ?? text;
			}
			if (text.Contains("rendezvous") || text.Contains("timeout", StringComparison.OrdinalIgnoreCase))
			{
				text = MonoController<LocalizationController>.Instance?.Get("ingame.ui.disconnected.reason.network-issue") ?? text;
			}
			Debug.Log("--- Disconnected from server ---");
			Debug.Log(text);
			LAST_NETWORK_ERROR = (_clientDisconnect ? null : text);
			_clientDisconnect = false;
		}
		else
		{
			Debug.Log("--- Server shutdown ---");
		}
		UnityEngine.SceneManagement.SceneManager.LoadScene("MAINMENU");
	}
}
