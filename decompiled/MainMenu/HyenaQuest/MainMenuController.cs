using System;
using System.Collections.Generic;
using FailCake;
using Steamworks;
using TMPro;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace HyenaQuest;

[DisallowMultipleComponent]
[DefaultExecutionOrder(-90)]
public class MainMenuController : MonoController<MainMenuController>
{
	public CinemachineStateDrivenCamera mainmenuCamera;

	public Animator mainmenuAnimator;

	public CanvasGroup idleGroup;

	public Button playButton;

	public Button tutorialButton;

	public Button settingsButton;

	public Button quitButton;

	public CanvasGroup playGroup;

	public GameObject playMainCanvas;

	public GameObject playServersCanvas;

	public GameObject playHostCanvas;

	public TMP_InputField playHostServerName;

	public GameObject disconnectCanvas;

	public TextMeshProUGUI disconnectReasonText;

	public GameObject setupCanvas;

	public Button setupOK;

	public TMP_Dropdown languageDropdown;

	public Slider brightnessSlider;

	public Slider playersSlider;

	public TextMeshProUGUI playerSliderCount;

	public GameObject playerCheatWarning;

	public GameObject discordMenu;

	public GameObject newsMenu;

	public GameObject tvScreen;

	public entity_sound_tester soundTester;

	public AudioMixer mixer;

	public GameObject secretYeen;

	public GameObject mainmenuMusic;

	private static readonly int Mode = Animator.StringToHash("MODE");

	private static readonly float FADE_SPEED = 4.5f;

	private static readonly Dictionary<Tuple<MainMenuMode, MainMenuMode>, float> CAMERA_SMOOTHING = new Dictionary<Tuple<MainMenuMode, MainMenuMode>, float>
	{
		{
			new Tuple<MainMenuMode, MainMenuMode>(MainMenuMode.IDLE, MainMenuMode.PLAY),
			2f
		},
		{
			new Tuple<MainMenuMode, MainMenuMode>(MainMenuMode.IDLE, MainMenuMode.OPTIONS_GRAPHICS),
			3f
		},
		{
			new Tuple<MainMenuMode, MainMenuMode>(MainMenuMode.OPTIONS_GRAPHICS, MainMenuMode.OPTIONS_AUDIO),
			1f
		},
		{
			new Tuple<MainMenuMode, MainMenuMode>(MainMenuMode.OPTIONS_GRAPHICS, MainMenuMode.OPTIONS_INPUT),
			1f
		},
		{
			new Tuple<MainMenuMode, MainMenuMode>(MainMenuMode.OPTIONS_INPUT, MainMenuMode.OPTIONS_GRAPHICS),
			1f
		},
		{
			new Tuple<MainMenuMode, MainMenuMode>(MainMenuMode.OPTIONS_AUDIO, MainMenuMode.OPTIONS_GRAPHICS),
			1f
		},
		{
			new Tuple<MainMenuMode, MainMenuMode>(MainMenuMode.OPTIONS_AUDIO, MainMenuMode.OPTIONS_INPUT),
			1f
		},
		{
			new Tuple<MainMenuMode, MainMenuMode>(MainMenuMode.OPTIONS_INPUT, MainMenuMode.OPTIONS_AUDIO),
			1f
		},
		{
			new Tuple<MainMenuMode, MainMenuMode>(MainMenuMode.OPTIONS_AUDIO, MainMenuMode.IDLE),
			4f
		},
		{
			new Tuple<MainMenuMode, MainMenuMode>(MainMenuMode.OPTIONS_GRAPHICS, MainMenuMode.IDLE),
			4f
		},
		{
			new Tuple<MainMenuMode, MainMenuMode>(MainMenuMode.OPTIONS_INPUT, MainMenuMode.IDLE),
			4f
		},
		{
			new Tuple<MainMenuMode, MainMenuMode>(MainMenuMode.SETUP, MainMenuMode.IDLE),
			7f
		},
		{
			new Tuple<MainMenuMode, MainMenuMode>(MainMenuMode.IDLE, MainMenuMode.SETUP),
			1E-05f
		}
	};

