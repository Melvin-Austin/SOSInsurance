using System;
using FailCake;
using Unity.Netcode;
using UnityEngine;

namespace HyenaQuest;

[RequireComponent(typeof(NetworkObject))]
public class entity_prop_phys_item_place : NetworkBehaviour
{
	public string AcceptID;

	public Vector3 snapPosition;

	public Vector3 snapAngle;

	public GameEvent<entity_item, bool, bool> OnItemUpdate = new GameEvent<entity_item, bool, bool>();

	private Collider _collider;

	private util_timer _snapTimer;

	private readonly NetVar<NetworkBehaviourReference> _itemNetwork = new NetVar<NetworkBehaviourReference>();

	public void Awake()
	{
		_collider = GetComponent<Collider>();
		if (!_collider)
		{
			_collider = GetComponentInChildren<Collider>(includeInactive: true);
		}
		if (!_collider)
		{
			throw new UnityException("Collider not found on entity_prop_phys_place");
		}
		_collider.isTrigger = true;
	}

	protected override void OnNetworkPostSpawn()
	{
		base.OnNetworkPostSpawn();
		if (base.IsClient)
		{
			_itemNetwork.RegisterOnValueChanged(delegate(NetworkBehaviourReference _, NetworkBehaviourReference newValue)
			{
				entity_item entity_item2 = NETController.Get<entity_item>(newValue);
				OnItemUpdate?.Invoke(entity_item2, entity_item2, param3: false);
			});
		}
	}

	public override void OnNetworkPreDespawn()
	{
		base.OnNetworkPreDespawn();
		if (base.IsClient)
		{
			_itemNetwork.OnValueChanged = null;
		}
	}

	public override void OnNetworkDespawn()
	{
		base.OnNetworkDespawn();
		if (base.IsServer)
		{
			_snapTimer?.Stop();
		}
	}

	public void OnTriggerStay(Collider other)
	{
		if (base.IsServer && (bool)other && (bool)other.gameObject)
		{
			entity_item entity_item2 = NETController.Get<entity_item>(_itemNetwork.Value);
			if ((!entity_item2 || !entity_item2.IsSpawned) && other.TryGetComponent<entity_item>(out var component) && component.IsSpawned && !component.IsBeingGrabbed() && component.GetLastOwnerID() != 254 && !(component.GetID() != AcceptID))
			{
				SetItem(component);
			}
		}
	}

	[Server]
	public void Eject(Vector3 direction)
	{
		if (!base.IsServer)
		{
			throw new UnityException("Eject can only be called on the server!");
		}
		entity_item entity_item2 = NETController.Get<entity_item>(_itemNetwork.Value);
		if ((bool)entity_item2)
		{
			if (!entity_item2.IsOwner)
			{
				entity_item2.NetworkObject.RemoveOwnership();
			}
			entity_item2.LaunchItem(direction);
			SetItem(null);
			NetController<SoundController>.Instance.Play3DSound("General/Entities/Place/eject_0.ogg", _collider.bounds.center, new AudioData
			{
				distance = 2f,
				volume = UnityEngine.Random.Range(0.6f, 0.8f),
				pitch = UnityEngine.Random.Range(0.8f, 1.2f)
			}, broadcast: true);
		}
	}

	public void Update()
	{
		if (base.IsServer)
		{
			entity_item entity_item2 = NETController.Get<entity_item>(_itemNetwork.Value);
			if (!entity_item2 || !entity_item2.IsSpawned || entity_item2.IsBeingGrabbed() || (entity_item2 is entity_item_pickable entity_item_pickable2 && entity_item_pickable2.HasOwner()))
			{
				SetItem(null);
			}
		}
	}

	private void SetItem(entity_item item)
	{
		if (!base.IsServer)
		{
			throw new UnityException("SetItem can only be called on the server!");
		}
		if (NETController.Get<entity_item>(_itemNetwork.Value) == item || ((bool)item && !item.IsSpawned))
		{
			return;
		}
		_snapTimer?.Stop();
		NetController<SoundController>.Instance.Play3DSound(item ? "General/Entities/Place/place_entity_1.ogg" : "General/Entities/Place/remove_entity_0.ogg", _collider.bounds.center, new AudioData
		{
			distance = 2f,
			volume = UnityEngine.Random.Range(0.6f, 0.8f),
			pitch = UnityEngine.Random.Range(0.8f, 1.2f)
		}, broadcast: true);
		if ((bool)item)
		{
			item.SetLocked(LOCK_TYPE.SOFT);
			_snapTimer = util_timer.Simple(0.05f, delegate
			{
				if ((bool)item)
				{
					item.Teleport(base.transform.position + snapPosition, Quaternion.Euler(snapAngle));
				}
			});
		}
		_itemNetwork.Value = item;
		OnItemUpdate?.Invoke(item, item, param3: true);
	}

	protected override void __initializeVariables()
	{
		if (_itemNetwork == null)
		{
			throw new Exception("entity_prop_phys_item_place._itemNetwork cannot be null. All NetworkVariableBase instances must be initialized.");
		}
		_itemNetwork.Initialize(this);
		__nameNetworkVariable(_itemNetwork, "_itemNetwork");
		NetworkVariableFields.Add(_itemNetwork);
		base.__initializeVariables();
	}

	protected override void __initializeRpcs()
	{
		base.__initializeRpcs();
	}

	protected internal override string __getTypeName()
	{
		return "entity_prop_phys_item_place";
	}
}
