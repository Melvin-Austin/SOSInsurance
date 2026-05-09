using System;
using System.Collections.Generic;
using ECM2;
using FailCake;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;
using ZLinq;

namespace HyenaQuest;

[DisallowMultipleComponent]
[RequireComponent(typeof(NetworkObject), typeof(NetworkTransform), typeof(entity_volume_affector))]
[RequireComponent(typeof(NetworkRigidbody), typeof(Rigidbody))]
public class entity_phys : NetworkBehaviour
{
	public GameEvent<LOCK_TYPE, bool> OnLockStatusChanged = new GameEvent<LOCK_TYPE, bool>();

	public GameEvent<entity_player, bool, bool> OnGrabbed = new GameEvent<entity_player, bool, bool>();

	public List<AudioClip> collideSounds;

	protected Rigidbody _rigidbody;

	protected entity_volume_affector _volume;

	protected Renderer[] _renderers;

	protected uint _physgunMask;

	protected uint _physgunSafeMask;

	protected uint _physgunFrozenMask;

	protected float _collideCooldown;

	protected float _lastLetgoTime;

	protected NetworkRigidbody _networkBody;

	protected NetworkTransform _networkTransform;

	private entity_player _owner;

	private entity_save_data _saveData;

	private byte _lastGrabbingOwnerId = byte.MaxValue;

	protected readonly NetVar<LOCK_TYPE> _locked = new NetVar<LOCK_TYPE>(LOCK_TYPE.NONE);

	protected readonly NetVar<byte> _grabbingOwnerId = new NetVar<byte>(byte.MaxValue);

	protected virtual void Init()
	{
		_rigidbody = GetComponent<Rigidbody>();
		if (!_rigidbody)
		{
			throw new UnityException("entity_phys requires a rigidbody component to work.");
		}
		_rigidbody.interpolation = RigidbodyInterpolation.Interpolate;
		_renderers = (from a in GetComponentsInChildren<Renderer>(includeInactive: false).AsValueEnumerable()
			where a.enabled && (a is MeshRenderer || a is SkinnedMeshRenderer)
			select a).ToArray();
		Renderer[] renderers = _renderers;
		if (renderers == null || renderers.Length <= 0)
		{
			throw new UnityException("entity_phys requires a Renderer component to work.");
		}
		_physgunMask = RenderingLayerMask.GetMask("Physgun");
		_physgunSafeMask = RenderingLayerMask.GetMask("PhysgunSafe");
		_physgunFrozenMask = RenderingLayerMask.GetMask("PhysgunFrozen");
		_volume = GetComponent<entity_volume_affector>();
		if (!_volume)
		{
			throw new UnityException("entity_volume_affector not found");
		}
		_networkTransform = GetComponent<NetworkTransform>();
		if (!_networkTransform)
		{
			throw new UnityException("NetworkTransform not found");
		}
		_networkBody = GetComponent<NetworkRigidbody>();
		if (!_networkBody)
		{
			throw new UnityException("NetworkRigidbody not found");
		}
		_saveData = GetComponent<entity_save_data>();
	}

	protected override void OnNetworkPostSpawn()
	{
		base.OnNetworkPostSpawn();
		if (!base.IsClient)
		{
			return;
		}
		_locked.RegisterOnValueChanged(delegate(LOCK_TYPE oldValue, LOCK_TYPE newValue)
		{
			if (oldValue != newValue)
			{
				base.tag = ((newValue == LOCK_TYPE.LOCKED) ? "ENTITY/LOCKED" : "Untagged");
				_networkBody?.SetIsKinematic(newValue != 0 || !base.IsOwner);
				RenderLayer(_physgunFrozenMask, newValue == LOCK_TYPE.SOFT_FROZEN);
				if (newValue == LOCK_TYPE.SOFT_FROZEN)
				{
					NetController<SoundController>.Instance?.Play3DSound("Ingame/Props/Metal/metal_break.ogg", base.transform.position, new AudioData
					{
						distance = 4f,
						pitch = UnityEngine.Random.Range(0.8f, 1.2f),
						volume = 0.4f
					});
				}
				OnLockStatusChanged?.Invoke(newValue, param2: false);
			}
		});
		_grabbingOwnerId.RegisterOnValueChanged(delegate(byte oldValue, byte newValue)
		{
			bool flag = newValue != byte.MaxValue;
			_owner = (flag ? MonoController<PlayerController>.Instance.GetPlayerEntityByID(newValue) : null);
			if (oldValue == PlayerController.LOCAL?.GetPlayerID() && oldValue != newValue)
			{
				PlayerController.LOCAL.CancelGrabbing();
			}
			OnGrabbed?.Invoke(PlayerController.LOCAL, flag, param3: false);
		});
	}

