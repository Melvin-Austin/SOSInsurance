using System;
using System.Collections.Generic;
using FailCake;
using Steamworks;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;

namespace HyenaQuest;

[DefaultExecutionOrder(-80)]
[DisallowMultipleComponent]
public class UIController : MonoController<UIController>
{
	public InputActionReference optionsAction;

	public GameObject uiCanvas;

	public GameObject uiOptions;

	public GameObject uiCrosshair;

	public GameObject outfitCanvas;

	public Button outfitClose;

	public Button disconnectButton;

	public Button inviteButton;

	public ui_dx_selector selector;

	public List<Sprite> interactionTextures = new List<Sprite>();

	public ui_fade eyes;

	public TextMeshProUGUI timerText;

	public GameObject aliveHUD;

	public GameObject viewBlocker;

	public GameObject scrapUIContainer;

	public Slider scrapProgress;

	public TextMeshProUGUI scrapText;

	public Slider health;

	public TextMeshProUGUI healthCounter;

	public GameObject deadHUD;

	public TextMeshPro spectateName;

	public Button listToggleButton;

	public GameObject playerList;

	public GameObject banList;

	public GameEvent<bool> OnOptionsToggle = new GameEvent<bool>();

	private util_fade_timer _selectorFade;

	private InteractionData _currentInteraction;

	private entity_item_vacuum _scrapContainer;

	private util_fade_timer _scrapSet;

	private bool _banListMode;

	private ui_inventory _inventory;

	private util_fade_timer _timerHit;

	private util_timer _cameraFreeze;

	public new void Awake()
	{
		base.Awake();
		if (!uiCanvas)
		{
			throw new UnityException("UI Canvas not found");
		}
		uiCanvas.SetActive(value: true);
		if (!uiOptions)
		{
			throw new UnityException("UI Options not found");
		}
		uiOptions.SetActive(value: false);
		if (!uiCrosshair)
		{
			throw new UnityException("UI Crosshair not found");
		}
		uiCrosshair.SetActive(value: false);
		if (!disconnectButton)
		{
			throw new UnityException("Disconnect button not found");
		}
		disconnectButton.onClick.AddListener(OnDisconnect);
		if (!inviteButton)
		{
			throw new UnityException("Invite button not found");
		}
		inviteButton.onClick.AddListener(InviteFriends);
		_inventory = GetComponentInChildren<ui_inventory>(includeInactive: true);
		if (!_inventory)
		{
			throw new UnityException("Inventory UI not found");
		}
		if (!outfitCanvas)
		{
			throw new UnityException("outfitCanvas not found");
		}
		outfitCanvas.SetActive(value: false);
		if (!outfitClose)
		{
			throw new UnityException("outfitClose not found");
		}
		outfitClose.onClick.AddListener(OnOutfitClose);
		if (!selector)
		{
			throw new UnityException("UI Selector not found");
		}
		List<Sprite> list = interactionTextures;
		if (list == null || list.Count <= 0)
		{
			throw new UnityException("Missing interactionTextures");
		}
		if (!timerText)
		{
			throw new UnityException("UI Timer Text not found");
		}
		SetTimer(enable: false);
		if (!eyes)
		{
			throw new UnityException("UI Eyes not found");
		}
		if (!health)
		{
			throw new UnityException("UI Health not found");
		}
		if (!healthCounter)
		{
			throw new UnityException("UI healthCounter not found");
		}
		if (!aliveHUD)
		{
			throw new UnityException("UI Alive HUD not found");
		}
		if (!viewBlocker)
		{
			throw new UnityException("UI View Blocker not found");
		}
		viewBlocker.SetActive(value: false);
		aliveHUD.SetActive(value: true);
		if (!deadHUD)
		{
			throw new UnityException("UI Dead HUD not found");
		}
		deadHUD.SetActive(value: false);
		if (!spectateName)
		{
			throw new UnityException("UI Spectate Name not found");
		}
		spectateName.text = "";
		if (!scrapUIContainer)
		{
			throw new UnityException("UI Scrap Container not found");
		}
		if (!scrapProgress)
		{
			throw new UnityException("UI Scrap Progress not found");
		}
		if (!scrapText)
		{
			throw new UnityException("UI Scrap Text not found");
		}
		scrapUIContainer.SetActive(value: false);
		if (!listToggleButton)
		{
			throw new UnityException("Missing listToggle");
		}
		listToggleButton.onClick.AddListener(OnListToggle);
		if (!playerList)
		{
			throw new UnityException("Missing playerList");
		}
		if (!banList)
		{
			throw new UnityException("Missing banList");
		}
		playerList.SetActive(value: true);
		banList.SetActive(value: false);
		ResetInteraction();
		CoreController.WaitFor(delegate(IngameController ingameCtrl)
		{
			ingameCtrl.OnStatusUpdated += new Action<INGAME_STATUS, bool>(OnIngameStatusUpdated);
			ingameCtrl.OnWorldTimerUpdate += new Action<uint, bool>(OnWorldTimerUpdate);
			ingameCtrl.OnPlayerCountUpdate += new Action<int, bool>(OnPlayerCountUpdate);
		});
		PlayerController.OnLocalPlayerSet += new Action(SetLocalPlayer);
		CoreController.WaitFor(delegate(SpectateController specCtrl)
		{
			specCtrl.OnSpectateUpdate += new Action<entity_player>(OnSpectateUpdate);
		});
		CoreController.WaitFor(delegate(SettingsController setCtrl)
		{
			setCtrl.OnSettingsUpdated += new Action(OnSettingsUpdated);
		});
		if (!optionsAction)
		{
			throw new UnityException("Options action not set in UIController");
		}
		optionsAction.action.performed += ToggleOptionsMenu;
		UpdateListLocalization();
	}

