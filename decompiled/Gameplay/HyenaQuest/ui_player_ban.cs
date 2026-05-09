using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace HyenaQuest;

public class ui_player_ban : MonoBehaviour
{
	public Button unban;

	public Button profile;

	public TextMeshProUGUI playerName;

	private ulong _id = ulong.MaxValue;

	public void Awake()
	{
		if (!unban)
		{
			throw new UnityException("Missing unban Button");
		}
		if (!profile)
		{
			throw new UnityException("Missing profile Button");
		}
		if (!playerName)
		{
			throw new UnityException("Missing playerName TextMeshProUGUI");
		}
		unban.onClick.AddListener(OnUnbanPress);
		profile.onClick.AddListener(OnProfileClick);
	}

	public void OnDestroy()
	{
		if ((bool)unban)
		{
			unban.onClick.RemoveListener(OnUnbanPress);
		}
		if ((bool)profile)
		{
			profile.onClick.RemoveListener(OnProfileClick);
		}
	}

	public void Setup(ulong id, string player)
	{
		if (id == ulong.MaxValue)
		{
			throw new UnityException("Invalid player id");
		}
		if (string.IsNullOrEmpty(player))
		{
			throw new UnityException("Invalid player name");
		}
		if ((bool)playerName)
		{
			_id = id;
			playerName.text = $"{player} - {id}";
		}
	}

	private void OnProfileClick()
	{
		if (_id != ulong.MaxValue)
		{
			Application.OpenURL($"https://steamcommunity.com/profiles/{_id}");
		}
	}

	private void OnUnbanPress()
	{
		if ((bool)MonoController<SettingsController>.Instance && _id != ulong.MaxValue)
		{
			MonoController<SettingsController>.Instance.RemoveFromBanList(_id);
		}
	}
}
