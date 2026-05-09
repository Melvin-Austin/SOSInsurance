using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FailCake;
using TMPro;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;
using ZLinq;

namespace HyenaQuest;

[DisallowMultipleComponent]
[RequireComponent(typeof(NetworkObject))]
[DefaultExecutionOrder(-70)]
public class ContractController : NetController<ContractController>
{
	public static readonly List<ContractModifiers> ALL_POSSIBLE_MODIFIERS = (from ContractModifiers m in Enum.GetValues(typeof(ContractModifiers))
		where m != ContractModifiers.NONE
		select m).ToList();

	public static readonly int BASE_DEBT = 130;

	public static readonly byte MAX_TASKS = 4;

	public static readonly float MAX_TASK_REWARD_RATIO = 0.3f;

	private static readonly int MAX_MODIFIERS_ON_CONTRACT = 3;

	private static readonly string[] COMPANY_NAME_PREFIXES = new string[100]
	{
		"GiggleCorp", "Spotted", "WhoopCo.", "Cackle", "Spotty", "Striped", "Laughing Hyena", "SnickerSnout", "ChuckleCheek", "BoneCrusher",
		"Matriarch", "Clanwise", "NightHowl", "Scavenger", "HyenaPack", "LaughterLine", "ManeTail", "AlphaSpots", "HyenaHustle", "CarrionCrew",
		"Beans", "Paws", "SwiftTail", "MoonRunner", "DustPaw", "Sunstripe", "ShadowFur", "FleetFoot", "QuickMuzzle", "BrightEyes",
		"SilentStep", "SharpTooth", "GoldenMane", "WildTrack", "DuneWalker", "RiverHowl", "EchoPack", "FeralLine", "StoneJaw", "RedSpot",
		"GreyBack", "WindChaser", "StarSnout", "PatchFur", "BoldBite", "GrassRunner", "NightProwl", "ThunderLaugh", "FrostEar", "SandStrider",
		"BrambleHide", "CopperTail", "IronClaw", "BlazeFur", "MirthMuzzle", "RidgeRunner", "TwilightClan", "Dappled", "Mirthful", "Rascal",
		"Rover", "FleetClan", "Howlstone", "Banter", "Gleam", "Rumble", "Tumble", "Dart", "Pounce", "Flicker",
		"Guffaw", "Snort", "Waggle", "Prance", "Jester", "Riff", "Raff", "Bark", "Yip", "Snip",
		"Snap", "Nibble", "Nib", "Titter", "Chortle", "Grin", "Wink", "Wheeze", "Snicker", "Glee",
		"Jape", "Jive", "Jolt", "Jinx", "Jumble", "Juggle", "Jabber", "Jolt", "JoltClan", "JoltPack"
	};

	private static readonly string[] COMPANY_NAME_SUFFIXES = new string[88]
	{
		"Logistics", "Inc.", "Deliveries", "Ltd.", "Services", "Haulage", "Transport", "Enterprises", "Scavengers", "Pack Services",
		"Couriers", "Clan", "Boneworks", "Express", "Freight", "Solutions", "Distribution", "Movers", "Shippers", "Carriers",
		"Fleet", "Group", "Partners", "Associates", "Network", "Systems", "Works", "Squad", "Crew", "Collective",
		"Outfit", "Union", "Syndicate", "Consortium", "Alliance", "Guild", "Company", "Transports", "Expedite", "Rapid",
		"Dispatch", "Relay", "Run", "Way", "Route", "Trackers", "Trail", "Pathways", "Conveyors", "Deliverers",
		"Mission", "Ops", "Force", "Division", "Sector", "Branch", "Wing", "Unit", "Division", "Hub",
		"Depot", "Base", "Point", "Node", "Terminal", "Port", "Yard", "Bay", "Dock", "Station",
		"Hangar", "Garage", "Vault", "Den", "Nest", "Burrow", "Lair", "Hideout", "Refuge", "Sanctuary",
		"Haven", "Retreat", "Camp", "Outpost", "Post", "HQ", "HQs", "HQ Inc."
	};

	public TextMeshPro infoText;

	public TextMeshPro deliveryInfo;

	public GameObject GAS_MODIFIER;

	public GameObject ICE_MODIFIER;

	public GameObject STATIC_MODIFIER;

	public GameEvent<Contract, bool> OnContractUpdate = new GameEvent<Contract, bool>();

	public GameEvent<bool> OnTasksUpdated = new GameEvent<bool>();

	private readonly HashSet<int> _usedAddresses = new HashSet<int>();

	private int _taskID;

	private int _worldGeneratedScrap;

	private float _roundWorldScrapRequired;

	private bool _hasSpeedDial;

	private util_timer _horrorTimer;

	private util_timer _flashTimer;