	public new void OnDestroy()
	{
		_scrapSet?.Stop();
		_timerHit?.Stop();
		ResetFrameFreeze();
		if ((bool)MonoController<StartupController>.Instance)
		{
			MonoController<StartupController>.Instance.ReleaseCursor("OPTIONS");
		}
		if ((bool)NetController<IngameController>.Instance)
		{
			NetController<IngameController>.Instance.OnStatusUpdated -= new Action<INGAME_STATUS, bool>(OnIngameStatusUpdated);
			NetController<IngameController>.Instance.OnWorldTimerUpdate -= new Action<uint, bool>(OnWorldTimerUpdate);
		}
		PlayerController.OnLocalPlayerSet -= new Action(SetLocalPlayer);
		if ((bool)MonoController<SpectateController>.Instance)
		{
			MonoController<SpectateController>.Instance.OnSpectateUpdate -= new Action<entity_player>(OnSpectateUpdate);
		}
		if ((bool)MonoController<SettingsController>.Instance)
		{
			MonoController<SettingsController>.Instance.OnSettingsUpdated -= new Action(OnSettingsUpdated);
		}
		if ((bool)optionsAction)
		{
			optionsAction.action.performed -= ToggleOptionsMenu;
		}
		if ((bool)disconnectButton)
		{
			disconnectButton.onClick.RemoveListener(OnDisconnect);
		}
		if ((bool)inviteButton)
		{
			inviteButton.onClick.RemoveListener(InviteFriends);
		}
		if ((bool)listToggleButton)
		{
			listToggleButton.onClick.RemoveListener(OnListToggle);
		}
		base.OnDestroy();
	}

	public void HideHUD(bool hidden)
	{
		if ((bool)selector && (bool)SDK.MainCamera)
		{
			selector.gameObject.SetActive(!hidden);
			int num = LayerMask.NameToLayer("HUD");
			int num2 = 1 << num;
			SDK.MainCamera.cullingMask = (hidden ? (SDK.MainCamera.cullingMask & ~num2) : (SDK.MainCamera.cullingMask | num2));
		}
	}

