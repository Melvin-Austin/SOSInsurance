using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using QFSW.QC;
using Steamworks;
using Unity.Mathematics;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

namespace HyenaQuest;

[DefaultExecutionOrder(1)]
[DisallowMultipleComponent]
public class ConsoleController : MonoController<ConsoleController>
{
	private LayerMask _groundLayer;

	public InputActionReference consoleAction;

	public static string WorldSeedOverride { get; private set; }

	public static string WorldIDOverride { get; private set; }

	[Command("server.kick", "Kicks the given player index, make sure you type the correct index by using server.players.list", Platform.AllPlatforms, MonoTargetType.Single)]
	[Server]
	private void ServerKickPlayer(byte targetId, string reason = "ingame.ui.disconnected.reason.kick")
	{
		if (!MonoController<PlayerController>.Instance)
		{
			throw new UnityException("Command available only ingame!");
		}
		if (!NetworkManager.Singleton || !NetworkManager.Singleton.IsHost)
		{
			throw new UnityException("Server only");
		}
		if (targetId == 0)
		{
			throw new UnityException("You cannot kick yourself");
		}
		Player player = MonoController<PlayerController>.Instance.GetPlayer(targetId);
		if (player == null)
		{
			throw new UnityException($"Player with id {targetId} not found");
		}
		NETController.Instance.DisconnectClientWithReason(player.connectionID, reason);
		UnityEngine.Debug.Log($"Kicked player {targetId} -> {reason}");
	}

	[Command("server.ban", "Bans the given player index, make sure you type the correct index by using server.players.list", Platform.AllPlatforms, MonoTargetType.Single)]
	[Server]
	private void ServerBanPlayer(byte targetId, string reason = "ingame.ui.disconnected.reason.ban")
	{
		if (!NETController.Instance || !NETController.Instance.IsHost)
		{
			throw new UnityException("Server only");
		}
		if (!MonoController<PlayerController>.Instance)
		{
			throw new UnityException("Command available only ingame!");
		}
		if (!MonoController<SettingsController>.Instance)
		{
			throw new UnityException("Missing SettingsController");
		}
		if (targetId == 0)
		{
			throw new UnityException("You cannot ban yourself");
		}
		Player player = MonoController<PlayerController>.Instance.GetPlayer(targetId);
		if (player == null)
		{
			throw new UnityException($"Player with id {targetId} not found");
		}
		CSteamID steamID = player.GetSteamID();
		if (!MonoController<SettingsController>.Instance.AddToBanList(steamID.m_SteamID, SteamFriends.GetFriendPersonaName(steamID)))
		{
			throw new UnityException("Already on ban list");
		}
		NETController.Instance.DisconnectClientWithReason(player.connectionID, reason);
		UnityEngine.Debug.Log($"Banned player {targetId} -> {reason}");
	}

	[Command("server.unban", "Unbans the given player index on the ban list. Use server.ban.list to see all the indexes and their steamids", Platform.AllPlatforms, MonoTargetType.Single)]
	[Server]
	private void ServerUnBanPlayer(int index)
	{
		if (!MonoController<SettingsController>.Instance)
		{
			throw new UnityException("Missing SettingsController");
		}
		if (!MonoController<SettingsController>.Instance.RemoveFromBanListByIndex(index))
		{
			throw new UnityException($"Index {index} not on the ban list");
		}
		UnityEngine.Debug.Log($"Unbanned player index {index}");
	}

	[Command("server.ban.list", "Displays the ban list", Platform.AllPlatforms, MonoTargetType.Single)]
	[Server]
	private void ServerBanList()
	{
		if (!MonoController<SettingsController>.Instance)
		{
			throw new UnityException("Missing SettingsController");
		}
		Dictionary<ulong, string> banList = MonoController<SettingsController>.Instance.GetBanList();
		if (banList == null || banList.Count == 0)
		{
			return;
		}
		int num = 0;
		foreach (KeyValuePair<ulong, string> item in banList)
		{
			UnityEngine.Debug.Log($"{num} -> {item.Value} : {item.Key}");
			num++;
		}
	}

	[Command("server.cheats", "Enables server cheats. This will disable achievements and stats until the game is restarted!", Platform.AllPlatforms, MonoTargetType.Single)]
	[Server]
	private void SetServerCheats(bool enable)
	{
		if (!NetworkManager.Singleton || !NetworkManager.Singleton.IsHost)
		{
			throw new UnityException("Server only");
		}
		if (enable)
		{
			NETController.Instance?.SetCheats(enable: true);
			NetController<NotificationController>.Instance?.BroadcastAllRPC(new NotificationData
			{
				id = "cheats-enabled",
				text = "ingame.ui.notification.cheats-enabled",
				duration = 10f,
				soundEffect = "Ingame/Notifications/UI_Notification_Denied_01_stereo.ogg"
			});
		}
	}

