using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using FailCake;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Samples.RebindUI;
using UnityEngine.UI;
using ZLinq;

namespace HyenaQuest;

[DisallowMultipleComponent]
[DefaultExecutionOrder(-94)]
public class OptionsController : MonoController<OptionsController>
{
	public bool hideGroupsWhenDisabled;

	public CanvasGroup mainGroup;

	public List<Button> leaveButtons = new List<Button>();

	public List<Button> inputButtons = new List<Button>();

	public List<Button> graphicsButtons = new List<Button>();

	public List<Button> audioButtons = new List<Button>();

	public CanvasGroup graphicsGroup;

	public TMP_Dropdown resolutionDropdown;

	public TMP_Dropdown monitorModeDropdown;

	public TMP_Dropdown targetFrameRateDropdown;

	public TMP_Dropdown monitorDropdown;

	public TMP_Dropdown shadowDropdown;

	public Toggle vsyncToggle;

	public Slider brightnessSlider;

	public TextMeshProUGUI fovText;

	public Slider fovSlider;

	public Toggle crosshairToggle;

	public TMP_Dropdown localizationDropdown;

	public CanvasGroup audioGroup;

	public Slider audioMasterSlider;

	public Slider audioMusicSlider;

	public Slider audioSFXSlider;

	public Slider audioMICROPHONESlider;

	public Toggle audioMuteOnUnfocusToggle;

	public TMP_Dropdown microphoneSelectDropdown;

	public TMP_Dropdown microphoneModeDropdown;

	public CanvasGroup inputGroup;

	public Toggle cameraInvertMouseY;

	public Toggle rotationInvertMouseX;

	public Toggle rotationInvertMouseY;

	public Toggle vibrationToggle;

	public Slider cameraSensitivitySlider;

	public Slider physgunSensitivitySlider;

	public GameEvent<OptionsState> OnMenuRequested = new GameEvent<OptionsState>();

	private OptionsState _currentState;

	private RebindActionUI[] _rebindActions;

	private util_timer _rebindTimer;

