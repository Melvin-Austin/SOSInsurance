using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

namespace HyenaQuest;

public class entity_player_inventory : NetworkBehaviour
{
	public GameEvent<int, entity_item_pickable, bool> OnInventoryUpdate = new GameEvent<int, entity_item_pickable, bool>();

	public GameEvent<int> OnInventorySlotUpdate = new GameEvent<int>();

	public InputActionReference cycleAction;

	public InputActionReference inventoryCycleAction;

	public InputActionReference dropAction;

	public InputActionReference useItemAction;

	private byte _selectedSlot;

	private entity_player _owner;

	private bool _pressedUseItem;

	private bool _pressingUseItem;

	private readonly NetworkList<NetworkBehaviourReference> _inventory = new NetworkList<NetworkBehaviourReference>(new NetworkBehaviourReference[IngameController.MAX_INVENTORY_SLOTS]);

	private readonly Dictionary<string, entity_item_pickable> _inventoryHash = new Dictionary<string, entity_item_pickable>();

	public void Awake()
	{
		_owner = GetComponent<entity_player>();
		if (!_owner)
		{
			throw new UnityException("entity_player_inventory requires entity_player component");
		}
		if (!cycleAction)
		{
			throw new UnityException("Missing cycleAction InputActionReference");
		}
		if (!inventoryCycleAction)
		{
			throw new UnityException("Missing inventoryCycleAction InputActionReference");
		}
		if (!dropAction)
		{
			throw new UnityException("Missing dropAction InputActionReference");
		}
		if (!useItemAction)
		{
			throw new UnityException("Missing useItemAction InputActionReference");
		}
	}

	protected override void OnNetworkPostSpawn()
	{
		base.OnNetworkPostSpawn();
		if (base.IsClient)
		{
			_inventory.OnListChanged += OnInventoryChanged;
			RebuildInventoryHash();
		}
	}

	public override void OnNetworkPreDespawn()
	{
		base.OnNetworkPreDespawn();
		if (base.IsClient)
		{
			_inventory.OnListChanged -= OnInventoryChanged;
		}
	}

	public override void OnNetworkDespawn()
	{
		if (base.IsServer)
		{
			DropAllItems();
		}
		if (base.IsClient && base.IsOwner)
		{
			cycleAction.action.performed -= OnInventoryCycle;
			inventoryCycleAction.action.performed -= OnInventoryKeyboardCycle;
			dropAction.action.performed -= OnDropPerformed;
			useItemAction.action.performed -= OnUseItemStart;
			useItemAction.action.canceled -= OnUseItemEnd;
		}
	}

	[Client]
	public void SetupControls()
	{
		if (base.IsOwner)
		{
			cycleAction.action.performed += OnInventoryCycle;
			inventoryCycleAction.action.performed += OnInventoryKeyboardCycle;
			dropAction.action.performed += OnDropPerformed;
			useItemAction.action.performed += OnUseItemStart;
			useItemAction.action.canceled += OnUseItemEnd;
		}
	}

	public void Update()
	{
		if (base.IsOwner && !_owner.IsDead() && _pressedUseItem != _pressingUseItem)
		{
			entity_item_pickable selectedInventoryItem = GetSelectedInventoryItem();
			if ((bool)selectedInventoryItem)
			{
				selectedInventoryItem.OnUse(_owner, _owner.GetAimingHit()?.collider, _pressingUseItem);
			}
			_pressedUseItem = _pressingUseItem;
		}
	}

	[Client]
	public void PickupItem(byte slot, entity_item_pickable item)
	{
		if (base.IsOwner && IsInventorySlotEmpty(slot) && slot != byte.MaxValue && (bool)item && !item.IsLocked())
		{
			PickupItemRPC(slot, item);
		}
	}