	public void SetViewBlocked(bool blocked)
	{
		if (!viewBlocker)
		{
			throw new UnityException("Missing view blocker");
		}
		viewBlocker.SetActive(blocked);
	}

	private void InviteFriends()
	{
		if (SteamworksController.IsSteamRunning && NETController.LOBBY_CONNECT_ID.HasValue)
		{
			SteamFriends.ActivateGameOverlayInviteDialog(new CSteamID(NETController.LOBBY_CONNECT_ID.Value));
		}
	}

	private void OnListToggle()
	{
		SetListMode(!_banListMode);
	}

	private void SetListMode(bool mode)
	{
		if (_banListMode != mode && (bool)banList && (bool)playerList)
		{
			_banListMode = mode;
			banList.SetActive(_banListMode);
			playerList.SetActive(!_banListMode);
			UpdateListLocalization();
		}
	}

	private void UpdateListLocalization()
	{
		if (!MonoController<LocalizationController>.Instance)
		{
			return;
		}
		MonoController<LocalizationController>.Instance.Cleanup("ban-list-mode");
		MonoController<LocalizationController>.Instance.Get("ban-list-mode", _banListMode ? "options.list.players" : "options.list.ban", delegate(string s)
		{
			if ((bool)listToggleButton)
			{
				TextMeshProUGUI componentInChildren = listToggleButton.GetComponentInChildren<TextMeshProUGUI>(includeInactive: true);
				if ((bool)componentInChildren)
				{
					componentInChildren.text = s;
				}
			}
		});
	}

	public void FrameFreeze(float time = 0.01f)
	{
		if (!SDK.MainCamera)
		{
			throw new UnityException("Missing main camera");
		}
		SDK.MainCamera.enabled = false;
		_cameraFreeze?.Stop();
		_cameraFreeze = util_timer.Simple(time, ResetFrameFreeze);
	}

	private void ResetFrameFreeze()
	{
		if ((bool)SDK.MainCamera)
		{
			SDK.MainCamera.enabled = true;
		}
		_cameraFreeze?.Stop();
	}

	public void RebuildInventory()
	{
		if (!_inventory)
		{
			throw new UnityException("Missing inventory");
		}
		_inventory.BuildInventory();
	}

	private void SetLocalPlayer()
	{
		entity_player_inventory inventory = PlayerController.LOCAL.GetInventory();
		if (!inventory)
		{
			throw new UnityException("Missing player inventory");
		}
		inventory.OnInventoryUpdate += new Action<int, entity_item_pickable, bool>(OnInventoryUpdate);
		inventory.OnInventorySlotUpdate += new Action<int>(OnInventorySlotUpdate);
		health.value = PlayerController.LOCAL.GetHealth();
		healthCounter.text = Mathf.RoundToInt(health.value).ToString();
		PlayerController.LOCAL.OnHealthUpdate += (Action<int, bool>)delegate(int val, bool server)
		{
			if (!server)
			{
				health.value = val;
				healthCounter.text = Mathf.RoundToInt(health.value).ToString();
			}
		};
		listToggleButton.gameObject.SetActive(PlayerController.LOCAL.GetPlayerID() == 0);
	}

	private void OnInventoryUpdate(int slot, entity_item_pickable item, bool server)
	{
		if (!server && slot >= 0)
		{
			if (!_inventory)
			{
				throw new UnityException("Missing inventory");
			}
			entity_player_inventory inventory = PlayerController.LOCAL.GetInventory();
			if (!inventory)
			{
				throw new UnityException("Missing player inventory");
			}
			if ((bool)_scrapContainer)
			{
				_scrapContainer.OnScrapAdded -= new Action<int, bool>(OnScrapAdded);
			}
			_scrapContainer = inventory.FindItemByID("item_vacuum") as entity_item_vacuum;
			if ((bool)_scrapContainer)
			{
				_scrapContainer.OnScrapAdded += new Action<int, bool>(OnScrapAdded);
				SetScrap(_scrapContainer.GetTotalScrap());
			}
			SetHasScrapContainer(_scrapContainer);
			_inventory?.UpdateInventorySlot(slot, item);
		}
	}

