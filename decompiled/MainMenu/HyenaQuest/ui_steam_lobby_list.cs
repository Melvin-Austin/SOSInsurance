using System.Collections.Generic;
using Steamworks;
using TMPro;
using UnityEngine;
using UnityEngine.Pool;
using UnityEngine.UI;

namespace HyenaQuest;

public class ui_steam_lobby_list : MonoBehaviour
{
	public Button refresh;

	public ScrollRect serverList;

	public GameObject serverPrefab;

	public GameObject loading;

	public GameObject noEmpty;

	public TextMeshProUGUI status;

	private readonly List<ui_steam_lobby> _lobbies = new List<ui_steam_lobby>();

	private IObjectPool<ui_steam_lobby> _lobbyPool;

	public void Awake()
	{
		if (!refresh)
		{
			throw new UnityException("Missing refresh GameObject");
		}
		if (!serverList)
		{
			throw new UnityException("Missing serverList GameObject");
		}
		serverList.gameObject.SetActive(value: false);
		if (!serverPrefab)
		{
			throw new UnityException("Missing serverPrefab GameObject");
		}
		if (!loading)
		{
			throw new UnityException("Missing loading GameObject");
		}
		loading.SetActive(value: false);
		if (!noEmpty)
		{
			throw new UnityException("Missing noEmpty GameObject");
		}
		noEmpty.SetActive(value: false);
		if (!status)
		{
			throw new UnityException("Missing status GameObject");
		}
		_lobbyPool = new ObjectPool<ui_steam_lobby>(CreateLobbyUI, OnGetLobbyUI, OnReleaseLobbyUI, OnDestroyLobbyUI, collectionCheck: true, 4, 200);
		refresh.onClick.AddListener(OnRefreshServers);
	}

	public void OnEnable()
	{
		if ((bool)MonoController<SteamworksController>.Instance && SteamworksController.IsSteamRunning)
		{
			OnRefreshServers();
		}
	}

	public void OnDisable()
	{
		if (!MonoController<SteamworksController>.Instance || !SteamworksController.IsSteamRunning)
		{
			return;
		}
		MonoController<SteamworksController>.Instance.CancelServerSearch();
		foreach (ui_steam_lobby lobby in _lobbies)
		{
			if ((bool)lobby)
			{
				_lobbyPool.Release(lobby);
			}
		}
		_lobbies.Clear();
	}

	public void OnDestroy()
	{
		if ((bool)refresh)
		{
			refresh.onClick.RemoveListener(OnRefreshServers);
		}
		if ((bool)MonoController<LocalizationController>.Instance)
		{
			MonoController<LocalizationController>.Instance.Cleanup("servers.status");
		}
		_lobbyPool?.Clear();
	}

	private void OnRefreshServers()
	{
		if (!MonoController<SteamworksController>.Instance)
		{
			return;
		}
		serverList.gameObject.SetActive(value: false);
		refresh.interactable = false;
		loading.SetActive(value: true);
		noEmpty.SetActive(value: false);
		foreach (ui_steam_lobby lobby in _lobbies)
		{
			if ((bool)lobby)
			{
				_lobbyPool.Release(lobby);
			}
		}
		_lobbies.Clear();
		MonoController<SteamworksController>.Instance.SearchLobbies(delegate(List<SteamLobby> lobbies)
		{
			refresh.interactable = true;
			loading.SetActive(value: false);
			foreach (SteamLobby lobby2 in lobbies)
			{
				CSteamID id = lobby2.id;
				if (id.IsLobby())
				{
					id = lobby2.id;
					if (id.IsValid() && !lobby2.isFull)
					{
						ui_steam_lobby ui_steam_lobby2 = _lobbyPool.Get();
						if (!ui_steam_lobby2)
						{
							throw new UnityException("ui_steam_lobby_list failed to get lobby from pool");
						}
						GameObject obj = ui_steam_lobby2.gameObject;
						id = lobby2.id;
						obj.name = "LOBBY-" + id.ToString();
						ui_steam_lobby2.SetLobby(lobby2);
						_lobbies.Add(ui_steam_lobby2);
					}
				}
			}
			if ((bool)MonoController<LocalizationController>.Instance)
			{
				MonoController<LocalizationController>.Instance.Cleanup("servers.status");
				MonoController<LocalizationController>.Instance.Get("servers.status", "mainmenu.servers.status", delegate(string v)
				{
					if ((bool)status)
					{
						status.text = v;
					}
				}, new Dictionary<string, string> { 
				{
					"total",
					(lobbies.Count > 200) ? "200+" : lobbies.Count.ToString()
				} });
			}
			bool flag = _lobbies.Count > 0;
			noEmpty.SetActive(!flag);
			serverList.gameObject.SetActive(flag);
			serverList.verticalNormalizedPosition = 1f;
			LayoutRebuilder.ForceRebuildLayoutImmediate(serverList.content);
		});
	}

	private ui_steam_lobby CreateLobbyUI()
	{
		GameObject obj = Object.Instantiate(serverPrefab, serverList.content);
		if (!obj)
		{
			throw new UnityException("ui_steam_lobby_list failed to instantiate serverPrefab");
		}
		ui_steam_lobby component = obj.GetComponent<ui_steam_lobby>();
		if (!component)
		{
			throw new UnityException("ui_steam_lobby_list serverPrefab missing ui_steam_lobby component");
		}
		return component;
	}

	private void OnGetLobbyUI(ui_steam_lobby lobbyUI)
	{
		lobbyUI.gameObject.SetActive(value: true);
	}

	private void OnReleaseLobbyUI(ui_steam_lobby lobbyUI)
	{
		lobbyUI.gameObject.SetActive(value: false);
	}

	private void OnDestroyLobbyUI(ui_steam_lobby lobbyUI)
	{
		if ((bool)lobbyUI)
		{
			Object.Destroy(lobbyUI.gameObject);
		}
	}
}
