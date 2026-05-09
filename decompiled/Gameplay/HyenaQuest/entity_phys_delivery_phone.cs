using Unity.Netcode;
using UnityEngine;

namespace HyenaQuest;

public class entity_phys_delivery_phone : entity_prop_delivery
{
	private float _lastShake;

	protected override void OnCollision(Collision collision)
	{
		if (base.IsOwner && !IsBeingGrabbed() && !(Time.time < _lastShake) && !(collision.relativeVelocity.sqrMagnitude <= 4f))
		{
			_lastShake = Time.time + 0.1f;
			PhoneShakeRPC(base.RpcTarget.Server);
		}
	}

	[Rpc(SendTo.SpecifiedInParams)]
	private void PhoneShakeRPC(RpcParams param)
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
			RpcAttribute.RpcAttributeParams attributeParams = default(RpcAttribute.RpcAttributeParams);
			FastBufferWriter bufferWriter = __beginSendRpc(402161941u, rpcParams, attributeParams, SendTo.SpecifiedInParams, RpcDelivery.Reliable);
			__endSendRpc(ref bufferWriter, 402161941u, param, attributeParams, SendTo.SpecifiedInParams, RpcDelivery.Reliable);
		}
		if (__rpc_exec_stage == __RpcExecStage.Execute)
		{
			__rpc_exec_stage = __RpcExecStage.Send;
			if (!base.IsServer)
			{
				throw new UnityException("Server only");
			}
			if (param.Receive.SenderClientId != base.OwnerClientId)
			{
				throw new UnityException("Owner only");
			}
			if (!NetController<ShakeController>.Instance)
			{
				throw new UnityException("Missing ShakeController");
			}
			NetController<ShakeController>.Instance.Shake3DRPC(base.transform.position, ShakeMode.SHAKE_ALL, 0.1f, 0.05f);
		}
	}

	protected override void __initializeVariables()
	{
		base.__initializeVariables();
	}

	protected override void __initializeRpcs()
	{
		__registerRpc(402161941u, __rpc_handler_402161941, "PhoneShakeRPC", RpcInvokePermission.Everyone);
		base.__initializeRpcs();
	}

	private static void __rpc_handler_402161941(NetworkBehaviour target, FastBufferReader reader, __RpcParams rpcParams)
	{
		NetworkManager networkManager = target.NetworkManager;
		if ((object)networkManager != null && networkManager.IsListening)
		{
			RpcParams ext = rpcParams.Ext;
			target.__rpc_exec_stage = __RpcExecStage.Execute;
			((entity_phys_delivery_phone)target).PhoneShakeRPC(ext);
			target.__rpc_exec_stage = __RpcExecStage.Send;
		}
	}

	protected internal override string __getTypeName()
	{
		return "entity_phys_delivery_phone";
	}
}