	public new void Awake()
	{
		base.Awake();
		if ((bool)resolutionDropdown)
		{
			resolutionDropdown.onValueChanged.AddListener(OnResolutionChanged);
		}
		if ((bool)monitorModeDropdown)
		{
			monitorModeDropdown.onValueChanged.AddListener(OnMonitorModeChanged);
		}
		if ((bool)targetFrameRateDropdown)
		{
			targetFrameRateDropdown.onValueChanged.AddListener(OnTargetFramerateChanged);
		}
		if ((bool)vsyncToggle)
		{
			vsyncToggle.onValueChanged.AddListener(OnVSyncChanged);
		}
		if ((bool)monitorDropdown)
		{
			monitorDropdown.onValueChanged.AddListener(OnMonitorChanged);
		}
		if ((bool)shadowDropdown)
		{
			shadowDropdown.onValueChanged.AddListener(OnShadowChanged);
		}
		if ((bool)brightnessSlider)
		{
			brightnessSlider.onValueChanged.AddListener(OnBrightnessChanged);
		}
		if ((bool)fovSlider)
		{
			fovSlider.onValueChanged.AddListener(OnFOVChanged);
		}
		if ((bool)crosshairToggle)
		{
			crosshairToggle.onValueChanged.AddListener(OnCrossHairChanged);
		}
		if ((bool)localizationDropdown)
		{
			localizationDropdown.onValueChanged.AddListener(OnLocaleChanged);
		}
		if ((bool)audioMasterSlider)
		{
			audioMasterSlider.onValueChanged.AddListener(delegate(float value)
			{
				SetAudioState(SoundMixer.MASTER, value);
			});
		}
		if ((bool)audioMusicSlider)
		{
			audioMusicSlider.onValueChanged.AddListener(delegate(float value)
			{
				SetAudioState(SoundMixer.MUSIC, value);
			});
		}
		if ((bool)audioSFXSlider)
		{
			audioSFXSlider.onValueChanged.AddListener(delegate(float value)
			{
				SetAudioState(SoundMixer.SFX, value);
			});
		}
		if ((bool)audioMICROPHONESlider)
		{
			audioMICROPHONESlider.onValueChanged.AddListener(delegate(float value)
			{
				SetAudioState(SoundMixer.MICROPHONE, value);
			});
		}
		if ((bool)audioMuteOnUnfocusToggle)
		{
			audioMuteOnUnfocusToggle.onValueChanged.AddListener(OnUnFocusMuteAudio);
		}
		if ((bool)microphoneSelectDropdown)
		{
			microphoneSelectDropdown.onValueChanged.AddListener(OnMicrophoneDeviceChanged);
		}
		if ((bool)microphoneModeDropdown)
		{
			microphoneModeDropdown.onValueChanged.AddListener(OnMicrophoneModeChanged);
		}
		if ((bool)cameraInvertMouseY)
		{
			cameraInvertMouseY.onValueChanged.AddListener(OnCameraInvertMouseY);
		}
		if ((bool)rotationInvertMouseX)
		{
			rotationInvertMouseX.onValueChanged.AddListener(OnRotationInvertMouseX);
		}
		if ((bool)rotationInvertMouseY)
		{
			rotationInvertMouseY.onValueChanged.AddListener(OnRotationInvertMouseY);
		}
		if ((bool)cameraSensitivitySlider)
		{
			cameraSensitivitySlider.onValueChanged.AddListener(delegate(float value)
			{
				SetInputSensitivity(isCamera: true, value);
			});
		}
		if ((bool)physgunSensitivitySlider)
		{
			physgunSensitivitySlider.onValueChanged.AddListener(delegate(float value)
			{
				SetInputSensitivity(isCamera: false, value);
			});
		}
		if ((bool)vibrationToggle)
		{
			vibrationToggle.onValueChanged.AddListener(OnVibrationChanged);
		}
		_rebindActions = GetComponentsInChildren<RebindActionUI>(includeInactive: true);
		RebindActionUI[] rebindActions = _rebindActions;
		foreach (RebindActionUI rebindActionUI in rebindActions)
		{
			if ((bool)rebindActionUI)
			{
				rebindActionUI.stopRebindEvent.AddListener(OnBindChange);
				rebindActionUI.resetBindEvent.AddListener(OnBindReset);
			}
		}
		if (hideGroupsWhenDisabled)
		{
			if ((bool)mainGroup)
			{
				mainGroup.gameObject.SetActive(value: true);
			}
			if ((bool)graphicsGroup)
			{
				graphicsGroup.gameObject.SetActive(value: false);
			}
			if ((bool)audioGroup)
			{
				audioGroup.gameObject.SetActive(value: false);
			}
			if ((bool)inputGroup)
			{
				inputGroup.gameObject.SetActive(value: false);
			}
		}
		foreach (Button leaveButton in leaveButtons)
		{
			leaveButton.onClick.AddListener(delegate
			{
				EnableMenu(enable: false);
			});
		}
		foreach (Button audioButton in audioButtons)
		{
			audioButton.onClick.AddListener(delegate
			{
				SetStatus(OptionsState.AUDIO);
			});
		}
		foreach (Button inputButton in inputButtons)
		{
			inputButton.onClick.AddListener(delegate
			{
				SetStatus(OptionsState.INPUT);
			});
		}
		foreach (Button graphicsButton in graphicsButtons)
		{
			graphicsButton.onClick.AddListener(delegate
			{
				SetStatus(OptionsState.GRAPHICS);
			});
		}
		CoreController.WaitFor(delegate(SettingsController settingsCtrl)
		{
			settingsCtrl.OnSettingsUpdated += new Action(OnSettingsUpdated);
			OnSettingsUpdated();
		});
	}

	public void EnableMenu(bool enable)
	{
		SetStatus(enable ? OptionsState.GRAPHICS : OptionsState.DISABLED);
	}

