using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using Steamworks;
using UnityEngine;
using UnityEngine.InputSystem;
using ZLinq;

namespace HyenaQuest;

[DisallowMultipleComponent]
[DefaultExecutionOrder(-121)]
public class SettingsController : MonoController<SettingsController>
{
	public static readonly string SETTINGS_VERSION = "7.0.0";

	public static readonly byte MAX_SAVE_SLOTS = 2;

	private static readonly string DISCORD_TOKEN_FILE = "DISCORD_TOKEN_DO_NOT_SHARE.bin";

	public InputActionAsset inputActions;

	public GameEvent OnSettingsUpdated = new GameEvent();

	public GameEvent<ulong, string, bool> OnBanListUpdated = new GameEvent<ulong, string, bool>();

	private Dictionary<ulong, string> _banList = new Dictionary<ulong, string>();

	private PlayerSettings _currentSettings;

	private SaveFileSettings _saveSettings;

	private string _bindings;

	private bool _loaded;

	public PlayerSettings CurrentSettings
	{
		get
		{
			return _currentSettings;
		}
		set
		{
			_currentSettings = value;
			OnSettingsUpdated?.Invoke();
		}
	}

	public new void Awake()
	{
		base.Awake();
		if (!inputActions)
		{
			throw new UnityException("Missing InputActionAsset component");
		}
		UnityEngine.Object.DontDestroyOnLoad(base.gameObject);
	}

	public new void OnDestroy()
	{
		Save();
		base.OnDestroy();
	}

	public Resolution[] GetResolutions()
	{
		Resolution[] resolutions = Screen.resolutions;
		List<Resolution> list = new List<Resolution>();
		double value = Screen.currentResolution.refreshRateRatio.value;
		Resolution[] array = resolutions;
		for (int i = 0; i < array.Length; i++)
		{
			Resolution item = array[i];
			if (Math.Abs(item.refreshRateRatio.value - value) < 0.009999999776482582 && item.width >= 1280 && item.height >= 720)
			{
				list.Add(item);
			}
		}
		if (list.Count == 0)
		{
			array = resolutions;
			for (int i = 0; i < array.Length; i++)
			{
				Resolution item2 = array[i];
				if (item2.width >= 1280 && item2.height >= 720)
				{
					list.Add(item2);
				}
			}
		}
		return list.ToArray();
	}

	public FullScreenMode[] GetScreenModes()
	{
		return new FullScreenMode[2]
		{
			FullScreenMode.FullScreenWindow,
			FullScreenMode.Windowed
		};
	}

	public string ScreenModeToString(FullScreenMode mode)
	{
		return mode switch
		{
			FullScreenMode.FullScreenWindow => "Fullscreen", 
			FullScreenMode.Windowed => "Windowed", 
			_ => null, 
		};
	}

	public float GetMicrophoneSetting(string playerID)
	{
		return _currentSettings.playerMicrophoneSettings.GetValueOrDefault(playerID, 1f);
	}

	public bool IsFirstTimer()
	{
		return !_currentSettings.bestTimes.AsValueEnumerable().Any((uint time) => time != 0);
	}

	public void SaveKeyBinds()
	{
		if (!inputActions)
		{
			throw new UnityException("Missing InputActionAsset component");
		}
		_bindings = inputActions.SaveBindingOverridesAsJson();
	}

	public void LoadKeyBinds()
	{
		_bindings = Encoding.UTF8.GetString(Convert.FromBase64String(_currentSettings.keybinds ?? ""));
		if (!string.IsNullOrEmpty(_bindings))
		{
			inputActions.LoadBindingOverridesFromJson(_bindings);
		}
	}

	private byte[] GetEncryptionKey()
	{
		string s = SystemInfo.deviceUniqueIdentifier ?? "";
		using SHA256 sHA = SHA256.Create();
		return sHA.ComputeHash(Encoding.UTF8.GetBytes(s));
	}

	private string GetDiscordTokenPath()
	{
		if (_saveSettings == null)
		{
			return null;
		}
		return Path.Combine(Path.GetDirectoryName(_saveSettings.FilePath) ?? Application.persistentDataPath, DISCORD_TOKEN_FILE);
	}

	public void SetDiscordRefreshToken(string token)
	{
	}

	public string GetDiscordRefreshToken()
	{
		return null;
	}

	public Dictionary<ulong, string> GetBanList()
	{
		return _banList;
	}

	public bool AddToBanList(ulong id, string playerName = null)
	{
		string text = (string.IsNullOrEmpty(playerName) ? "Unknown" : playerName);
		if (!_banList.TryAdd(id, text) || id == 0L)
		{
			return false;
		}
		util_save.Save("Ban-List", _banList, _saveSettings);
		OnBanListUpdated?.Invoke(id, text, param3: true);
		return true;
	}

