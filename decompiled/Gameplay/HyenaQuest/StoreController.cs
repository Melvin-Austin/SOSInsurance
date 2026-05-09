using System;
using System.Collections;
using System.Collections.Generic;
using FailCake;
using Unity.Netcode;
using UnityEngine;
using ZLinq;
using ZLinq.Linq;

namespace HyenaQuest;

[RequireComponent(typeof(NetworkObject))]
[DisallowMultipleComponent]
[DefaultExecutionOrder(-70)]
public class StoreController : NetController<StoreController>
{
	public entity_door storeDoor;

	public entity_door flagDoor;

	public GameObject storeMusic;

	public entity_monster_eyes storeEyes;

	public List<entity_store_selector> selectors = new List<entity_store_selector>();

	public entity_movement_networked fakeItem;

	public GameObject itemTubeTemplate;

	public Transform itemSpawnPosition;

	private readonly List<StoreItem> _items = new List<StoreItem>();

	private readonly HashSet<StoreItem> _boughtThisGame = new HashSet<StoreItem>();

	private readonly Queue<StoreItem> _processingItems = new Queue<StoreItem>();

	private StoreItem _currentProcessingItem;

	private util_timer _pipeTimer;

	private readonly NetworkList<byte> _storeItems = new NetworkList<byte>(new byte[6] { 255, 255, 255, 255, 255, 255 });

	private bool _wasOpen;

	public new void Awake()
	{
		base.Awake();
		if (!storeDoor)
		{
			throw new UnityException("Missing storeDoor");
		}
		if (!flagDoor)
		{
			throw new UnityException("Missing flagDoor");
		}
		storeDoor.OnDoorUpdate += new Action<bool>(OnDoorUpdate);
		if (!storeMusic)
		{
			throw new UnityException("Missing storeMusic");
		}
		storeMusic.SetActive(value: false);
		if (!storeEyes)
		{
			throw new UnityException("Missing storeEyes");
		}
		if (!fakeItem)
		{
			throw new UnityException("Missing fakeItem");
		}
		if (!itemTubeTemplate)
		{
			throw new UnityException("Missing itemTubeTemplate");
		}
		if (!itemSpawnPosition)
		{
			throw new UnityException("Missing itemSpawnPosition");
		}
		StoreItem[] array = Resources.LoadAll<StoreItem>("Store");
		if (array.Length == 0)
		{
			Debug.LogWarning("No StoreItems found in Resources/Store folder");
		}
		using ValueEnumerator<ArrayWhere<StoreItem>, StoreItem> valueEnumerator = (from item in array.AsValueEnumerable()
			where item
			select item).GetEnumerator<ArrayWhere<StoreItem>, StoreItem>();
		while (valueEnumerator.MoveNext())
		{
			StoreItem current = valueEnumerator.Current;
			if ((bool)current)
			{
				_items.Add(current);
			}
		}
	}

	public new void OnDestroy()
	{
		if ((bool)storeDoor)
		{
			storeDoor.OnDoorUpdate -= new Action<bool>(OnDoorUpdate);
		}
		base.OnDestroy();
	}

	public override void OnNetworkSpawn()
	{
		base.OnNetworkSpawn();
		if (base.IsServer)
		{
			CoreController.WaitFor(delegate(IngameController ingameCtrl)
			{
				ingameCtrl.OnStatusUpdated += new Action<INGAME_STATUS, bool>(OnStatusUpdated);
			});
		}
	}

	public override void OnNetworkDespawn()
	{
		base.OnNetworkDespawn();
		if (base.IsServer)
		{
			_pipeTimer?.Stop();
			fakeItem.StopMovement();
			if ((bool)NetController<IngameController>.Instance)
			{
				NetController<IngameController>.Instance.OnStatusUpdated -= new Action<INGAME_STATUS, bool>(OnStatusUpdated);
			}
		}
	}

	public List<StoreItem> GetStoreItems()
	{
		return _items;
	}

	protected override void OnNetworkPostSpawn()
	{
		base.OnNetworkPostSpawn();
		if (base.IsClient)
		{
			_storeItems.OnListChanged += OnListChanged;
			OnListChanged(default(NetworkListEvent<byte>));
		}
	}

	public override void OnNetworkPreDespawn()
	{
		base.OnNetworkPreDespawn();
		if (base.IsClient)
		{
			_storeItems.OnListChanged -= OnListChanged;
		}
	}