	private MainMenuMode _currentMode = MainMenuMode.NONE;

	private util_timer _matchmakeErrorTimer;

	public new void Awake()
	{
		base.Awake();
		if (!mainmenuCamera)
		{
			throw new UnityException("MainmenuCamera not initialized.");
		}
		if (!mainmenuAnimator)
		{
			throw new UnityException("MainmenuAnimator not initialized.");
		}
		if (!idleGroup)
		{
			throw new UnityException("IdleGroup not initialized.");
		}
		if (!soundTester)
		{
			throw new UnityException("SoundTester not initialized.");
		}
		if (!mixer)
		{
			throw new UnityException("Mixer not initialized.");
		}
		if (!discordMenu)
		{
			throw new UnityException("discordMenu not initialized.");
		}
		if (!newsMenu)
		{
			throw new UnityException("newsMenu not initialized.");
		}
		newsMenu.SetActive(value: true);
		discordMenu.SetActive(value: false);
		if (!tvScreen)
		{
			throw new UnityException("TVScreen not initialized.");
		}
		tvScreen.SetActive(value: true);
		if (!secretYeen)
		{
			throw new UnityException("SecretYeen not initialized.");
		}
		secretYeen.SetActive(UnityEngine.Random.Range(0, 100) <= 10);
		if (!playHostServerName)
		{
			throw new UnityException("Missing playHostServerName");
		}
		if (playHostServerName.placeholder is TextMeshProUGUI textMeshProUGUI)
		{
			textMeshProUGUI.text = (SteamworksController.IsSteamRunning ? (SteamFriends.GetPersonaName() + "'s server") : MonoController<LocalizationController>.Instance.Get("mainmenu.server-name"));
		}
		if (!playGroup)
		{
			throw new UnityException("Missing playGroup");
		}
		if (!playButton)
		{
			throw new UnityException("Missing playButton");
		}
		if (!tutorialButton)
		{
			throw new UnityException("Missing tutorialButton");
		}
		if (!settingsButton)
		{
			throw new UnityException("Missing settingsButton");
		}
		if (!quitButton)
		{
			throw new UnityException("Missing quitButton");
		}
		if (!playMainCanvas)
		{
			throw new UnityException("PlayMainCanvas not initialized.");
		}
		playMainCanvas.SetActive(value: false);
		if (!playServersCanvas)
		{
			throw new UnityException("PlayJoinCanvas not initialized.");
		}
		playServersCanvas.SetActive(value: false);
		if (!playHostCanvas)
		{
			throw new UnityException("PlayHostCanvas not initialized.");
		}
		playHostCanvas.SetActive(value: false);
		if (!disconnectCanvas)
		{
			throw new UnityException("DisconnectCanvas not initialized.");
		}
		if (!disconnectReasonText)
		{
			throw new UnityException("DisconnectReasonText not initialized.");
		}
		disconnectCanvas.SetActive(value: false);
		if (!mainmenuMusic)
		{
			throw new UnityException("MainmenuMusic not initialized.");
		}
		mainmenuMusic.SetActive(value: false);
		if (!setupCanvas)
		{
			throw new UnityException("setupCanvas not initialized.");
		}
		if (!setupOK)
		{
			throw new UnityException("languageOK not initialized.");
		}
		if (!languageDropdown)
		{
			throw new UnityException("languageDropdown not initialized.");
		}
		if (!brightnessSlider)
		{
			throw new UnityException("BrightnessSlider not initialized.");
		}
		if (!playersSlider)
		{
			throw new UnityException("playersSlider not initialized.");
		}
		if (!playerCheatWarning)
		{
			throw new UnityException("playerCheatWarning not initialized.");
		}
		if (!playerSliderCount)
		{
			throw new UnityException("playerSliderCount not initialized.");
		}
		playersSlider.maxValue = NETController.MAX_CHEAT_PLAYERS;
		playersSlider.minValue = 1f;
		playersSlider.value = NETController.DEFAULT_MAX_PLAYERS;
		playersSlider.wholeNumbers = true;
		playersSlider.SetValueWithoutNotify(NETController.MAX_PLAYERS);
		playerCheatWarning.SetActive(value: false);
		setupCanvas.SetActive(value: false);
		CoreController.WaitFor(delegate(StartupController startCtrl)
		{
			startCtrl.RequestCursor("MAINMENU");
		});
		CoreController.WaitFor(delegate(OptionsController optCtrl)
		{
			optCtrl.OnMenuRequested += new Action<OptionsState>(OnOptionsMenuRequested);
		});
		CoreController.WaitFor(delegate(SettingsController setCtrl)
		{
			setCtrl.OnSettingsUpdated += new Action(OnSettingsUpdated);
			SetMode((!setCtrl.CurrentSettings.configured) ? MainMenuMode.SETUP : MainMenuMode.IDLE);
		});
		playButton.onClick.AddListener(OnPlayButtonClicked);
		tutorialButton.onClick.AddListener(OnTutorialButtonClicked);
		settingsButton.onClick.AddListener(OnSettingsButtonClicked);
		quitButton.onClick.AddListener(OnQuitButtonClicked);
		setupOK.onClick.AddListener(OnSetupAccepted);
		playersSlider.onValueChanged.AddListener(OnMaxPlayersAdjusted);
		brightnessSlider.onValueChanged.AddListener(OnBrightnessAdjusted);
		languageDropdown.onValueChanged.AddListener(OnLocaleChanged);
		if (!string.IsNullOrEmpty(NETController.LAST_NETWORK_ERROR))
		{
			disconnectReasonText.text = NETController.LAST_NETWORK_ERROR;
			disconnectCanvas.SetActive(value: true);
		}
		OnMaxPlayersAdjusted(NETController.MAX_PLAYERS);
	}

