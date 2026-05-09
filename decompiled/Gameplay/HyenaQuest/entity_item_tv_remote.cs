using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Video;

namespace HyenaQuest;

public class entity_item_tv_remote : entity_phys_usable
{
	public List<VideoClip> videos = new List<VideoClip>();

	public entity_tv tv;

	private readonly NetVar<byte> _channel = new NetVar<byte>(byte.MaxValue, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

	protected override void Init()
	{
		base.Init();
		if (!tv)
		{
			throw new UnityException("entity_item_tv_remote requires a tv component");
		}
	}

	[Client]
	public override InteractionData InteractionSelector(Collider obj)
	{
		return new InteractionData(Interaction.INTERACT, _renderers, (_channel.Value == byte.MaxValue) ? "ingame.ui.hints.grab" : "ingame.ui.hints.off");
	}

	[Client]
	public override void OnUse(entity_player ply)
	{
		if ((bool)ply && _channel.Value != byte.MaxValue)
		{
			RequestVideoRPC(byte.MaxValue);
		}
	}

	[Rpc(SendTo.Owner, InvokePermission = RpcInvokePermission.Everyone)]
	public void RequestVideoRPC(byte videoIndex)
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
			FastBufferWriter bufferWriter = __beginSendRpc(1838591887u, rpcParams, attributeParams, SendTo.Owner, RpcDelivery.Reliable);
			bufferWriter.WriteValueSafe(in videoIndex, default(FastBufferWriter.ForPrimitives));
			__endSendRpc(ref bufferWriter, 1838591887u, rpcParams, attributeParams, SendTo.Owner, RpcDelivery.Reliable);
		}
		if (__rpc_exec_stage == __RpcExecStage.Execute)
		{
			__rpc_exec_stage = __RpcExecStage.Send;
			_channel.Value = videoIndex;
		}
	}

	protected override void OnNetworkPostSpawn()
	{
		base.OnNetworkPostSpawn();
		if (!base.IsClient)
		{
			return;
		}
		_channel.RegisterOnValueChanged(delegate(byte oldValue, byte newValue)
		{
			if (oldValue != newValue && (bool)tv)
			{
				bool flag = newValue != byte.MaxValue;
				if (flag)
				{
					tv.SetVideoClip(videos[newValue % videos.Count]);
				}
				tv.SetPlaying(flag, newValue < 2);
			}
		});
	}

	public override void OnNetworkPreDespawn()
	{
		base.OnNetworkPreDespawn();
		if (base.IsClient)
		{
			_channel.OnValueChanged = null;
		}
	}

	public override void OnNetworkSpawn()
	{
		base.OnNetworkSpawn();
		if (base.IsServer)
		{
			if (!NetController<IngameController>.Instance)
			{
				throw new UnityException("IngameController is not initialized");
			}
			NetController<IngameController>.Instance.OnStatusUpdated += new Action<INGAME_STATUS, bool>(OnStatusUpdated);
			tv.onVideoEnd += new Action(OnVideoEnd);
		}
	}

	private void OnVideoEnd()
	{
		if (base.IsServer)
		{
			RequestVideoRPC(byte.MaxValue);
		}
	}

	public override void OnNetworkDespawn()
	{
		base.OnNetworkDespawn();
		if (base.IsServer)
		{
			if ((bool)NetController<IngameController>.Instance)
			{
				NetController<IngameController>.Instance.OnStatusUpdated -= new Action<INGAME_STATUS, bool>(OnStatusUpdated);
			}
			if ((bool)tv)
			{
				tv.onVideoEnd -= new Action(OnVideoEnd);
			}
		}
	}

	private void OnStatusUpdated(INGAME_STATUS status, bool server)
	{
		if (server && _channel.Value <= 1)
		{
			OnVideoEnd();
		}
	}

	protected override void OnCollision(Collision col)
	{
		if (base.IsOwner && col != null && (bool)col.gameObject && !(col.relativeVelocity.magnitude < 8f))
		{
			_channel.Value = (byte)new List<int>(from i in Enumerable.Range(2, videos.Count - 2)
				where i != _channel.Value
				select i).OrderBy((int _) => UnityEngine.Random.value).FirstOrDefault();
		}
	}

	protected override void __initializeVariables()
	{
		if (_channel == null)
		{
			throw new Exception("entity_item_tv_remote._channel cannot be null. All NetworkVariableBase instances must be initialized.");
		}
		_channel.Initialize(this);
		__nameNetworkVariable(_channel, "_channel");
		NetworkVariableFields.Add(_channel);
		base.__initializeVariables();
	}

	protected override void __initializeRpcs()
	{
		__registerRpc(1838591887u, __rpc_handler_1838591887, "RequestVideoRPC", RpcInvokePermission.Everyone);
		base.__initializeRpcs();
	}

	private static void __rpc_handler_1838591887(NetworkBehaviour target, FastBufferReader reader, __RpcParams rpcParams)
	{
		NetworkManager networkManager = target.NetworkManager;
		if ((object)networkManager != null && networkManager.IsListening)
		{
			reader.ReadValueSafe(out byte value, default(FastBufferWriter.ForPrimitives));
			target.__rpc_exec_stage = __RpcExecStage.Execute;
			((entity_item_tv_remote)target).RequestVideoRPC(value);
			target.__rpc_exec_stage = __RpcExecStage.Send;
		}
	}

	protected internal override string __getTypeName()
	{
		return "entity_item_tv_remote";
	}
}