	[Command("server.players.list", "Lists the connected players and their index", Platform.AllPlatforms, MonoTargetType.Single)]
	[Server]
	private void ListPlayers()
	{
		if (!MonoController<PlayerController>.Instance)
		{
			throw new UnityException("Command available only ingame!");
		}
		if (!NetworkManager.Singleton || !NetworkManager.Singleton.IsHost)
		{
			throw new UnityException("Server only");
		}
		Dictionary<byte, Player> players = MonoController<PlayerController>.Instance.players;
		if (players == null || players.Count == 0)
		{
			UnityEngine.Debug.Log("No players connected :(");
			return;
		}
		UnityEngine.Debug.Log("Connected players:");
		foreach (KeyValuePair<byte, Player> item in players)
		{
			if ((bool)item.Value.player)
			{
				byte iD = item.Value.GetID();
				UnityEngine.Debug.Log(string.Format("{0}Index: {1} -> Name: {2} -> SteamID: {3}", (iD == 0) ? "[HOST] " : "", iD, item.Value.player.GetPlayerName(), item.Value.steamID));
			}
		}
	}

	private bool CanCheat()
	{
		if (NETController.SV_CHEATS)
		{
			return true;
		}
		UnityEngine.Debug.LogWarning("Command requires `server.cheats` enabled. This will disable all stats and achievements tracking.");
		return false;
	}

	[Command("suicide", "Kills the player", Platform.AllPlatforms, MonoTargetType.Single)]
	[Server]
	private void Suicide()
	{
		if (!MonoController<PlayerController>.Instance)
		{
			throw new UnityException("Command available only ingame!");
		}
		if (!NetworkManager.Singleton || !NetworkManager.Singleton.IsClient)
		{
			throw new UnityException("Client only");
		}
		if (!NetController<IngameController>.Instance)
		{
			throw new UnityException("Command available only ingame!");
		}
		if (NetController<IngameController>.Instance.Status() != INGAME_STATUS.PLAYING)
		{
			throw new UnityException("Command only available while playing");
		}
		entity_player lOCAL = PlayerController.LOCAL;
		if (!lOCAL)
		{
			throw new UnityException("No local player found");
		}
		lOCAL.TakeHealth(byte.MaxValue, DamageType.NECK_SNAP);
	}

	[Command("player.kill", "Kills the player", Platform.AllPlatforms, MonoTargetType.Single)]
	[Server]
	private void KillPlayer(byte targetIndex)
	{
		if (!MonoController<PlayerController>.Instance)
		{
			throw new UnityException("Command available only ingame!");
		}
		if (!NetworkManager.Singleton || !NetworkManager.Singleton.IsHost)
		{
			throw new UnityException("Server only");
		}
		if (CanCheat())
		{
			(MonoController<PlayerController>.Instance.GetPlayer(targetIndex) ?? throw new UnityException($"Player with index {targetIndex} not found")).player.Kill(DamageType.NECK_SNAP);
		}
	}

	[Command("player.kill.all", "Kills all players", Platform.AllPlatforms, MonoTargetType.Single)]
	[Server]
	private void KillPlayers()
	{
		if (!MonoController<PlayerController>.Instance)
		{
			throw new UnityException("Command available only ingame!");
		}
		if (!NetworkManager.Singleton || !NetworkManager.Singleton.IsHost)
		{
			throw new UnityException("Server only");
		}
		if (!CanCheat())
		{
			return;
		}
		foreach (entity_player item in MonoController<PlayerController>.Instance.GetAlivePlayers() ?? throw new UnityException("No alive players"))
		{
			if ((bool)item)
			{
				item.Kill(DamageType.NECK_SNAP);
			}
		}
	}

	[Command("player.revive", "Revives the player", Platform.AllPlatforms, MonoTargetType.Single)]
	[Server]
	private void RevivePlayer(byte targetIndex)
	{
		if (!MonoController<PlayerController>.Instance)
		{
			throw new UnityException("Command available only ingame!");
		}
		if (!NetworkManager.Singleton || !NetworkManager.Singleton.IsHost)
		{
			throw new UnityException("Server only");
		}
		if (CanCheat())
		{
			(MonoController<PlayerController>.Instance.GetPlayer(targetIndex) ?? throw new UnityException($"Player with index {targetIndex} not found")).player.Revive();
		}
	}

	[Command("player.revive.all", "Revives all players", Platform.AllPlatforms, MonoTargetType.Single)]
	[Server]
	private void RevivePlayers()
	{
		if (!MonoController<PlayerController>.Instance)
		{
			throw new UnityException("Command available only ingame!");
		}
		if (!NetworkManager.Singleton || !NetworkManager.Singleton.IsHost)
		{
			throw new UnityException("Server only");
		}
		if (!CanCheat())
		{
			return;
		}
		foreach (entity_player item in MonoController<PlayerController>.Instance.GetDeadPlayers() ?? throw new UnityException("No dead players"))
		{
			if ((bool)item)
			{
				item.Revive();
			}
		}
	}

