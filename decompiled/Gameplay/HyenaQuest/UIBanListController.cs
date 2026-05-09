using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace HyenaQuest;

[DefaultExecutionOrder(-81)]
[DisallowMultipleComponent]
public class UIBanListController : MonoBehaviour
{
	public ScrollRect banList;

	public GameObject banPrefab;

	private readonly Dictionary<ulong, GameObject> _banPrefabs = new Dictionary<ulong, GameObject>();

	public void Awake()
	{
		if (!banList)
		{
			throw new UnityException("UIBanListController requires a ScrollRect component for banList");
		}
		if (!banPrefab)
		{
			throw new UnityException("UIBanListController requires a GameObject component for banPrefab");
		}
		if (!MonoController<SettingsController>.Instance)
		{
			throw new UnityException("SettingsController not found");
		}
		MonoController<SettingsController>.Instance.OnBanListUpdated += new Action<ulong, string, bool>(OnBanListUpdated);
		BuildList();
	}

	public void OnDestroy()
	{
		if ((bool)MonoController<SettingsController>.Instance)
		{
			MonoController<SettingsController>.Instance.OnBanListUpdated -= new Action<ulong, string, bool>(OnBanListUpdated);
		}
	}

	private void OnBanListUpdated(ulong playerID, string ply, bool isAdd)
	{
		if (!isAdd)
		{
			GameObject value;
			if (playerID == ulong.MaxValue)
			{
				ClearList();
			}
			else if (_banPrefabs.TryGetValue(playerID, out value))
			{
				UnityEngine.Object.Destroy(value);
				_banPrefabs.Remove(playerID);
			}
		}
		else if (playerID != ulong.MaxValue && !string.IsNullOrEmpty(ply))
		{
			CreateBan(playerID, ply);
			UpdateBanList();
		}
	}

	private void ClearList()
	{
		foreach (KeyValuePair<ulong, GameObject> banPrefab in _banPrefabs)
		{
			if ((bool)banPrefab.Value)
			{
				UnityEngine.Object.Destroy(banPrefab.Value);
			}
		}
		_banPrefabs.Clear();
		UpdateBanList();
	}

	private void CreateBan(ulong id, string ply)
	{
		if (!banPrefab)
		{
			throw new UnityException("Missing banPrefab");
		}
		if (!banList)
		{
			throw new UnityException("Missing banList");
		}
		GameObject gameObject = UnityEngine.Object.Instantiate(banPrefab, banList.content);
		if (!gameObject)
		{
			throw new UnityException("Failed to instantiate ban prefab");
		}
		gameObject.name = $"Player-{id}";
		ui_player_ban component = gameObject.GetComponent<ui_player_ban>();
		if (!component)
		{
			throw new UnityException("banPrefab missing ui_player_ban component");
		}
		component.Setup(id, ply);
		if (!_banPrefabs.TryAdd(id, gameObject))
		{
			UnityEngine.Object.Destroy(gameObject);
		}
	}

	private void BuildList()
	{
		if (!MonoController<SettingsController>.Instance)
		{
			throw new UnityException("SettingsController not found");
		}
		foreach (KeyValuePair<ulong, string> ban in MonoController<SettingsController>.Instance.GetBanList())
		{
			CreateBan(ban.Key, ban.Value);
		}
		UpdateBanList();
	}

	private void UpdateBanList()
	{
		if (!banList)
		{
			throw new UnityException("Missing banList");
		}
		LayoutRebuilder.ForceRebuildLayoutImmediate(banList.content);
	}
}