	[Rpc(SendTo.Server)]
	public void RequestBuyItemRPC(byte storeIndex)
	{
		NetworkManager networkManager = base.NetworkManager;
		if ((object)networkManager == null || !networkManager.IsListening)
		{
			Debug.LogError("Rpc methods can only be invoked after starting the NetworkManager!");
			return;
		}
		if (__rpc_exec_stage != __RpcExecStage.Execute)
		{
			RpcAttribute.RpcAttributeParams attributeParams = default(RpcAttribute.RpcAttributeParams);
			RpcParams rpcParams = default(RpcParams);
			FastBufferWriter bufferWriter = __beginSendRpc(2906273970u, rpcParams, attributeParams, SendTo.Server, RpcDelivery.Reliable);
			bufferWriter.WriteValueSafe(in storeIndex, default(FastBufferWriter.ForPrimitives));
			__endSendRpc(ref bufferWriter, 2906273970u, rpcParams, attributeParams, SendTo.Server, RpcDelivery.Reliable);
		}
		if (__rpc_exec_stage == __RpcExecStage.Execute)
		{
			__rpc_exec_stage = __RpcExecStage.Send;
			RequestBuyItem(storeIndex);
		}
	}

	public Dictionary<string, StoreItem> GetStoreItemLookup()
	{
		Dictionary<string, StoreItem> dictionary = new Dictionary<string, StoreItem>();
		foreach (StoreItem item in _items)
		{
			if ((bool)item.itemPrefab && item.itemPrefab.TryGetComponent<entity_item>(out var component))
			{
				dictionary.TryAdd(component.GetID(), item);
			}
		}
		return dictionary;
	}

	[Server]
	public void SpawnItems(List<SaveDataItems> items)
	{
		if (!base.IsServer)
		{
			throw new UnityException("Server only");
		}
		Dictionary<string, StoreItem> storeItemLookup = GetStoreItemLookup();
		foreach (SaveDataItems item in items)
		{
			if (string.IsNullOrEmpty(item.id) || Guid.TryParse(item.id, out var _) || !storeItemLookup.TryGetValue(item.id, out var value))
			{
				continue;
			}
			GameObject gameObject = UnityEngine.Object.Instantiate(value.itemPrefab, item.position, item.rotation);
			if (!gameObject)
			{
				continue;
			}
			if (!gameObject.TryGetComponent<NetworkObject>(out var component))
			{
				UnityEngine.Object.Destroy(gameObject);
				continue;
			}
			component.Spawn(destroyWithScene: true);
			if (item.data != null && gameObject.TryGetComponent<entity_item>(out var component2))
			{
				component2.Load(item.data);
			}
		}
	}

	[Server]
	private void RequestBuyItem(byte storeIndex)
	{
		if (!base.IsServer)
		{
			throw new UnityException("Server only");
		}
		if (storeIndex == byte.MaxValue)
		{
			throw new UnityException("Invalid item, should be locked");
		}
		if (!NetController<CurrencyController>.Instance)
		{
			throw new UnityException("Missing CurrencyController");
		}
		if (!NetController<IngameController>.Instance)
		{
			throw new UnityException("Missing IngameController");
		}
		if (NetController<IngameController>.Instance.Status() != 0)
		{
			return;
		}
		byte b = _storeItems[storeIndex];
		if (b == byte.MaxValue)
		{
			return;
		}
		StoreItem storeItem = _items[b];
		if (!storeItem)
		{
			return;
		}
		if (storeItem.itemPrice > 0 && !NetController<CurrencyController>.Instance.Pay(storeItem.itemPrice))
		{
			OnItemFailRPC();
			return;
		}
		NetController<SoundController>.Instance?.Play3DSound($"Ingame/Store/buy_{UnityEngine.Random.Range(0, 2)}.ogg", storeMusic.transform, new AudioData
		{
			pitch = UnityEngine.Random.Range(0.85f, 1.2f),
			distance = 6f
		}, broadcast: true);
		_processingItems.Enqueue(storeItem);
		_storeItems.Set(storeIndex, byte.MaxValue, forceUpdate: true);
		_boughtThisGame.Add(storeItem);
		if (_boughtThisGame.Count >= _items.Count)
		{
			NetController<StatsController>.Instance?.UnlockAchievementSV(STEAM_ACHIEVEMENTS.ACHIEVEMENT_STAN, ulong.MaxValue);
		}
		SpawnRequestedItems();
		OnItemBoughtRPC();
	}

