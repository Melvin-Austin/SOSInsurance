using Unity.Netcode;
using UnityEngine;

namespace HyenaQuest;

public class entity_item_grenade : entity_item_pickable
{
	public GameObject grenadePrefab;

	private bool _used;

	public override string GetID()
	{
		return "item_grenade";
	}

	[Client]
	public override void OnUse(entity_player ply, Collider obj, bool pressing)
	{
		if ((bool)ply && !_used && pressing)
		{
			if (!NetController<IngameController>.Instance)
			{
				throw new UnityException("Missing IngameController");
			}
			IngameController instance = NetController<IngameController>.Instance;
			if ((object)instance == null || instance.IsShipArea(ply))
			{
				NetController<NotificationController>.Instance?.CreateNotification(new NotificationData
				{
					id = "item-grenade-cannot-use",
					text = "ingame.ui.notification.reject-use",
					duration = 2f,
					soundEffect = "Ingame/Entities/Terminal/142608__autistic-lucario__error.ogg",
					soundVolume = 0.05f
				});
			}
			else
			{
				ThrowGrenadeRPC();
			}
		}
	}

	protected override void Init()
	{
		base.Init();
		if (!grenadePrefab)
		{
			throw new UnityException("entity_item_grenade requires grenadePrefab");
		}
	}

	[Rpc(SendTo.Server)]
	private void ThrowGrenadeRPC()
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
			FastBufferWriter bufferWriter = __beginSendRpc(1684124540u, rpcParams, attributeParams, SendTo.Server, RpcDelivery.Reliable);
			__endSendRpc(ref bufferWriter, 1684124540u, rpcParams, attributeParams, SendTo.Server, RpcDelivery.Reliable);
		}
		if (__rpc_exec_stage != __RpcExecStage.Execute)
		{
			return;
		}
		__rpc_exec_stage = __RpcExecStage.Send;
		if (_used)
		{
			return;
		}
		entity_player grabbingOwner = GetGrabbingOwner();
		if ((bool)grabbingOwner)
		{
			GameObject obj = Object.Instantiate(grenadePrefab, base.transform.position + grabbingOwner.transform.forward * 0.1f, base.transform.rotation);
			if (!obj)
			{
				throw new UnityException("entity_item_grenade requires grenadePrefab");
			}
			entity_phys component = obj.GetComponent<entity_phys>();
			if (!component)
			{
				throw new UnityException("entity_item_grenade requires entity_phys component on prefab");
			}
			component.NetworkObject.Spawn();
			Rigidbody component2 = component.GetComponent<Rigidbody>();
			if (!component2)
			{
				throw new UnityException("entity_item_grenade requires Rigidbody component on prefab");
			}
			component2.AddForce(grabbingOwner.transform.forward * 30f, ForceMode.Impulse);
			NetController<SoundController>.Instance.Play3DSound("Ingame/Items/Grenade/pin.ogg", base.transform.position, new AudioData
			{
				pitch = Random.Range(0.8f, 1.2f),
				distance = 2f,
				volume = 0.4f
			}, broadcast: true);
			_used = true;
			Destroy();
		}
	}

	protected override void __initializeVariables()
	{
		base.__initializeVariables();
	}

	protected override void __initializeRpcs()
	{
		__registerRpc(1684124540u, __rpc_handler_1684124540, "ThrowGrenadeRPC", RpcInvokePermission.Everyone);
		base.__initializeRpcs();
	}

	private static void __rpc_handler_1684124540(NetworkBehaviour target, FastBufferReader reader, __RpcParams rpcParams)
	{
		NetworkManager networkManager = target.NetworkManager;
		if ((object)networkManager != null && networkManager.IsListening)
		{
			target.__rpc_exec_stage = __RpcExecStage.Execute;
			((entity_item_grenade)target).ThrowGrenadeRPC();
			target.__rpc_exec_stage = __RpcExecStage.Send;
		}
	}

	protected internal override string __getTypeName()
	{
		return "entity_item_grenade";
	}
}