	private readonly NetVar<Contract> _generatedContract = new NetVar<Contract>();

	private readonly NetworkList<Task> _tasks = new NetworkList<Task>();

	private readonly NetVar<bool> _horrorMode = new NetVar<bool>(value: false);

	public new void Awake()
	{
		base.Awake();
		if (!infoText)
		{
			throw new UnityException("Missing TextMeshProUGUI component for infoText");
		}
		if (!deliveryInfo)
		{
			throw new UnityException("Missing TextMeshProUGUI component for deliveryInfo");
		}
		if (!GAS_MODIFIER)
		{
			throw new UnityException("Missing GAS_MODIFIER GameObject");
		}
		GAS_MODIFIER.SetActive(value: false);
		if (!ICE_MODIFIER)
		{
			throw new UnityException("Missing ICE_MODIFIER GameObject");
		}
		ICE_MODIFIER.SetActive(value: false);
		if (!STATIC_MODIFIER)
		{
			throw new UnityException("Missing STATIC_MODIFIER GameObject");
		}
		STATIC_MODIFIER.SetActive(value: false);
	}

	public override void OnNetworkSpawn()
	{
		base.OnNetworkSpawn();
		CoreController.WaitFor(delegate(IngameController ingCtrl)
		{
			ingCtrl.OnStatusUpdated += new Action<INGAME_STATUS, bool>(OnIngameStatusUpdated);
			if (base.IsServer)
			{
				ingCtrl.OnRoundUpdate += new Action<byte, bool>(OnRoundUpdate);
				OnRoundUpdate(ingCtrl.GetCurrentRound(), server: true);
			}
		});
		CoreController.WaitFor(delegate(MapController mapCtrl)
		{
			mapCtrl.OnMapGenerated += new Action<bool>(OnMapGenerated);
		});
	}

	public override void OnNetworkDespawn()
	{
		base.OnNetworkDespawn();
		if ((bool)NetController<IngameController>.Instance)
		{
			NetController<IngameController>.Instance.OnStatusUpdated -= new Action<INGAME_STATUS, bool>(OnIngameStatusUpdated);
			if (base.IsServer)
			{
				NetController<IngameController>.Instance.OnRoundUpdate -= new Action<byte, bool>(OnRoundUpdate);
			}
		}
		if ((bool)NetController<MapController>.Instance)
		{
			NetController<MapController>.Instance.OnMapGenerated -= new Action<bool>(OnMapGenerated);
		}
		MonoController<LocalizationController>.Instance?.Cleanup("ContractController.status.text");
		_horrorTimer?.Stop();
		_flashTimer?.Stop();
	}

	protected override void OnNetworkPostSpawn()
	{
		base.OnNetworkPostSpawn();
		if (!base.IsClient)
		{
			return;
		}
		_generatedContract.RegisterOnValueChanged(delegate
		{
			UpdateInfoText();
			OnContractUpdate?.Invoke(_generatedContract.Value, param2: false);
		});
		_horrorMode.RegisterOnValueChanged(delegate(bool _, bool newVal)
		{
			if ((bool)NetController<MapController>.Instance && newVal)
			{
				NetController<MapController>.Instance.StopAmbientMusic();
				_horrorTimer?.Stop();
				_horrorTimer = util_timer.Simple(UnityEngine.Random.Range(2, 4), delegate
				{
					NetController<MapController>.Instance.SwitchAmbientToHorror();
				});
			}
		});
		_tasks.OnListChanged += OnTaskListUpdate;
		UpdateDeliveryText();
	}

	public override void OnNetworkPreDespawn()
	{
		base.OnNetworkPreDespawn();
		if (base.IsClient)
		{
			_generatedContract.OnValueChanged = null;
			_horrorMode.OnValueChanged = null;
			_tasks.OnListChanged -= OnTaskListUpdate;
		}
	}

	public Task? FindTaskByID(int taskID)
	{
		for (int i = 0; i < _tasks.Count; i++)
		{
			if (_tasks[i].ID == taskID)
			{
				return _tasks[i];
			}
		}
		return null;
	}

	public int GetTaskIndex(Task task)
	{
		int result = -1;
		for (int i = 0; i < _tasks.Count; i++)
		{
			if (_tasks[i].ID == task.ID)
			{
				return i;
			}
		}
		return result;
	}

	[Server]
	public bool CompleteTask(int taskID, TaskBonus bonus)
	{
		if (!base.IsServer)
		{
			throw new UnityException("Server only");
		}
		Task? task = FindTaskByID(taskID);
		if (!task.HasValue)
		{
			return false;
		}
		return CompleteTask(task.Value, bonus);
	}

