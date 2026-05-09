using System;
using System.Collections;
using System.Collections.Generic;
using FailCake;
using Steamworks;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

namespace HyenaQuest;

[DefaultExecutionOrder(-120)]
[DisallowMultipleComponent]
public class StartupController : MonoController<StartupController>
{
	private readonly Dictionary<string, CURSOR_REQUEST> _cursorStack = new Dictionary<string, CURSOR_REQUEST>();

	private InputActionMap _inputActionMap;

	private InputActionMap _inputUIActionMap;

	public new void Awake()
	{
		base.Awake();
		Physics2D.simulationMode = SimulationMode2D.Script;
		if (!MonoController<SettingsController>.Instance)
		{
			throw new UnityException("Missing SettingsController");
		}
		_inputActionMap = MonoController<SettingsController>.Instance.inputActions.FindActionMap("Gameplay");
		_inputUIActionMap = MonoController<SettingsController>.Instance.inputActions.FindActionMap("UI");
		Application.focusChanged += OnFocusChanged;
		OnCursorRequestUpdate();
		StartCoroutine(StartupGame());
		UnityEngine.Object.DontDestroyOnLoad(base.gameObject);
	}

	public new void OnDestroy()
	{
		Application.focusChanged -= OnFocusChanged;
		if ((bool)MonoController<SettingsController>.Instance)
		{
			MonoController<SettingsController>.Instance.OnSettingsUpdated -= new Action(OnSettingsUpdated);
		}
		base.OnDestroy();
	}

	public InputActionMap GetIngameActions()
	{
		return _inputActionMap;
	}

	public void RequestCursor(string cursorID, CURSOR_REQUEST requestType = CURSOR_REQUEST.UI_CONTROL_BLOCK)
	{
		if (!string.IsNullOrEmpty(cursorID) && _cursorStack.TryAdd(cursorID, requestType))
		{
			OnCursorRequestUpdate();
		}
	}

	public void ReleaseCursor(string cursorID)
	{
		if (!string.IsNullOrEmpty(cursorID) && _cursorStack.Remove(cursorID))
		{
			OnCursorRequestUpdate();
		}
	}

	public void ClearCursorRequests()
	{
		_cursorStack.Clear();
		OnCursorRequestUpdate();
	}

	public void OnCursorRequestUpdate()
	{
		bool num = _cursorStack.Count > 0;
		bool flag = _cursorStack.ContainsValue(CURSOR_REQUEST.UI_CONTROL_BLOCK);
		Cursor.lockState = ((!num) ? CursorLockMode.Locked : CursorLockMode.None);
		Cursor.visible = num;
		if (_inputActionMap != null)
		{
			bool flag2 = false;
			flag2 = SteamworksController.IsSteamRunning && SteamworksController.IsOverlayOpen;
			if (!(flag || flag2))
			{
				_inputActionMap.Enable();
			}
			else
			{
				_inputActionMap.Disable();
			}
		}
	}

	private IEnumerator StartupGame()
	{
		yield return new WaitUntil(() => MonoController<SteamworksController>.Instance);
		yield return new WaitUntil(() => NETController.Instance);
		yield return new WaitForEndOfFrame();
		yield return new WaitForEndOfFrame();
		if (!MonoController<SteamworksController>.Instance.Init())
		{
			Application.Quit();
			yield break;
		}
		yield return new WaitForEndOfFrame();
		yield return new WaitForEndOfFrame();
		yield return new WaitUntil(() => MonoController<SettingsController>.Instance);
		MonoController<SettingsController>.Instance.Load();
		yield return new WaitUntil(() => MonoController<LocalizationController>.Instance);
		MonoController<LocalizationController>.Instance.Init();
		yield return new WaitUntil(() => MonoController<PostProcessController>.Instance);
		yield return MonoController<PostProcessController>.Instance.Init();
		yield return new WaitUntil(() => MonoController<DiscordController>.Instance);
		MonoController<DiscordController>.Instance.Init();
		NETController.Instance.Init();
		MonoController<SettingsController>.Instance.OnSettingsUpdated += new Action(OnSettingsUpdated);
		yield return CheckGameStartup();
	}

	private void OnFocusChanged(bool focused)
	{
		if ((bool)MonoController<SettingsController>.Instance)
		{
			AudioListener.volume = (focused ? 1f : (MonoController<SettingsController>.Instance.CurrentSettings.muteOnUnfocus ? 0f : 1f));
		}
	}

	private void FallbackResolution(out int width, out int height)
	{
		try
		{
			Resolution currentResolution = Screen.currentResolution;
			width = currentResolution.width;
			height = currentResolution.height;
		}
		catch (Exception ex)
		{
			Debug.LogWarning("[Display] Screen.currentResolution failed: " + ex.Message);
			width = 0;
			height = 0;
		}
		if (width <= 0 || height <= 0)
		{
			width = 1920;
			height = 1080;
			Debug.LogWarning("[Display] Screen.currentResolution returned invalid data, using 1920x1080 fallback");
		}
	}

	private bool ShouldRefreshScreen(int targetW, int targetH, FullScreenMode targetMode)
	{
		if (Screen.width == targetW && Screen.height == targetH)
		{
			return Screen.fullScreenMode != targetMode;
		}
		return true;
	}