	public void SetStatus(OptionsState state)
	{
		if (_currentState != state)
		{
			_currentState = state;
			mainGroup.interactable = state == OptionsState.DISABLED;
			graphicsGroup.interactable = state == OptionsState.GRAPHICS;
			audioGroup.interactable = state == OptionsState.AUDIO;
			inputGroup.interactable = state == OptionsState.INPUT;
			if (hideGroupsWhenDisabled)
			{
				graphicsGroup.gameObject.SetActive(graphicsGroup.interactable);
				audioGroup.gameObject.SetActive(audioGroup.interactable);
				inputGroup.gameObject.SetActive(inputGroup.interactable);
				mainGroup.gameObject.SetActive(mainGroup.interactable);
			}
			OnMenuRequested?.Invoke(state);
		}
	}

	public new void OnDestroy()
	{
		resolutionDropdown?.onValueChanged.RemoveAllListeners();
		monitorModeDropdown?.onValueChanged.RemoveAllListeners();
		monitorDropdown?.onValueChanged.RemoveAllListeners();
		shadowDropdown?.onValueChanged.RemoveAllListeners();
		targetFrameRateDropdown?.onValueChanged.RemoveAllListeners();
		vsyncToggle?.onValueChanged.RemoveAllListeners();
		brightnessSlider?.onValueChanged.RemoveAllListeners();
		fovSlider?.onValueChanged.RemoveAllListeners();
		crosshairToggle?.onValueChanged.RemoveAllListeners();
		localizationDropdown?.onValueChanged.RemoveAllListeners();
		audioMasterSlider?.onValueChanged.RemoveAllListeners();
		audioMusicSlider?.onValueChanged.RemoveAllListeners();
		audioSFXSlider?.onValueChanged.RemoveAllListeners();
		audioMICROPHONESlider?.onValueChanged.RemoveAllListeners();
		audioMuteOnUnfocusToggle?.onValueChanged.RemoveAllListeners();
		microphoneModeDropdown?.onValueChanged.RemoveAllListeners();
		microphoneSelectDropdown?.onValueChanged.RemoveAllListeners();
		cameraInvertMouseY?.onValueChanged.RemoveAllListeners();
		rotationInvertMouseX?.onValueChanged.RemoveAllListeners();
		rotationInvertMouseY?.onValueChanged.RemoveAllListeners();
		cameraSensitivitySlider?.onValueChanged.RemoveAllListeners();
		physgunSensitivitySlider?.onValueChanged.RemoveAllListeners();
		vibrationToggle?.onValueChanged.RemoveAllListeners();
		RebindActionUI[] rebindActions = _rebindActions;
		foreach (RebindActionUI rebindActionUI in rebindActions)
		{
			if ((bool)rebindActionUI)
			{
				rebindActionUI.stopRebindEvent.RemoveAllListeners();
				rebindActionUI.resetBindEvent.RemoveAllListeners();
			}
		}
		foreach (Button leaveButton in leaveButtons)
		{
			leaveButton.onClick.RemoveAllListeners();
		}
		foreach (Button audioButton in audioButtons)
		{
			audioButton.onClick.RemoveAllListeners();
		}
		foreach (Button inputButton in inputButtons)
		{
			inputButton.onClick.RemoveAllListeners();
		}
		foreach (Button graphicsButton in graphicsButtons)
		{
			graphicsButton.onClick.RemoveAllListeners();
		}
		if ((bool)MonoController<SettingsController>.Instance)
		{
			MonoController<SettingsController>.Instance.OnSettingsUpdated -= new Action(OnSettingsUpdated);
		}
		_rebindTimer?.Stop();
		base.OnDestroy();
	}

	private void OnUnFocusMuteAudio(bool mute)
	{
		if ((bool)MonoController<SettingsController>.Instance)
		{
			PlayerSettings currentSettings = MonoController<SettingsController>.Instance.CurrentSettings;
			currentSettings.muteOnUnfocus = mute;
			MonoController<SettingsController>.Instance.CurrentSettings = currentSettings;
		}
	}

	private void OnMicrophoneDeviceChanged(int index)
	{
		if ((bool)MonoController<SettingsController>.Instance)
		{
			PlayerSettings currentSettings = MonoController<SettingsController>.Instance.CurrentSettings;
			currentSettings.microphoneDevice = index;
			MonoController<SettingsController>.Instance.CurrentSettings = currentSettings;
		}
	}

