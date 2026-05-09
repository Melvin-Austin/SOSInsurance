using System;
using Unity.Netcode;
using UnityEngine;

namespace HyenaQuest;

public class entity_item_lowgrav : entity_item_pickable
{
	public AudioSource audioSource;

	public Light lightSource;

	private float _cooldownTimer;

	private readonly NetVar<bool> _isActive = new NetVar<bool>(value: false);

	[Client]
	public override void OnUse(entity_player ply, Collider obj, bool pressing)
	{
		if ((bool)ply && !(Time.time < _cooldownTimer) && pressing)
		{
			_cooldownTimer = Time.time + 0.05f;
			ToggleRPC();
		}
	}

	public bool IsActive()
	{
		return _isActive.Value;
	}

	public override string GetID()
	{
		return "item_lowgrav";
	}

	protected override void Init()
	{
		base.Init();
		if (!lightSource)
		{
			throw new UnityException("Missing LightSource component!");
		}
		lightSource.enabled = false;
		if (!audioSource)
		{
			throw new UnityException("Missing AudioSource component!");
		}
		audioSource.Stop();
	}

	[Rpc(SendTo.Server)]
	private void ToggleRPC()
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
			FastBufferWriter bufferWriter = __beginSendRpc(2584949073u, rpcParams, attributeParams, SendTo.Server, RpcDelivery.Reliable);
			__endSendRpc(ref bufferWriter, 2584949073u, rpcParams, attributeParams, SendTo.Server, RpcDelivery.Reliable);
		}
		if (__rpc_exec_stage == __RpcExecStage.Execute)
		{
			__rpc_exec_stage = __RpcExecStage.Send;
			_isActive.Value = !_isActive.Value;
		}
	}

	protected override void OnNetworkPostSpawn()
	{
		base.OnNetworkPostSpawn();
		if (!base.IsClient)
		{
			return;
		}
		_isActive.RegisterOnValueChanged(delegate(bool _, bool newValue)
		{
			if ((bool)lightSource)
			{
				lightSource.enabled = newValue;
			}
			if ((bool)audioSource)
			{
				if (newValue)
				{
					audioSource.Play();
				}
				else
				{
					audioSource.Stop();
				}
			}
			NetController<SoundController>.Instance?.Play3DSound("Ingame/Items/LowGrav/low_grav_" + (newValue ? "on" : "off") + ".ogg", base.transform.position, new AudioData
			{
				volume = UnityEngine.Random.Range(0.5f, 0.8f),
				pitch = UnityEngine.Random.Range(0.8f, 1.2f),
				distance = 3f
			});
		});
	}

	public override void OnNetworkPreDespawn()
	{
		base.OnNetworkPreDespawn();
		if (base.IsClient)
		{
			_isActive.OnValueChanged = null;
		}
	}

	protected override void InternalChangeOwner(byte newOwner, bool server)
	{
		base.InternalChangeOwner(newOwner, server);
		if (server)
		{
			_isActive.Value = false;
		}
	}

	protected override void __initializeVariables()
	{
		if (_isActive == null)
		{
			throw new Exception("entity_item_lowgrav._isActive cannot be null. All NetworkVariableBase instances must be initialized.");
		}
		_isActive.Initialize(this);
		__nameNetworkVariable(_isActive, "_isActive");
		NetworkVariableFields.Add(_isActive);
		base.__initializeVariables();
	}

	protected override void __initializeRpcs()
	{
		__registerRpc(2584949073u, __rpc_handler_2584949073, "ToggleRPC", RpcInvokePermission.Everyone);
		base.__initializeRpcs();
	}

	private static void __rpc_handler_2584949073(NetworkBehaviour target, FastBufferReader reader, __RpcParams rpcParams)
	{
		NetworkManager networkManager = target.NetworkManager;
		if ((object)networkManager != null && networkManager.IsListening)
		{
			target.__rpc_exec_stage = __RpcExecStage.Execute;
			((entity_item_lowgrav)target).ToggleRPC();
			target.__rpc_exec_stage = __RpcExecStage.Send;
		}
	}

	protected internal override string __getTypeName()
	{
		return "entity_item_lowgrav";
	}
}
