using System;
using System.Collections.Generic;
using FailCake;
using UnityEngine;
using UnityEngine.Animations;
using ZLinq;
using ZLinq.Linq;

namespace HyenaQuest;

public class entity_item_pickable : entity_item
{
	public bool inventoryRender = true;

	public bool ownerRendering;

	public Vector3 inventoryAngle = Vector3.zero;

	public Vector3 inventoryOffset = Vector3.zero;

	public PlayerItemRenderer renderingItem = PlayerItemRenderer.YEEN_SKATES;

	public ItemLocation location = ItemLocation.CHEST;

	public Vector3 previewSize = Vector3.one;

	public Vector3 previewAngle = Vector3.zero;

	public Vector3 previewPosition = Vector3.zero;

	public Axis previewSquish = Axis.Z;

	public MeshFilter previewMeshFilter;

	public MeshRenderer previewMeshRenderer;

	public MeshRenderer equipedMeshRenderer;

	public GameEvent<entity_player, bool> OnOwnerChange = new GameEvent<entity_player, bool>();

	protected Vector3 _lastValidLocation = Vector3.negativeInfinity;

	protected entity_player _ownerPlayer;

	protected util_timer _tempFix;

	protected readonly NetVar<byte> _slot = new NetVar<byte>(byte.MaxValue);

	protected readonly NetVar<byte> _owner = new NetVar<byte>(byte.MaxValue);

	protected override void Init()
	{
		base.Init();
		if (!previewMeshFilter)
		{
			throw new UnityException("previewMeshFilter is not set");
		}
		if (!previewMeshRenderer)
		{
			throw new UnityException("previewMeshRenderer is not set");
		}
	}

	public Mesh GetMesh()
	{
		return previewMeshFilter?.sharedMesh;
	}

	public Material[] GetMaterials()
	{
		return previewMeshRenderer?.sharedMaterials;
	}

	[Client]
	public override InteractionData InteractionSelector(Collider obj)
	{
		if (IsLocked())
		{
			return new InteractionData(Interaction.INTERACT_LOCKED, _renderers);
		}
		bool flag = HasOwner() || IsBeingGrabbed();
		return new InteractionData(Interaction.INTERACT, _renderers, flag ? "ingame.ui.hints.steal" : "ingame.ui.hints.pickup");
	}

	public Player GetInventoryOwner()
	{
		if (!HasOwner())
		{
			return null;
		}
		return MonoController<PlayerController>.Instance?.GetPlayer(_owner.Value);
	}

	public override void OnNetworkDespawn()
	{
		_tempFix?.Stop();
		if (!base.IsServer)
		{
			return;
		}
		Player inventoryOwner = GetInventoryOwner();
		if (inventoryOwner != null)
		{
			entity_player_inventory inventory = inventoryOwner.player.GetInventory();
			if ((bool)inventory)
			{
				inventory.ClearInventorySlot(this);
			}
		}
	}

	public new void Update()
	{
		RenderOutline(IsBeingGrabbed());
		if ((bool)_rigidbody)
		{
			_rigidbody.interpolation = ((!_ownerPlayer) ? RigidbodyInterpolation.Interpolate : RigidbodyInterpolation.None);
			_rigidbody.useGravity = !_ownerPlayer && !IsBeingGrabbed() && !_volume.InsideAnyVolume(waterOnly: true);
		}
	}

	public void LateUpdate()
	{
		Transform parentTransform = GetParentTransform();
		if ((bool)parentTransform)
		{
			base.transform.position = parentTransform.position + parentTransform.rotation * inventoryOffset;
			if (base.transform.position != Vector3.negativeInfinity)
			{
				_lastValidLocation = base.transform.position;
			}
			base.transform.rotation = parentTransform.rotation * Quaternion.Euler(inventoryAngle);
		}
	}

	[Client]
	public bool IsItemOwner()
	{
		return _owner.Value == PlayerController.LOCAL?.GetPlayerID();
	}