	[Server]
	public bool CompleteTask(Task task, TaskBonus bonus)
	{
		if (!base.IsServer)
		{
			throw new UnityException("Server only");
		}
		if (NetController<IngameController>.Instance.Status() != INGAME_STATUS.PLAYING)
		{
			return false;
		}
		int num = NetController<CurrencyController>.Instance?.PayDebt(task.Reward, bonus) ?? 0;
		string text = GetBonusMessage(bonus) + "%##%\n";
		if (num != task.Reward)
		{
			text += $"<color=#777><s><rotate=-90>€</rotate>{task.Reward}</s></color>";
			text += $" <rotate=-90>€</rotate>{num}";
		}
		else
		{
			text += $"<rotate=-90>€</rotate>{num}";
		}
		NetController<NotificationController>.Instance?.BroadcastAllRPC(new NotificationData
		{
			id = $"complete-task-{task.ID}",
			text = text,
			duration = 8f,
			soundEffect = "Ingame/Notifications/success-0.ogg",
			detailedIndex = task.DeliveryPrefabIndex
		});
		UpdateTask(task, failed: false);
		return true;
	}

	[Server]
	public bool FailTask(int taskID)
	{
		if (!base.IsServer)
		{
			throw new UnityException("Server only");
		}
		Task? task = FindTaskByID(taskID);
		if (task.HasValue)
		{
			return FailTask(task.Value);
		}
		return false;
	}

	[Server]
	public bool FailTask(Task task)
	{
		if (!base.IsServer)
		{
			throw new UnityException("Server only");
		}
		if (!MonoController<LocalizationController>.Instance)
		{
			throw new UnityException("Missing LocalizationController");
		}
		IngameController instance = NetController<IngameController>.Instance;
		if ((object)instance != null && instance.Status() == INGAME_STATUS.PLAYING)
		{
			NetController<NotificationController>.Instance?.BroadcastAllRPC(new NotificationData
			{
				id = $"failed-task-{task.ID}",
				text = "ingame.ui.notification.delivery-failed",
				duration = 8f,
				soundEffect = "Ingame/Notifications/notice-1.ogg"
			});
		}
		UpdateTask(task, failed: true);
		return true;
	}

	public Contract GetPickedContract()
	{
		return _generatedContract.Value;
	}

	public List<Task> GetAffordableTasks(int totalScrap, bool sort = true)
	{
		List<Task> list = new List<Task>();
		foreach (Task task in _tasks)
		{
			if (!task.HasDeliveryItem && totalScrap >= task.ScrapRequired)
			{
				list.Add(task);
			}
		}
		if (sort)
		{
			list.Sort((Task a, Task b) => a.ScrapRequired.CompareTo(b.ScrapRequired));
		}
		return list;
	}

	public NetworkList<Task> GetTasks()
	{
		return _tasks;
	}

	[Server]
	public void GenerateRoundTasks()
	{
		if (!base.IsServer)
		{
			throw new UnityException("Server only");
		}
		ClearRoundTasks();
		if (!NetController<IngameController>.Instance)
		{
			throw new UnityException("Missing IngameController");
		}
		if (!NetController<DeliveryController>.Instance)
		{
			throw new UnityException("Missing DeliveryController");
		}
		if (!NetController<CurrencyController>.Instance)
		{
			throw new UnityException("Missing CurrencyController");
		}
		if (!NetController<MapController>.Instance)
		{
			throw new UnityException("Missing MapController");
		}
		if (!NetController<MapController>.Instance.IsGenerated())
		{
			throw new UnityException("World is not generated!");
		}
		int currentRound = NetController<IngameController>.Instance.GetCurrentRound();
		_worldGeneratedScrap = NetController<ScrapController>.Instance.GetWorldScrap();
		_roundWorldScrapRequired = NETController.Instance.GetConnectedSteamIDs().Count switch
		{
			1 => 0.5f, 
			2 => 0.55f, 
			3 => 0.65f, 
			_ => 0.68f, 
		};
		int num = Mathf.FloorToInt((float)_worldGeneratedScrap * _roundWorldScrapRequired);
		float num2 = (float)(currentRound - 1) * 75f;
		float num3 = 1f;
		int a = Mathf.RoundToInt(((float)BASE_DEBT + num2) * num3);
		int b = Mathf.FloorToInt((float)num * 0.75f);
		int initialDebt = Mathf.Min(a, b);
		NetController<CurrencyController>.Instance.SetInitialDebt(initialDebt);
		GenerateDeliveryTasks(MAX_TASKS);
	}

