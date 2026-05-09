using System;
using PaintIn3D;
using Unity.Netcode;
using UnityEngine;

namespace HyenaQuest;

[RequireComponent(typeof(CwPaintableMeshTexture))]
[RequireComponent(typeof(NetworkObject))]
public class entity_paint_multiplayer : NetworkBehaviour
{
	private const string MESSAGE_CHANNEL_PREFIX = "PAINT-DATA-";

	private CwPaintableMeshTexture _paintable;

	private entity_button _clearButton;

	private util_net_picture _netPicture;

	private string _messageChannel;

	public void Awake()
	{
		_paintable = GetComponent<CwPaintableMeshTexture>();
		if (!_paintable)
		{
			throw new UnityException("entity_paint_multiplayer requires CwPaintableMeshTexture component");
		}
		_clearButton = GetComponentInChildren<entity_button>(includeInactive: true);
		if (!_clearButton)
		{
			throw new UnityException("entity_paint_multiplayer requires entity_button component");
		}
	}

	public override void OnNetworkSpawn()
	{
		base.OnNetworkSpawn();
		_messageChannel = "PAINT-DATA-" + base.NetworkObjectId;
		if (!base.IsServer)
		{
			_netPicture = new util_net_picture(this, _messageChannel, null, OnDataReceived);
			RequestPaintDataRPC(base.RpcTarget.Server);
			return;
		}
		_netPicture = new util_net_picture(this, _messageChannel, () => _paintable.GetPngData(), null, alwaysReloadOnRequest: true);
		CoreController.WaitFor(delegate(SettingsController settingsCtrl)
		{
			byte[] array = settingsCtrl.LoadPaintData();
			_paintable.LoadFromData(array);
			if (array != null && array.Length > 0)
			{
				_netPicture.PreloadData(array);
			}
		});
		_clearButton.OnUSE += new Action<entity_player>(OnUSE);
	}

	public override void OnNetworkDespawn()
	{
		base.OnNetworkDespawn();
		if (base.IsServer)
		{
			if ((bool)_paintable && (bool)MonoController<SettingsController>.Instance)
			{
				MonoController<SettingsController>.Instance.SavePaintData(_paintable.GetPngData());
			}
			if ((bool)_clearButton)
			{
				_clearButton.OnUSE -= new Action<entity_player>(OnUSE);
			}
		}
		_netPicture?.Dispose();
		_netPicture = null;
	}

	[Server]
	public void SetPaintData(string data)
	{
		if (!base.IsServer)
		{
			throw new UnityException("SetPaintData can only be called on the server");
		}
		if (string.IsNullOrEmpty(data))
		{
			throw new UnityException("Data cannot be empty, call clear instead");
		}
		_paintable.LoadFromData(Convert.FromBase64String(data));
		_netPicture?.Transmit(NETController.Instance.LocalClient.ClientId);
	}

	[Server]
	private void OnUSE(entity_player ply)
	{
		if (!base.IsServer)
		{
			throw new UnityException("OnUSE can only be called on the server");
		}
		if ((bool)_paintable && (bool)MonoController<SettingsController>.Instance)
		{
			_netPicture.MarkDirty();
			ResetPaintDataRPC();
		}
	}

	[Rpc(SendTo.ClientsAndHost)]
	private void ResetPaintDataRPC()
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
			FastBufferWriter bufferWriter = __beginSendRpc(3316154609u, rpcParams, attributeParams, SendTo.ClientsAndHost, RpcDelivery.Reliable);
			__endSendRpc(ref bufferWriter, 3316154609u, rpcParams, attributeParams, SendTo.ClientsAndHost, RpcDelivery.Reliable);
		}
		if (__rpc_exec_stage == __RpcExecStage.Execute)
		{
			__rpc_exec_stage = __RpcExecStage.Send;
			if (base.IsClient && (bool)_paintable)
			{
				_paintable.Clear();
			}
		}
	}

	[Rpc(SendTo.SpecifiedInParams)]
	private void RequestPaintDataRPC(RpcParams param)
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
			FastBufferWriter bufferWriter = __beginSendRpc(788476142u, rpcParams, attributeParams, SendTo.SpecifiedInParams, RpcDelivery.Reliable);
			__endSendRpc(ref bufferWriter, 788476142u, param, attributeParams, SendTo.SpecifiedInParams, RpcDelivery.Reliable);
		}
		if (__rpc_exec_stage == __RpcExecStage.Execute)
		{
			__rpc_exec_stage = __RpcExecStage.Send;
			if (!base.IsServer)
			{
				throw new UnityException("RequestPaintDataRPC can only be called on the server");
			}
			_netPicture?.AddRequest(param.Receive.SenderClientId);
		}
	}

	private void OnDataReceived(byte[] paintData)
	{
		if ((bool)_paintable)
		{
			_paintable.LoadFromData(paintData);
		}
	}

	protected override void __initializeVariables()
	{
		base.__initializeVariables();
	}

	protected override void __initializeRpcs()
	{
		__registerRpc(3316154609u, __rpc_handler_3316154609, "ResetPaintDataRPC", RpcInvokePermission.Everyone);
		__registerRpc(788476142u, __rpc_handler_788476142, "RequestPaintDataRPC", RpcInvokePermission.Everyone);
		base.__initializeRpcs();
	}

	private static void __rpc_handler_3316154609(NetworkBehaviour target, FastBufferReader reader, __RpcParams rpcParams)
	{
		NetworkManager networkManager = target.NetworkManager;
		if ((object)networkManager != null && networkManager.IsListening)
		{
			target.__rpc_exec_stage = __RpcExecStage.Execute;
			((entity_paint_multiplayer)target).ResetPaintDataRPC();
			target.__rpc_exec_stage = __RpcExecStage.Send;
		}
	}

	private static void __rpc_handler_788476142(NetworkBehaviour target, FastBufferReader reader, __RpcParams rpcParams)
	{
		NetworkManager networkManager = target.NetworkManager;
		if ((object)networkManager != null && networkManager.IsListening)
		{
			RpcParams ext = rpcParams.Ext;
			target.__rpc_exec_stage = __RpcExecStage.Execute;
			((entity_paint_multiplayer)target).RequestPaintDataRPC(ext);
			target.__rpc_exec_stage = __RpcExecStage.Send;
		}
	}

	protected internal override string __getTypeName()
	{
		return "entity_paint_multiplayer";
	}
}