	[Server]
	public void UpdateInventorySlot(byte slot, entity_item_pickable item)
	{
		if (!base.IsServer)
		{
			throw new UnityException("UpdateInventorySlot can only be called on the server!");
		}
		if (_inventory != null && _inventory.Count != 0)
		{
			int inventorySize = GetInventorySize();
			if (slot < inventorySize && slot < _inventory.Count)
			{
				_inventory[slot] = item;
				OnInventoryUpdate?.Invoke(slot, item, param3: true);
			}
		}
	}

	[Server]
	public void ClearInventorySlot(entity_item_pickable itm)
	{
		if (!base.IsServer)
		{
			throw new UnityException("ClearInventorySlot can only be called on the server!");
		}
		if (_inventory != null && _inventory.Count != 0 && (bool)itm)
		{
			byte slot = itm.GetSlot();
			if (slot < GetInventorySize() && slot < _inventory.Count)
			{
				UpdateInventorySlot(slot, null);
			}
		}
	}

	[Server]
	public void DropAllItems()
	{
		if (!base.IsServer)
		{
			throw new UnityException("DropAllItems can only be called on the server!");
		}
		if (_inventory == null)
		{
			return;
		}
		foreach (NetworkBehaviourReference item2 in _inventory)
		{
			entity_item_pickable item = GetItem(item2);
			if ((bool)item && item.IsSpawned)
			{
				DropItem(item);
			}
		}
	}