	[Server]
	private float GetDistanceMultiplier(int address)
	{
		if (!base.IsServer)
		{
			throw new UnityException("Server only");
		}
		entity_delivery_spot deliverySpotByAddress = NetController<DeliveryController>.Instance.GetDeliverySpotByAddress(address);
		if (!deliverySpotByAddress)
		{
			return 1f;
		}
		entity_room_base entity_room_base2 = null;
		HashSet<entity_room> spawnedRooms = NetController<MapController>.Instance.GetSpawnedRooms();
		if (spawnedRooms != null && spawnedRooms.Count > 0)
		{
			Vector3 spotPos = deliverySpotByAddress.transform.position;
			entity_room_base2 = (from r in spawnedRooms.AsValueEnumerable()
				orderby Vector3.SqrMagnitude(r.transform.position - spotPos)
				select r).First();
		}
		if (!entity_room_base2)
		{
			return 1f;
		}
		int num = CalculateRoomDistance(entity_room_base2);
		return Mathf.Clamp(0.75f + (float)num * 0.15f, 0.75f, 1.5f);
	}

	[Server]
	private int CalculateRoomDistance(entity_room_base targetRoom)
	{
		if (!base.IsServer)
		{
			throw new UnityException("Server only");
		}
		if (!NetController<MapController>.Instance)
		{
			throw new UnityException("Missing MapController");
		}
		entity_room startRoom = NetController<MapController>.Instance.GetStartRoom();
		if (!startRoom || startRoom == targetRoom)
		{
			return 0;
		}
		Queue<(entity_room_base, int)> queue = new Queue<(entity_room_base, int)>();
		HashSet<entity_room_base> hashSet = new HashSet<entity_room_base>();
		queue.Enqueue((startRoom, 0));
		hashSet.Add(startRoom);
		while (queue.Count > 0)
		{
			var (entity_room_base2, num) = queue.Dequeue();
			if (entity_room_base2 == targetRoom)
			{
				return num;
			}
			foreach (entity_room_base adjacentRoom in entity_room_base2.GetAdjacentRooms())
			{
				if ((bool)adjacentRoom && hashSet.Add(adjacentRoom))
				{
					queue.Enqueue((adjacentRoom, num + 1));
				}
			}
		}
		float num2 = Vector3.Distance(startRoom.transform.position, targetRoom.transform.position);
		float num3 = 20f;
		return Mathf.Max(1, Mathf.RoundToInt(num2 / num3));
	}

	[Server]
	private void GenerateDeliveryTasks(byte count)
	{
		if (!base.IsServer)
		{
			throw new UnityException("Server only");
		}
		if (!NetController<CurrencyController>.Instance)
		{
			throw new UnityException("Missing CurrencyController");
		}
		if (!NetController<ScrapController>.Instance)
		{
			throw new UnityException("Missing ScrapController");
		}
		if (!NetController<DeliveryController>.Instance)
		{
			throw new UnityException("Missing DeliveryController");
		}
		if (!NetController<MapController>.Instance)
		{
			throw new UnityException("Missing MapController");
		}
		int initialDebt = NetController<CurrencyController>.Instance.GetInitialDebt();
		int currentRound = NetController<IngameController>.Instance.GetCurrentRound();
		float num = 1f;
		float num2 = (float)_worldGeneratedScrap * _roundWorldScrapRequired / (float)initialDebt / num;
		HashSet<int> addresses = NetController<DeliveryController>.Instance.GetAddresses();
		if (addresses.Count == 0)
		{
			throw new UnityException("No delivery addresses available! Failed to generate map?");
		}
		float num3 = 1f / Mathf.Sqrt(1f + (float)(currentRound - 1) * 0.15f);
		float num4 = 1f;
		float num5 = num3 * num4;
		float minInclusive = 0.18f * num5;
		float maxInclusive = 0.24f * num5;
		float num6 = (float)_worldGeneratedScrap * _roundWorldScrapRequired;
		List<Task> list = new List<Task>();
		List<int> list2 = new List<int>();
		for (int i = 0; i < count; i++)
		{
			List<int> list3 = addresses.AsValueEnumerable().Except(_usedAddresses).Except(list2)
				.ToList();
			if (list3.Count == 0)
			{
				break;
			}
			int num7 = list3[UnityEngine.Random.Range(0, list3.Count)];
			list2.Add(num7);
			float distanceMultiplier = GetDistanceMultiplier(num7);
			UnityEngine.Random.State state = UnityEngine.Random.state;
			float num8 = UnityEngine.Random.Range(minInclusive, maxInclusive);
			float num9 = UnityEngine.Random.Range(0.9f, 1.1f);
			UnityEngine.Random.state = state;
			int num10 = Mathf.RoundToInt((float)Mathf.RoundToInt((float)initialDebt * num8) * distanceMultiplier);
			num10 = Mathf.CeilToInt((float)num10 / 5f) * 5;
			num10 = Mathf.Min(num10, Mathf.RoundToInt((float)initialDebt * MAX_TASK_REWARD_RATIO));
			float num11 = num2 * num9;
			float num12 = Mathf.Lerp(1.15f, 0.65f, Mathf.InverseLerp(0.75f, 1.5f, distanceMultiplier));
			int b = Mathf.CeilToInt((float)num10 * num11 * num12);
			b = Mathf.Max(Mathf.CeilToInt((float)num10 * 0.45f), b);
			Task task = default(Task);
			task.ID = _taskID++;
			task.Reward = num10;
			task.DeliveryPrefabIndex = (byte)UnityEngine.Random.Range(0, NetController<DeliveryController>.Instance.propPrefabs.Count);
			task.Address = num7;
			task.ScrapRequired = b;
			Task item = task;
			list.Add(item);
		}
		if (list.Count == 0)
		{
			throw new UnityException("No tasks generated!!");
		}
		int num13 = list.AsValueEnumerable().Sum((Task t) => t.Reward);
		int num14 = list.AsValueEnumerable().Sum((Task t) => t.ScrapRequired);
		if (count == MAX_TASKS && num13 < initialDebt)
		{
			float b2 = 1.33f / (1f + (float)Mathf.Max(0, currentRound - 2) * 0.18f);
			b2 = Mathf.Max(0.5f, b2);
			float num15 = (float)initialDebt * b2 / (float)num13;
			for (int j = 0; j < list.Count; j++)
			{
				Task value = list[j];
				value.Reward = Mathf.CeilToInt((float)value.Reward * num15 / 5f) * 5;
				list[j] = value;
			}
		}
		int num16 = 0;
		for (int k = 0; k < _tasks.Count; k++)
		{
			if (!_tasks[k].HasDeliveryItem)
			{
				num16 += _tasks[k].ScrapRequired;
			}
		}
		if ((float)(num16 + num14) > num6)
		{
			float b3 = (num6 * 0.95f - (float)num16) / (float)num14;
			b3 = Mathf.Max(0.5f, b3);
			for (int l = 0; l < list.Count; l++)
			{
				Task value2 = list[l];
				int b4 = Mathf.CeilToInt((float)value2.ScrapRequired * b3);
				int a = Mathf.CeilToInt((float)value2.Reward * 0.6f);
				value2.ScrapRequired = Mathf.Max(a, b4);
				list[l] = value2;
			}
		}
		for (int m = 0; m < list.Count; m++)
		{
			_usedAddresses.Add(list[m].Address);
			RegisterTask(list[m]);
		}
	}