	public bool RemoveFromBanList(ulong id)
	{
		if (!_banList.Remove(id))
		{
			return false;
		}
		util_save.Save("Ban-List", _banList, _saveSettings);
		OnBanListUpdated?.Invoke(id, null, param3: false);
		return true;
	}

	public bool RemoveFromBanListByIndex(int index)
	{
		if (index < 0 || index >= _banList.Count)
		{
			return false;
		}
		return RemoveFromBanList(_banList.Keys.AsValueEnumerable().ElementAt(index));
	}

	public void ClearBanList()
	{
		_banList.Clear();
		OnBanListUpdated?.Invoke(ulong.MaxValue, null, param3: false);
		util_save.Save("Ban-List", _banList, _saveSettings);
	}

	public byte[] LoadPaintData()
	{
		string text = util_save.Load("PaintData", "", _saveSettings);
		if (!string.IsNullOrEmpty(text))
		{
			return Convert.FromBase64String(text);
		}
		return null;
	}

	public void SavePaintData(byte[] paintData)
	{
		if (paintData != null && paintData.Length > 0)
		{
			util_save.Save("PaintData", Convert.ToBase64String(paintData), _saveSettings);
		}
	}

	public byte[] LoadYeenOfMonth()
	{
		string text = util_save.Load<string>("PaintData-MonthYeen", null, _saveSettings);
		if (!string.IsNullOrEmpty(text))
		{
			return Convert.FromBase64String(text);
		}
		return null;
	}

	public void SaveYeenMonthPaintData(byte[] paintData)
	{
		if (paintData != null && paintData.Length > 0)
		{
			string value = Convert.ToBase64String(paintData);
			if (!string.IsNullOrEmpty(value))
			{
				util_save.Save("PaintData-MonthYeen", value, _saveSettings);
			}
		}
	}

	public byte FindEmptySaveSlot()
	{
		for (byte b = 0; b < MAX_SAVE_SLOTS; b++)
		{
			if (!HasSave(b))
			{
				return b;
			}
		}
		return byte.MaxValue;
	}

	public List<SaveData> GetSaves()
	{
		List<SaveData> list = new List<SaveData>();
		for (byte b = 0; b < MAX_SAVE_SLOTS; b++)
		{
			if (HasSave(b))
			{
				list.Add(util_save.Load($"SAVE-{b}", default(SaveData), _saveSettings));
			}
		}
		return list;
	}

	public void DeleteSave(SaveData data)
	{
		if (HasSave(data.slot))
		{
			util_save.DeleteKey($"SAVE-{data.slot}", _saveSettings);
		}
	}

	public bool HasSave(byte slot)
	{
		if (slot > MAX_SAVE_SLOTS)
		{
			return false;
		}
		return util_save.KeyExists($"SAVE-{slot}", _saveSettings);
	}

	public bool SaveGame(SaveData data)
	{
		if (data.slot == byte.MaxValue)
		{
			throw new UnityException("Failed to find available slot");
		}
		try
		{
			util_save.Save($"SAVE-{data.slot}", data, _saveSettings);
			Debug.Log($"Data saved on slot {data.slot}!");
		}
		catch
		{
			Debug.LogError($"Failed to save game on slot {data.slot}");
			return false;
		}
		return true;
	}