	[Rpc(SendTo.Server)]
	public void DropItemRPC(NetworkBehaviourReference refObj)
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
			FastBufferWriter bufferWriter = __beginSendRpc(3078871644u, rpcParams, attributeParams, SendTo.Server, RpcDelivery.Reliable);
			bufferWriter.WriteValueSafe(in refObj, default(FastBufferWriter.ForNetworkSerializable));
			__endSendRpc(ref bufferWriter, 3078871644u, rpcParams, attributeParams, SendTo.Server, RpcDelivery.Reliable);
		}
		if (__rpc_exec_stage == __RpcExecStage.Execute)
		{
			__rpc_exec_stage = __RpcExecStage.Send;
			entity_item_pickable item = GetItem(refObj);
			if (!item)
			{
				throw new UnityException("Invalid item ID");
			}
			DropItem(item);
		}
	}

	[Server]
	public void DropItem(entity_item_pickable item)
	{
		if (!base.IsServer)
		{
			throw new UnityException("DropItem can only be called on the server!");
		}
		if (!item)
		{
			throw new UnityException("Invalid item ID");
		}
		byte slot = item.GetSlot();
		item.RemoveFromInventory();
		UpdateInventorySlot(slot, null);
	}

	public byte GetInventorySlot()
	{
		return _selectedSlot;
	}

	public bool IsInventorySlotEmpty(byte slot = byte.MaxValue)
	{
		if (_inventory == null || _inventory.Count == 0)
		{
			return true;
		}
		int inventorySize = GetInventorySize();
		byte b = ((slot == byte.MaxValue) ? _selectedSlot : slot);
		if (b >= inventorySize || b >= _inventory.Count)
		{
			return false;
		}
		return !GetItem(_inventory[b]);
	}

	public entity_item_pickable FindItemByID(string itemID)
	{
		if (string.IsNullOrEmpty(itemID) || !_inventoryHash.TryGetValue(itemID, out var value))
		{
			return null;
		}
		return value;
	}

	public byte GetAvailableSlot()
	{
		int num = Math.Min(_inventory.Count, GetInventorySize());
		for (byte b = 0; b < num; b++)
		{
			if (!GetItem(_inventory[b]))
			{
				return b;
			}
		}
		return byte.MaxValue;
	}

	[Client]
	public entity_item_pickable GetSelectedInventoryItem()
	{
		if (!base.IsClient)
		{
			throw new UnityException("GetSelectedInventoryItem can only be called on the client!");
		}
		if (!base.IsOwner || _selectedSlot >= _inventory.Count)
		{
			return null;
		}
		return GetItem(_inventory[_selectedSlot]);
	}

	public bool HasItem(entity_item_pickable item)
	{
		if (!item || !item.IsSpawned)
		{
			return false;
		}
		return HasItem(item.GetID());
	}

	public bool HasItem(string itemID)
	{
		if (!string.IsNullOrEmpty(itemID))
		{
			return _inventoryHash.ContainsKey(itemID);
		}
		return false;
	}

	public NetworkList<NetworkBehaviourReference> GetInventory()
	{
		return _inventory;
	}

	public Dictionary<string, entity_item_pickable> GetInventoryCache()
	{
		return _inventoryHash;
	}

	private void OnUseItemStart(InputAction.CallbackContext obj)
	{
		_pressingUseItem = true;
	}

	private void OnUseItemEnd(InputAction.CallbackContext obj)
	{
		_pressingUseItem = false;
	}

	private byte GetInventorySize()
	{
		return NetController<IngameController>.Instance?.GetMaxInventorySlots() ?? 1;
	}

	private void RebuildInventoryHash()
	{
		_inventoryHash.Clear();
		foreach (NetworkBehaviourReference item2 in _inventory)
		{
			entity_item_pickable item = GetItem(item2);
			if ((bool)item && !string.IsNullOrEmpty(item.GetID()))
			{
				_inventoryHash[item.GetID()] = item;
			}
		}
	}

	private void OnInventoryChanged(NetworkListEvent<NetworkBehaviourReference> changeEvent)
	{
		if (changeEvent.Index >= _inventory.Count)
		{
			throw new UnityException("Invalid inventory index");
		}
		RebuildInventoryHash();
		OnInventoryUpdate?.Invoke(changeEvent.Index, NETController.Get<entity_item_pickable>(changeEvent.Value), param3: false);
	}

	private entity_item_pickable GetItem(NetworkBehaviourReference refObj)
	{
		if ((bool)NETController.Instance)
		{
			return NETController.Get<entity_item_pickable>(refObj);
		}
		return null;
	}

	[Rpc(SendTo.Server)]
	private void PickupItemRPC(byte slot, NetworkBehaviourReference refObj)
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
			FastBufferWriter bufferWriter = __beginSendRpc(1335654483u, rpcParams, attributeParams, SendTo.Server, RpcDelivery.Reliable);
			bufferWriter.WriteValueSafe(in slot, default(FastBufferWriter.ForPrimitives));
			bufferWriter.WriteValueSafe(in refObj, default(FastBufferWriter.ForNetworkSerializable));
			__endSendRpc(ref bufferWriter, 1335654483u, rpcParams, attributeParams, SendTo.Server, RpcDelivery.Reliable);
		}
		if (__rpc_exec_stage != __RpcExecStage.Execute)
		{
			return;
		}
		__rpc_exec_stage = __RpcExecStage.Send;
		if (!base.IsServer)
		{
			throw new UnityException("PickupItemRPC can only be called on the server!");
		}
		entity_item_pickable item = GetItem(refObj);
		if (!item)
		{
			throw new UnityException("Invalid item");
		}
		if (HasItem(item) || !item.CanPickUp(this) || !IsInventorySlotEmpty(slot) || item.IsLocked())
		{
			return;
		}
		Player player = MonoController<PlayerController>.Instance.GetPlayer(_owner.GetPlayerID());
		if (player != null)
		{
			if (!item.SetInventory(slot, player))
			{
				throw new UnityException("Failed to set inventory owner");
			}
			UpdateInventorySlot(slot, item);
		}
	}

	[Client]
	private void SetSelectedSlot(byte slot)
	{
		if (!base.IsClient)
		{
			throw new UnityException("SetSelectedSlot can only be called on the client!");
		}
		if (_owner.IsDead())
		{
			return;
		}
		int inventorySize = GetInventorySize();
		byte b = (byte)Mathf.Clamp(slot, 0, inventorySize - 1);
		if (b != _selectedSlot && _pressedUseItem)
		{
			entity_item_pickable selectedInventoryItem = GetSelectedInventoryItem();
			if ((bool)selectedInventoryItem)
			{
				selectedInventoryItem.OnUse(_owner, null, pressing: false);
			}
			_pressingUseItem = false;
			_pressedUseItem = false;
		}
		_selectedSlot = b;
		OnInventorySlotUpdate?.Invoke(_selectedSlot);
	}

	[Client]
	private void OnInventoryKeyboardCycle(InputAction.CallbackContext ctx)
	{
		if (ctx.control is KeyControl keyControl && !_owner.IsDead())
		{
			switch (keyControl.keyCode)
			{
			case Key.Digit1:
				SetSelectedSlot(0);
				break;
			case Key.Digit2:
				SetSelectedSlot(1);
				break;
			case Key.Digit3:
				SetSelectedSlot(2);
				break;
			case Key.Digit4:
				SetSelectedSlot(3);
				break;
			case Key.Digit5:
				SetSelectedSlot(4);
				break;
			}
		}
	}

	[Client]
	private void OnInventoryCycle(InputAction.CallbackContext ctx)
	{
		if (_owner.IsDead())
		{
			return;
		}
		entity_player_physgun physgun = _owner.GetPhysgun();
		if ((bool)physgun && (bool)physgun.GetGrabbingObject())
		{
			return;
		}
		Vector2 vector = ctx.ReadValue<Vector2>();
		int inventorySize = GetInventorySize();
		float y = vector.y;
		int num;
		if (!(y < 0f))
		{
			if (y == 0f)
			{
				return;
			}
			num = (_selectedSlot - 1 + inventorySize) % inventorySize;
		}
		else
		{
			num = (_selectedSlot + 1) % inventorySize;
		}
		SetSelectedSlot((byte)num);
	}

	[Client]
	private void OnDropPerformed(InputAction.CallbackContext ctx)
	{
		if (!_owner.IsDead() && !IsInventorySlotEmpty())
		{
			DropItemRPC(_inventory[_selectedSlot]);
		}
	}

	protected override void __initializeVariables()
	{
		if (_inventory == null)
		{
			throw new Exception("entity_player_inventory._inventory cannot be null. All NetworkVariableBase instances must be initialized.");
		}
		_inventory.Initialize(this);
		__nameNetworkVariable(_inventory, "_inventory");
		NetworkVariableFields.Add(_inventory);
		base.__initializeVariables();
	}

	protected override void __initializeRpcs()
	{
		__registerRpc(3078871644u, __rpc_handler_3078871644, "DropItemRPC", RpcInvokePermission.Everyone);
		__registerRpc(1335654483u, __rpc_handler_1335654483, "PickupItemRPC", RpcInvokePermission.Everyone);
		base.__initializeRpcs();
	}

	private static void __rpc_handler_3078871644(NetworkBehaviour target, FastBufferReader reader, __RpcParams rpcParams)
	{
		NetworkManager networkManager = target.NetworkManager;
		if ((object)networkManager != null && networkManager.IsListening)
		{
			reader.ReadValueSafe(out NetworkBehaviourReference value, default(FastBufferWriter.ForNetworkSerializable));
			target.__rpc_exec_stage = __RpcExecStage.Execute;
			((entity_player_inventory)target).DropItemRPC(value);
			target.__rpc_exec_stage = __RpcExecStage.Send;
		}
	}

	private static void __rpc_handler_1335654483(NetworkBehaviour target, FastBufferReader reader, __RpcParams rpcParams)
	{
		NetworkManager networkManager = target.NetworkManager;
		if ((object)networkManager != null && networkManager.IsListening)
		{
			reader.ReadValueSafe(out byte value, default(FastBufferWriter.ForPrimitives));
			reader.ReadValueSafe(out NetworkBehaviourReference value2, default(FastBufferWriter.ForNetworkSerializable));
			target.__rpc_exec_stage = __RpcExecStage.Execute;
			((entity_player_inventory)target).PickupItemRPC(value, value2);
			target.__rpc_exec_stage = __RpcExecStage.Send;
		}
	}

	protected internal override string __getTypeName()
	{
		return "entity_player_inventory";
	}
}