	private void SpawnRequestedItems()
	{
		if ((bool)_currentProcessingItem || _processingItems.Count == 0)
		{
			return;
		}
		_currentProcessingItem = _processingItems.Dequeue();
		_pipeTimer?.Stop();
		_pipeTimer = util_timer.Simple(1f, delegate
		{
			NetController<SoundController>.Instance?.Play3DSound("Ingame/Store/pipe_move.ogg", storeMusic.transform, new AudioData
			{
				pitch = UnityEngine.Random.Range(0.85f, 1.2f),
				distance = 6f,
				volume = 0.35f
			}, broadcast: true);
			fakeItem.StartMovement(reset: true, delegate
			{
				if (!_currentProcessingItem)
				{
					throw new UnityException("Invalid processing item");
				}
				NetController<SoundController>.Instance?.Play3DSound("Ingame/Store/store_fwomp.ogg", itemSpawnPosition.transform, new AudioData
				{
					pitch = UnityEngine.Random.Range(0.85f, 1.2f),
					distance = 6f
				}, broadcast: true);
				StartCoroutine(CreateItem(_currentProcessingItem.itemPrefab));
			});
		});
	}

	[Rpc(SendTo.ClientsAndHost)]
	private void OnItemBoughtRPC()
	{
		NetworkManager networkManager = base.NetworkManager;
		if ((object)networkManager == null || !networkManager.IsListening)
		{
			Debug.LogError("Rpc methods can only be invoked after starting the NetworkManager!");
			return;
		}
		if (__rpc_exec_stage != __RpcExecStage.Execute)
		{
			RpcAttribute.RpcAttributeParams attributeParams = default(RpcAttribute.RpcAttributeParams);
			RpcParams rpcParams = default(RpcParams);
			FastBufferWriter bufferWriter = __beginSendRpc(4028782343u, rpcParams, attributeParams, SendTo.ClientsAndHost, RpcDelivery.Reliable);
			__endSendRpc(ref bufferWriter, 4028782343u, rpcParams, attributeParams, SendTo.ClientsAndHost, RpcDelivery.Reliable);
		}
		if (__rpc_exec_stage == __RpcExecStage.Execute)
		{
			__rpc_exec_stage = __RpcExecStage.Send;
			if ((bool)storeEyes)
			{
				storeEyes.Agree();
				NetController<SoundController>.Instance?.Play3DSound("Ingame/Entities/Tv/138115__snakebarney__tv-off-short.ogg", storeMusic.transform, new AudioData
				{
					pitch = UnityEngine.Random.Range(0.85f, 1.2f),
					distance = 4f
				});
				NetController<SoundController>.Instance?.Play3DSound($"Ingame/Store/voice/voice_{UnityEngine.Random.Range(0, 4)}.ogg", storeMusic.transform, new AudioData
				{
					volume = 0.3f,
					distance = 4f
				});
			}
		}
	}

	[Rpc(SendTo.ClientsAndHost)]
	private void OnItemFailRPC()
	{
		NetworkManager networkManager = base.NetworkManager;
		if ((object)networkManager == null || !networkManager.IsListening)
		{
			Debug.LogError("Rpc methods can only be invoked after starting the NetworkManager!");
			return;
		}
		if (__rpc_exec_stage != __RpcExecStage.Execute)
		{
			RpcAttribute.RpcAttributeParams attributeParams = default(RpcAttribute.RpcAttributeParams);
			RpcParams rpcParams = default(RpcParams);
			FastBufferWriter bufferWriter = __beginSendRpc(4202277632u, rpcParams, attributeParams, SendTo.ClientsAndHost, RpcDelivery.Reliable);
			__endSendRpc(ref bufferWriter, 4202277632u, rpcParams, attributeParams, SendTo.ClientsAndHost, RpcDelivery.Reliable);
		}
		if (__rpc_exec_stage == __RpcExecStage.Execute)
		{
			__rpc_exec_stage = __RpcExecStage.Send;
			if ((bool)storeEyes)
			{
				storeEyes.Disagree();
				NetController<SoundController>.Instance?.Play3DSound("Ingame/Entities/Terminal/142608__autistic-lucario__error.ogg", storeMusic.transform, new AudioData
				{
					pitch = UnityEngine.Random.Range(0.85f, 1.2f),
					distance = 4f,
					volume = 0.5f
				});
				NetController<SoundController>.Instance?.Play3DSound($"Ingame/Store/voice/no_credits_{UnityEngine.Random.Range(0, 4)}.ogg", storeMusic.transform, new AudioData
				{
					volume = 0.3f,
					distance = 4f
				});
			}
		}
	}