	public override void OnNetworkPreDespawn()
	{
		base.OnNetworkPreDespawn();
		if (base.IsClient)
		{
			_locked.OnValueChanged = null;
			_grabbingOwnerId.OnValueChanged = null;
		}
	}

	public override void OnGainedOwnership()
	{
		base.OnGainedOwnership();
		if ((bool)_networkTransform)
		{
			_networkTransform.Interpolate = true;
		}
	}

	public override void OnNetworkSpawn()
	{
		base.OnNetworkSpawn();
		Init();
		if (base.IsServer)
		{
			if (!MonoController<PlayerController>.Instance)
			{
				throw new UnityException("Missing PlayerController");
			}
			MonoController<PlayerController>.Instance.OnPlayerRemoved += new Action<entity_player, bool>(OnPlayerRemove);
		}
	}

	public override void OnNetworkDespawn()
	{
		base.OnNetworkDespawn();
		if (base.IsServer && (bool)MonoController<PlayerController>.Instance)
		{
			MonoController<PlayerController>.Instance.OnPlayerRemoved -= new Action<entity_player, bool>(OnPlayerRemove);
		}
	}

	[Server]
	public virtual SaveDataItems? LoadData(SaveData saveData, string id = null)
	{
		if (!base.IsServer)
		{
			throw new UnityException("Server only");
		}
		if (string.IsNullOrEmpty(id))
		{
			if (!_saveData || string.IsNullOrEmpty(_saveData.UNIQUE_ID))
			{
				return null;
			}
			id = _saveData.UNIQUE_ID;
		}
		if (string.IsNullOrEmpty(id) || saveData.items == null)
		{
			return null;
		}
		SaveDataItems saveDataItems = saveData.items.AsValueEnumerable().FirstOrDefault((SaveDataItems itm) => string.Equals(itm.id, id));
		if (string.IsNullOrEmpty(saveDataItems.id))
		{
			return null;
		}
		if (saveDataItems.data != null)
		{
			Load(saveDataItems.data);
		}
		saveData.items.Remove(saveDataItems);
		if ((bool)_networkTransform)
		{
			_networkTransform.SetState(saveDataItems.position, saveDataItems.rotation, base.transform.localScale, teleportDisabled: false);
		}
		return saveDataItems;
	}

	[Server]
	public virtual Dictionary<string, string> Save()
	{
		return null;
	}

	[Server]
	public virtual void Load(Dictionary<string, string> data)
	{
	}

	[Server]
	public void Teleport(Vector3 position, Quaternion rotation)
	{
		if (!base.IsServer)
		{
			throw new UnityException("Server only");
		}
		if ((bool)_networkTransform)
		{
			_networkTransform.SetState(position, rotation, base.transform.localScale, teleportDisabled: false);
		}
	}

	[Client]
	public virtual InteractionData InteractionSelector(Collider obj)
	{
		bool flag = CanGrab();
		bool flag2 = IsBeingGrabbed();
		return new InteractionData((!(flag || flag2)) ? Interaction.INTERACT_LOCKED : Interaction.INTERACT, _renderers, flag2 ? "ingame.ui.hints.steal" : (flag ? "ingame.ui.hints.grab" : null));
	}

	[Client]
	public virtual void SetGrabbing(bool grabbing)
	{
		if (!grabbing || CanGrab())
		{
			_rigidbody.solverIterations = (grabbing ? 20 : Physics.defaultSolverIterations);
			_rigidbody.solverVelocityIterations = (grabbing ? 20 : Physics.defaultSolverVelocityIterations);
			SetGrabbingRPC(grabbing, base.RpcTarget.Server);
		}
	}

	public byte GetOwnerID()
	{
		return _grabbingOwnerId.Value;
	}

	public entity_player GetGrabbingOwner()
	{
		return _owner;
	}

	public byte GetLastOwnerID()
	{
		return _lastGrabbingOwnerId;
	}