	private void OnMicrophoneModeChanged(int index)
	{
		if ((bool)MonoController<SettingsController>.Instance)
		{
			PlayerSettings currentSettings = MonoController<SettingsController>.Instance.CurrentSettings;
			VoiceChatMode[] array = (VoiceChatMode[])Enum.GetValues(typeof(VoiceChatMode));
			if (array.Length > index)
			{
				currentSettings.microphoneMode = array[index];
				MonoController<SettingsController>.Instance.CurrentSettings = currentSettings;
			}
		}
	}

	private void SetAudioState(SoundMixer master, float value)
	{
		if ((bool)MonoController<SettingsController>.Instance)
		{
			PlayerSettings currentSettings = MonoController<SettingsController>.Instance.CurrentSettings;
			switch (master)
			{
			case SoundMixer.MASTER:
				currentSettings.masterVolume = value;
				break;
			case SoundMixer.MUSIC:
				currentSettings.musicVolume = value;
				break;
			case SoundMixer.SFX:
				currentSettings.sfxVolume = value;
				break;
			case SoundMixer.MICROPHONE:
				currentSettings.microphoneVolume = value;
				break;
			}
			MonoController<SettingsController>.Instance.CurrentSettings = currentSettings;
		}
	}

	private void OnRotationInvertMouseX(bool enable)
	{
		if ((bool)MonoController<SettingsController>.Instance)
		{
			PlayerSettings currentSettings = MonoController<SettingsController>.Instance.CurrentSettings;
			currentSettings.invertPhysRotationX = enable;
			MonoController<SettingsController>.Instance.CurrentSettings = currentSettings;
		}
	}

	private void OnRotationInvertMouseY(bool enable)
	{
		if ((bool)MonoController<SettingsController>.Instance)
		{
			PlayerSettings currentSettings = MonoController<SettingsController>.Instance.CurrentSettings;
			currentSettings.invertPhysRotationY = enable;
			MonoController<SettingsController>.Instance.CurrentSettings = currentSettings;
		}
	}

	private void OnCameraInvertMouseY(bool newVal)
	{
		if ((bool)MonoController<SettingsController>.Instance)
		{
			PlayerSettings currentSettings = MonoController<SettingsController>.Instance.CurrentSettings;
			currentSettings.invertMouseY = newVal;
			MonoController<SettingsController>.Instance.CurrentSettings = currentSettings;
		}
	}

	private void SetInputSensitivity(bool isCamera, float value)
	{
		if ((bool)MonoController<SettingsController>.Instance)
		{
			PlayerSettings currentSettings = MonoController<SettingsController>.Instance.CurrentSettings;
			if (isCamera)
			{
				currentSettings.mouseSensitivity = value;
			}
			else
			{
				currentSettings.physgunRotateSensitivity = value;
			}
			MonoController<SettingsController>.Instance.CurrentSettings = currentSettings;
		}
	}

	private void OnResolutionChanged(int index)
	{
		if ((bool)MonoController<SettingsController>.Instance)
		{
			PlayerSettings currentSettings = MonoController<SettingsController>.Instance.CurrentSettings;
			Resolution[] resolutions = MonoController<SettingsController>.Instance.GetResolutions();
			if (resolutions != null && resolutions.Length > index)
			{
				currentSettings.screenResolution = resolutions[index];
				MonoController<SettingsController>.Instance.CurrentSettings = currentSettings;
			}
		}
	}

	private void OnMonitorModeChanged(int index)
	{
		if ((bool)MonoController<SettingsController>.Instance)
		{
			PlayerSettings currentSettings = MonoController<SettingsController>.Instance.CurrentSettings;
			FullScreenMode[] screenModes = MonoController<SettingsController>.Instance.GetScreenModes();
			if (screenModes.Length > index)
			{
				currentSettings.screenMode = screenModes[index];
				MonoController<SettingsController>.Instance.CurrentSettings = currentSettings;
			}
		}
	}

	private void OnVSyncChanged(bool newVal)
	{
		if ((bool)MonoController<SettingsController>.Instance)
		{
			PlayerSettings currentSettings = MonoController<SettingsController>.Instance.CurrentSettings;
			currentSettings.vsyncEnabled = newVal;
			MonoController<SettingsController>.Instance.CurrentSettings = currentSettings;
		}
	}