	private void OnInventorySlotUpdate(int slot)
	{
		if (!_inventory)
		{
			throw new UnityException("Missing inventory");
		}
		_inventory?.UpdateInventorySelectedSlot(slot);
	}

	private void OnScrapAdded(int scrap, bool server)
	{
		if (server || !scrapText || !scrapProgress)
		{
			return;
		}
		int maxContainerScrap = NetController<ScrapController>.Instance?.GetMaxContainerScrap() ?? 200;
		int oldValue = int.Parse(scrapText.text);
		scrapText.text = scrap.ToString();
		_scrapSet?.Stop();
		_scrapSet = util_fade_timer.Fade(5f, oldValue, scrap, delegate(float f)
		{
			if ((bool)scrapProgress && (bool)scrapText)
			{
				float num = (f - (float)oldValue) / (float)(scrap - oldValue);
				float num2 = 1f + Mathf.Sin(num * MathF.PI) * 0.5f;
				scrapText.transform.localScale = Vector3.one * num2;
				scrapProgress.value = f / (float)maxContainerScrap;
			}
		});
	}

	private void SetHasScrapContainer(bool enable)
	{
		if (!scrapUIContainer)
		{
			throw new UnityException("Missing scrap container");
		}
		_scrapSet?.Stop();
		scrapUIContainer.SetActive(enable);
	}

	private void SetScrap(int scrapAmount)
	{
		if (!scrapText)
		{
			throw new UnityException("Missing scrap text");
		}
		if (!scrapProgress)
		{
			throw new UnityException("Missing scrap progress");
		}
		int num = NetController<ScrapController>.Instance?.GetMaxContainerScrap() ?? 200;
		scrapText.text = scrapAmount.ToString();
		scrapProgress.value = (float)scrapAmount / (float)num;
	}

	public void SetFade(bool fadeIn, Color color, float speed = 0.25f)
	{
		if ((bool)eyes)
		{
			eyes.SetColor(color);
			StartFade(fadeIn, speed);
		}
	}

	public void SetFade(bool fadeIn, float speed = 0.25f)
	{
		if ((bool)eyes)
		{
			eyes.SetColor(Color.black);
			StartFade(fadeIn, speed);
		}
	}

	private void StartFade(bool fadeIn, float speed)
	{
		if ((bool)eyes)
		{
			if (speed >= 10f)
			{
				eyes.SetAlpha(fadeIn ? 100 : 0);
				return;
			}
			eyes.fadeSpeed = speed;
			eyes.fadeIn = fadeIn;
			eyes.Play();
		}
	}

	public void SetInteraction(InteractionData data)
	{
		if (data == null)
		{
			selector?.ClearAll();
			_currentInteraction = null;
			SettingsController instance = MonoController<SettingsController>.Instance;
			if ((object)instance != null && instance.CurrentSettings.crosshair)
			{
				uiCrosshair?.SetActive(PlayerController.LOCAL?.GetPhysgun()?.IsGrabbing() != true);
			}
		}
		else if (!(_currentInteraction == data))
		{
			_currentInteraction = data;
			uiCrosshair?.SetActive(value: false);
			if ((bool)selector && data.renderers != null)
			{
				selector.Highlight(data.renderers, data.hint, (data.interaction != 0) ? interactionTextures[(int)(data.interaction - 1)] : null);
			}
		}
	}

	private void ResetInteraction()
	{
		selector?.ClearAll();
		_currentInteraction = null;
	}