	public new void OnDestroy()
	{
		if ((bool)MonoController<OptionsController>.Instance)
		{
			MonoController<OptionsController>.Instance.OnMenuRequested -= new Action<OptionsState>(OnOptionsMenuRequested);
		}
		if ((bool)MonoController<SettingsController>.Instance)
		{
			MonoController<SettingsController>.Instance.OnSettingsUpdated -= new Action(OnSettingsUpdated);
		}
		if ((bool)playButton)
		{
			playButton.onClick.RemoveAllListeners();
		}
		if ((bool)tutorialButton)
		{
			tutorialButton.onClick.RemoveAllListeners();
		}
		if ((bool)settingsButton)
		{
			settingsButton.onClick.RemoveAllListeners();
		}
		if ((bool)quitButton)
		{
			quitButton.onClick.RemoveAllListeners();
		}
		if ((bool)setupOK)
		{
			setupOK.onClick.RemoveAllListeners();
		}
		if ((bool)playersSlider)
		{
			playersSlider.onValueChanged.RemoveAllListeners();
		}
		if ((bool)languageDropdown)
		{
			languageDropdown.onValueChanged.RemoveAllListeners();
		}
		if ((bool)brightnessSlider)
		{
			brightnessSlider.onValueChanged.RemoveAllListeners();
		}
		_matchmakeErrorTimer?.Stop();
		MonoController<StartupController>.Instance?.ReleaseCursor("MAINMENU");
		MonoController<LocalizationController>.Instance?.Cleanup("mainmenu-max-players");
		base.OnDestroy();
	}

	public void OnDisconnectReasonAccept()
	{
		if (!disconnectCanvas)
		{
			throw new UnityException("DisconnectCanvas not initialized.");
		}
		if (!disconnectReasonText)
		{
			throw new UnityException("DisconnectReasonText not initialized.");
		}
		NETController.LAST_NETWORK_ERROR = null;
		disconnectCanvas.SetActive(value: false);
	}

	public void ToggleDiscordMenu(bool open)
	{
		if ((bool)newsMenu && (bool)discordMenu)
		{
			newsMenu.SetActive(!open);
			discordMenu.SetActive(open);
		}
	}

