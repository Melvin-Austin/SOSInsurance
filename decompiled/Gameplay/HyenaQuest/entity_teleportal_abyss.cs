using System;
using Unity.Netcode;
using UnityEngine;

namespace HyenaQuest;

public class entity_teleportal_abyss : entity_teleportal
{
	[Header("GameObject")]
	public entity_led led;

	public GameObject ambient;

	private Action<entity_teleportal_abyss, byte> _callback;

	private readonly NetVar<PortalOverride> _portalOverride = new NetVar<PortalOverride>();

	public new void Awake()
	{
		base.Awake();
		if (!led)
		{
			throw new UnityException("LED missing!");
		}
		if (!ambient)
		{
			throw new UnityException("Ambient light missing!");
		}
	}

	protected override void OnNetworkPostSpawn()
	{
		base.OnNetworkPostSpawn();
		if (base.IsClient)
		{
			_active.RegisterOnValueChanged(delegate(bool _, bool newValue)
			{
				led.SetActive(newValue);
				ambient.SetActive(newValue);
			});
		}
	}

	public override void OnNetworkPreDespawn()
	{
		base.OnNetworkPreDespawn();
		if (base.IsClient)
		{
			_active.OnValueChanged = null;
		}
	}

	[Server]
	public void OverridePortal(PortalOverride portal, Action<entity_teleportal_abyss, byte> callback)
	{
		if (base.IsSpawned)
		{
			_portalOverride.Value = portal;
			_callback = callback;
		}
	}

	[Client]
	protected override void Teleport()
	{
		if (_portalOverride.Value == null)
		{
			base.Teleport();
			return;
		}
		PlayerController.LOCAL.SetPosition(_portalOverride.Value.pos, Quaternion.Euler(_portalOverride.Value.angle));
		Physics.SyncTransforms();
		OnPlayerTeleportedRPC(PlayerController.LOCAL.GetPlayerID());
	}

	[Rpc(SendTo.Server)]
	protected override void OnPlayerTeleportedRPC(byte playerID)
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
			FastBufferWriter bufferWriter = __beginSendRpc(2347568881u, rpcParams, attributeParams, SendTo.Server, RpcDelivery.Reliable);
			bufferWriter.WriteValueSafe(in playerID, default(FastBufferWriter.ForPrimitives));
			__endSendRpc(ref bufferWriter, 2347568881u, rpcParams, attributeParams, SendTo.Server, RpcDelivery.Reliable);
		}
		if (__rpc_exec_stage == __RpcExecStage.Execute)
		{
			__rpc_exec_stage = __RpcExecStage.Send;
			_callback?.Invoke(this, playerID);
		}
	}

	protected override void __initializeVariables()
	{
		if (_portalOverride == null)
		{
			throw new Exception("entity_teleportal_abyss._portalOverride cannot be null. All NetworkVariableBase instances must be initialized.");
		}
		_portalOverride.Initialize(this);
		__nameNetworkVariable(_portalOverride, "_portalOverride");
		NetworkVariableFields.Add(_portalOverride);
		base.__initializeVariables();
	}

	protected override void __initializeRpcs()
	{
		__registerRpc(2347568881u, __rpc_handler_2347568881, "OnPlayerTeleportedRPC", RpcInvokePermission.Everyone);
		base.__initializeRpcs();
	}

	private static void __rpc_handler_2347568881(NetworkBehaviour target, FastBufferReader reader, __RpcParams rpcParams)
	{
		NetworkManager networkManager = target.NetworkManager;
		if ((object)networkManager != null && networkManager.IsListening)
		{
			reader.ReadValueSafe(out byte value, default(FastBufferWriter.ForPrimitives));
			target.__rpc_exec_stage = __RpcExecStage.Execute;
			((entity_teleportal_abyss)target).OnPlayerTeleportedRPC(value);
			target.__rpc_exec_stage = __RpcExecStage.Send;
		}
	}

	protected internal override string __getTypeName()
	{
		return "entity_teleportal_abyss";
	}
}
