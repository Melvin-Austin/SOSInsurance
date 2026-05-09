using System.Collections.Generic;
using FailCake;
using MetaVoiceChat;
using Steamworks;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace HyenaQuest;

public class ui_player : MonoBehaviour
{
	public TextMeshProUGUI playerName;

	public Slider volumeSlider;

	public Button kickButton;

	public Button banButton;

	public Button profileButton;

	private entity_player _owner;

	private CONFIRMATION_MODE _confirmationMode;

	private util_timer _timer;

	private float _lastResetTime;

	private TextMeshProUGUI _kickButtonText;

	private TextMeshProUGUI _banButtonText;

	private ColorBlock _kickButtonOriginalColors;

	private ColorBlock _banButtonOriginalColors;

	private string _originalPlayerName;

	private bool IsServer
	{
		get
		{
			if ((bool)NETController.Instance)
			{
				return NETController.Instance.IsServer;
			}
			return false;
		}
	}

	public void Awake()
	{
		if (!playerName)
		{
			throw new UnityException("ui_player requires a TextMeshProUGUI component for playerName");
		}
		if (!volumeSlider)
		{
			throw new UnityException("ui_player requires a Slider component for volumeSlider");
		}
		if (!profileButton)
		{
			throw new UnityException("ui_player requires profileButton");
		}
		volumeSlider.onValueChanged.AddListener(OnVolumeSliderChanged);
		profileButton.onClick.AddListener(OnProfileClick);
	}

	public void OnDestroy()
	{
		_timer?.Stop();
		if ((bool)volumeSlider)
		{
			volumeSlider.onValueChanged.RemoveAllListeners();
		}
		if ((bool)kickButton)
		{
			kickButton.onClick.RemoveAllListeners();
		}
		if ((bool)profileButton)
		{
			profileButton.onClick.RemoveAllListeners();
		}
		ClearLocalization();
		SaveSettings();
	}

	public void OnDisable()
	{
		ClearLocalization();
		ResetConfirmation();
		SaveSettings();
	}

	public void Setup(entity_player ply)
	{
		if (!ply)
		{
			throw new UnityException("Setup requires a valid player");
		}
		_owner = ply;
		_originalPlayerName = ply.GetPlayerName();
		playerName.text = _originalPlayerName;
		float playerVolume = MonoController<SettingsController>.Instance.GetMicrophoneSetting(_owner.GetSteamID().ToString());
		volumeSlider.SetValueWithoutNotify(playerVolume);
		_timer?.Stop();
		_timer = util_timer.Simple(0.35f, delegate
		{
			OnVolumeSliderChanged(playerVolume);
		});
		if (!IsServer)
		{
			Object.Destroy(kickButton.gameObject);
			Object.Destroy(banButton.gameObject);
			kickButton = null;
			banButton = null;
			return;
		}
		_kickButtonText = kickButton.GetComponentInChildren<TextMeshProUGUI>(includeInactive: true);
		if (!_kickButtonText)
		{
			throw new UnityException("Missing TextMeshProUGUI on kick button");
		}
		_banButtonText = banButton.GetComponentInChildren<TextMeshProUGUI>(includeInactive: true);
		if (!_banButtonText)
		{
			throw new UnityException("Missing TextMeshProUGUI on ban button");
		}
		_kickButtonOriginalColors = kickButton.colors;
		_banButtonOriginalColors = banButton.colors;
		SetButtonsTexts(null, "ingame.ui.button.ban", "ingame.ui.button.kick");
		kickButton.onClick.AddListener(OnKickOrConfirm);
		banButton.onClick.AddListener(OnBanOrCancel);
	}

