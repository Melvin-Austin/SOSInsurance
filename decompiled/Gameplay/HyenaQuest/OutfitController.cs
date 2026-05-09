using System;
using Unity.Netcode;
using UnityEngine;

namespace HyenaQuest;

[RequireComponent(typeof(NetworkObject))]
public class OutfitController : NetController<OutfitController>
{
	public entity_button startOutfit;

	public GameObject shopAudio;

	public Transform insidePos;

	public Transform outsidePos;

	public Transform cameraPos;

	public new void Awake()
	{
		base.Awake();
		if (!startOutfit)
		{
			throw new UnityException("Missing entity_button startOutfit");
		}
		if (!insidePos)
		{
			throw new UnityException("Missing Transform insidePos");
		}
		if (!outsidePos)
		{
			throw new UnityException("Missing Transform outsidePos");
		}
		if (!cameraPos)
		{
			throw new UnityException("Missing Transform cameraPos");
		}
		if (!shopAudio)
		{
			throw new UnityException("Missing shopAudio");
		}
	}

	public override void OnNetworkSpawn()
	{
		base.OnNetworkSpawn();
		CoreController.WaitFor(delegate(IngameController ingame)
		{
			ingame.OnStatusUpdated += new Action<INGAME_STATUS, bool>(OnStatusUpdated);
		});
		if (base.IsServer)
		{
			startOutfit.OnUSE += new Action<entity_player>(OnUSE);
		}
	}

	[Server]
	private void OnUSE(entity_player ply)
	{
		if (!base.IsServer)
		{
			throw new UnityException("Server only");
		}
		if (!startOutfit.IsLocked() && (bool)ply && !ply.IsDead())
		{
			RequestOutfitChangeRPC(base.RpcTarget.Single(ply.GetConnectionID(), RpcTargetUse.Temp));
		}
	}

	[Rpc(SendTo.SpecifiedInParams)]
	private void RequestOutfitChangeRPC(RpcParams target)
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
			FastBufferWriter bufferWriter = __beginSendRpc(1817913660u, target, attributeParams, SendTo.SpecifiedInParams, RpcDelivery.Reliable);
			__endSendRpc(ref bufferWriter, 1817913660u, target, attributeParams, SendTo.SpecifiedInParams, RpcDelivery.Reliable);
		}
		if (__rpc_exec_stage == __RpcExecStage.Execute)
		{
			__rpc_exec_stage = __RpcExecStage.Send;
			if (!PlayerController.LOCAL)
			{
				throw new UnityException("Local player not found");
			}
			PlayerController.LOCAL.SetInOutfitMode(set: true);
		}
	}

	public override void OnNetworkDespawn()
	{
		base.OnNetworkDespawn();
		if ((bool)NetController<IngameController>.Instance)
		{
			NetController<IngameController>.Instance.OnStatusUpdated -= new Action<INGAME_STATUS, bool>(OnStatusUpdated);
		}
		if (base.IsServer && (bool)startOutfit)
		{
			startOutfit.OnUSE -= new Action<entity_player>(OnUSE);
		}
	}

	private void OnStatusUpdated(INGAME_STATUS status, bool server)
	{
		if ((bool)shopAudio && (bool)startOutfit && (bool)PlayerController.LOCAL)
		{
			bool flag = status == INGAME_STATUS.ROUND_END || status == INGAME_STATUS.GAMEOVER || status == INGAME_STATUS.PLAYING;
			shopAudio.SetActive(!flag);
			if (server)
			{
				startOutfit.SetLocked(flag);
			}
			if (flag)
			{
				PlayerController.LOCAL.SetInOutfitMode(set: false);
			}
		}
	}

	protected override void __initializeVariables()
	{
		base.__initializeVariables();
	}

	protected override void __initializeRpcs()
	{
		__registerRpc(1817913660u, __rpc_handler_1817913660, "RequestOutfitChangeRPC", RpcInvokePermission.Everyone);
		base.__initializeRpcs();
	}

	private static void __rpc_handler_1817913660(NetworkBehaviour target, FastBufferReader reader, __RpcParams rpcParams)
	{
		NetworkManager networkManager = target.NetworkManager;
		if ((object)networkManager != null && networkManager.IsListening)
		{
			RpcParams ext = rpcParams.Ext;
			target.__rpc_exec_stage = __RpcExecStage.Execute;
			((OutfitController)target).RequestOutfitChangeRPC(ext);
			target.__rpc_exec_stage = __RpcExecStage.Send;
		}
	}

	protected internal override string __getTypeName()
	{
		return "OutfitController";
	}
}
