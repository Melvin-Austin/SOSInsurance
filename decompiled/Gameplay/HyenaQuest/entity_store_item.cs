using System.Collections;
using Unity.Netcode;
using UnityEngine;

namespace HyenaQuest;

public class entity_store_item : entity_phys_usable
{
	public GameObject itemPrefab;

	private bool _opened;

	[Client]
	public override void OnUse(entity_player ply)
	{
		if ((bool)ply)
		{
			OnUseRPC();
		}
	}

	[Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
	private void OnUseRPC()
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
				InvokePermission = RpcInvokePermission.Everyone
			};
			RpcParams rpcParams = default(RpcParams);
			FastBufferWriter bufferWriter = __beginSendRpc(3156543244u, rpcParams, attributeParams, SendTo.Server, RpcDelivery.Reliable);
			__endSendRpc(ref bufferWriter, 3156543244u, rpcParams, attributeParams, SendTo.Server, RpcDelivery.Reliable);
		}
		if (__rpc_exec_stage == __RpcExecStage.Execute)
		{
			__rpc_exec_stage = __RpcExecStage.Send;
			if (base.IsSpawned)
			{
				Open();
			}
		}
	}

	[Server]
	private void Open()
	{
		if (!base.IsServer)
		{
			throw new UnityException("Open called on client, but should be called on server!");
		}
		if (!_opened)
		{
			_opened = true;
			StartCoroutine(CreateItem());
		}
	}

	private IEnumerator CreateItem()
	{
		if (!itemPrefab)
		{
			throw new UnityException("entity_store_item requires itemPrefab to be set");
		}
		AsyncInstantiateOperation<GameObject> instantiateOperation = Object.InstantiateAsync(itemPrefab, base.transform.position, Quaternion.identity);
		yield return instantiateOperation;
		GameObject[] result = instantiateOperation.Result;
		GameObject obj = ((result != null) ? result[0] : null);
		if (!obj)
		{
			throw new UnityException("Failed to instantiate prop");
		}
		NetworkObject component = obj.GetComponent<NetworkObject>();
		if (!component)
		{
			throw new UnityException("NetworkObject not found on itemPrefab");
		}
		component.Spawn(destroyWithScene: true);
		NetController<EffectController>.Instance?.PlayEffectRPC(EffectType.CONFETTI_SPHERE, base.transform.position, new EffectSettings(30, playSound: true));
		NetController<EffectController>.Instance?.PlayEffectRPC(EffectType.SMOKE, base.transform.position, new EffectSettings(5, playSound: false));
		base.NetworkObject.Despawn();
	}

	protected override void __initializeVariables()
	{
		base.__initializeVariables();
	}

	protected override void __initializeRpcs()
	{
		__registerRpc(3156543244u, __rpc_handler_3156543244, "OnUseRPC", RpcInvokePermission.Everyone);
		base.__initializeRpcs();
	}

	private static void __rpc_handler_3156543244(NetworkBehaviour target, FastBufferReader reader, __RpcParams rpcParams)
	{
		NetworkManager networkManager = target.NetworkManager;
		if ((object)networkManager != null && networkManager.IsListening)
		{
			target.__rpc_exec_stage = __RpcExecStage.Execute;
			((entity_store_item)target).OnUseRPC();
			target.__rpc_exec_stage = __RpcExecStage.Send;
		}
	}

	protected internal override string __getTypeName()
	{
		return "entity_store_item";
	}
}
