using System;
using FailCake;
using Unity.Netcode;
using UnityEngine;

namespace HyenaQuest;

public class entity_prop_delivery_cannister : entity_prop_delivery
{
	public GameObject gas;

	public Transform detection;

	private util_timer _gasLeak;

	private readonly NetVar<byte> _leaking = new NetVar<byte>(0);

	private int _layer;

	public void FixedUpdate()
	{
		if (base.IsOwner && _leaking.Value == 1 && (bool)gas && (bool)_rigidbody)
		{
			if (Physics.BoxCast(halfExtents: new Vector3(0.1f, 0.1f, 0.1f), center: detection.position, direction: -detection.up, hitInfo: out var hitInfo, orientation: detection.rotation, maxDistance: 0.15f, layerMask: _layer, queryTriggerInteraction: QueryTriggerInteraction.Ignore))
			{
				PerformRicochet(hitInfo);
			}
			else
			{
				_rigidbody.AddForce(-detection.up * 0.35f, ForceMode.VelocityChange);
			}
		}
	}

	protected override void OnNetworkPostSpawn()
	{
		base.OnNetworkPostSpawn();
		if (!base.IsClient)
		{
			return;
		}
		_leaking.RegisterOnValueChanged(delegate(byte _, byte newValue)
		{
			if ((bool)gas)
			{
				gas.SetActive(newValue == 1);
			}
		});
	}

	public override void OnNetworkPreDespawn()
	{
		base.OnNetworkPreDespawn();
		if (base.IsClient)
		{
			_leaking.OnValueChanged = null;
		}
	}

	public override bool CanGrab()
	{
		if (base.CanGrab())
		{
			return _leaking.Value != 1;
		}
		return false;
	}

	protected override void Init()
	{
		base.Init();
		if (!gas)
		{
			throw new UnityException("Missing gas GameObject");
		}
		gas.SetActive(value: false);
		if (!detection)
		{
			throw new UnityException("Missing detection GameObject");
		}
		_layer = LayerMask.GetMask("entity_ground", "entity_phys");
	}

	protected override void OnDamage(byte newHealth)
	{
		if (_leaking.Value != 1)
		{
			base.OnDamage(newHealth);
			if (base.IsOwner && _leaking.Value == 0 && _leaking.Value == 0 && UnityEngine.Random.value < 0.4f)
			{
				StartLeakRPC();
			}
		}
	}

	protected override bool CanTakeDamage()
	{
		if (base.CanTakeDamage())
		{
			return _leaking.Value != 1;
		}
		return false;
	}

	private void PerformRicochet(RaycastHit hit)
	{
		if ((bool)detection)
		{
			Vector3 toDirection = Vector3.Reflect(-detection.up, hit.normal);
			Quaternion rotation = Quaternion.FromToRotation(-Vector3.up, toDirection);
			float num = Mathf.Max(_rigidbody.linearVelocity.magnitude, 10f);
			_rigidbody.MoveRotation(rotation);
			_rigidbody.linearVelocity = toDirection.normalized * num;
			NetController<SoundController>.Instance?.Play3DSound($"Ingame/Props/Metal/metal_damage_{UnityEngine.Random.Range(0, 4)}.ogg", hit.point, new AudioData
			{
				pitch = UnityEngine.Random.Range(0.8f, 1.2f),
				distance = 3f
			}, broadcast: true);
		}
	}

	[Rpc(SendTo.Server)]
	private void StartLeakRPC()
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
			FastBufferWriter bufferWriter = __beginSendRpc(2179755645u, rpcParams, attributeParams, SendTo.Server, RpcDelivery.Reliable);
			__endSendRpc(ref bufferWriter, 2179755645u, rpcParams, attributeParams, SendTo.Server, RpcDelivery.Reliable);
		}
		if (__rpc_exec_stage != __RpcExecStage.Execute)
		{
			return;
		}
		__rpc_exec_stage = __RpcExecStage.Send;
		if (_leaking.Value == 0)
		{
			if (!base.IsOwner)
			{
				base.NetworkObject.RemoveOwnership();
			}
			CancelGrabbing();
			_leaking.SetSpawnValue(1);
			_gasLeak?.Stop();
			_gasLeak = util_timer.Simple(UnityEngine.Random.Range(4, 6), delegate
			{
				_leaking.SetSpawnValue(2);
			});
		}
	}

	protected override void __initializeVariables()
	{
		if (_leaking == null)
		{
			throw new Exception("entity_prop_delivery_cannister._leaking cannot be null. All NetworkVariableBase instances must be initialized.");
		}
		_leaking.Initialize(this);
		__nameNetworkVariable(_leaking, "_leaking");
		NetworkVariableFields.Add(_leaking);
		base.__initializeVariables();
	}

	protected override void __initializeRpcs()
	{
		__registerRpc(2179755645u, __rpc_handler_2179755645, "StartLeakRPC", RpcInvokePermission.Everyone);
		base.__initializeRpcs();
	}

	private static void __rpc_handler_2179755645(NetworkBehaviour target, FastBufferReader reader, __RpcParams rpcParams)
	{
		NetworkManager networkManager = target.NetworkManager;
		if ((object)networkManager != null && networkManager.IsListening)
		{
			target.__rpc_exec_stage = __RpcExecStage.Execute;
			((entity_prop_delivery_cannister)target).StartLeakRPC();
			target.__rpc_exec_stage = __RpcExecStage.Send;
		}
	}

	protected internal override string __getTypeName()
	{
		return "entity_prop_delivery_cannister";
	}
}
