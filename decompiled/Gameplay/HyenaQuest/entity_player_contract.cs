using System;
using TMPro;
using UnityEngine;

namespace HyenaQuest;

public class entity_player_contract : MonoBehaviour
{
	[Header("Settings")]
	public int playerIndex;

	public GameObject model;

	private TextMeshPro _text;

	public void Awake()
	{
		if (!MonoController<PlayerController>.Instance)
		{
			throw new UnityException("PlayerController not found");
		}
		if (!model)
		{
			throw new UnityException("Model object not found");
		}
		model.SetActive(value: false);
		_text = model.GetComponentInChildren<TextMeshPro>(includeInactive: true);
		if (!_text)
		{
			throw new UnityException("TextMeshPro component not found");
		}
		_text.text = "";
		CoreController.WaitFor(delegate(PlayerController plyCtrl)
		{
			plyCtrl.OnPlayerCreated += new Action<entity_player, bool>(OnPlayerCreate);
			plyCtrl.OnPlayerRemoved += new Action<entity_player, bool>(OnPlayerRemoved);
		});
	}

	public void OnDestroy()
	{
		if ((bool)MonoController<PlayerController>.Instance)
		{
			MonoController<PlayerController>.Instance.OnPlayerCreated -= new Action<entity_player, bool>(OnPlayerCreate);
			MonoController<PlayerController>.Instance.OnPlayerRemoved -= new Action<entity_player, bool>(OnPlayerRemoved);
		}
	}

	private void OnPlayerRemoved(entity_player ply, bool server)
	{
		if ((bool)ply && ply.GetPlayerID() == playerIndex)
		{
			model.SetActive(value: false);
			_text.text = "";
		}
	}

	private void OnPlayerCreate(entity_player ply, bool server)
	{
		if (!server && (bool)ply && ply.GetPlayerID() == playerIndex)
		{
			model.SetActive(value: true);
			_text.text = ply.GetPlayerName().Substring(0, Mathf.Min(16, ply.GetPlayerName().Length));
		}
	}
}
