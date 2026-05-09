using System;
using Unity.Netcode;
using UnityEngine;

namespace HyenaQuest;

[DisallowMultipleComponent]
[DefaultExecutionOrder(-70)]
[RequireComponent(typeof(NetworkObject))]
public class LightController : NetController<LightController>
{
	public GameEvent<PowerGrid, LightCommand, bool> OnLightAreaCommand = new GameEvent<PowerGrid, LightCommand, bool>();

	[Server]
	public void ExecuteAllLightCommand(LightCommand command)
	{
		foreach (PowerGrid value in Enum.GetValues(typeof(PowerGrid)))
		{
			OnLightAreaCommand?.Invoke(value, command, param3: true);
		}
		ExecuteAllLightCommandRPC(command);
	}

	public override void OnNetworkSpawn()
	{
		base.OnNetworkSpawn();
		CoreController.WaitFor(delegate(PowerController powerCtrl)
		{
			powerCtrl.OnGridUpdate += new Action<PowerGrid, bool, bool>(OnGridUpdate);
			powerCtrl.OnGridWarning += new Action(GridWarning);
		});
	}

	public override void OnNetworkDespawn()
	{
		base.OnNetworkDespawn();
		if ((bool)NetController<PowerController>.Instance)
		{
			NetController<PowerController>.Instance.OnGridUpdate -= new Action<PowerGrid, bool, bool>(OnGridUpdate);
			NetController<PowerController>.Instance.OnGridWarning -= new Action(GridWarning);
		}
	}

	[Rpc(SendTo.ClientsAndHost)]
	private void ExecuteAllLightCommandRPC(LightCommand command)
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
			FastBufferWriter bufferWriter = __beginSendRpc(235229105u, rpcParams, attributeParams, SendTo.ClientsAndHost, RpcDelivery.Reliable);
			bufferWriter.WriteValueSafe(in command, default(FastBufferWriter.ForEnums));
			__endSendRpc(ref bufferWriter, 235229105u, rpcParams, attributeParams, SendTo.ClientsAndHost, RpcDelivery.Reliable);
		}
		if (__rpc_exec_stage != __RpcExecStage.Execute)
		{
			return;
		}
		__rpc_exec_stage = __RpcExecStage.Send;
		foreach (PowerGrid value in Enum.GetValues(typeof(PowerGrid)))
		{
			OnLightAreaCommand?.Invoke(value, command, param3: false);
		}
	}

	[Client]
	private void GridWarning()
	{
		foreach (PowerGrid value in Enum.GetValues(typeof(PowerGrid)))
		{
			OnLightAreaCommand?.Invoke(value, LightCommand.FLICKER, param3: false);
		}
	}

	private void OnGridUpdate(PowerGrid area, bool on, bool server)
	{
		OnLightAreaCommand?.Invoke(area, on ? LightCommand.ON : ((area != 0) ? LightCommand.FLICKER_OFF : LightCommand.OFF), server);
	}

	protected override void __initializeVariables()
	{
		base.__initializeVariables();
	}

	protected override void __initializeRpcs()
	{
		__registerRpc(235229105u, __rpc_handler_235229105, "ExecuteAllLightCommandRPC", RpcInvokePermission.Everyone);
		base.__initializeRpcs();
	}

	private static void __rpc_handler_235229105(NetworkBehaviour target, FastBufferReader reader, __RpcParams rpcParams)
	{
		NetworkManager networkManager = target.NetworkManager;
		if ((object)networkManager != null && networkManager.IsListening)
		{
			reader.ReadValueSafe(out LightCommand value, default(FastBufferWriter.ForEnums));
			target.__rpc_exec_stage = __RpcExecStage.Execute;
			((LightController)target).ExecuteAllLightCommandRPC(value);
			target.__rpc_exec_stage = __RpcExecStage.Send;
		}
	}

	protected internal override string __getTypeName()
	{
		return "LightController";
	}
}
