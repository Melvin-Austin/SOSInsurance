using System;
using UnityEngine;

namespace HyenaQuest;

[DisallowMultipleComponent]
[DefaultExecutionOrder(-121)]
public class DiscordController : MonoController<DiscordController>
{
	public static readonly ulong CLIENT_ID = 1396198253858656256uL;

	public static readonly uint STEAM_ID = 3376480u;

	public GameEvent<bool> OnLinkUpdate = new GameEvent<bool>();

	public GameEvent<bool> OnAuthorizing = new GameEvent<bool>();

	public GameEvent<ulong> OnActivityJoin = new GameEvent<ulong>();

	private string _token;

	private string _codeVerifier;

	private string _userID;

	private string _currentStatus;

	private bool _canInvite;

	private bool _authorizing;

	public new void Awake()
	{
		base.Awake();
		UnityEngine.Object.DontDestroyOnLoad(base.gameObject);
	}

	public void Init()
	{
	}

	public new void OnDestroy()
	{
		base.OnDestroy();
	}

	public void LinkDiscordAccount(Action<bool> callback = null)
	{
		callback?.Invoke(obj: true);
	}

	public void UnLinkDiscordAccount(Action<bool> callback = null)
	{
		callback?.Invoke(obj: true);
	}

	public string GetUserID()
	{
		return null;
	}

	public bool IsAccountLinked()
	{
		return false;
	}

	public void SetStatus(string status)
	{
	}

	public void ResetStatus()
	{
	}

	public void StartNewTime(int maxTime = 0)
	{
	}

	public void ClearTime()
	{
	}

	public void SetParty(string partyId, int partySize, bool privateParty = false)
	{
	}

	public void JoinParty(string partyId, int currentSize, int maxSize, bool privateParty = false, string status = "In Lobby")
	{
	}

	public void LeaveLobby()
	{
	}

	public void UpdatePartySize(int currentSize)
	{
	}

	public void SetCanInvite(bool canInvite)
	{
	}

	public bool IsAuthorizing()
	{
		return _authorizing;
	}
}