	[Client]
	private void OnTaskListUpdate(NetworkListEvent<Task> changeEvent)
	{
		OnTasksUpdated?.Invoke(param1: false);
		UpdateDeliveryText();
	}

	[Client]
	private void UpdateDeliveryText()
	{
		if (!base.IsClient)
		{
			throw new UnityException("Client only");
		}
		if (!deliveryInfo)
		{
			throw new UnityException("Missing TextMeshProUGUI component for deliveryInfo");
		}
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.Append("<size=140%>");
		foreach (Task task in _tasks)
		{
			if (!task.HasDeliveryItem)
			{
				string text = $" <voffset=-7>{task.Address}</voffset>";
				string text2 = $"<mark=#ffaa0011> <rotate=-90>€</rotate> {task.Reward:N0} </mark> | {task.ScrapRequired:N0} SCP";
				stringBuilder.AppendLine(text + "<line-height=0>");
				stringBuilder.AppendLine("<align=\"right\"><size=100%>" + text2 + "</size></align></line-height>\n");
			}
		}
		stringBuilder.AppendLine("</size>");
		deliveryInfo.text = stringBuilder.ToString();
	}

	[Server]
	private string GenerateUniqueCompanyName()
	{
		string obj = COMPANY_NAME_PREFIXES[UnityEngine.Random.Range(0, COMPANY_NAME_PREFIXES.Length)];
		string text = COMPANY_NAME_SUFFIXES[UnityEngine.Random.Range(0, COMPANY_NAME_SUFFIXES.Length)];
		return (obj + " " + text).Trim();
	}

	[Server]
	private ContractModifiers DetermineContractModifiers(int currentRound)
	{
		if (currentRound <= 4 || !ALL_POSSIBLE_MODIFIERS.Any())
		{
			return ContractModifiers.NONE;
		}
		ContractModifiers contractModifiers = ContractModifiers.NONE;
		List<ContractModifiers> list = new List<ContractModifiers>(ALL_POSSIBLE_MODIFIERS);
		float num = 0.2f * (float)(currentRound - 2) + 0.25f;
		if (UnityEngine.Random.value < num && list.Any())
		{
			int index = UnityEngine.Random.Range(0, list.Count);
			ContractModifiers contractModifiers2 = list[index];
			contractModifiers |= contractModifiers2;
			list.RemoveAt(index);
			if (IsModifierInRange(contractModifiers2, 10, 19))
			{
				list.RemoveAll((ContractModifiers m) => IsModifierInRange(m, 10, 19));
			}
			if (list.Any() && CountSetFlags(contractModifiers) < MAX_MODIFIERS_ON_CONTRACT)
			{
				float num2 = 0.1f * (float)(currentRound - 2) + 0.15f;
				if (UnityEngine.Random.value < num2)
				{
					index = UnityEngine.Random.Range(0, list.Count);
					contractModifiers |= list[index];
				}
			}
		}
		return contractModifiers;
	}