	private void OnSetupAccepted()
	{
		if ((bool)MonoController<SettingsController>.Instance)
		{
			PlayerSettings currentSettings = MonoController<SettingsController>.Instance.CurrentSettings;
			currentSettings.configured = true;
			MonoController<SettingsController>.Instance.CurrentSettings = currentSettings;
			SetMode(MainMenuMode.IDLE);
		}
	}

	private void OnBrightnessAdjusted(float val)
	{
		if ((bool)MonoController<SettingsController>.Instance)
		{
			PlayerSettings currentSettings = MonoController<SettingsController>.Instance.CurrentSettings;
			currentSettings.brightness = val;
			MonoController<SettingsController>.Instance.CurrentSettings = currentSettings;
		}
	}

	private void OnLocaleChanged(int locale)
	{
		if ((bool)MonoController<SettingsController>.Instance)
		{
			PlayerSettings currentSettings = MonoController<SettingsController>.Instance.CurrentSettings;
			LOCALE[] array = (LOCALE[])Enum.GetValues(typeof(LOCALE));
			if (array.Length > locale)
			{
				currentSettings.localization = array[locale];
				MonoController<SettingsController>.Instance.CurrentSettings = currentSettings;
			}
		}
	}

	private void OnMaxPlayersAdjusted(float maxPly)
	{
		if (!MonoController<LocalizationController>.Instance)
		{
			return;
		}
		NETController.MAX_PLAYERS = Mathf.Clamp((int)maxPly, 1, NETController.MAX_CHEAT_PLAYERS);
		MonoController<LocalizationController>.Instance.Cleanup("mainmenu-max-players");
		MonoController<LocalizationController>.Instance.Get("mainmenu-max-players", "mainmenu.host.max-players", delegate(string s)
		{
			if ((bool)playerSliderCount)
			{
				playerSliderCount.text = s;
			}
		}, new Dictionary<string, string> { 
		{
			"0",
			NETController.MAX_PLAYERS.ToString()
		} });
		playerCheatWarning.SetActive(NETController.MAX_PLAYERS > NETController.DEFAULT_MAX_PLAYERS);
	}

	private void OnQuitButtonClicked()
	{
		Application.Quit();
	}

	private void OnSettingsButtonClicked()
	{
		if ((bool)MonoController<OptionsController>.Instance)
		{
			MonoController<OptionsController>.Instance.EnableMenu(enable: true);
		}
		SetMode(MainMenuMode.OPTIONS_GRAPHICS);
	}

	private void OnPlayButtonClicked()
	{
		SetMode(MainMenuMode.PLAY);
	}

	private void OnTutorialButtonClicked()
	{
		HostTrainingMode();
	}

	private void OnOptionsMenuRequested(OptionsState state)
	{
		switch (state)
		{
		case OptionsState.DISABLED:
			SetMode(MainMenuMode.IDLE);
			break;
		case OptionsState.GRAPHICS:
			SetMode(MainMenuMode.OPTIONS_GRAPHICS);
			break;
		case OptionsState.AUDIO:
			SetMode(MainMenuMode.OPTIONS_AUDIO);
			break;
		case OptionsState.INPUT:
			SetMode(MainMenuMode.OPTIONS_INPUT);
			break;
		}
	}

	private void OnSettingsUpdated()
	{
		if (!MonoController<SettingsController>.Instance)
		{
			throw new UnityException("SettingsController not initialized.");
		}
		if (!MonoController<LocalizationController>.Instance)
		{
			throw new UnityException("LocalizationController not initialized.");
		}
		PlayerSettings currentSettings = MonoController<SettingsController>.Instance.CurrentSettings;
		mixer.SetFloat("MasterVolume", Mathf.Lerp(-60f, 0f, currentSettings.masterVolume));
		mixer.SetFloat("MusicVolume", Mathf.Lerp(-60f, 0f, currentSettings.musicVolume));
		mixer.SetFloat("SFXVolume", Mathf.Lerp(-60f, -5f, currentSettings.sfxVolume));
		mixer.SetFloat("MicrophoneVolume", Mathf.Lerp(-60f, 0f, currentSettings.microphoneVolume));
		LOCALE[] values = (LOCALE[])Enum.GetValues(typeof(LOCALE));
		OptionsController.UpdateDropdown(languageDropdown, values, currentSettings.localization, (LOCALE mode) => MonoController<LocalizationController>.Instance?.GetLanguageName(mode) ?? mode.ToString(), (LOCALE value, LOCALE compare) => value == compare);
		brightnessSlider.SetValueWithoutNotify(currentSettings.brightness);
	}

