using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace HyenaQuest;

[DefaultExecutionOrder(-81)]
[DisallowMultipleComponent]
public class UIPlayerListController : MonoBehaviour
{
	public ScrollRect playerList;

	public GameObject playerPrefab;

	public GridLayoutGroup ghostList;

	public GameObject ghostPrefab;

	private readonly Dictionary<string, List<GameObject>> _playerEntries = new Dictionary<string, List<GameObject>>();

	public void Awake()
	{
		if (!playerList)
		{
			throw new UnityException("UIPlayerListController requires a ScrollRect component for playerList");
		}
		if (!playerPrefab)
		{
			throw new UnityException("UIPlayerListController requires a GameObject component for playerPrefab");
		}
		if (!ghostList)
		{
			throw new UnityException("Missing ghost list");
		}
		if (!ghostPrefab)
		{
			throw new UnityException("Missing ghost prefab");
		}
		CoreController.WaitFor(delegate(PlayerController plyCtrl)
		{
			plyCtrl.OnPlayerCreated += new Action<entity_player, bool>(OnPlayerCreated);
			plyCtrl.OnPlayerRemoved += new Action<entity_player, bool>(OnPlayerRemoved);
			foreach (entity_player allPlayer in MonoController<PlayerController>.Instance.GetAllPlayers())
			{
				OnPlayerCreated(allPlayer, server: false);
			}
		});
	}

	public void OnDestroy()
	{
		if ((bool)MonoController<PlayerController>.Instance)
		{
			MonoController<PlayerController>.Instance.OnPlayerCreated -= new Action<entity_player, bool>(OnPlayerCreated);
			MonoController<PlayerController>.Instance.OnPlayerRemoved -= new Action<entity_player, bool>(OnPlayerRemoved);
		}
	}

	private void OnPlayerRemoved(entity_player ply, bool server)
	{
		if (!ply || server)
		{
			return;
		}
		string text = ply.GetSteamID().ToString();
		if (string.IsNullOrEmpty(text))
		{
			throw new UnityException("UIPlayerListController player has no ID");
		}
		if (!_playerEntries.TryGetValue(text, out var value))
		{
			return;
		}
		foreach (GameObject item in value)
		{
			if ((bool)item)
			{
				UnityEngine.Object.Destroy(item);
			}
		}
		_playerEntries.Remove(text);
		UpdatePlayerList();
	}

	private void OnPlayerCreated(entity_player ply, bool server)
	{
		if (!(!ply || server) && !(ply == PlayerController.LOCAL))
		{
			string text = ply.GetSteamID().ToString();
			if (string.IsNullOrEmpty(text))
			{
				throw new UnityException("UIPlayerListController player has no ID");
			}
			if (!_playerEntries.ContainsKey(text))
			{
				_playerEntries[text] = new List<GameObject>();
				SetupPlayerList(text, ply);
				SetupGhost(text, ply);
			}
		}
	}

	private void SetupGhost(string steamID, entity_player ply)
	{
		if ((bool)ply && !string.IsNullOrEmpty(steamID))
		{
			if (!ghostPrefab)
			{
				throw new UnityException("Missing ghostPrefab");
			}
			if (!ghostList)
			{
				throw new UnityException("Missing ghostList");
			}
			GameObject gameObject = UnityEngine.Object.Instantiate(ghostPrefab, ghostList.transform);
			if (!gameObject)
			{
				throw new UnityException("UIPlayerListController failed to instantiate ghostPrefab");
			}
			gameObject.name = "VoiceGhost-" + steamID;
			ui_dead_player component = gameObject.GetComponent<ui_dead_player>();
			if (!component)
			{
				throw new UnityException("UIPlayerListController ghostPrefab missing ui_dead_player component");
			}
			component.Setup(ply);
			_playerEntries[steamID].Add(gameObject);
		}
	}

	private void SetupPlayerList(string steamID, entity_player ply)
	{
		if (!playerPrefab)
		{
			throw new UnityException("Missing playerPrefab");
		}
		if (!playerList)
		{
			throw new UnityException("Missing playerList");
		}
		GameObject gameObject = UnityEngine.Object.Instantiate(playerPrefab, playerList.content);
		if (!gameObject)
		{
			throw new UnityException("UIPlayerListController failed to instantiate playerPrefab");
		}
		gameObject.name = "Player-" + steamID;
		ui_player component = gameObject.GetComponent<ui_player>();
		if (!component)
		{
			throw new UnityException("UIPlayerListController playerPrefab missing ui_player component");
		}
		component.Setup(ply);
		_playerEntries[steamID].Add(gameObject);
		UpdatePlayerList();
	}

	private void UpdatePlayerList()
	{
		if (!playerList)
		{
			throw new UnityException("Missing playerList");
		}
		playerList.verticalNormalizedPosition = 1f;
		LayoutRebuilder.ForceRebuildLayoutImmediate(playerList.content);
	}
}
