using System;
using System.Collections.Generic;
using FailCake;
using Unity.Netcode;
using UnityEngine;
using ZLinq;

namespace HyenaQuest;

[DisallowMultipleComponent]
[DefaultExecutionOrder(-70)]
[RequireComponent(typeof(NetworkObject))]
public class DeliveryController : NetController<DeliveryController>
{
	public static readonly float DELIVERY_MAKER_SPEED = 2f;

	public List<GameObject> propPrefabs;

	public entity_door deliveryDoor;

	public Transform deliverySpawnPoint;

	public entity_led hasEnoughScrapLED;

	private readonly List<entity_prop_delivery> _currentProps = new List<entity_prop_delivery>();

	private readonly List<entity_delivery_spot> _deliveryRegistry = new List<entity_delivery_spot>();

	private util_timer _spawnTimer;

	private readonly HashSet<int> _generatedAddresses = new HashSet<int>();

	private float _deliverySpeed = DELIVERY_MAKER_SPEED;

	private readonly NetVar<bool> _doorClosed = new NetVar<bool>(value: false);

	private readonly NetVar<bool> _hasDeliveryScrap = new NetVar<bool>(value: false);

	public new void Awake()
	{
		base.Awake();
		if (propPrefabs.Count == 0)
		{
			throw new UnityException("No prop prefabs set for DeliveryController");
		}
		if (!deliveryDoor)
		{
			throw new UnityException("Missing entity_door component for deliveryDoor");
		}
		if (!deliverySpawnPoint)
		{
			throw new UnityException("Missing Transform for deliverySpawnPoint");
		}
		if (!hasEnoughScrapLED)
		{
			throw new UnityException("Missing entity_led for hasEnoughScrapLED");
		}
		SDK.OnDeliverySpotRegister = RegisterDeliverySpot;
		SDK.OnDeliverySpotUnregister = UnregisterDeliverySpot;
	}

	public override void OnDestroy()
	{
		SDK.OnDeliverySpotRegister = null;
		SDK.OnDeliverySpotUnregister = null;
		base.OnDestroy();
	}

	public override void OnNetworkSpawn()
	{
		base.OnNetworkSpawn();
		if (base.IsServer)
		{
			CoreController.WaitFor(delegate(IngameController ingameCtrl)
			{
				ingameCtrl.OnStatusUpdated += new Action<INGAME_STATUS, bool>(OnIngameStatusUpdated);
			});
			CoreController.WaitFor(delegate(ScrapController scrpCtrl)
			{
				scrpCtrl.OnShipScrapUpdate += new Action<int, bool>(OnShipScrapUpdate);
			});
		}
	}

	public override void OnNetworkDespawn()
	{
		base.OnNetworkDespawn();
		if (base.IsServer)
		{
			_spawnTimer?.Stop();
			if ((bool)NetController<IngameController>.Instance)
			{
				NetController<IngameController>.Instance.OnStatusUpdated -= new Action<INGAME_STATUS, bool>(OnIngameStatusUpdated);
			}
			if ((bool)NetController<ScrapController>.Instance)
			{
				NetController<ScrapController>.Instance.OnShipScrapUpdate -= new Action<int, bool>(OnShipScrapUpdate);
			}
		}
	}

	protected override void OnNetworkPostSpawn()
	{
		base.OnNetworkPostSpawn();
		if (!base.IsClient)
		{
			return;
		}
		_doorClosed.RegisterOnValueChanged(delegate(bool oldValue, bool newValue)
		{
			if (oldValue != newValue)
			{
				if (!newValue)
				{
					NetController<EffectController>.Instance.PlayEffect(EffectType.SMOKE, deliverySpawnPoint.position, new EffectSettings
					{
						playSound = false
					});
				}
				deliveryDoor.SetOpen(!newValue);
			}
		});
		_hasDeliveryScrap.RegisterOnValueChanged(delegate(bool _, bool newValue)
		{
			if ((bool)hasEnoughScrapLED)
			{
				hasEnoughScrapLED.SetActive(newValue);
			}
		});
	}

	public override void OnNetworkPreDespawn()
	{
		base.OnNetworkPreDespawn();
		if (base.IsClient)
		{
			_doorClosed.OnValueChanged = null;
			_hasDeliveryScrap.OnValueChanged = null;
		}
	}

	public void RegisterDeliverySpot(entity_delivery_spot spot)
	{
		if (_deliveryRegistry.Contains(spot))
		{
			throw new UnityException("Delivery spot already registered");
		}
		_deliveryRegistry.Add(spot);
	}

	public void UnregisterDeliverySpot(entity_delivery_spot spot)
	{
		_deliveryRegistry.Remove(spot);
	}

	public int GetTotalDeliverySpots()
	{
		return _deliveryRegistry.Count;
	}

	public entity_delivery_spot GetDeliverySpotByAddress(int address)
	{
		foreach (entity_delivery_spot item in _deliveryRegistry)
		{
			if ((bool)item && item.GetDeliveryAddress() == address)
			{
				return item;
			}
		}
		return null;
	}

