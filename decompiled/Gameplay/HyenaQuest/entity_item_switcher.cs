using Unity.Netcode;
using UnityEngine;
using ZLinq;
using ZLinq.Linq;

namespace HyenaQuest;

public class entity_item_switcher : entity_item_pickable
{
	private float _useCooldown;

	private bool _used;

	[Client]
	public override void OnUse(entity_player ply, Collider obj, bool pressing)
	{
		if ((bool)ply && pressing && !(Time.time < _useCooldown))
		{
			_useCooldown = Time.time + 2f;
			SwitchRPC();
		}
	}

	public override string GetID()
	{
		return "item_switcher";
	}

	[Rpc(SendTo.Server)]
	private void SwitchRPC()
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
			FastBufferWriter bufferWriter = __beginSendRpc(2848426202u, rpcParams, attributeParams, SendTo.Server, RpcDelivery.Reliable);
			__endSendRpc(ref bufferWriter, 2848426202u, rpcParams, attributeParams, SendTo.Server, RpcDelivery.Reliable);
		}
		if (__rpc_exec_stage != __RpcExecStage.Execute)
		{
			return;
		}
		__rpc_exec_stage = __RpcExecStage.Send;
		if (!_used)
		{
			if (!MonoController<PlayerController>.Instance)
			{
				throw new UnityException("Missing PlayerController");
			}
			Player inventoryOwner = GetInventoryOwner();
			if (inventoryOwner == null || !inventoryOwner.player)
			{
				throw new UnityException("Invalid owner");
			}
			ValueEnumerable<ListWhere<entity_player>, entity_player> source = from pl in MonoController<PlayerController>.Instance.GetAlivePlayers(new entity_player[1] { inventoryOwner.player }).AsValueEnumerable()
				where (bool)pl && !pl.InOutfitMode()
				select pl;
			int num = source.Count();
			if (num < 1)
			{
				NetController<SoundController>.Instance?.Play3DSound("Ingame/Entities/Terminal/142608__autistic-lucario__error.ogg", base.transform.position, new AudioData
				{
					distance = 4f,
					volume = 0.5f,
					pitch = Random.Range(0.8f, 1.2f)
				}, broadcast: true);
				return;
			}
			NetController<SoundController>.Instance?.Play3DSound("Ingame/Items/Switch/switch.ogg", base.transform.position, new AudioData
			{
				distance = 4f,
				pitch = 0.8f,
				parent = inventoryOwner.player
			}, broadcast: true);
			_used = true;
			entity_player obj = source.ElementAt(Random.Range(0, num));
			Vector3 position = obj.transform.position;
			obj.SetPositionRPC(inventoryOwner.player.transform.position);
			inventoryOwner.player.SetPositionRPC(position);
			Destroy();
		}
	}

	protected override void __initializeVariables()
	{
		base.__initializeVariables();
	}

	protected override void __initializeRpcs()
	{
		__registerRpc(2848426202u, __rpc_handler_2848426202, "SwitchRPC", RpcInvokePermission.Everyone);
		base.__initializeRpcs();
	}

	private static void __rpc_handler_2848426202(NetworkBehaviour target, FastBufferReader reader, __RpcParams rpcParams)
	{
		NetworkManager networkManager = target.NetworkManager;
		if ((object)networkManager != null && networkManager.IsListening)
		{
			target.__rpc_exec_stage = __RpcExecStage.Execute;
			((entity_item_switcher)target).SwitchRPC();
			target.__rpc_exec_stage = __RpcExecStage.Send;
		}
	}

	protected internal override string __getTypeName()
	{
		return "entity_item_switcher";
	}
}