	private void OnDoorUpdate(bool open)
	{
		if (open)
		{
			storeMusic.SetActive(value: true);
		}
	}

	[Server]
	public void ClearStore()
	{
		if (!base.IsServer)
		{
			throw new UnityException("Server only");
		}
		for (byte b = 0; b < selectors.Count; b++)
		{
			_storeItems.Set(b, byte.MaxValue, b == selectors.Count - 1);
		}
	}

	[Server]
	private void GenerateStoreItems()
	{
		if (!base.IsServer)
		{
			throw new UnityException("Server only");
		}
		if (!NetController<IngameController>.Instance)
		{
			throw new UnityException("Missing IngameController");
		}
		byte currentRound = NetController<IngameController>.Instance.GetCurrentRound();
		int playerCount = NetController<IngameController>.Instance.GetConnectedPlayers();
		Dictionary<string, byte> spawnedItems = new Dictionary<string, byte>();
		entity_item[] array = UnityEngine.Object.FindObjectsByType<entity_item>(FindObjectsInactive.Exclude);
		if (array != null)
		{
			entity_item[] array2 = array;
			foreach (entity_item entity_item2 in array2)
			{
				if ((bool)entity_item2)
				{
					if (!spawnedItems.ContainsKey(entity_item2.GetID()))
					{
						spawnedItems[entity_item2.GetID()] = 1;
					}
					else
					{
						spawnedItems[entity_item2.GetID()]++;
					}
				}
			}
		}
		List<StoreItem> list = _items.AsValueEnumerable().Where(delegate(StoreItem item)
		{
			if (item.minRounds > currentRound || playerCount < item.minPlayers)
			{
				return false;
			}
			byte value;
			return string.IsNullOrEmpty(item.limit.itemID) || !spawnedItems.TryGetValue(item.limit.itemID, out value) || value < item.limit.limit;
		}).ToList();
		if (list.Count == 0)
		{
			throw new UnityException($"Failed to gather any items for round {currentRound}");
		}
		List<StoreItem> list2 = new List<StoreItem>(list);
		int num = Mathf.Min(UnityEngine.Random.Range(4, selectors.Count + 1), list2.Count);
		byte b = 0;
		for (int j = 0; j < num; j++)
		{
			if (list2.Count <= 0)
			{
				break;
			}
			int index = WeightedRandom(list2);
			_storeItems.Set(b++, (byte)_items.IndexOf(list2[index]));
			list2.RemoveAt(index);
		}
		for (int k = b; k < selectors.Count; k++)
		{
			_storeItems.Set(k, byte.MaxValue, k == selectors.Count - 1);
		}
	}

	private int WeightedRandom(List<StoreItem> items)
	{
		float num = 0f;
		for (int i = 0; i < items.Count; i++)
		{
			num += Mathf.Max(items[i].priority, 0.01f);
		}
		float num2 = UnityEngine.Random.Range(0f, num);
		for (int j = 0; j < items.Count; j++)
		{
			num2 -= Mathf.Max(items[j].priority, 0.01f);
			if (num2 <= 0f)
			{
				return j;
			}
		}
		return items.Count - 1;
	}

	[Server]
	private IEnumerator CreateItem(GameObject itemTemplate)
	{
		if (!itemTemplate)
		{
			throw new UnityException("itemTemplate is null");
		}
		AsyncInstantiateOperation<GameObject> instantiateOperation = UnityEngine.Object.InstantiateAsync(itemTubeTemplate, itemSpawnPosition.position, itemSpawnPosition.rotation);
		yield return instantiateOperation;
		GameObject[] result = instantiateOperation.Result;
		GameObject obj = ((result != null) ? result[0] : null);
		if (!obj)
		{
			throw new UnityException("Failed to instantiate prop");
		}
		entity_store_item component = obj.GetComponent<entity_store_item>();
		if (!component)
		{
			throw new UnityException("entity_store_item not found on itemTubeTemplate");
		}
		NetworkObject component2 = obj.GetComponent<NetworkObject>();
		if (!component2)
		{
			throw new UnityException("NetworkObject not found on itemTubeTemplate");
		}
		component.itemPrefab = itemTemplate;
		component2.Spawn(destroyWithScene: true);
		yield return new WaitForFixedUpdate();
		_currentProcessingItem = null;
		SpawnRequestedItems();
	}

