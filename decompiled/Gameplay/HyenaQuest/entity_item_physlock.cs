using Unity.Netcode;
using UnityEngine;

namespace HyenaQuest;

public class entity_item_physlock : entity_item_pickable
{
	private entity_phys _frozenPhys;

	private float _cooldown;

	public override void OnUse(entity_player ply, Collider col, bool pressing)
	{
		if ((bool)ply && pressing && !(Time.time < _cooldown))
		{
			entity_player_physgun physgun = ply.GetPhysgun();
			if (!physgun || !physgun.IsGrabbing())
			{
				NetController<NotificationController>.Instance.CreateNotification(new NotificationData
				{
					id = "physlock-error",
					text = "ingame.ui.notification.physlock.no-phys",
					duration = 2f,
					soundEffect = "Ingame/Entities/Terminal/142608__autistic-lucario__error.ogg",
					soundVolume = 0.25f
				});
			}
			else
			{
				_cooldown = Time.time + 0.5f;
				FreezePropRPC(ply.GetPlayerID());
			}
		}
	}

	public override string GetID()
	{
		return "item_physlock";
	}

	public override void OnNetworkDespawn()
	{
		base.OnNetworkDespawn();
		if (base.IsServer)
		{
			ResetFrozen();
		}
	}

	protected override void InternalChangeOwner(byte newOwner, bool server)
	{
		base.InternalChangeOwner(newOwner, server);
		if (server)
		{
			ResetFrozen();
		}
	}

	[Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Owner)]
	private void FreezePropRPC(byte playerID)
	{
		NetworkManager networkManager = base.NetworkManager;
		if ((object)networkManager == null || !networkManager.IsListening)
		{
			Debug.LogError("Rpc methods can only be invoked after starting the NetworkManager!");
			return;
		}
		if (__rpc_exec_stage != __RpcExecStage.Execute)
		{
			RpcAttribute.RpcAttributeParams attributeParams = new RpcAttribute.RpcAttributeParams
			{
				InvokePermission = RpcInvokePermission.Owner
			};
			RpcParams rpcParams = default(RpcParams);
			FastBufferWriter bufferWriter = __beginSendRpc(1890965422u, rpcParams, attributeParams, SendTo.Server, RpcDelivery.Reliable);
			bufferWriter.WriteValueSafe(in playerID, default(FastBufferWriter.ForPrimitives));
			__endSendRpc(ref bufferWriter, 1890965422u, rpcParams, attributeParams, SendTo.Server, RpcDelivery.Reliable);
		}
		if (__rpc_exec_stage != __RpcExecStage.Execute)
		{
			return;
		}
		__rpc_exec_stage = __RpcExecStage.Send;
		if (!base.IsServer)
		{
			throw new UnityException("Not Server");
		}
		Player player = MonoController<PlayerController>.Instance.GetPlayer(playerID);
		if (!player.player)
		{
			return;
		}
		entity_player_physgun physgun = player.player.GetPhysgun();
		if (!physgun)
		{
			return;
		}
		entity_phys grabbingObject = physgun.GetGrabbingObject();
		if ((bool)grabbingObject && grabbingObject.GetLockType() == LOCK_TYPE.NONE)
		{
			if ((bool)_frozenPhys)
			{
				_frozenPhys.SetLocked(LOCK_TYPE.NONE);
			}
			_frozenPhys = grabbingObject;
			_frozenPhys.SetLocked(LOCK_TYPE.SOFT_FROZEN);
		}
	}

	[Server]
	private void ResetFrozen()
	{
		if (!base.IsServer)
		{
			throw new UnityException("Not Server");
		}
		if ((bool)_frozenPhys)
		{
			_frozenPhys.SetLocked(LOCK_TYPE.NONE);
		}
	}

	protected override void __initializeVariables()
	{
		base.__initializeVariables();
	}

	protected override void __initializeRpcs()
	{
		__registerRpc(1890965422u, __rpc_handler_1890965422, "FreezePropRPC", RpcInvokePermission.Owner);
		base.__initializeRpcs();
	}

	private static void __rpc_handler_1890965422(NetworkBehaviour target, FastBufferReader reader, __RpcParams rpcParams)
	{
		NetworkManager networkManager = target.NetworkManager;
		if ((object)networkManager != null && networkManager.IsListening)
		{
			reader.ReadValueSafe(out byte value, default(FastBufferWriter.ForPrimitives));
			target.__rpc_exec_stage = __RpcExecStage.Execute;
			((entity_item_physlock)target).FreezePropRPC(value);
			target.__rpc_exec_stage = __RpcExecStage.Send;
		}
	}

	protected internal override string __getTypeName()
	{
		return "entity_item_physlock";
	}
}