	private bool IsModifierInRange(ContractModifiers modifier, int minBit, int maxBit)
	{
		if (modifier == (ContractModifiers)0)
		{
			return false;
		}
		int i;
		for (i = 0; ((uint)modifier & (uint)(1 << i)) == 0; i++)
		{
		}
		if (i >= minBit)
		{
			return i <= maxBit;
		}
		return false;
	}

	[Server]
	public void GenerateNextContract(ContractModifiers overrideModifiers = ContractModifiers.NONE)
	{
		if (!base.IsServer)
		{
			throw new UnityException("Server only");
		}
		ClearRoundTasks();
		int currentRound = NetController<IngameController>.Instance.GetCurrentRound();
		_generatedContract.SetSpawnValue(new Contract
		{
			modifiers = ((overrideModifiers != ContractModifiers.NONE) ? overrideModifiers : ((currentRound == 1) ? ContractModifiers.NONE : DetermineContractModifiers(currentRound))),
			name = new FixedString128Bytes(GenerateUniqueCompanyName())
		});
		OnContractUpdate?.Invoke(_generatedContract.Value, param2: true);
	}

	[Client]
	private void UpdateInfoText()
	{
		if (!base.IsClient || !infoText)
		{
			return;
		}
		if (!NetController<IngameController>.Instance)
		{
			throw new UnityException("Missing IngameController");
		}
		if (!MonoController<LocalizationController>.Instance)
		{
			throw new UnityException("Missing LocalizationController");
		}
		if (NetController<IngameController>.Instance.Status() != 0)
		{
			MonoController<LocalizationController>.Instance.Get("ContractController.status.text", "ingame.contract.signed", delegate(string v)
			{
				StringBuilder stringBuilder = new StringBuilder();
				stringBuilder.AppendLine("<align=\"right\"><size=120%>" + _generatedContract.Value.name.ToString() + "</size></align>");
				stringBuilder.AppendLine("\u00af\u00af\u00af\u00af\u00af\u00af\u00af\u00af\u00af\u00af\u00af\u00af\u00af\u00af\u00af\u00af\u00af\u00af\u00af\u00af\u00af\u00af\u00af\u00af\u00af\u00af\u00af\u00af\u00af\u00af\u00af\u00af\u00af\u00af\u00af\u00af\u00af\u00af\u00af\u00af");
				stringBuilder.AppendLine(v);
				stringBuilder.AppendLine();
				stringBuilder.AppendLine();
				string[] array = MonoController<LocalizationController>.Instance.Get("ingame.contract.generated.management")?.Split(new string[1] { "<##>" }, StringSplitOptions.None);
				if (array != null)
				{
					stringBuilder.AppendLine(array[UnityEngine.Random.Range(0, array.Length)].Trim());
				}
				stringBuilder.AppendLine();
				stringBuilder.AppendLine("<align=\"right\"> " + MonoController<LocalizationController>.Instance.Get("ingame.contract.signed.author") + ", \n██████ ████████ </align>");
				infoText.text = stringBuilder.ToString();
			});
			return;
		}
		MonoController<LocalizationController>.Instance.Get("ContractController.status.text", "ingame.contract.generated.main-title", delegate(string v)
		{
			StringBuilder stringBuilder2 = new StringBuilder();
			stringBuilder2.AppendLine("<align=\"right\"><size=120%>" + v + "</size></align>");
			stringBuilder2.AppendLine("\u00af\u00af\u00af\u00af\u00af\u00af\u00af\u00af\u00af\u00af\u00af\u00af\u00af\u00af\u00af\u00af\u00af\u00af\u00af\u00af\u00af\u00af\u00af\u00af\u00af\u00af\u00af\u00af\u00af\u00af\u00af\u00af\u00af\u00af\u00af\u00af\u00af\u00af\u00af\u00af");
			stringBuilder2.AppendLine(_generatedContract.Value.name.ToString() ?? "");
			stringBuilder2.AppendLine("└ " + MonoController<LocalizationController>.Instance.Get("ingame.contract.generated.request-type"));
			stringBuilder2.AppendLine();
			stringBuilder2.AppendLine();
			string[] array2 = MonoController<LocalizationController>.Instance.Get("ingame.contract.generated.title")?.Split(new string[1] { "<##>" }, StringSplitOptions.None);
			if (array2 != null)
			{
				stringBuilder2.AppendLine(array2[UnityEngine.Random.Range(0, array2.Length)].Trim());
			}
			array2 = MonoController<LocalizationController>.Instance.Get("ingame.contract.generated.subtitle")?.Split(new string[1] { "<##>" }, StringSplitOptions.None);
			if (array2 != null)
			{
				stringBuilder2.AppendLine(array2[UnityEngine.Random.Range(0, array2.Length)].Trim());
			}
			stringBuilder2.AppendLine();
			stringBuilder2.AppendLine("<align=\"center\">");
			stringBuilder2.AppendLine(MonoController<LocalizationController>.Instance.Get("ingame.contract.start"));
			stringBuilder2.AppendLine("</align>");
			infoText.text = stringBuilder2.ToString();
		});
	}