	public void Update()
	{
		if (_confirmationMode != 0 && IsServer && Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
		{
			GameObject gameObject = EventSystem.current?.currentSelectedGameObject;
			if (!(gameObject == kickButton?.gameObject) && !(gameObject == banButton?.gameObject))
			{
				ResetConfirmation();
			}
		}
	}

	private void OnProfileClick()
	{
		if ((bool)_owner)
		{
			Application.OpenURL($"https://steamcommunity.com/profiles/{_owner.GetSteamID()}");
		}
	}

	[Server]
	private void OnKickOrConfirm()
	{
		if (!IsServer)
		{
			throw new UnityException("Server only");
		}
		if (!_owner)
		{
			return;
		}
		switch (_confirmationMode)
		{
		case CONFIRMATION_MODE.BAN:
		{
			CSteamID steamIDFriend = new CSteamID(_owner.GetSteamID());
			if (MonoController<SettingsController>.Instance.AddToBanList(steamIDFriend.m_SteamID, SteamFriends.GetFriendPersonaName(steamIDFriend)))
			{
				NETController.Instance.DisconnectClientWithReason(_owner.GetConnectionID(), "ingame.ui.disconnected.reason.ban");
				base.gameObject.SetActive(value: false);
			}
			ResetConfirmation();
			break;
		}
		case CONFIRMATION_MODE.KICK:
			NETController.Instance.DisconnectClientWithReason(_owner.GetConnectionID(), "ingame.ui.disconnected.reason.kick");
			base.gameObject.SetActive(value: false);
			ResetConfirmation();
			break;
		default:
			if (!(Time.time - _lastResetTime < 0.2f))
			{
				_confirmationMode = CONFIRMATION_MODE.KICK;
				SetConfirmMode("ingame.ui.button.kick");
			}
			break;
		}
	}

	[Server]
	private void OnBanOrCancel()
	{
		if (!IsServer)
		{
			throw new UnityException("Server only");
		}
		if ((bool)_owner)
		{
			if (_confirmationMode != 0)
			{
				ResetConfirmation();
			}
			else if (!(Time.time < _lastResetTime))
			{
				_confirmationMode = CONFIRMATION_MODE.BAN;
				SetConfirmMode("ingame.ui.button.ban");
			}
		}
	}

	private void SetConfirmMode(string id)
	{
		SetButtonsTexts(id, "general.cancel", "general.confirm");
		EventSystem.current.SetSelectedGameObject(null);
		if ((bool)banButton)
		{
			ColorBlock colors = banButton.colors;
			colors.normalColor = Color.red;
			colors.pressedColor = colors.normalColor;
			colors.highlightedColor = colors.normalColor * 0.3f;
			colors.selectedColor = colors.normalColor;
			banButton.colors = colors;
		}
		if ((bool)kickButton)
		{
			ColorBlock colors2 = kickButton.colors;
			colors2.normalColor = new Color(0f, 0.41f, 0f, 1f);
			colors2.selectedColor = colors2.normalColor;
			colors2.pressedColor = colors2.normalColor;
			colors2.highlightedColor = colors2.normalColor * 0.3f;
			kickButton.colors = colors2;
		}
	}

	private void ResetConfirmation()
	{
		if (_confirmationMode != 0)
		{
			_confirmationMode = CONFIRMATION_MODE.NONE;
			_lastResetTime = Time.time + 0.2f;
			SetButtonsTexts(null, "ingame.ui.button.ban", "ingame.ui.button.kick");
			if ((bool)banButton)
			{
				banButton.colors = _banButtonOriginalColors;
			}
			if ((bool)kickButton)
			{
				kickButton.colors = _kickButtonOriginalColors;
			}
		}
	}

	private void SetButtonsTexts(string plyID, string banID, string kickID)
	{
		if (!MonoController<LocalizationController>.Instance)
		{
			return;
		}
		ClearLocalization();
		if ((bool)playerName)
		{
			if (!string.IsNullOrEmpty(plyID))
			{
				MonoController<LocalizationController>.Instance.Get($"info-ply-ui-{_owner?.GetPlayerID()}", plyID, delegate(string str)
				{
					if ((bool)playerName)
					{
						playerName.text = str;
					}
				}, new Dictionary<string, string> { 
				{
					"0",
					" '" + _originalPlayerName + "' ?"
				} });
			}
			else
			{
				playerName.text = _originalPlayerName;
			}
		}
		if ((bool)_banButtonText)
		{
			MonoController<LocalizationController>.Instance.Get($"ban-ply-ui-{_owner?.GetPlayerID()}", banID, delegate(string str)
			{
				if ((bool)_banButtonText)
				{
					_banButtonText.text = str;
				}
			}, new Dictionary<string, string> { { "0", "" } });
		}
		if (!_kickButtonText)
		{
			return;
		}
		MonoController<LocalizationController>.Instance.Get($"kick-ply-ui-{_owner?.GetPlayerID()}", kickID, delegate(string str)
		{
			if ((bool)_kickButtonText)
			{
				_kickButtonText.text = str;
			}
		}, new Dictionary<string, string> { { "0", "" } });
	}

	private void ClearLocalization()
	{
		if ((bool)MonoController<LocalizationController>.Instance)
		{
			MonoController<LocalizationController>.Instance.Cleanup($"info-ply-ui-{_owner?.GetPlayerID()}");
			MonoController<LocalizationController>.Instance.Cleanup($"ban-ply-ui-{_owner?.GetPlayerID()}");
			MonoController<LocalizationController>.Instance.Cleanup($"kick-ply-ui-{_owner?.GetPlayerID()}");
		}
	}

	private void OnVolumeSliderChanged(float volume)
	{
		if (!(_owner == PlayerController.LOCAL))
		{
			MetaVc voice = _owner.GetVoice();
			if (!voice)
			{
				throw new UnityException("Missing player MetaVc");
			}
			AudioSource voiceOutputSource = _owner.GetVoiceOutputSource();
			if (!voiceOutputSource)
			{
				throw new UnityException("Missing player VCAudioOutput");
			}
			voiceOutputSource.volume = volume;
			voice.isOutputMuted.Value = volume <= 0.01f;
		}
	}

	private void SaveSettings()
	{
		if ((bool)MonoController<SettingsController>.Instance && (bool)_owner)
		{
			PlayerSettings currentSettings = MonoController<SettingsController>.Instance.CurrentSettings;
			currentSettings.playerMicrophoneSettings[_owner.GetSteamID().ToString()] = volumeSlider?.value ?? 1f;
			MonoController<SettingsController>.Instance.CurrentSettings = currentSettings;
		}
	}
}