	public virtual bool IsBeingGrabbed()
	{
		return _grabbingOwnerId.Value != byte.MaxValue;
	}

	public Vector3 GetVelocity()
	{
		if ((bool)_rigidbody)
		{
			return _rigidbody.linearVelocity;
		}
		return Vector3.zero;
	}

	[Server]
	public virtual void Destroy()
	{
		if (!base.IsServer)
		{
			throw new UnityException("Destroy can only be called on the server.");
		}
		base.NetworkObject.Despawn();
	}

	[Rpc(SendTo.Server)]
	public virtual void DestroyRPC()
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
			FastBufferWriter bufferWriter = __beginSendRpc(1909066713u, rpcParams, attributeParams, SendTo.Server, RpcDelivery.Reliable);
			__endSendRpc(ref bufferWriter, 1909066713u, rpcParams, attributeParams, SendTo.Server, RpcDelivery.Reliable);
		}
		if (__rpc_exec_stage == __RpcExecStage.Execute)
		{
			__rpc_exec_stage = __RpcExecStage.Send;
			Destroy();
		}
	}

	[Client]
	public virtual void SetVelocity(Vector3 force)
	{
		if (base.IsOwner && !IsLocked() && (bool)_rigidbody && !_rigidbody.isKinematic)
		{
			_rigidbody.angularVelocity = Vector3.zero;
			_rigidbody.linearVelocity = force.clampedTo(12f);
		}
	}

	[Client]
	public virtual void SetRotation(Quaternion rotation)
	{
		if (base.IsOwner && !IsLocked() && (bool)_rigidbody && !_rigidbody.isKinematic)
		{
			Quaternion a = rotation * Quaternion.Inverse(base.transform.rotation);
			if (Quaternion.Dot(a, Quaternion.identity) < 0f)
			{
				a = new Quaternion(0f - a.x, 0f - a.y, 0f - a.z, 0f - a.w);
			}
			a.ToAngleAxis(out var angle, out var axis);
			if (angle > 0.01f)
			{
				Vector3 angularVelocity = axis * (angle * (MathF.PI / 180f) * 50f);
				_rigidbody.angularVelocity = angularVelocity;
			}
		}
	}

	public void OnCollisionEnter(Collision collision)
	{
		if (collision != null && (bool)collision.gameObject)
		{
			OnPreCollision(collision);
		}
	}

	protected virtual void OnPreCollision(Collision collision)
	{
		if (_collideCooldown > Time.time)
		{
			return;
		}
		_collideCooldown = Time.time + 0.2f;
		if (base.IsOwner)
		{
			float magnitude = collision.relativeVelocity.magnitude;
			if (magnitude > 0.05f && collideSounds.Count > 0)
			{
				NetController<SoundController>.Instance?.Play3DSound(collideSounds[UnityEngine.Random.Range(0, collideSounds.Count)], base.transform, new AudioData
				{
					pitch = UnityEngine.Random.Range(0.85f, 1.15f),
					distance = 3f,
					volume = Mathf.Clamp(magnitude * 0.02f, 0.08f, 1f)
				}, broadcast: true);
			}
		}
		if (!IsBeingGrabbed() && (bool)collision.rigidbody && collision.gameObject.TryGetComponent<entity_phys>(out var component))
		{
			if (!component.IsBeingGrabbed())
			{
				return;
			}
		}
		else if (!base.IsOwner)
		{
			return;
		}
		OnCollision(collision);
	}

	public virtual void OnThrow()
	{
	}

	protected virtual void OnCollision(Collision col)
	{
	}

	public void Update()
	{
		RenderOutline(IsBeingGrabbed());
		if (base.IsOwner && (bool)_volume && (bool)_rigidbody)
		{
			_rigidbody.useGravity = !IsBeingGrabbed() && !_volume.InsideAnyVolume(waterOnly: true, fullOnly: true);
		}
	}

	public virtual Bounds GetBounds()
	{
		Bounds? bounds = null;
		Renderer[] renderers = _renderers;
		foreach (Renderer renderer in renderers)
		{
			if ((bool)renderer && renderer.enabled)
			{
				if (!bounds.HasValue)
				{
					bounds = renderer.bounds;
					continue;
				}
				Bounds value = bounds.Value;
				value.Encapsulate(renderer.bounds);
				bounds = value;
			}
		}
		return bounds ?? new Bounds(base.transform.position, Vector3.zero);
	}

	[Server]
	public void LaunchItem(Vector3 force)
	{
		if (!_rigidbody || IsBeingGrabbed())
		{
			return;
		}
		CancelGrabbing();
		SetLocked(LOCK_TYPE.NONE);
		_lastGrabbingOwnerId = 254;
		util_timer.Simple(0.05f, delegate
		{
			if ((bool)_rigidbody)
			{
				_rigidbody.isKinematic = false;
				_rigidbody.AddForce(force, ForceMode.Impulse);
			}
		});
	}

	[Server]
	public virtual void SetLocked(LOCK_TYPE locked)
	{
		if (!base.IsServer)
		{
			throw new UnityException("Not Server");
		}
		if (_locked.Value != locked)
		{
			_locked.SetSpawnValue(locked);
			_grabbingOwnerId.SetSpawnValue(byte.MaxValue);
			OnLockStatusChanged?.Invoke(locked, param2: true);
		}
	}

	public virtual bool IsLocked()
	{
		return _locked.Value == LOCK_TYPE.LOCKED;
	}

	public virtual bool IsSoftLocked()
	{
		LOCK_TYPE value = _locked.Value;
		return value == LOCK_TYPE.SOFT || value == LOCK_TYPE.SOFT_FROZEN;
	}

	public LOCK_TYPE GetLockType()
	{
		return _locked.Value;
	}

	public virtual void RenderLayer(uint mask, bool render)
	{
		Renderer[] renderers = _renderers;
		if (renderers == null || renderers.Length <= 0)
		{
			return;
		}
		renderers = _renderers;
		foreach (Renderer renderer in renderers)
		{
			if ((bool)renderer)
			{
				if (render)
				{
					renderer.renderingLayerMask |= mask;
				}
				else
				{
					renderer.renderingLayerMask &= ~mask;
				}
			}
		}
	}

	public virtual void RenderOutline(bool render)
	{
		bool flag = GetGrabbingOwner()?.GetPhysgun().IsProtected() ?? false;
		RenderLayer(_physgunSafeMask | _physgunMask, render: false);
		if (render)
		{
			RenderLayer(flag ? _physgunSafeMask : _physgunMask, render: true);
		}
	}

	protected virtual void RenderOutline(Renderer mesh, bool render)
	{
		if ((bool)mesh)
		{
			bool flag = GetGrabbingOwner()?.GetPhysgun().IsProtected() ?? false;
			mesh.renderingLayerMask &= ~(_physgunSafeMask | _physgunMask);
			if (render)
			{
				mesh.renderingLayerMask |= (flag ? _physgunSafeMask : _physgunMask);
			}
		}
	}

	public bool IsPhysOwner(entity_player ply)
	{
		if ((bool)ply)
		{
			return _grabbingOwnerId.Value == ply.GetPlayerID();
		}
		return false;
	}

	[Rpc(SendTo.SpecifiedInParams, InvokePermission = RpcInvokePermission.Everyone)]
	private void SetGrabbingRPC(bool grabbing, RpcParams param)
	{
		NetworkManager networkManager = base.NetworkManager;
		if ((object)networkManager == null || !networkManager.IsListening)
		{
			Debug.LogError("Rpc methods can only be invoked after starting the NetworkManager!");
			return;
		}
		if (__rpc_exec_stage != __RpcExecStage.Execute)
		{
			RpcParams rpcParams = param;
			RpcAttribute.RpcAttributeParams attributeParams = new RpcAttribute.RpcAttributeParams
			{
				InvokePermission = RpcInvokePermission.Everyone
			};
			FastBufferWriter bufferWriter = __beginSendRpc(2068018132u, rpcParams, attributeParams, SendTo.SpecifiedInParams, RpcDelivery.Reliable);
			bufferWriter.WriteValueSafe(in grabbing, default(FastBufferWriter.ForPrimitives));
			__endSendRpc(ref bufferWriter, 2068018132u, param, attributeParams, SendTo.SpecifiedInParams, RpcDelivery.Reliable);
		}
		if (__rpc_exec_stage != __RpcExecStage.Execute)
		{
			return;
		}
		__rpc_exec_stage = __RpcExecStage.Send;
		Player playerByConnection = MonoController<PlayerController>.Instance.GetPlayerByConnection(param.Receive.SenderClientId);
		if (playerByConnection != null && (bool)playerByConnection.player)
		{
			if (grabbing)
			{
				SetGrabbingOwner(playerByConnection.player);
			}
			else if (IsPhysOwner(playerByConnection.player))
			{
				CancelGrabbing();
			}
		}
	}

	[Server]
	protected virtual bool SetGrabbingOwner(entity_player ply)
	{
		if (!base.IsServer)
		{
			throw new UnityException("SetGrabbingOwner can only be called on the server.");
		}
		if (!ply || !CanGrab())
		{
			return false;
		}
		if (base.OwnerClientId != ply.GetConnectionID())
		{
			base.NetworkObject.ChangeOwnership(ply.GetConnectionID());
		}
		LOCK_TYPE value = _locked.Value;
		if (value == LOCK_TYPE.SOFT || value == LOCK_TYPE.SOFT_FROZEN)
		{
			SetLocked(LOCK_TYPE.NONE);
		}
		SetGrabbingOwner(ply.GetPlayerID());
		OnGrabbed.Invoke(ply, param2: true, param3: true);
		return true;
	}

	public virtual bool CanGrab()
	{
		return !IsLocked();
	}

	[Server]
	protected virtual void CancelGrabbing()
	{
		if (base.IsServer && _grabbingOwnerId.Value != byte.MaxValue)
		{
			SetGrabbingOwner(byte.MaxValue);
			_lastLetgoTime = Time.time;
			OnGrabbed.Invoke(null, param2: false, param3: true);
		}
	}

	[Server]
	protected virtual void SetGrabbingOwner(byte id)
	{
		if (!base.IsServer)
		{
			throw new UnityException("SetOwnerID can only be called on the server.");
		}
		_lastGrabbingOwnerId = _grabbingOwnerId.Value;
		_grabbingOwnerId.SetSpawnValue(id);
	}

	private void OnPlayerRemove(entity_player ply, bool server)
	{
		if (server && IsPhysOwner(ply))
		{
			CancelGrabbing();
		}
	}

	protected override void __initializeVariables()
	{
		if (_locked == null)
		{
			throw new Exception("entity_phys._locked cannot be null. All NetworkVariableBase instances must be initialized.");
		}
		_locked.Initialize(this);
		__nameNetworkVariable(_locked, "_locked");
		NetworkVariableFields.Add(_locked);
		if (_grabbingOwnerId == null)
		{
			throw new Exception("entity_phys._grabbingOwnerId cannot be null. All NetworkVariableBase instances must be initialized.");
		}
		_grabbingOwnerId.Initialize(this);
		__nameNetworkVariable(_grabbingOwnerId, "_grabbingOwnerId");
		NetworkVariableFields.Add(_grabbingOwnerId);
		base.__initializeVariables();
	}

	protected override void __initializeRpcs()
	{
		__registerRpc(1909066713u, __rpc_handler_1909066713, "DestroyRPC", RpcInvokePermission.Everyone);
		__registerRpc(2068018132u, __rpc_handler_2068018132, "SetGrabbingRPC", RpcInvokePermission.Everyone);
		base.__initializeRpcs();
	}

	private static void __rpc_handler_1909066713(NetworkBehaviour target, FastBufferReader reader, __RpcParams rpcParams)
	{
		NetworkManager networkManager = target.NetworkManager;
		if ((object)networkManager != null && networkManager.IsListening)
		{
			target.__rpc_exec_stage = __RpcExecStage.Execute;
			((entity_phys)target).DestroyRPC();
			target.__rpc_exec_stage = __RpcExecStage.Send;
		}
	}

	private static void __rpc_handler_2068018132(NetworkBehaviour target, FastBufferReader reader, __RpcParams rpcParams)
	{
		NetworkManager networkManager = target.NetworkManager;
		if ((object)networkManager != null && networkManager.IsListening)
		{
			reader.ReadValueSafe(out bool value, default(FastBufferWriter.ForPrimitives));
			RpcParams ext = rpcParams.Ext;
			target.__rpc_exec_stage = __RpcExecStage.Execute;
			((entity_phys)target).SetGrabbingRPC(value, ext);
			target.__rpc_exec_stage = __RpcExecStage.Send;
		}
	}

	protected internal override string __getTypeName()
	{
		return "entity_phys";
	}
}