	private int CountSetFlags(ContractModifiers modifiers)
	{
		if (modifiers == ContractModifiers.NONE)
		{
			return 0;
		}
		int num = 0;
		foreach (ContractModifiers value in Enum.GetValues(typeof(ContractModifiers)))
		{
			if (value != ContractModifiers.NONE && (int)(object)value != 0 && (modifiers & value) == value)
			{
				num++;
			}
		}
		return num;
	}

	public int GetDangerLevel()
	{
		return CountSetFlags(_generatedContract.Value.modifiers);
	}

	private string GetBonusMessage(TaskBonus bonus)
	{
		return bonus switch
		{
			TaskBonus.HALF => "ingame.ui.notification.task-completed.half-bonus", 
			TaskBonus.FULL => "ingame.ui.notification.task-completed.full-bonus", 
			_ => "ingame.ui.notification.task-completed.no-bonus", 
		};
	}

	[Server]
	private void UpdateTask(Task task, bool failed)
	{
		if (!base.IsServer)
		{
			throw new UnityException("Server only");
		}
		_usedAddresses.Remove(task.Address);
		UnregisterTask(task);
		GenerateDeliveryTasks(1);
	}

	[Server]
	private void ClearRoundTasks()
	{
		if (!base.IsServer)
		{
			throw new UnityException("Server only");
		}
		for (int num = _tasks.Count - 1; num >= 0; num--)
		{
			UnregisterTask(_tasks[num]);
		}
		_tasks.Clear();
		_taskID = 0;
		_usedAddresses.Clear();
	}

	private void OnRoundUpdate(byte round, bool server)
	{
		if (server)
		{
			GenerateNextContract();
		}
	}

	private void OnMapGenerated(bool server)
	{
		if (!server)
		{
			Contract pickedContract = GetPickedContract();
			WorldSettings worldSettings = NetController<MapController>.Instance?.GetGeneratedWorld();
			if (!worldSettings)
			{
				throw new UnityException("Generated world is null");
			}
			GAS_MODIFIER.SetActive(worldSettings.modifiers.HasFlag(ContractModifiers.TOXIC_GAS_WORLD) && pickedContract.modifiers.HasFlag(ContractModifiers.TOXIC_GAS_WORLD));
			ICE_MODIFIER.SetActive(worldSettings.modifiers.HasFlag(ContractModifiers.ICE_WORLD) && pickedContract.modifiers.HasFlag(ContractModifiers.ICE_WORLD));
			STATIC_MODIFIER.SetActive(worldSettings.modifiers.HasFlag(ContractModifiers.DELIVERY_MALFUNCTION) && pickedContract.modifiers.HasFlag(ContractModifiers.DELIVERY_MALFUNCTION));
		}
	}

	private void OnIngameStatusUpdated(INGAME_STATUS newStatus, bool server)
	{
		_horrorTimer?.Stop();
		_flashTimer?.Stop();
		switch (newStatus)
		{
		case INGAME_STATUS.IDLE:
		case INGAME_STATUS.GAMEOVER:
			if (server)
			{
				ClearRoundTasks();
				break;
			}
			GAS_MODIFIER.SetActive(value: false);
			ICE_MODIFIER.SetActive(value: false);
			STATIC_MODIFIER.SetActive(value: false);
			UpdateInfoText();
			break;
		case INGAME_STATUS.GENERATE:
			if (!server)
			{
				UpdateInfoText();
			}
			break;
		case INGAME_STATUS.PLAYING:
		{
			if (!server)
			{
				break;
			}
			WorldSettings obj = NetController<MapController>.Instance?.GetGeneratedWorld();
			if (!obj)
			{
				throw new UnityException("Generated world is null");
			}
			Contract pickedContract = GetPickedContract();
			if (obj.modifiers.HasFlag(ContractModifiers.DARKNESS_WORLD) && pickedContract.modifiers.HasFlag(ContractModifiers.DARKNESS_WORLD))
			{
				_horrorTimer?.Stop();
				_horrorTimer = util_timer.Simple(UnityEngine.Random.Range(8, 14), delegate
				{
					SetHorrorMode(on: true);
				});
			}
			break;
		}
		case INGAME_STATUS.ROUND_END:
			if (server)
			{
				SetHorrorMode(on: false);
			}
			break;
		case INGAME_STATUS.WAITING_PLAY_CONFIRMATION:
			break;
		}
	}