	[Shared]
	private void OnListChanged(NetworkListEvent<byte> changeEvent)
	{
		bool isOpen = false;
		for (byte b = 0; b < selectors.Count; b++)
		{
			if ((bool)selectors[b])
			{
				byte b2 = ((b < _storeItems.Count) ? _storeItems[b] : byte.MaxValue);
				StoreItem storeItem = ((b2 == byte.MaxValue) ? null : _items[b2]);
				selectors[b].SetItem(storeItem, b);
				if ((bool)storeItem)
				{
					isOpen = true;
				}
			}
		}
		OnStoreStatusUpdate(isOpen);
	}

	private void OnStoreStatusUpdate(bool isOpen)
	{
		if (isOpen != _wasOpen)
		{
			_wasOpen = isOpen;
			if (isOpen)
			{
				NetController<SoundController>.Instance?.Play3DSound("Ingame/Store/opening.ogg", storeMusic.transform, new AudioData
				{
					pitch = UnityEngine.Random.Range(0.85f, 1.2f),
					distance = 6f,
					volume = 0.4f
				});
			}
			else
			{
				storeMusic.SetActive(value: false);
			}
			storeDoor.SetOpen(isOpen);
			flagDoor.SetOpen(isOpen);
		}
	}

	private void OnStatusUpdated(INGAME_STATUS status, bool server)
	{
		if (!server)
		{
			return;
		}
		switch (status)
		{
		case INGAME_STATUS.IDLE:
			if (NetController<IngameController>.Instance.OldStatus() == INGAME_STATUS.ROUND_END)
			{
				GenerateStoreItems();
			}
			break;
		case INGAME_STATUS.GAMEOVER:
			_boughtThisGame.Clear();
			ClearStore();
			break;
		default:
			ClearStore();
			break;
		}
	}

	protected override void __initializeVariables()
	{
		if (_storeItems == null)
		{
			throw new Exception("StoreController._storeItems cannot be null. All NetworkVariableBase instances must be initialized.");
		}
		_storeItems.Initialize(this);
		__nameNetworkVariable(_storeItems, "_storeItems");
		NetworkVariableFields.Add(_storeItems);
		base.__initializeVariables();
	}

	protected override void __initializeRpcs()
	{
		__registerRpc(2906273970u, __rpc_handler_2906273970, "RequestBuyItemRPC", RpcInvokePermission.Everyone);
		__registerRpc(4028782343u, __rpc_handler_4028782343, "OnItemBoughtRPC", RpcInvokePermission.Everyone);
		__registerRpc(4202277632u, __rpc_handler_4202277632, "OnItemFailRPC", RpcInvokePermission.Everyone);
		base.__initializeRpcs();
	}

	private static void __rpc_handler_2906273970(NetworkBehaviour target, FastBufferReader reader, __RpcParams rpcParams)
	{
		NetworkManager networkManager = target.NetworkManager;
		if ((object)networkManager != null && networkManager.IsListening)
		{
			reader.ReadValueSafe(out byte value, default(FastBufferWriter.ForPrimitives));
			target.__rpc_exec_stage = __RpcExecStage.Execute;
			((StoreController)target).RequestBuyItemRPC(value);
			target.__rpc_exec_stage = __RpcExecStage.Send;
		}
	}

	private static void __rpc_handler_4028782343(NetworkBehaviour target, FastBufferReader reader, __RpcParams rpcParams)
	{
		NetworkManager networkManager = target.NetworkManager;
		if ((object)networkManager != null && networkManager.IsListening)
		{
			target.__rpc_exec_stage = __RpcExecStage.Execute;
			((StoreController)target).OnItemBoughtRPC();
			target.__rpc_exec_stage = __RpcExecStage.Send;
		}
	}

	private static void __rpc_handler_4202277632(NetworkBehaviour target, FastBufferReader reader, __RpcParams rpcParams)
	{
		NetworkManager networkManager = target.NetworkManager;
		if ((object)networkManager != null && networkManager.IsListening)
		{
			target.__rpc_exec_stage = __RpcExecStage.Execute;
			((StoreController)target).OnItemFailRPC();
			target.__rpc_exec_stage = __RpcExecStage.Send;
		}
	}

	protected internal override string __getTypeName()
	{
		return "StoreController";
	}
}
