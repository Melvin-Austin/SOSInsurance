using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace HyenaQuest;

public class ui_steam_lobby : MonoBehaviour
{
	public TextMeshProUGUI lobbyName;

	public TextMeshProUGUI lobbySlots;

	public Button join;

	public GameObject modded;

	public GameObject cheated;

	public TextMeshProUGUI lobbyRound;

	private SteamLobby _lobby;

	public void Awake()
	{
		if (!lobbyName)
		{
			throw new UnityException("Missing name GameObject");
		}
		if (!lobbySlots)
		{
			throw new UnityException("Missing slots GameObject");
		}
		if (!modded)
		{
			throw new UnityException("Missing modded GameObject");
		}
		if (!join)
		{
			throw new UnityException("Missing join GameObject");
		}
		if (!cheated)
		{
			throw new UnityException("Missing cheated GameObject");
		}
		if (!lobbyRound)
		{
			throw new UnityException("Missing lobbyRound GameObject");
		}
		join.onClick.AddListener(JoinLobby);
	}

	public void OnDestroy()
	{
		if ((bool)join)
		{
			join.onClick.RemoveListener(JoinLobby);
		}
	}

	public void SetLobby(SteamLobby lobby)
	{
		if (!lobby.id.IsValid() || !lobby.id.IsLobby())
		{
			throw new UnityException("Invalid Lobby ID");
		}
		_lobby = lobby;
		lobbyName.text = lobby.name;
		lobbySlots.text = $"{lobby.players} / {lobby.maxPlayers}";
		lobbyRound.text = lobby.round.ToString();
		modded.SetActive(lobby.isModded);
		cheated.SetActive(lobby.isCheating);
		join.interactable = !lobby.isFull;
	}

	private void JoinLobby()
	{
		if (!_lobby.id.IsValid() || !_lobby.id.IsLobby())
		{
			throw new UnityException("Invalid Lobby ID");
		}
		StartCoroutine(NETController.Instance.ConnectToServer(_lobby.id.m_SteamID));
	}
}