	public bool IsHorrorMode()
	{
		return _horrorMode.Value;
	}

	private void SetHorrorMode(bool on)
	{
		_horrorMode.Value = on;
		NetController<PowerController>.Instance.SetPoweredArea(PowerGrid.MAP, !on);
		List<entity_player> plys = MonoController<PlayerController>.Instance.GetAllPlayers();
		plys.Shuffle();
		_flashTimer?.Stop();
		if (on)
		{
			_flashTimer = util_timer.Create(plys.Count, UnityEngine.Random.Range(0.2f, 1.2f), delegate(int i)
			{
				_flashTimer.SetDelay(UnityEngine.Random.Range(0.2f, 1.2f));
				if ((bool)plys[i])
				{
					plys[i].SetFlashlight(on: true);
				}
			});
			return;
		}
		foreach (entity_player item in plys)
		{
			if ((bool)item)
			{
				item.SetFlashlight(on: false);
			}
		}
	}

	[Server]
	private void RegisterTask(Task task)
	{
		if (!base.IsServer)
		{
			throw new UnityException("Server only");
		}
		_tasks.Add(task);
		if (!NetController<PhoneController>.Instance)
		{
			throw new UnityException("Missing PhoneController");
		}
		NetController<PhoneController>.Instance.Register(task.Address.ToString(), delegate
		{
			if (!NetController<DeliveryController>.Instance)
			{
				throw new UnityException("Missing DeliveryController");
			}
			if (!NetController<ScrapController>.Instance)
			{
				throw new UnityException("Missing ScrapController");
			}
			if (!MonoController<LocalizationController>.Instance)
			{
				throw new UnityException("Missing LocalizationController");
			}
			if (task.HasDeliveryItem)
			{
				return new List<string> { "ingame.phone.status.invalid" };
			}
			if (!NetController<ScrapController>.Instance.Pay(task.ScrapRequired))
			{
				return new List<string> { "ingame.world.delivery-maker.no-scrap" };
			}
			NetController<DeliveryController>.Instance.CreateDelivery(task);
			task.HasDeliveryItem = true;
			int taskIndex = GetTaskIndex(task);
			if (taskIndex != -1)
			{
				_tasks[taskIndex] = task;
			}
			return new List<string> { "ingame.world.delivery-maker.creating" };
		});
		OnTasksUpdated?.Invoke(param1: true);
	}

	[Server]
	private void UnregisterTask(Task task)
	{
		if (!base.IsServer)
		{
			throw new UnityException("Server only");
		}
		for (int num = _tasks.Count - 1; num >= 0; num--)
		{
			if (_tasks[num].ID == task.ID)
			{
				_tasks.RemoveAt(num);
				break;
			}
		}
		NetController<PhoneController>.Instance?.Unregister(task.Address.ToString());
		OnTasksUpdated?.Invoke(param1: true);
	}

	protected override void __initializeVariables()
	{
		if (_generatedContract == null)
		{
			throw new Exception("ContractController._generatedContract cannot be null. All NetworkVariableBase instances must be initialized.");
		}
		_generatedContract.Initialize(this);
		__nameNetworkVariable(_generatedContract, "_generatedContract");
		NetworkVariableFields.Add(_generatedContract);
		if (_tasks == null)
		{
			throw new Exception("ContractController._tasks cannot be null. All NetworkVariableBase instances must be initialized.");
		}
		_tasks.Initialize(this);
		__nameNetworkVariable(_tasks, "_tasks");
		NetworkVariableFields.Add(_tasks);
		if (_horrorMode == null)
		{
			throw new Exception("ContractController._horrorMode cannot be null. All NetworkVariableBase instances must be initialized.");
		}
		_horrorMode.Initialize(this);
		__nameNetworkVariable(_horrorMode, "_horrorMode");
		NetworkVariableFields.Add(_horrorMode);
		base.__initializeVariables();
	}

	protected override void __initializeRpcs()
	{
		base.__initializeRpcs();
	}

	protected internal override string __getTypeName()
	{
		return "ContractController";
	}
}