	private void OnTargetFramerateChanged(int index)
	{
		if (!MonoController<SettingsController>.Instance)
		{
			return;
		}
		PlayerSettings currentSettings = MonoController<SettingsController>.Instance.CurrentSettings;
		if (!currentSettings.vsyncEnabled)
		{
			PlayerTargetFramerate[] array = (PlayerTargetFramerate[])Enum.GetValues(typeof(PlayerTargetFramerate));
			if (array.Length > index)
			{
				currentSettings.targetFrameRate = array[index];
				MonoController<SettingsController>.Instance.CurrentSettings = currentSettings;
			}
		}
	}

	private void OnMonitorChanged(int index)
	{
		if (!MonoController<SettingsController>.Instance)
		{
			return;
		}
		PlayerSettings currentSettings = MonoController<SettingsController>.Instance.CurrentSettings;
		List<DisplayInfo> list = new List<DisplayInfo>();
		Screen.GetDisplayLayout(list);
		if (list.Count != 0)
		{
			if (index < 0 || index >= list.Count)
			{
				index = 0;
			}
			Screen.MoveMainWindowTo(list[index], Vector2Int.zero);
			currentSettings.monitor = index;
			MonoController<SettingsController>.Instance.CurrentSettings = currentSettings;
		}
	}

	private void OnShadowChanged(int index)
	{
		if ((bool)MonoController<SettingsController>.Instance)
		{
			PlayerSettings currentSettings = MonoController<SettingsController>.Instance.CurrentSettings;
			ShadowQuality[] array = (ShadowQuality[])Enum.GetValues(typeof(ShadowQuality));
			if (array.Length > index)
			{
				currentSettings.shadow = array[index];
				MonoController<SettingsController>.Instance.CurrentSettings = currentSettings;
			}
		}
	}

	private void OnBrightnessChanged(float extraBrightness)
	{
		if ((bool)MonoController<SettingsController>.Instance)
		{
			PlayerSettings currentSettings = MonoController<SettingsController>.Instance.CurrentSettings;
			currentSettings.brightness = extraBrightness;
			MonoController<SettingsController>.Instance.CurrentSettings = currentSettings;
		}
	}

	private void OnFOVChanged(float fov)
	{
		if ((bool)MonoController<SettingsController>.Instance)
		{
			PlayerSettings currentSettings = MonoController<SettingsController>.Instance.CurrentSettings;
			currentSettings.fov = fov;
			MonoController<SettingsController>.Instance.CurrentSettings = currentSettings;
			if ((bool)fovText)
			{
				fovText.text = $"FOV - {fov}";
			}
		}
	}

	private void OnLocaleChanged(int index)
	{
		if ((bool)MonoController<SettingsController>.Instance)
		{
			PlayerSettings currentSettings = MonoController<SettingsController>.Instance.CurrentSettings;
			LOCALE[] array = (LOCALE[])Enum.GetValues(typeof(LOCALE));
			if (array.Length > index)
			{
				currentSettings.localization = array[index];
				MonoController<SettingsController>.Instance.CurrentSettings = currentSettings;
			}
		}
	}

	private void OnCrossHairChanged(bool enable)
	{
		if ((bool)MonoController<SettingsController>.Instance)
		{
			PlayerSettings currentSettings = MonoController<SettingsController>.Instance.CurrentSettings;
			currentSettings.crosshair = enable;
			MonoController<SettingsController>.Instance.CurrentSettings = currentSettings;
		}
	}

	private void OnVibrationChanged(bool enable)
	{
		if ((bool)MonoController<SettingsController>.Instance)
		{
			PlayerSettings currentSettings = MonoController<SettingsController>.Instance.CurrentSettings;
			currentSettings.disableVibration = !enable;
			MonoController<SettingsController>.Instance.CurrentSettings = currentSettings;
		}
	}

	private void OnBindChange(RebindActionUI element, InputActionRebindingExtensions.RebindingOperation op)
	{
		if ((bool)MonoController<SettingsController>.Instance)
		{
			MonoController<SettingsController>.Instance.SaveKeyBinds();
		}
	}