	[Command("player.teleport", "Teleports the player to another player", Platform.AllPlatforms, MonoTargetType.Single)]
	[Server]
	private void TeleportPlayer(byte index, byte targetIndex)
	{
		if (!MonoController<PlayerController>.Instance)
		{
			throw new UnityException("Command available only ingame!");
		}
		if (!NetworkManager.Singleton || !NetworkManager.Singleton.IsHost)
		{
			throw new UnityException("Server only");
		}
		if (CanCheat())
		{
			Player obj = MonoController<PlayerController>.Instance.GetPlayer(index) ?? throw new UnityException($"Player with index {index} not found");
			Player player = MonoController<PlayerController>.Instance.GetPlayer(targetIndex);
			if (player == null)
			{
				throw new UnityException($"Player with index {targetIndex} not found");
			}
			obj.player.SetPositionRPC(player.player.chest.transform.position + player.player.chest.transform.forward * 0.5f, quaternion.identity);
		}
	}

	[Command("player.health", "Set player's health", Platform.AllPlatforms, MonoTargetType.Single)]
	[Server]
	private void SetPlayerHealth(byte index, byte health)
	{
		if (!MonoController<PlayerController>.Instance)
		{
			throw new UnityException("Command available only ingame!");
		}
		if (!NetworkManager.Singleton || !NetworkManager.Singleton.IsHost)
		{
			throw new UnityException("Server only");
		}
		if (CanCheat())
		{
			(MonoController<PlayerController>.Instance.GetPlayer(index) ?? throw new UnityException($"Player with index {index} not found")).player.SetHealthRPC(health);
		}
	}

	[Command("steamworks.stats.reset", "Reset all stats and achievements", Platform.AllPlatforms, MonoTargetType.Single)]
	private void SteamworksResetAllStats(bool resetAchievements, bool sure)
	{
		if (!NetController<StatsController>.Instance)
		{
			throw new UnityException("Stats reset can only be called ingame");
		}
		if (!sure)
		{
			throw new UnityException("You need to be sure about this, add 'true' at the end of the command");
		}
		NetController<StatsController>.Instance.ResetAllStatsAndAchievements(resetAchievements);
	}

	[Command("game.start", "Starts the game (skipping the lever pull)", Platform.AllPlatforms, MonoTargetType.Single)]
	[Server]
	private void StartSkipPress()
	{
		if (!NetController<IngameController>.Instance)
		{
			throw new UnityException("Command available only ingame!");
		}
		if (!NetworkManager.Singleton || !NetworkManager.Singleton.IsHost)
		{
			throw new UnityException("Server only");
		}
		if (NetController<IngameController>.Instance.Status() != 0)
		{
			throw new UnityException("Cannot set the start the round is running");
		}
		NetController<IngameController>.Instance.SetGameStatus(INGAME_STATUS.GENERATE);
	}

	[Command("game.status", "Sets the game status (might break the game, do not use this)", Platform.AllPlatforms, MonoTargetType.Single)]
	[Server]
	private void SetGameStatus(INGAME_STATUS status)
	{
		if (!NetController<IngameController>.Instance)
		{
			throw new UnityException("Command available only ingame!");
		}
		if (!NetworkManager.Singleton || !NetworkManager.Singleton.IsHost)
		{
			throw new UnityException("Server only");
		}
		if (CanCheat())
		{
			NetController<IngameController>.Instance.SetGameStatus(status);
		}
	}

	[Command("world.round", "Sets the world round", Platform.AllPlatforms, MonoTargetType.Single)]
	[Server]
	private void SetWorldRound(byte round)
	{
		if (!NetController<IngameController>.Instance || !NetController<ContractController>.Instance)
		{
			throw new UnityException("Command available only ingame!");
		}
		if (!NetworkManager.Singleton || !NetworkManager.Singleton.IsHost)
		{
			throw new UnityException("Server only");
		}
		if (CanCheat())
		{
			NetController<IngameController>.Instance.SetRound(round);
		}
	}

	[Command("world.contract", "Force a specified world contract", Platform.AllPlatforms, MonoTargetType.Single)]
	[Server]
	private void ForceWorldContract(ContractModifiers modifiers = ContractModifiers.NONE)
	{
		if (!NetController<ContractController>.Instance)
		{
			throw new UnityException("Command available only ingame!");
		}
		if (!NetworkManager.Singleton || !NetworkManager.Singleton.IsHost)
		{
			throw new UnityException("Server only");
		}
		if (CanCheat())
		{
			NetController<ContractController>.Instance.GenerateNextContract(modifiers);
		}
	}

	[Command("world.time", "Add world time", Platform.AllPlatforms, MonoTargetType.Single)]
	[Server]
	private void AddWorldTime(int time)
	{
		if (!NetController<IngameController>.Instance)
		{
			throw new UnityException("Command available only ingame!");
		}
		if (!NetworkManager.Singleton || !NetworkManager.Singleton.IsHost)
		{
			throw new UnityException("Server only");
		}
		if (CanCheat())
		{
			NetController<IngameController>.Instance.AddTemporaryWorldTime(time);
		}
	}