	public void SetModeUI(string mode)
	{
		SetMode(Enum.Parse<MainMenuMode>(mode));
	}

	public void StartHosting(int mode)
	{
		switch (mode)
		{
		case 0:
			NETController.LOBBY_VISIBILITY = ELobbyType.k_ELobbyTypeFriendsOnly;
			break;
		case 1:
			NETController.LOBBY_VISIBILITY = ELobbyType.k_ELobbyTypePublic;
			break;
		case 2:
			NETController.LOBBY_VISIBILITY = ELobbyType.k_ELobbyTypePrivate;
			break;
		}
		NETController.LOBBY_NAME = playHostServerName?.text;
		NETController.LOBBY_CONNECT_ID = null;
		NETController.Instance.SetCheats(NETController.MAX_PLAYERS > NETController.DEFAULT_MAX_PLAYERS);
		SceneManager.LoadScene("LOADING");
	}

	private void HostTrainingMode()
	{
		NETController.LOBBY_CONNECT_ID = 1337uL;
		SceneManager.LoadScene("LOADING");
	}

	private void SetMode(MainMenuMode mode)
	{
		if (_currentMode == mode)
		{
			return;
		}
		if (CAMERA_SMOOTHING.TryGetValue(new Tuple<MainMenuMode, MainMenuMode>(_currentMode, mode), out var value))
		{
			mainmenuCamera.DefaultBlend.Time = value;
		}
		if ((_currentMode == MainMenuMode.IDLE && mode != 0) || (_currentMode != 0 && mode == MainMenuMode.IDLE))
		{
			float initial = ((mode == MainMenuMode.IDLE) ? 0f : 0.75f);
			float target = ((mode == MainMenuMode.IDLE) ? 0.75f : 0f);
			util_fade_timer.Fade(FADE_SPEED, initial, target, delegate(float alpha)
			{
				idleGroup.alpha = alpha;
			}, delegate(float alpha)
			{
				idleGroup.alpha = alpha;
			});
		}
		else
		{
			idleGroup.alpha = 0f;
		}
		idleGroup.interactable = mode == MainMenuMode.IDLE;
		_currentMode = mode;
		mainmenuAnimator.SetInteger(Mode, (int)_currentMode);
		soundTester.enabled = _currentMode == MainMenuMode.OPTIONS_AUDIO;
		tvScreen.SetActive(_currentMode == MainMenuMode.IDLE);
		playGroup.gameObject.SetActive(_currentMode == MainMenuMode.PLAY);
		SetPlayMode(MainMenuPlayingMode.IDLE);
		setupCanvas.SetActive(_currentMode == MainMenuMode.SETUP);
		languageDropdown.interactable = _currentMode == MainMenuMode.SETUP;
		brightnessSlider.interactable = _currentMode == MainMenuMode.SETUP;
		mainmenuMusic.SetActive(_currentMode != MainMenuMode.SETUP);
	}

	public void SetPlayModeUI(string mode)
	{
		if (string.Equals(mode, "TRAINING", StringComparison.InvariantCultureIgnoreCase))
		{
			HostTrainingMode();
		}
		else
		{
			SetPlayMode(Enum.Parse<MainMenuPlayingMode>(mode));
		}
	}

	private void SetPlayMode(MainMenuPlayingMode mode)
	{
		playMainCanvas.SetActive(mode == MainMenuPlayingMode.IDLE);
		playServersCanvas.SetActive(mode == MainMenuPlayingMode.SERVERS);
		playHostCanvas.SetActive(mode == MainMenuPlayingMode.HOST);
	}
}
