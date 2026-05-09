using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

namespace HyenaQuest;

public class entity_item_medkit : entity_item_pickable
{
	private entity_led_controller _chargesLed;

	private float _useCooldown;

	public readonly NetVar<byte> _charges = new NetVar<byte>(3);

	protected override void OnNetworkPostSpawn()
	{
		base.OnNetworkPostSpawn();
		if (base.IsClient)
		{
			_charges.RegisterOnValueChanged(delegate(byte _, byte newValue)
			{
				_chargesLed?.SetActive(newValue);
			});
		}
	}

	public override void OnNetworkPreDespawn()
	{
		base.OnNetworkPreDespawn();
		if (base.IsClient)
		{
			_charges.OnValueChanged = null;
		}
	}

	[Client]
	public override void OnUse(entity_player ply, Collider obj, bool pressing)
	{
		if ((bool)ply && pressing && !(Time.time < _useCooldown))
		{
			_useCooldown = Time.time + 0.5f;
			if (ply.IsDead() || ply.GetHealth() >= 100)
			{
				NetController<NotificationController>.Instance?.CreateNotification(new NotificationData
				{
					id = "health-full-error",
					text = "ingame.ui.notification.full-health",
					duration = 2f,
					soundEffect = "Ingame/Entities/Terminal/142608__autistic-lucario__error.ogg",
					soundVolume = 0.05f
				});
			}
			else
			{
				HealRPC();
			}
		}
	}

	[Server]
	public override Dictionary<string, string> Save()
	{
		if (base.IsSpawned && !base.IsServer)
		{
			throw new UnityException("Server only");
		}
		return new Dictionary<string, string> { 
		{
			"health",
			_charges.Value.ToString()
		} };
	}

	[Server]
	public override void Load(Dictionary<string, string> data)
	{
		if (base.IsSpawned && !base.IsServer)
		{
			throw new UnityException("Server only");
		}
		if (data.TryGetValue("health", out var value))
		{
			_charges.SetSpawnValue(byte.Parse(value));
		}
	}

	public override string GetID()
	{
		return "item_medkit";
	}

	protected override void Init()
	{
		base.Init();
		_chargesLed = GetComponentInChildren<entity_led_controller>(includeInactive: true);
		if (!_chargesLed)
		{
			throw new UnityException("Missing entity_led_controller");
		}
	}

	[Rpc(SendTo.Server)]
	private void HealRPC()
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
			FastBufferWriter bufferWriter = __beginSendRpc(1119157765u, rpcParams, attributeParams, SendTo.Server, RpcDelivery.Reliable);
			__endSendRpc(ref bufferWriter, 1119157765u, rpcParams, attributeParams, SendTo.Server, RpcDelivery.Reliable);
		}
		if (__rpc_exec_stage != __RpcExecStage.Execute)
		{
			return;
		}
		__rpc_exec_stage = __RpcExecStage.Send;
		if (_charges.Value <= 0)
		{
			return;
		}
		Player inventoryOwner = GetInventoryOwner();
		if (inventoryOwner != null && (bool)inventoryOwner.player)
		{
			OnHealRPC(base.RpcTarget.Single(inventoryOwner.connectionID, RpcTargetUse.Temp));
			_charges.Value = (byte)Mathf.Max(0, _charges.Value - 1);
			if (_charges.Value == 0)
			{
				Destroy();
			}
		}
	}

	[Rpc(SendTo.SpecifiedInParams)]
	private void OnHealRPC(RpcParams param)
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
			FastBufferWriter bufferWriter = __beginSendRpc(314723936u, param, attributeParams, SendTo.SpecifiedInParams, RpcDelivery.Reliable);
			__endSendRpc(ref bufferWriter, 314723936u, param, attributeParams, SendTo.SpecifiedInParams, RpcDelivery.Reliable);
		}
		if (__rpc_exec_stage == __RpcExecStage.Execute)
		{
			__rpc_exec_stage = __RpcExecStage.Send;
			if ((bool)PlayerController.LOCAL)
			{
				PlayerController.LOCAL.AddHealth(50);
				NetController<ShakeController>.Instance?.LocalShake(ShakeMode.SHAKE_ALL, 0.1f, 0.05f);
				MonoController<UIController>.Instance?.SetFade(fadeIn: false, new Color(0f, 0.2f, 0f, 0.4f), 3f);
				NetController<SoundController>.Instance?.PlaySound("Ingame/Items/Medkit/heal.ogg", new AudioData
				{
					volume = 0.5f,
					pitch = UnityEngine.Random.Range(0.8f, 1.2f)
				});
			}
		}
	}

	protected override void __initializeVariables()
	{
		if (_charges == null)
		{
			throw new Exception("entity_item_medkit._charges cannot be null. All NetworkVariableBase instances must be initialized.");
		}
		_charges.Initialize(this);
		__nameNetworkVariable(_charges, "_charges");
		NetworkVariableFields.Add(_charges);
		base.__initializeVariables();
	}

	protected override void __initializeRpcs()
	{
		__registerRpc(1119157765u, __rpc_handler_1119157765, "HealRPC", RpcInvokePermission.Everyone);
		__registerRpc(314723936u, __rpc_handler_314723936, "OnHealRPC", RpcInvokePermission.Everyone);
		base.__initializeRpcs();
	}

	private static void __rpc_handler_1119157765(NetworkBehaviour target, FastBufferReader reader, __RpcParams rpcParams)
	{
		NetworkManager networkManager = target.NetworkManager;
		if ((object)networkManager != null && networkManager.IsListening)
		{
			target.__rpc_exec_stage = __RpcExecStage.Execute;
			((entity_item_medkit)target).HealRPC();
			target.__rpc_exec_stage = __RpcExecStage.Send;
		}
	}

	private static void __rpc_handler_314723936(NetworkBehaviour target, FastBufferReader reader, __RpcParams rpcParams)
	{
		NetworkManager networkManager = target.NetworkManager;
		if ((object)networkManager != null && networkManager.IsListening)
		{
			RpcParams ext = rpcParams.Ext;
			target.__rpc_exec_stage = __RpcExecStage.Execute;
			((entity_item_medkit)target).OnHealRPC(ext);
			target.__rpc_exec_stage = __RpcExecStage.Send;
		}
	}

	protected internal override string __getTypeName()
	{
		return "entity_item_medkit";
	}
}