	[Command("world.seed", "Force a world seed for the next map generation", Platform.AllPlatforms, MonoTargetType.Single)]
	[Server]
	private void ForceWorldSeed(string seed)
	{
		if (!NetController<MapController>.Instance)
		{
			throw new UnityException("Command available only ingame!");
		}
		if (!NetworkManager.Singleton || !NetworkManager.Singleton.IsHost)
		{
			throw new UnityException("Server only");
		}
		if (CanCheat())
		{
			WorldSeedOverride = seed;
			UnityEngine.Debug.Log("World seed set to: " + seed);
		}
	}

	[Command("world.type", "Force a world type for the next map generation", Platform.AllPlatforms, MonoTargetType.Single)]
	[Server]
	private void ForceWorldType(string id = null)
	{
		if (!NetController<MapController>.Instance)
		{
			throw new UnityException("Command available only ingame!");
		}
		if (!NetworkManager.Singleton || !NetworkManager.Singleton.IsHost)
		{
			throw new UnityException("Server only");
		}
		if (CanCheat())
		{
			if (!string.IsNullOrEmpty(id) && !NetController<MapController>.Instance.HasWorld(id))
			{
				throw new UnityException("Invalid world " + id + ", not found, run world.list to see all available world");
			}
			WorldIDOverride = id;
			UnityEngine.Debug.Log("World type set to: " + WorldIDOverride);
		}
	}

	[Command("world.list", "Force a world type for the next map generation", Platform.AllPlatforms, MonoTargetType.Single)]
	[Server]
	private string ListRegisteredWorlds()
	{
		if (!NetController<MapController>.Instance)
		{
			throw new UnityException("Command available only ingame!");
		}
		List<WorldSettings> worlds = NetController<MapController>.Instance.GetWorlds();
		StringBuilder stringBuilder = new StringBuilder();
		foreach (WorldSettings item in worlds)
		{
			if ((bool)item)
			{
				stringBuilder.AppendLine("-  " + item.name);
			}
		}
		return stringBuilder.ToString();
	}

	[Command("world.test.generate", "Generates the given world index without starting the game", Platform.AllPlatforms, MonoTargetType.Single)]
	[Server]
	private void GenerateWorld(string id)
	{
		if (!Application.isPlaying)
		{
			throw new UnityException("Cannot generate world while not ingame");
		}
		if (!NetworkManager.Singleton || !NetworkManager.Singleton.IsHost)
		{
			throw new UnityException("Server only");
		}
		if (!NetController<MapController>.Instance)
		{
			throw new UnityException("Missing MapController");
		}
		if (string.IsNullOrEmpty(id))
		{
			throw new UnityException("World id cannot be empty");
		}
		SetServerCheats(enable: true);
		StartCoroutine(NetController<MapController>.Instance.CleanupAndGenerate(id, WorldSeedOverride, delegate(EntryRenderSettings shell)
		{
			NetController<IngameController>.Instance.MapRenderingSettings(shell);
		}, delegate
		{
			GameObject obj = GameObject.Find("[EXIT-DOOR]");
			if (!obj)
			{
				throw new UnityException("Main door not found");
			}
			entity_door component = obj.GetComponent<entity_door>();
			if (!component)
			{
				throw new UnityException("Main door component not found");
			}
			component.SetOpen(newValue: true);
			GameObject gameObject = GameObject.Find("[CITY]");
			if ((bool)gameObject)
			{
				gameObject.SetActive(value: false);
			}
		}));
	}

	[Command("world.test.cleanup", "Cleans up the generated world", Platform.AllPlatforms, MonoTargetType.Single)]
	[Server]
	public void CleanWorld()
	{
		if (!Application.isPlaying)
		{
			throw new UnityException("Cannot cleanup world while not ingame");
		}
		if (!NetworkManager.Singleton || !NetworkManager.Singleton.IsHost)
		{
			throw new UnityException("Server only");
		}
		if (!NetController<MapController>.Instance)
		{
			throw new UnityException("Missing MapController");
		}
		if (CanCheat())
		{
			StartCoroutine(NetController<MapController>.Instance.Cleanup());
		}
	}

	[Command("delivery.spawn", "Force spawn a delivery", Platform.AllPlatforms, MonoTargetType.Single)]
	[Server]
	private void ForceWorldContract(byte index)
	{
		if (!NetController<IngameController>.Instance)
		{
			throw new UnityException("Command available only ingame!");
		}
		if (!NetworkManager.Singleton || !NetworkManager.Singleton.IsHost)
		{
			throw new UnityException("Server only");
		}
		if (CanCheat())
		{
			int address = 0;
			if (NetController<IngameController>.Instance.Status() == INGAME_STATUS.PLAYING)
			{
				HashSet<int> addresses = NetController<DeliveryController>.Instance.GetAddresses();
				address = addresses.ToList()[UnityEngine.Random.Range(0, addresses.Count)];
			}
			NetController<DeliveryController>.Instance.CreateDelivery(new Task
			{
				Address = address,
				DeliveryPrefabIndex = index,
				HasDeliveryItem = false,
				Reward = 0,
				ScrapRequired = 0
			});
		}
	}