	[Server]
	public void SetDeliverySpeed(float speed)
	{
		if (!base.IsServer)
		{
			throw new UnityException("Server only");
		}
		_deliverySpeed = speed;
	}

	[Server]
	public void CreateDelivery(Task task, Action onComplete = null)
	{
		if (!base.IsServer)
		{
			throw new UnityException("Server only");
		}
		if (task.DeliveryPrefabIndex >= propPrefabs.Count)
		{
			throw new UnityException("Invalid DeliveryPrefabIndex in DeliveryTask");
		}
		NetController<SoundController>.Instance.Play3DSound("Ingame/Entities/DeliverySpawner/vaccummachine_start.ogg", deliverySpawnPoint.position, new AudioData
		{
			distance = 3f
		}, broadcast: true);
		_doorClosed.Value = true;
		_spawnTimer?.Stop();
		_spawnTimer = util_timer.Simple(1f, delegate
		{
			SpawnDeliverable(task);
			_spawnTimer?.Stop();
			_spawnTimer = util_timer.Simple(_deliverySpeed, delegate
			{
				NetController<SoundController>.Instance.Play3DSound("Ingame/Entities/DeliverySpawner/vaccummachine_stop.ogg", deliverySpawnPoint.position, new AudioData
				{
					distance = 3f
				}, broadcast: true);
				_doorClosed.Value = false;
				onComplete?.Invoke();
			});
		});
	}

	[Server]
	public void GenerateAddresses()
	{
		if (_deliveryRegistry.Count == 0)
		{
			Debug.LogWarning("No delivery spots registered");
			return;
		}
		foreach (entity_delivery_spot item in _deliveryRegistry)
		{
			if ((bool)item)
			{
				int num;
				do
				{
					num = UnityEngine.Random.Range(1000, 10000);
				}
				while (!_generatedAddresses.Add(num));
				item.SetDeliveryAddress(num);
			}
		}
	}

	public HashSet<int> GetAddresses()
	{
		return _generatedAddresses;
	}

	[Server]
	private void SpawnDeliverable(Task task)
	{
		if (!base.IsServer)
		{
			throw new UnityException("Server only");
		}
		if (!deliverySpawnPoint)
		{
			throw new UnityException("Missing Transform for deliverySpawnPoint");
		}
		GameObject obj = propPrefabs[task.DeliveryPrefabIndex];
		if (!obj)
		{
			throw new UnityException("Missing delivery prefab");
		}
		GameObject obj2 = UnityEngine.Object.Instantiate(obj, deliverySpawnPoint.position, Quaternion.identity);
		if (!obj2)
		{
			throw new UnityException("Failed to instantiate prop");
		}
		entity_prop_delivery component = obj2.GetComponent<entity_prop_delivery>();
		if (!component)
		{
			throw new UnityException("entity_prop_delivery required");
		}
		component.NetworkObject.Spawn(destroyWithScene: true);
		component.SetTask(task.ID, task.Address);
		_currentProps.Add(component);
	}

	[Server]
	private void Clear()
	{
		if (_spawnTimer != null)
		{
			_spawnTimer.Stop();
		}
		foreach (entity_prop_delivery currentProp in _currentProps)
		{
			if ((bool)currentProp)
			{
				currentProp.SetTask(-1);
				currentProp.NetworkObject.Despawn();
			}
		}
		_currentProps.Clear();
		_generatedAddresses.Clear();
	}

	private void OnShipScrapUpdate(int scrap, bool server)
	{
		if (server)
		{
			if ((bool)NetController<ContractController>.Instance)
			{
				_hasDeliveryScrap.Value = NetController<ContractController>.Instance.GetAffordableTasks(scrap).AsValueEnumerable().Any();
			}
			else if (!NetController<TutorialController>.Instance)
			{
				_hasDeliveryScrap.Value = false;
			}
		}
	}

	private void OnIngameStatusUpdated(INGAME_STATUS newStatus, bool server)
	{
		if (server && newStatus <= INGAME_STATUS.GENERATE)
		{
			Clear();
		}
	}

	protected override void __initializeVariables()
	{
		if (_doorClosed == null)
		{
			throw new Exception("DeliveryController._doorClosed cannot be null. All NetworkVariableBase instances must be initialized.");
		}
		_doorClosed.Initialize(this);
		__nameNetworkVariable(_doorClosed, "_doorClosed");
		NetworkVariableFields.Add(_doorClosed);
		if (_hasDeliveryScrap == null)
		{
			throw new Exception("DeliveryController._hasDeliveryScrap cannot be null. All NetworkVariableBase instances must be initialized.");
		}
		_hasDeliveryScrap.Initialize(this);
		__nameNetworkVariable(_hasDeliveryScrap, "_hasDeliveryScrap");
		NetworkVariableFields.Add(_hasDeliveryScrap);
		base.__initializeVariables();
	}

	protected override void __initializeRpcs()
	{
		base.__initializeRpcs();
	}

	protected internal override string __getTypeName()
	{
		return "DeliveryController";
	}
}
