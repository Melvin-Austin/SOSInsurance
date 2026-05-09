using Unity.Netcode;
using UnityEngine;

namespace HyenaQuest;

public class entity_item_dsafe : entity_item_pickable
{
	[Rpc(SendTo.Server)]
	public void OnUseItemRPC()
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
			FastBufferWriter bufferWriter = __beginSendRpc(3338047333u, rpcParams, attributeParams, SendTo.Server, RpcDelivery.Reliable);
			__endSendRpc(ref bufferWriter, 3338047333u, rpcParams, attributeParams, SendTo.Server, RpcDelivery.Reliable);
		}
		if (__rpc_exec_stage != __RpcExecStage.Execute)
		{
			return;
		}
		__rpc_exec_stage = __RpcExecStage.Send;
		Player inventoryOwner = GetInventoryOwner();
		if (inventoryOwner == null || !inventoryOwner.player)
		{
			return;
		}
		IngameController instance = NetController<IngameController>.Instance;
		if ((object)instance != null && instance.Status() == INGAME_STATUS.PLAYING)
		{
			ulong connectionID = inventoryOwner.player.GetConnectionID();
			if (connectionID == inventoryOwner.connectionID)
			{
				NetController<StatsController>.Instance?.UnlockAchievementSV(STEAM_ACHIEVEMENTS.ACHIEVEMENT_CHEAT_DEATH, connectionID);
				base.NetworkObject.Despawn();
			}
		}
	}

	[Server]
	public override void Destroy()
	{
		if (!base.IsServer)
		{
			throw new UnityException("Destroy can only be called on the server.");
		}
		if (!HasOwner())
		{
			base.Destroy();
		}
	}

	public override string GetID()
	{
		return "item_dsafe";
	}

	protected override void __initializeVariables()
	{
		base.__initializeVariables();
	}

	protected override void __initializeRpcs()
	{
		__registerRpc(3338047333u, __rpc_handler_3338047333, "OnUseItemRPC", RpcInvokePermission.Everyone);
		base.__initializeRpcs();
	}

	private static void __rpc_handler_3338047333(NetworkBehaviour target, FastBufferReader reader, __RpcParams rpcParams)
	{
		NetworkManager networkManager = target.NetworkManager;
		if ((object)networkManager != null && networkManager.IsListening)
		{
			target.__rpc_exec_stage = __RpcExecStage.Execute;
			((entity_item_dsafe)target).OnUseItemRPC();
			target.__rpc_exec_stage = __RpcExecStage.Send;
		}
	}

	protected internal override string __getTypeName()
	{
		return "entity_item_dsafe";
	}
}