	[Command("scrap.add", "Add scrap to the ship", Platform.AllPlatforms, MonoTargetType.Single)]
	[Server]
	public void AddScrap(int amount)
	{
		if (!NetworkManager.Singleton || !NetworkManager.Singleton.IsHost)
		{
			throw new UnityException("Server only");
		}
		if (!NetController<IngameController>.Instance)
		{
			throw new UnityException("Command available only ingame!");
		}
		if (CanCheat())
		{
			if (amount <= 0)
			{
				throw new UnityException("Amount must be greater than 0");
			}
			NetController<ScrapController>.Instance.Add(amount);
		}
	}

	[Command("scrap.containers.add", "Add scrap to all containers (DEBUG)", Platform.AllPlatforms, MonoTargetType.Single)]
	[Server]
	public void AddContainerScrap(int amount)
	{
		if (!NetworkManager.Singleton || !NetworkManager.Singleton.IsHost)
		{
			throw new UnityException("Server only");
		}
		if (!NetController<IngameController>.Instance)
		{
			throw new UnityException("Command available only ingame!");
		}
		if (!CanCheat())
		{
			return;
		}
		entity_item_vacuum[] array = UnityEngine.Object.FindObjectsByType<entity_item_vacuum>(FindObjectsInactive.Exclude);
		foreach (entity_item_vacuum entity_item_vacuum2 in array)
		{
			if ((bool)entity_item_vacuum2 && entity_item_vacuum2.IsSpawned)
			{
				entity_item_vacuum2.AddScrap(amount);
			}
		}
	}

	[Command("world.debt", "Sets the player debt", Platform.AllPlatforms, MonoTargetType.Single)]
	[Server]
	private void SetGameDebt(int debt)
	{
		if (!NetController<CurrencyController>.Instance)
		{
			throw new UnityException("Command available only ingame!");
		}
		if (!NetworkManager.Singleton || !NetworkManager.Singleton.IsHost)
		{
			throw new UnityException("Server only");
		}
		if (CanCheat())
		{
			NetController<CurrencyController>.Instance.SetDebt(debt);
		}
	}

	[Command("world.currency", "Sets the player currency", Platform.AllPlatforms, MonoTargetType.Single)]
	[Server]
	private void SetGameCurrency(int currency)
	{
		if (!NetController<CurrencyController>.Instance)
		{
			throw new UnityException("Command available only ingame!");
		}
		if (!NetworkManager.Singleton || !NetworkManager.Singleton.IsHost)
		{
			throw new UnityException("Server only");
		}
		if (CanCheat())
		{
			NetController<CurrencyController>.Instance.SetCurrency(currency);
		}
	}

	[Command("world.gameover", "Get caught by the police, force gameover", Platform.AllPlatforms, MonoTargetType.Single)]
	[Server]
	private void ForceGameOver()
	{
		if (!NetController<IngameController>.Instance)
		{
			throw new UnityException("Command available only ingame!");
		}
		if (!NetworkManager.Singleton || !NetworkManager.Singleton.IsHost)
		{
			throw new UnityException("Server only");
		}
		if (NetController<IngameController>.Instance.Status() != 0)
		{
			throw new UnityException("Cannot force gameover while the round is running");
		}
		NetController<IngameController>.Instance.TriggerGameOverScene();
	}

	[Command("mouse.camera.sensitivity", "Sets the camera sensitivity", Platform.AllPlatforms, MonoTargetType.Single)]
	public void SetCameraSensitivity(float sense)
	{
		if ((bool)MonoController<SettingsController>.Instance)
		{
			PlayerSettings currentSettings = MonoController<SettingsController>.Instance.CurrentSettings;
			currentSettings.mouseSensitivity = sense;
			MonoController<SettingsController>.Instance.CurrentSettings = currentSettings;
		}
	}

	[Command("mouse.phys.sensitivity", "Sets the physgun rotation sensitivity", Platform.AllPlatforms, MonoTargetType.Single)]
	public void SetPhysSensitivity(float sense)
	{
		if ((bool)MonoController<SettingsController>.Instance)
		{
			PlayerSettings currentSettings = MonoController<SettingsController>.Instance.CurrentSettings;
			currentSettings.physgunRotateSensitivity = sense;
			MonoController<SettingsController>.Instance.CurrentSettings = currentSettings;
		}
	}