	public bool HasOwner()
	{
		return _owner.Value != byte.MaxValue;
	}

	protected override void OnNetworkPostSpawn()
	{
		base.OnNetworkPostSpawn();
		if (!base.IsClient)
		{
			return;
		}
		_owner.RegisterOnValueChanged(delegate(byte oldValue, byte newValue)
		{
			if (oldValue != newValue)
			{
				bool colliderTriggers = newValue != byte.MaxValue;
				SetColliderTriggers(colliderTriggers);
				SetRendererVisibility(oldValue, newValue);
				InternalChangeOwner(newValue, server: false);
			}
		});
	}

	public override void OnNetworkPreDespawn()
	{
		base.OnNetworkPreDespawn();
		if (base.IsClient)
		{
			_owner.OnValueChanged = null;
		}
	}

	private void SetNetworkTransform(bool enable)
	{
		if (!_networkTransform)
		{
			return;
		}
		_networkTransform.enabled = enable;
		if (!enable || !base.IsOwner)
		{
			return;
		}
		_networkTransform.SetState(_lastValidLocation, base.transform.rotation, base.transform.lossyScale, teleportDisabled: false);
		_tempFix?.Stop();
		_tempFix = util_timer.Simple(0.1f, delegate
		{
			if ((bool)_networkTransform)
			{
				_networkTransform.SetState(_lastValidLocation, base.transform.rotation, base.transform.lossyScale, teleportDisabled: false);
			}
		});
	}

	protected virtual void InternalChangeOwner(byte newOwner, bool server)
	{
		_ownerPlayer = ((newOwner == byte.MaxValue) ? null : MonoController<PlayerController>.Instance.GetPlayerEntityByID(newOwner));
		if (server)
		{
			SetLocked(LOCK_TYPE.NONE);
		}
		OnOwnerChange.Invoke(_ownerPlayer, server);
	}

	private void SetColliderTriggers(bool trigger)
	{
		List<Collider> list = new List<Collider> { GetComponent<Collider>() };
		list.AddRange(GetComponentsInChildren<Collider>(includeInactive: true));
		if (list == null || list.Count <= 0)
		{
			return;
		}
		using ValueEnumerator<ListWhere<Collider>, Collider> valueEnumerator = (from col in list.AsValueEnumerable()
			where col
			select col).GetEnumerator<ListWhere<Collider>, Collider>();
		while (valueEnumerator.MoveNext())
		{
			valueEnumerator.Current.isTrigger = trigger;
		}
	}

	private void SetRendererVisibility(byte oldOwnerID, byte newOwnerID)
	{
		if (!PlayerController.LOCAL)
		{
			return;
		}
		Renderer[] renderers = _renderers;
		if (renderers == null || renderers.Length <= 0)
		{
			return;
		}
		byte playerID = PlayerController.LOCAL.GetPlayerID();
		if (!ownerRendering)
		{
			renderers = _renderers;
			foreach (Renderer renderer in renderers)
			{
				if ((bool)renderer)
				{
					renderer.enabled = newOwnerID == byte.MaxValue || (inventoryRender && newOwnerID != playerID);
				}
			}
		}
		if ((bool)equipedMeshRenderer)
		{
			equipedMeshRenderer.enabled = newOwnerID != byte.MaxValue;
		}
		if (!inventoryRender)
		{
			entity_player playerEntityByID = MonoController<PlayerController>.Instance.GetPlayerEntityByID(oldOwnerID);
			if ((bool)playerEntityByID)
			{
				playerEntityByID.GetItemRenderer(renderingItem).gameObject.SetActive(value: false);
			}
			entity_player playerEntityByID2 = MonoController<PlayerController>.Instance.GetPlayerEntityByID(newOwnerID);
			if ((bool)playerEntityByID2)
			{
				playerEntityByID2.GetItemRenderer(renderingItem).gameObject.SetActive(value: true);
			}
		}
	}