	public void ShowOutfitMenu(bool show)
	{
		if (!outfitCanvas)
		{
			throw new UnityException("Missing outfitCanvas");
		}
		outfitCanvas.SetActive(show);
	}

	private void OnOutfitClose()
	{
		if ((bool)PlayerController.LOCAL)
		{
			PlayerController.LOCAL.SetInOutfitMode(set: false);
		}
	}

	private void OnPlayerCountUpdate(int players, bool server)
	{
		if ((bool)inviteButton && NETController.LOBBY_CONNECT_ID.HasValue)
		{
			int num = (SteamworksController.IsSteamRunning ? SteamMatchmaking.GetLobbyMemberLimit(new CSteamID(NETController.LOBBY_CONNECT_ID.Value)) : NETController.DEFAULT_MAX_PLAYERS);
			inviteButton.gameObject.SetActive(players < num);
		}
	}

	private void OnSettingsUpdated()
	{
		if ((bool)MonoController<SettingsController>.Instance)
		{
			PlayerSettings currentSettings = MonoController<SettingsController>.Instance.CurrentSettings;
			uiCrosshair?.SetActive(currentSettings.crosshair);
			SDK.MainCamera?.UpdateVolumeStack();
		}
	}

	private void OnDisconnect()
	{
		if (!NETController.Instance)
		{
			throw new UnityException("Missing NetworkController");
		}
		NETController.Instance.Disconnect();
	}

	private void ToggleOptionsMenu(InputAction.CallbackContext obj)
	{
		if (!MonoController<OptionsController>.Instance)
		{
			throw new UnityException("Missing OptionsController");
		}
		if (!MonoController<StartupController>.Instance)
		{
			throw new UnityException("Missing StartupController");
		}
		if ((bool)NetController<ChatController>.Instance && NetController<ChatController>.Instance.IsChatOpen())
		{
			NetController<ChatController>.Instance.CloseChat();
		}
		else if ((bool)uiOptions)
		{
			bool flag = !uiOptions.activeSelf;
			MonoController<OptionsController>.Instance.SetStatus(OptionsState.DISABLED);
			uiOptions.SetActive(flag);
			if (flag)
			{
				uiOptions.GetComponentInChildren<Button>()?.Select();
			}
			selector.gameObject.SetActive(!flag);
			SetListMode(mode: false);
			if (flag)
			{
				MonoController<StartupController>.Instance.RequestCursor("OPTIONS");
			}
			else
			{
				MonoController<StartupController>.Instance.ReleaseCursor("OPTIONS");
			}
			OnOptionsToggle?.Invoke(flag);
		}
	}

	private void OnIngameStatusUpdated(INGAME_STATUS status, bool server)
	{
		if (!server && (bool)timerText)
		{
			SetTimer(status == INGAME_STATUS.PLAYING);
		}
	}

	private void OnWorldTimerUpdate(uint time, bool server)
	{
		if (server || !timerText)
		{
			return;
		}
		string text = TimeUtils.SecondsToTime(time).Replace(" ", ":");
		timerText.text = ((time <= 30) ? ("<shake a=0.7>" + text + "</shake>") : text);
		_timerHit?.Stop();
		_timerHit = util_fade_timer.Fade(12f, 8f, 0f, delegate(float f)
		{
			if ((bool)timerText)
			{
				timerText.transform.localPosition = new Vector3(0f, 0f - f, 0f);
			}
		});
	}

	private void SetTimer(bool enable)
	{
		if (!timerText)
		{
			throw new UnityException("Missing timer text");
		}
		timerText.transform.parent.gameObject.SetActive(enable);
		_timerHit?.Stop();
	}

	private void OnSpectateUpdate(entity_player spectate)
	{
		aliveHUD.SetActive(!spectate);
		deadHUD.SetActive(spectate);
		if ((bool)spectate)
		{
			spectateName.text = ((spectate == PlayerController.LOCAL) ? "" : spectate.GetPlayerName());
		}
	}
}