	[Command("mouse.invertY", "Inverts the mouse Y", Platform.AllPlatforms, MonoTargetType.Single)]
	public void SetMouseInvertY(bool invert)
	{
		if ((bool)MonoController<SettingsController>.Instance)
		{
			PlayerSettings currentSettings = MonoController<SettingsController>.Instance.CurrentSettings;
			currentSettings.invertMouseY = invert;
			MonoController<SettingsController>.Instance.CurrentSettings = currentSettings;
		}
	}

	[Command("graphics.brightness", "Sets the game brightness (0 to 1)", Platform.AllPlatforms, MonoTargetType.Single)]
	public void SetGraphicsBrightness(float bright)
	{
		if ((bool)MonoController<SettingsController>.Instance)
		{
			PlayerSettings currentSettings = MonoController<SettingsController>.Instance.CurrentSettings;
			currentSettings.brightness = Mathf.Clamp01(bright);
			MonoController<SettingsController>.Instance.CurrentSettings = currentSettings;
		}
	}

	[Command("graphics.vsync", "Turn VSYNC on / off", Platform.AllPlatforms, MonoTargetType.Single)]
	public void SetGraphicsVSync(bool on)
	{
		if ((bool)MonoController<SettingsController>.Instance)
		{
			PlayerSettings currentSettings = MonoController<SettingsController>.Instance.CurrentSettings;
			currentSettings.vsyncEnabled = on;
			MonoController<SettingsController>.Instance.CurrentSettings = currentSettings;
		}
	}

	[Command("graphics.framerate", "Set the target framerate, only works if VSync is disabled", Platform.AllPlatforms, MonoTargetType.Single)]
	public void SetGraphicsFramerate(PlayerTargetFramerate framerate)
	{
		if ((bool)MonoController<SettingsController>.Instance)
		{
			PlayerSettings currentSettings = MonoController<SettingsController>.Instance.CurrentSettings;
			if (currentSettings.vsyncEnabled)
			{
				throw new UnityException("Cannot set framerate while VSync is enabled");
			}
			currentSettings.targetFrameRate = framerate;
			MonoController<SettingsController>.Instance.CurrentSettings = currentSettings;
		}
	}

	[Command("hud.hide", "Hide the ingame HUD. Useful for screenshots or videos", Platform.AllPlatforms, MonoTargetType.Single)]
	public void HideIngameHUD(bool hidden)
	{
		if (!NetController<IngameController>.Instance || !MonoController<UIController>.Instance)
		{
			throw new UnityException("Command available only ingame!");
		}
		if (NetController<IngameController>.Instance.Status() == INGAME_STATUS.GAMEOVER)
		{
			throw new UnityException("Cannot hide the HUD while the game is over");
		}
		if (NetController<IngameController>.Instance.Status() == INGAME_STATUS.ROUND_END)
		{
			throw new UnityException("Cannot hide the HUD while the game is ending");
		}
		MonoController<UIController>.Instance.HideHUD(hidden);
	}

	[Command("audio.master", "Sets the master audio volume", Platform.AllPlatforms, MonoTargetType.Single)]
	public void SetAudioMasterVolume(float volume)
	{
		if ((bool)MonoController<SettingsController>.Instance)
		{
			PlayerSettings currentSettings = MonoController<SettingsController>.Instance.CurrentSettings;
			currentSettings.masterVolume = Mathf.Clamp(volume, 0f, 1f);
			MonoController<SettingsController>.Instance.CurrentSettings = currentSettings;
		}
	}

	[Command("audio.voice", "Sets the voice audio volume", Platform.AllPlatforms, MonoTargetType.Single)]
	public void SetAudioVoiceVolume(float volume)
	{
		if ((bool)MonoController<SettingsController>.Instance)
		{
			PlayerSettings currentSettings = MonoController<SettingsController>.Instance.CurrentSettings;
			currentSettings.microphoneVolume = Mathf.Clamp(volume, 0f, 1f);
			MonoController<SettingsController>.Instance.CurrentSettings = currentSettings;
		}
	}

	[Command("audio.music", "Sets the music audio volume", Platform.AllPlatforms, MonoTargetType.Single)]
	public void SetAudioMusicVolume(float volume)
	{
		if ((bool)MonoController<SettingsController>.Instance)
		{
			PlayerSettings currentSettings = MonoController<SettingsController>.Instance.CurrentSettings;
			currentSettings.musicVolume = Mathf.Clamp(volume, 0f, 1f);
			MonoController<SettingsController>.Instance.CurrentSettings = currentSettings;
		}
	}

	[Command("audio.sfx", "Sets the sound effects audio volume", Platform.AllPlatforms, MonoTargetType.Single)]
	public void SetAudioSFXVolume(float volume)
	{
		if ((bool)MonoController<SettingsController>.Instance)
		{
			PlayerSettings currentSettings = MonoController<SettingsController>.Instance.CurrentSettings;
			currentSettings.sfxVolume = Mathf.Clamp(volume, 0f, 1f);
			MonoController<SettingsController>.Instance.CurrentSettings = currentSettings;
		}
	}