	protected override void OnPreCollision(Collision collision)
	{
		if (!HasOwner())
		{
			base.OnPreCollision(collision);
		}
	}

	[Server]
	public virtual bool SetInventory(byte slot, Player owner)
	{
		if (owner == null || IsLocked())
		{
			return false;
		}
		MonoController<PlayerController>.Instance.GetPlayer(_owner.Value)?.player.GetInventory().UpdateInventorySlot(_slot.Value, null);
		_slot.Value = slot;
		SetParent(owner);
		SetInventoryOwner(owner, revokeOwner: false);
		return true;
	}

	[Server]
	public virtual void RemoveFromInventory()
	{
		_slot.Value = byte.MaxValue;
		RemoveParent();
		SetInventoryOwner(null, revokeOwner: true);
	}

	public override bool CanGrab()
	{
		if (base.CanGrab())
		{
			return !HasOwner();
		}
		return false;
	}

	[Server]
	protected virtual void SetInventoryOwner(Player owner, bool revokeOwner)
	{
		if (!base.IsServer)
		{
			throw new UnityException("Server only");
		}
		CancelGrabbing();
		if (owner != null && (bool)owner.player)
		{
			base.NetworkObject.ChangeOwnership(owner.connectionID);
		}
		else if (revokeOwner)
		{
			base.NetworkObject.RemoveOwnership();
			if ((bool)_networkTransform)
			{
				_networkTransform.SetState(_lastValidLocation, base.transform.rotation, base.transform.lossyScale, teleportDisabled: false);
			}
		}
		_owner.Value = owner?.GetID() ?? byte.MaxValue;
		InternalChangeOwner(_owner.Value, server: true);
	}

	public byte GetSlot()
	{
		return _slot.Value;
	}

	public virtual bool CanPickUp(entity_player_inventory inventory)
	{
		return true;
	}

	[Server]
	private void RemoveParent()
	{
		if (!base.NetworkObject.TryRemoveParent())
		{
			throw new UnityException("Failed to remove parent");
		}
	}

	[Server]
	private void SetParent(Player newOwner)
	{
		if (!base.NetworkObject.TrySetParent(newOwner.player.transform, worldPositionStays: false))
		{
			throw new UnityException("Failed to set parent");
		}
		base.transform.position = Vector3.zero;
	}

	private Transform GetParentTransform()
	{
		if (!_ownerPlayer)
		{
			return null;
		}
		return location switch
		{
			ItemLocation.HEAD => _ownerPlayer.GetHeadTransform(), 
			ItemLocation.CHEST => _ownerPlayer.GetChestTransform(), 
			ItemLocation.RIGHT_HAND => _ownerPlayer.GetRightHandTransform(), 
			ItemLocation.LEFT_HAND => _ownerPlayer.GetLeftHandTransform(), 
			ItemLocation.HIPS => _ownerPlayer.GetHipsTransform(), 
			_ => throw new UnityException("Invalid item location"), 
		};
	}

	protected override void __initializeVariables()
	{
		if (_slot == null)
		{
			throw new Exception("entity_item_pickable._slot cannot be null. All NetworkVariableBase instances must be initialized.");
		}
		_slot.Initialize(this);
		__nameNetworkVariable(_slot, "_slot");
		NetworkVariableFields.Add(_slot);
		if (_owner == null)
		{
			throw new Exception("entity_item_pickable._owner cannot be null. All NetworkVariableBase instances must be initialized.");
		}
		_owner.Initialize(this);
		__nameNetworkVariable(_owner, "_owner");
		NetworkVariableFields.Add(_owner);
		base.__initializeVariables();
	}

	protected override void __initializeRpcs()
	{
		base.__initializeRpcs();
	}

	protected internal override string __getTypeName()
	{
		return "entity_item_pickable";
	}
}