	private void OnSettingsUpdated()
	{
		if (!MonoController<SettingsController>.Instance)
		{
			return;
		}
		PlayerSettings settings = MonoController<SettingsController>.Instance.CurrentSettings;
		Application.targetFrameRate = (int)settings.targetFrameRate;
		QualitySettings.vSyncCount = (settings.vsyncEnabled ? 1 : 0);
		if (GraphicsSettings.currentRenderPipeline is UniversalRenderPipelineAsset universalRenderPipelineAsset)
		{
			switch (settings.shadow)
			{
			case ShadowQuality.HIGH:
				universalRenderPipelineAsset.additionalLightsShadowmapResolution = 8192;
				universalRenderPipelineAsset.maxAdditionalLightsCount = 4;
				universalRenderPipelineAsset.shadowDistance = 20f;
				universalRenderPipelineAsset.shadowCascadeCount = 4;
				break;
			case ShadowQuality.MEDIUM:
				universalRenderPipelineAsset.additionalLightsShadowmapResolution = 4096;
				universalRenderPipelineAsset.maxAdditionalLightsCount = 3;
				universalRenderPipelineAsset.shadowDistance = 20f;
				universalRenderPipelineAsset.shadowCascadeCount = 3;
				break;
			case ShadowQuality.LOW:
				universalRenderPipelineAsset.additionalLightsShadowmapResolution = 2048;
				universalRenderPipelineAsset.maxAdditionalLightsCount = 2;
				universalRenderPipelineAsset.shadowDistance = 20f;
				universalRenderPipelineAsset.shadowCascadeCount = 2;
				break;
			case ShadowQuality.OFF:
				universalRenderPipelineAsset.additionalLightsShadowmapResolution = 256;
				universalRenderPipelineAsset.maxAdditionalLightsCount = 0;
				universalRenderPipelineAsset.shadowDistance = 0f;
				break;
			}
		}
		List<DisplayInfo> list = new List<DisplayInfo>();
		try
		{
			Screen.GetDisplayLayout(list);
		}
		catch (Exception ex)
		{
			Debug.LogWarning("[Display] GetDisplayLayout failed: " + ex.Message);
		}
		if (list.Count == 0)
		{
			FallbackResolution(out var width, out var height);
			int num = width;
			int num2 = height;
			int targetW;
			int targetH;
			if (settings.screenMode == FullScreenMode.Windowed)
			{
				targetW = Mathf.Clamp(settings.screenResolution.width, 1280, num);
				targetH = Mathf.Clamp(settings.screenResolution.height, 720, num2);
			}
			else
			{
				targetW = num;
				targetH = num2;
			}
			if (!ShouldRefreshScreen(targetW, targetH, settings.screenMode))
			{
				return;
			}
			util_timer.Simple(0.1f, delegate
			{
				try
				{
					Screen.SetResolution(targetW, targetH, settings.screenMode);
				}
				catch (Exception ex2)
				{
					Debug.LogError("[Display] SetResolution failed (fallback path): " + ex2.Message);
				}
			});
			return;
		}
		int num3 = settings.monitor;
		if (num3 < 0 || num3 >= list.Count)
		{
			num3 = 0;
			settings.monitor = num3;
			MonoController<SettingsController>.Instance.CurrentSettings = settings;
		}
		DisplayInfo displayInfo = list[num3];
		int width2 = displayInfo.width;
		int height2 = displayInfo.height;
		if (displayInfo.refreshRate.numerator == 0 || width2 <= 0 || height2 <= 0)
		{
			FallbackResolution(out width2, out height2);
		}
		int num4 = width2;
		int num5 = height2;
		int targetW2;
		int targetH2;
		if (settings.screenMode == FullScreenMode.Windowed)
		{
			targetW2 = Mathf.Clamp(settings.screenResolution.width, 1280, num4);
			targetH2 = Mathf.Clamp(settings.screenResolution.height, 720, num5);
		}
		else
		{
			targetW2 = num4;
			targetH2 = num5;
		}
		if (!ShouldRefreshScreen(targetW2, targetH2, settings.screenMode))
		{
			return;
		}
		util_timer.Simple(0.1f, delegate
		{
			try
			{
				Screen.SetResolution(targetW2, targetH2, settings.screenMode);
			}
			catch (Exception ex3)
			{
				Debug.LogError("[Display] SetResolution failed: " + ex3.Message);
			}
		});
	}

	private IEnumerator CheckGameStartup()
	{
		if (SteamworksController.IsSteamRunning)
		{
			if (SteamApps.GetLaunchCommandLine(out var pszCommandLine, 32768) > 0 && !string.IsNullOrEmpty(pszCommandLine))
			{
				if (ulong.TryParse(pszCommandLine.Replace("+connect_lobby", ""), out var result))
				{
					NETController.LOBBY_CONNECT_ID = result;
					SceneManager.LoadScene("LOADING");
					yield break;
				}
			}
			else
			{
				string[] commandLineArgs = Environment.GetCommandLineArgs();
				for (int i = 0; i < commandLineArgs.Length - 1; i++)
				{
					if (commandLineArgs[i].Equals("+connect_lobby", StringComparison.OrdinalIgnoreCase) && ulong.TryParse(commandLineArgs[i + 1], out var result2) && result2 != 0)
					{
						NETController.LOBBY_CONNECT_ID = result2;
						SceneManager.LoadScene("LOADING");
						yield break;
					}
				}
			}
		}
		yield return new WaitForSecondsRealtime(2f);
		if (!NETController.LOBBY_CONNECT_ID.HasValue)
		{
			SceneManager.LoadScene("MAINMENU");
		}
	}
}