	[Command("voice.mode", "Set the voice activation mode", Platform.AllPlatforms, MonoTargetType.Single)]
	public void SetMicrophoneMode(VoiceChatMode mode)
	{
		if ((bool)MonoController<SettingsController>.Instance)
		{
			PlayerSettings currentSettings = MonoController<SettingsController>.Instance.CurrentSettings;
			currentSettings.microphoneMode = mode;
			MonoController<SettingsController>.Instance.CurrentSettings = currentSettings;
		}
	}

	[Command("voice.microphone.set", "Set the active microphone", Platform.AllPlatforms, MonoTargetType.Single)]
	public void SetMicrophoneIndex(int index)
	{
		if ((bool)MonoController<SettingsController>.Instance)
		{
			PlayerSettings currentSettings = MonoController<SettingsController>.Instance.CurrentSettings;
			currentSettings.microphoneDevice = index;
			MonoController<SettingsController>.Instance.CurrentSettings = currentSettings;
		}
	}

	[Command("voice.microphone.list", "Lists all available microphones", Platform.AllPlatforms, MonoTargetType.Single)]
	public void ListMicrophoneIndex()
	{
		List<string> microphones = OptionsController.GetMicrophones(nameFilter: true);
		if (microphones == null)
		{
			UnityEngine.Debug.Log("No microphones found");
			return;
		}
		for (int i = 0; i < microphones.Count; i++)
		{
			UnityEngine.Debug.Log($"Microphone {i}: {microphones[i]}");
		}
	}

	[Command("blood.disable", "Disables blood decals, even tough it's purple", Platform.AllPlatforms, MonoTargetType.Single)]
	public void DisableBloodDecal(bool disabled)
	{
		if (!MonoController<BloodController>.Instance)
		{
			throw new UnityException("Command only available while ingame");
		}
		BloodController.DISABLE_BLOOD = disabled;
	}

	[Command("item.spawn", "Spawns the given item id", Platform.AllPlatforms, MonoTargetType.Single)]
	[Server]
	private void SpawnItem(string itemID, int amount = 1)
	{
		if (amount <= 0)
		{
			throw new UnityException("Amount must be greater than 0");
		}
		if (!NetworkManager.Singleton || !NetworkManager.Singleton.IsHost)
		{
			throw new UnityException("Server only");
		}
		if (!CanCheat())
		{
			return;
		}
		Player host = MonoController<PlayerController>.Instance.GetHost();
		if (host == null)
		{
			throw new UnityException("Host player not found");
		}
		if (!NetController<StoreController>.Instance)
		{
			throw new UnityException("StoreController not found");
		}
		Dictionary<string, StoreItem> storeItemLookup = NetController<StoreController>.Instance.GetStoreItemLookup();
		if (storeItemLookup.Count == 0)
		{
			throw new UnityException("No items registered");
		}
		if (!storeItemLookup.TryGetValue(itemID, out var value))
		{
			throw new UnityException("Item with id " + itemID + " not found");
		}
		for (int i = 0; i < amount; i++)
		{
			GameObject obj = UnityEngine.Object.Instantiate(value.itemPrefab, host.player.transform.position + new Vector3(0f, 2f, 0f), Quaternion.identity);
			if (!obj)
			{
				throw new UnityException("Failed to spawn the item");
			}
			if (!obj.TryGetComponent<NetworkObject>(out var component))
			{
				throw new UnityException("Failed to spawn the item");
			}
			component.Spawn();
		}
	}

	[Command("upgrade.buy", "Buys the given upgrade ID", Platform.AllPlatforms, MonoTargetType.Single)]
	[Server]
	private void BuyUpgrade(string upgradeID, int amount = 1)
	{
		if (amount <= 0)
		{
			throw new UnityException("Amount must be greater than 0");
		}
		if (!NetworkManager.Singleton || !NetworkManager.Singleton.IsHost)
		{
			throw new UnityException("Server only");
		}
		if (CanCheat())
		{
			if (MonoController<PlayerController>.Instance.GetHost() == null)
			{
				throw new UnityException("Host player not found");
			}
			if (!NetController<UpgradeController>.Instance)
			{
				throw new UnityException("UpgradeController not found");
			}
			entity_ship_upgrade upgrade = NetController<UpgradeController>.Instance.GetUpgrade(upgradeID);
			if (!upgrade)
			{
				throw new UnityException("Upgrade " + upgradeID + " not found");
			}
			NetController<UpgradeController>.Instance.ActivateUpgrade(upgrade, isLoad: false, amount);
		}
	}

