using System;
using Unity.Netcode;
using UnityEngine;

namespace HyenaQuest;

[RequireComponent(typeof(NetworkObject))]
[RequireComponent(typeof(Collider))]
public class entity_teleportal : NetworkBehaviour
{
	public NetVar<byte> linkedPortalID = new NetVar<byte>(byte.MaxValue);

	public NetVar<byte> portalID = new NetVar<byte>(byte.MaxValue);

	protected Collider _area;

	protected entity_teleportal _linkedPortal;

	protected readonly NetVar<bool> _active = new NetVar<bool>(value: true);

	public void Awake()
	{
		_area = GetComponent<Collider>();
		if (!_area)
		{
			throw new UnityException("Area collider missing!");
		}
	}

	protected override void OnNetworkPostSpawn()
	{
		base.OnNetworkPostSpawn();
		if (base.IsClient)
		{
			linkedPortalID.RegisterOnValueChanged(delegate
			{
				LinkPortalUpdate();
			});
		}
	}

	public override void OnNetworkPreDespawn()
	{
		base.OnNetworkPreDespawn();
		if (base.IsClient)
		{
			linkedPortalID.OnValueChanged = null;
		}
	}

	[Client]
	private void LinkPortalUpdate()
	{
		if (linkedPortalID.Value == byte.MaxValue)
		{
			_linkedPortal = null;
			return;
		}
		entity_teleportal[] array = UnityEngine.Object.FindObjectsByType<entity_teleportal>(FindObjectsSortMode.None);
		if (array == null || array.Length <= 0)
		{
			throw new UnityException("No portals found!");
		}
		entity_teleportal[] array2 = array;
		foreach (entity_teleportal entity_teleportal2 in array2)
		{
			byte iD = entity_teleportal2.GetID();
			if (iD != byte.MaxValue && iD == linkedPortalID.Value)
			{
				_linkedPortal = entity_teleportal2;
				Debug.Log($"Linked portal {linkedPortalID.Value} to {GetID()}");
				return;
			}
		}
		Debug.LogWarning($"Portal with ID {linkedPortalID.Value} not found! Late joiner?");
	}

	[Server]
	public void SetID(byte id)
	{
		portalID.SetSpawnValue(id);
	}

	public byte GetID()
	{
		return portalID.Value;
	}

	[Server]
	public void SetPortal(entity_teleportal portal)
	{
		if (!portal)
		{
			linkedPortalID.SetSpawnValue(byte.MaxValue);
			return;
		}
		byte iD = portal.GetID();
		if (iD == byte.MaxValue)
		{
			throw new UnityException($"Invalid portal ID {iD}!");
		}
		linkedPortalID.SetSpawnValue(iD);
	}

	public void FixedUpdate()
	{
		if (base.IsClient && ShouldTeleport())
		{
			Teleport();
		}
	}

	[Client]
	protected virtual bool ShouldTeleport()
	{
		if (!_active.Value || !_linkedPortal)
		{
			return false;
		}
		return util_teleportal.ShouldTeleport(_area.bounds);
	}

	[Client]
	protected virtual void Teleport()
	{
		if ((bool)PlayerController.LOCAL)
		{
			byte playerID = PlayerController.LOCAL.GetPlayerID();
			util_teleportal.TeleportLocalClient(base.transform, _linkedPortal.transform);
			OnPlayerTeleportedRPC(playerID);
		}
	}

	[Rpc(SendTo.Server)]
	protected virtual void OnPlayerTeleportedRPC(byte playerID)
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
			FastBufferWriter bufferWriter = __beginSendRpc(802076804u, rpcParams, attributeParams, SendTo.Server, RpcDelivery.Reliable);
			bufferWriter.WriteValueSafe(in playerID, default(FastBufferWriter.ForPrimitives));
			__endSendRpc(ref bufferWriter, 802076804u, rpcParams, attributeParams, SendTo.Server, RpcDelivery.Reliable);
		}
		if (__rpc_exec_stage == __RpcExecStage.Execute)
		{
			__rpc_exec_stage = __RpcExecStage.Send;
		}
	}

	protected override void __initializeVariables()
	{
		if (linkedPortalID == null)
		{
			throw new Exception("entity_teleportal.linkedPortalID cannot be null. All NetworkVariableBase instances must be initialized.");
		}
		linkedPortalID.Initialize(this);
		__nameNetworkVariable(linkedPortalID, "linkedPortalID");
		NetworkVariableFields.Add(linkedPortalID);
		if (portalID == null)
		{
			throw new Exception("entity_teleportal.portalID cannot be null. All NetworkVariableBase instances must be initialized.");
		}
		portalID.Initialize(this);
		__nameNetworkVariable(portalID, "portalID");
		NetworkVariableFields.Add(portalID);
		if (_active == null)
		{
			throw new Exception("entity_teleportal._active cannot be null. All NetworkVariableBase instances must be initialized.");
		}
		_active.Initialize(this);
		__nameNetworkVariable(_active, "_active");
		NetworkVariableFields.Add(_active);
		base.__initializeVariables();
	}

	protected override void __initializeRpcs()
	{
		__registerRpc(802076804u, __rpc_handler_802076804, "OnPlayerTeleportedRPC", RpcInvokePermission.Everyone);
		base.__initializeRpcs();
	}

	private static void __rpc_handler_802076804(NetworkBehaviour target, FastBufferReader reader, __RpcParams rpcParams)
	{
		NetworkManager networkManager = target.NetworkManager;
		if ((object)networkManager != null && networkManager.IsListening)
		{
			reader.ReadValueSafe(out byte value, default(FastBufferWriter.ForPrimitives));
			target.__rpc_exec_stage = __RpcExecStage.Execute;
			((entity_teleportal)target).OnPlayerTeleportedRPC(value);
			target.__rpc_exec_stage = __RpcExecStage.Send;
		}
	}

	protected internal override string __getTypeName()
	{
		return "entity_teleportal";
	}
}