	public void Load()
	{
		_saveSettings = new SaveFileSettings($"{(SteamworksController.IsSteamRunning ? SteamUser.GetSteamID().m_SteamID : 0)}.json");
		DeleteLegacyES3(_saveSettings.FilePath);
		DeleteLegacyDiscordKey(_saveSettings);
		string text;
		try
		{
			text = util_save.Load("VERSION", SETTINGS_VERSION, _saveSettings);
		}
		catch (FormatException)
		{
			Debug.LogWarning("Settings file corrupted. Deleting and recreating..");
			util_save.DeleteFile(_saveSettings);
			text = SETTINGS_VERSION;
		}
		if (text != SETTINGS_VERSION)
		{
			Debug.LogWarning("Detected old settings version " + text + ". Deleting..");
			util_save.DeleteFile(_saveSettings);
		}
		util_save.Save("VERSION", SETTINGS_VERSION, _saveSettings);
		PlayerSettings playerSettings = default(PlayerSettings);
		playerSettings.screenMode = FullScreenMode.FullScreenWindow;
		playerSettings.screenResolution = Screen.currentResolution;
		playerSettings.targetFrameRate = PlayerTargetFramerate.TARGET_120;
		playerSettings.vsyncEnabled = false;
		playerSettings.monitor = -1;
		playerSettings.brightness = 0f;
		playerSettings.fov = 60f;
		playerSettings.configured = false;
		playerSettings.crosshair = true;
		playerSettings.disableVibration = false;
		playerSettings.accessories = 0uL;
		playerSettings.keybinds = "";
		playerSettings.masterVolume = 1f;
		playerSettings.musicVolume = 1f;
		playerSettings.sfxVolume = 1f;
		playerSettings.microphoneVolume = 1f;
		playerSettings.muteOnUnfocus = false;
		playerSettings.localization = LOCALE.EN;
		playerSettings.mouseSensitivity = 0.5f;
		playerSettings.physgunRotateSensitivity = 0.5f;
		playerSettings.invertMouseY = false;
		playerSettings.invertPhysRotationX = true;
		playerSettings.invertPhysRotationY = false;
		playerSettings.bestTimes = new uint[255];
		playerSettings.microphoneDevice = 0;
		playerSettings.microphoneMode = VoiceChatMode.OPEN;
		playerSettings.shadow = ShadowQuality.HIGH;
		playerSettings.playerMicrophoneSettings = new Dictionary<string, float>();
		PlayerSettings playerSettings2 = playerSettings;
		try
		{
			_currentSettings = util_save.Load("Settings", playerSettings2, _saveSettings);
		}
		catch (FormatException)
		{
			Debug.LogWarning("Settings data corrupted. Using defaults..");
			_currentSettings = playerSettings2;
		}
		if (_currentSettings.monitor == -1)
		{
			List<DisplayInfo> displayLayoutSafe = GetDisplayLayoutSafe();
			_currentSettings.monitor = DetectMainMonitorIndex(displayLayoutSafe);
		}
		if (_currentSettings.bestTimes == null)
		{
			_currentSettings.bestTimes = new uint[255];
		}
		LoadKeyBinds();
		try
		{
			_banList = util_save.Load("Ban-List", new Dictionary<ulong, string>(), _saveSettings);
		}
		catch (FormatException)
		{
			Debug.LogWarning("Ban list data corrupted. Using empty list..");
			_banList = new Dictionary<ulong, string>();
		}
		if (_currentSettings.playerMicrophoneSettings == null)
		{
			_currentSettings.playerMicrophoneSettings = new Dictionary<string, float>();
		}
		OnSettingsUpdated?.Invoke();
		_loaded = true;
	}

	private void Save()
	{
		if (!_loaded || _saveSettings == null)
		{
			return;
		}
		_currentSettings.keybinds = Convert.ToBase64String(Encoding.UTF8.GetBytes(_bindings ?? ""));
		try
		{
			util_save.Save("Settings", _currentSettings, _saveSettings);
		}
		catch (FormatException)
		{
			util_save.DeleteFile(_saveSettings);
			util_save.Save("Settings", _currentSettings, _saveSettings);
		}
	}

	private static List<DisplayInfo> GetDisplayLayoutSafe()
	{
		List<DisplayInfo> list = new List<DisplayInfo>();
		try
		{
			Screen.GetDisplayLayout(list);
		}
		catch (Exception ex)
		{
			Debug.LogWarning("[Settings] GetDisplayLayout failed: " + ex.Message);
		}
		return list;
	}

	private static int DetectMainMonitorIndex(List<DisplayInfo> displays)
	{
		if (displays == null || displays.Count == 0)
		{
			return 0;
		}
		try
		{
			int num = displays.FindIndex((DisplayInfo d) => d.Equals(Screen.mainWindowDisplayInfo));
			return (num >= 0) ? num : 0;
		}
		catch (Exception ex)
		{
			Debug.LogWarning("[Settings] mainWindowDisplayInfo failed: " + ex.Message);
			return 0;
		}
	}

	private static void DeleteLegacyES3(string jsonPath)
	{
		string path = Path.ChangeExtension(jsonPath, ".es3");
		if (!File.Exists(path))
		{
			return;
		}
		try
		{
			File.Delete(path);
			Debug.Log("Deleted es3 file " + Path.GetFileName(path));
		}
		catch (Exception ex)
		{
			Debug.LogWarning("Failed to es3 legacy file: " + ex.Message);
		}
	}

	private static void DeleteLegacyDiscordKey(SaveFileSettings settings)
	{
		try
		{
			if (util_save.KeyExists("DiscordDK", settings))
			{
				util_save.DeleteKey("DiscordDK", settings);
			}
		}
		catch (Exception ex)
		{
			Debug.LogWarning("[Settings] Failed to clean up legacy DiscordDK key: " + ex.Message);
		}
	}
}