	[Command("monster.spawn", "Spawns the given enemy id on the mouse position", Platform.AllPlatforms, MonoTargetType.Single)]
	[Server]
	private void SpawnMonster(string monsterID)
	{
		if (!NetworkManager.Singleton || !NetworkManager.Singleton.IsHost)
		{
			throw new UnityException("Server only");
		}
		if (CanCheat())
		{
			GameObject monsterByID = NetController<MonsterController>.Instance.GetMonsterByID(monsterID);
			if (!monsterByID)
			{
				throw new UnityException("Monster " + monsterID + " not found");
			}
			if (!SDK.MainCamera)
			{
				throw new UnityException("Main camera not found");
			}
			if (!Physics.Raycast(SDK.MainCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f)), out var hitInfo, float.MaxValue, _groundLayer))
			{
				throw new UnityException("Failed to find a valid spawn point");
			}
			Vector3 point = hitInfo.point;
			NetController<MonsterController>.Instance.SpawnMonster(monsterByID, point);
			UnityEngine.Debug.Log("Monster " + monsterID + " spawned >:)");
		}
	}

	[Command("localization.set", "Sets the given localization", Platform.AllPlatforms, MonoTargetType.Single)]
	private void SetLocalization(LOCALE id)
	{
		if (!MonoController<LocalizationController>.Instance)
		{
			throw new UnityException("Missing LocalizationController");
		}
		MonoController<LocalizationController>.Instance.SetLanguage(id);
	}

	[Command("localization.list", "Lists available localizations", Platform.AllPlatforms, MonoTargetType.Single)]
	private void ListLocalization()
	{
		if (!MonoController<LocalizationController>.Instance)
		{
			throw new UnityException("Missing LocalizationController");
		}
		foreach (string language in MonoController<LocalizationController>.Instance.GetLanguages())
		{
			UnityEngine.Debug.Log(language);
		}
	}

	[Command("clear", "Clear console", Platform.AllPlatforms, MonoTargetType.Single)]
	private void ClearConsole()
	{
		QuantumConsole.Instance?.ClearConsole();
	}

	[Command("quit", "Quits the game", Platform.AllPlatforms, MonoTargetType.Single)]
	private void DebugQuit()
	{
		Application.Quit();
	}

	[Command("disconnect", "Disconnects from the server", Platform.AllPlatforms, MonoTargetType.Single)]
	private void DebugDisconnect()
	{
		if (!NETController.Instance || !NETController.Instance.IsConnectedClient)
		{
			throw new UnityException("Not connected to a server");
		}
		NETController.Instance.Disconnect();
	}

	[Command("log", "Opens the folder with the player.log files. Useful for sending the errors to the devs", Platform.AllPlatforms, MonoTargetType.Single)]
	private void DebugLog()
	{
		try
		{
			string persistentDataPath = Application.persistentDataPath;
			if (string.IsNullOrEmpty(persistentDataPath))
			{
				throw new UnityException("Failed to get the log file path");
			}
			UnityEngine.Debug.Log("Opening log folder: " + persistentDataPath);
			if (new Process
			{
				StartInfo = new ProcessStartInfo
				{
					FileName = "explorer.exe",
					Arguments = persistentDataPath,
					UseShellExecute = true
				}
			}.Start())
			{
				return;
			}
			UnityEngine.Debug.LogError("Failed to open log folder");
			throw new UnityException("Failed to open log folder");
		}
		catch (Exception ex)
		{
			UnityEngine.Debug.LogError("Error opening log folder: " + ex.Message);
			throw new UnityException("Failed to open log folder: " + ex.Message);
		}
	}

	public new void Awake()
	{
		base.Awake();
		if (!QuantumConsole.Instance)
		{
			throw new UnityException("QuantumConsole not found");
		}
		if (!consoleAction)
		{
			throw new UnityException("consoleAction not found");
		}
		QuantumConsole.Instance.OnActivate += OnConsoleActivate;
		QuantumConsole.Instance.OnDeactivate += OnConsoleDeactivate;
		consoleAction.action.performed += OnConsoleToggle;
		_groundLayer = LayerMask.GetMask("entity_ground");
		UnityEngine.Object.DontDestroyOnLoad(base.gameObject);
	}

	public new void OnDestroy()
	{
		if ((bool)QuantumConsole.Instance)
		{
			QuantumConsole.Instance.OnActivate -= OnConsoleActivate;
			QuantumConsole.Instance.OnDeactivate -= OnConsoleDeactivate;
		}
		if ((bool)consoleAction)
		{
			consoleAction.action.performed -= OnConsoleToggle;
		}
		base.OnDestroy();
	}

	private void OnConsoleToggle(InputAction.CallbackContext obj)
	{
		if ((bool)QuantumConsole.Instance)
		{
			QuantumConsole.Instance.Toggle();
		}
	}

	private void OnConsoleDeactivate()
	{
		if ((bool)MonoController<StartupController>.Instance)
		{
			MonoController<StartupController>.Instance.ReleaseCursor("CONSOLE");
		}
	}

	private void OnConsoleActivate()
	{
		if ((bool)MonoController<StartupController>.Instance)
		{
			MonoController<StartupController>.Instance.RequestCursor("CONSOLE");
		}
	}
}