	private void OnBindReset()
	{
		if ((bool)MonoController<SettingsController>.Instance)
		{
			MonoController<SettingsController>.Instance.SaveKeyBinds();
		}
	}

	private void OnSettingsUpdated()
	{
		if (!MonoController<SettingsController>.Instance)
		{
			throw new UnityException("Missing SettingsController");
		}
		PlayerSettings settings = MonoController<SettingsController>.Instance.CurrentSettings;
		if ((bool)resolutionDropdown)
		{
			resolutionDropdown.interactable = settings.screenMode == FullScreenMode.Windowed;
			if (settings.screenMode != FullScreenMode.Windowed)
			{
				resolutionDropdown.ClearOptions();
				resolutionDropdown.AddOptions(new List<TMP_Dropdown.OptionData>
				{
					new TMP_Dropdown.OptionData("DISABLED")
				});
				resolutionDropdown.SetValueWithoutNotify(0);
				resolutionDropdown.RefreshShownValue();
			}
			else
			{
				util_timer.Simple(0.1f, delegate
				{
					Resolution[] resolutions = MonoController<SettingsController>.Instance.GetResolutions();
					UpdateDropdown(resolutionDropdown, resolutions, settings.screenResolution.ToResolution(), (Resolution res) => $"{res.width}x{res.height}", (Resolution value, Resolution compare) => value.width == compare.width && value.height == compare.height);
				});
			}
		}
		if ((bool)vsyncToggle)
		{
			vsyncToggle.SetIsOnWithoutNotify(settings.vsyncEnabled);
		}
		if ((bool)monitorModeDropdown)
		{
			FullScreenMode[] screenModes = MonoController<SettingsController>.Instance.GetScreenModes();
			if (screenModes != null && screenModes.Length > 0)
			{
				UpdateDropdown(monitorModeDropdown, screenModes, settings.screenMode, (FullScreenMode mode) => MonoController<SettingsController>.Instance.ScreenModeToString(mode) ?? "", (FullScreenMode value, FullScreenMode compare) => value == compare);
			}
		}
		if ((bool)monitorDropdown)
		{
			int[] values = Display.displays.Select((Display _, int index) => index).ToArray();
			UpdateDropdown(monitorDropdown, values, settings.monitor, (int display) => display.ToString() ?? "", (int value, int compare) => value == compare);
		}
		if ((bool)shadowDropdown)
		{
			ShadowQuality[] values2 = (ShadowQuality[])Enum.GetValues(typeof(ShadowQuality));
			UpdateDropdown(shadowDropdown, values2, settings.shadow, (ShadowQuality display) => display.ToString() ?? "", (ShadowQuality value, ShadowQuality compare) => value == compare);
		}
		if ((bool)targetFrameRateDropdown)
		{
			if (settings.vsyncEnabled)
			{
				targetFrameRateDropdown.ClearOptions();
				targetFrameRateDropdown.AddOptions(new List<TMP_Dropdown.OptionData>
				{
					new TMP_Dropdown.OptionData("V-SYNC ENABLED")
				});
				targetFrameRateDropdown.SetValueWithoutNotify(0);
				targetFrameRateDropdown.RefreshShownValue();
			}
			else
			{
				PlayerTargetFramerate[] values3 = (PlayerTargetFramerate[])Enum.GetValues(typeof(PlayerTargetFramerate));
				UpdateDropdown(targetFrameRateDropdown, values3, settings.targetFrameRate, (PlayerTargetFramerate mode) => $"{(int)mode} FPS", (PlayerTargetFramerate value, PlayerTargetFramerate compare) => value == compare);
			}
		}
		if ((bool)brightnessSlider)
		{
			brightnessSlider.SetValueWithoutNotify(settings.brightness);
		}
		if ((bool)fovSlider)
		{
			fovSlider.SetValueWithoutNotify(settings.fov);
			if ((bool)fovText)
			{
				fovText.text = $"FOV - {settings.fov}";
			}
		}
		if ((bool)crosshairToggle)
		{
			crosshairToggle.SetIsOnWithoutNotify(settings.crosshair);
		}
		if ((bool)localizationDropdown)
		{
			LOCALE[] values4 = (LOCALE[])Enum.GetValues(typeof(LOCALE));
			UpdateDropdown(localizationDropdown, values4, settings.localization, (LOCALE mode) => MonoController<LocalizationController>.Instance?.GetLanguageName(mode) ?? mode.ToString(), (LOCALE value, LOCALE compare) => value == compare);
		}
		if ((bool)audioMasterSlider)
		{
			audioMasterSlider.SetValueWithoutNotify(settings.masterVolume);
		}
		if ((bool)audioMusicSlider)
		{
			audioMusicSlider.SetValueWithoutNotify(settings.musicVolume);
		}
		if ((bool)audioSFXSlider)
		{
			audioSFXSlider.SetValueWithoutNotify(settings.sfxVolume);
		}
		if ((bool)audioMICROPHONESlider)
		{
			audioMICROPHONESlider.SetValueWithoutNotify(settings.microphoneVolume);
		}
		if ((bool)audioMuteOnUnfocusToggle)
		{
			audioMuteOnUnfocusToggle.SetIsOnWithoutNotify(settings.muteOnUnfocus);
		}
		if ((bool)microphoneModeDropdown)
		{
			VoiceChatMode[] values5 = (VoiceChatMode[])Enum.GetValues(typeof(VoiceChatMode));
			UpdateDropdown(microphoneModeDropdown, values5, settings.microphoneMode, (VoiceChatMode mode) => mode.ToString().Replace("_", " "), (VoiceChatMode value, VoiceChatMode compare) => value == compare);
		}
		if ((bool)microphoneSelectDropdown)
		{
			List<string> microphones = GetMicrophones(nameFilter: false);
			if (microphones != null && microphones.Count > 0)
			{
				int index2 = Math.Clamp(settings.microphoneDevice, 0, microphones.Count - 1);
				UpdateDropdown(microphoneSelectDropdown, microphones.ToArray(), microphones[index2], (string device) => device, (string value, string compare) => value == compare);
			}
		}
		if ((bool)cameraInvertMouseY)
		{
			cameraInvertMouseY.SetIsOnWithoutNotify(settings.invertMouseY);
		}
		if ((bool)rotationInvertMouseX)
		{
			rotationInvertMouseX.SetIsOnWithoutNotify(settings.invertPhysRotationX);
		}
		if ((bool)rotationInvertMouseY)
		{
			rotationInvertMouseY.SetIsOnWithoutNotify(settings.invertPhysRotationY);
		}
		if ((bool)cameraSensitivitySlider)
		{
			cameraSensitivitySlider.SetValueWithoutNotify(settings.mouseSensitivity);
		}
		if ((bool)physgunSensitivitySlider)
		{
			physgunSensitivitySlider.SetValueWithoutNotify(settings.physgunRotateSensitivity);
		}
		if ((bool)vibrationToggle)
		{
			vibrationToggle.SetIsOnWithoutNotify(!settings.disableVibration);
		}
	}

	public static List<string> GetMicrophones(bool nameFilter)
	{
		return Microphone.devices.AsValueEnumerable().Select(delegate(string device)
		{
			if (!nameFilter)
			{
				return device;
			}
			Match match = Regex.Match(device, "\\(([^)]+)\\)");
			return (!match.Success) ? device : match.Groups[1].Value.Trim();
		}).ToList();
	}

	public static void UpdateDropdown<TItem, TCurrent>(TMP_Dropdown dropdown, TItem[] values, TCurrent current, Func<TItem, string> render, Func<TItem, TCurrent, bool> compare)
	{
		if (values == null)
		{
			return;
		}
		dropdown.ClearOptions();
		List<TMP_Dropdown.OptionData> list = new List<TMP_Dropdown.OptionData>();
		int? num = null;
		for (int i = 0; i < values.Length; i++)
		{
			TItem val = values[i];
			list.Add(new TMP_Dropdown.OptionData(render(val)));
			if (compare(val, current))
			{
				num = i;
			}
		}
		dropdown.MultiSelect = false;
		dropdown.AddOptions(list);
		dropdown.SetValueWithoutNotify(num ?? (list.Count - 1));
		dropdown.RefreshShownValue();
	}
}
