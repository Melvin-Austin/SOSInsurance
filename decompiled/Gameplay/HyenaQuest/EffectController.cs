using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Pool;

namespace HyenaQuest;

[DisallowMultipleComponent]
[DefaultExecutionOrder(-70)]
[RequireComponent(typeof(NetworkObject))]
public class EffectController : NetController<EffectController>
{
	[Header("Settings")]
	public List<GameObject> effectPrefabs;

	private readonly Dictionary<EffectType, ObjectPool<entity_particle_effect>> _effectPool = new Dictionary<EffectType, ObjectPool<entity_particle_effect>>();

	private readonly Dictionary<EffectType, GameObject> _effectPrefabLookup = new Dictionary<EffectType, GameObject>();

	public new void Awake()
	{
		base.Awake();
		foreach (GameObject effectPrefab in effectPrefabs)
		{
			if (!effectPrefab)
			{
				throw new UnityException("EffectController: EffectPrefab is null");
			}
			if (!effectPrefab.TryGetComponent<entity_particle_effect>(out var component))
			{
				throw new UnityException("EffectController: EffectPrefab " + effectPrefab.name + " does not have entity_particle_effect component");
			}
			EffectType type = component.GetEffectType();
			if (!_effectPrefabLookup.TryAdd(type, effectPrefab))
			{
				throw new UnityException($"EffectController: EffectPrefab for {type} already exists");
			}
			_effectPool.Add(type, new ObjectPool<entity_particle_effect>(() => CreateNewEffect(type), OnGetEffectFromPool, OnReleaseEffectToPool, OnDestroyPooledEffect, collectionCheck: true, 2, 10));
		}
	}

	[Rpc(SendTo.ClientsAndHost)]
	public void PlayEffectRPC(EffectType type, Vector3 pos, EffectSettings settings)
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
			FastBufferWriter bufferWriter = __beginSendRpc(1726284182u, rpcParams, attributeParams, SendTo.ClientsAndHost, RpcDelivery.Reliable);
			bufferWriter.WriteValueSafe(in type, default(FastBufferWriter.ForEnums));
			bufferWriter.WriteValueSafe(in pos);
			bufferWriter.WriteValueSafe(in settings, default(FastBufferWriter.ForNetworkSerializable));
			__endSendRpc(ref bufferWriter, 1726284182u, rpcParams, attributeParams, SendTo.ClientsAndHost, RpcDelivery.Reliable);
		}
		if (__rpc_exec_stage == __RpcExecStage.Execute)
		{
			__rpc_exec_stage = __RpcExecStage.Send;
			PlayEffect(type, pos, settings);
		}
	}

	[Client]
	public void PlayEffect(EffectType type, Vector3 pos, EffectSettings settings)
	{
		if (!(settings.chance > 0f) || !(Random.value >= settings.chance))
		{
			if (!_effectPool.TryGetValue(type, out var value))
			{
				throw new UnityException($"EffectController: EffectPool for {type} not found");
			}
			entity_particle_effect obj = value.Get();
			obj.transform.position = pos;
			obj.count = settings.count;
			obj.delay = settings.delay;
			obj.Play(settings.playSound, settings.volume);
		}
	}

	public void OnParticleFinish(entity_particle_effect effect)
	{
		if (effect.gameObject.activeSelf && _effectPool.TryGetValue(effect.GetEffectType(), out var value))
		{
			value.Release(effect);
		}
	}

	private entity_particle_effect CreateNewEffect(EffectType type)
	{
		if (!_effectPrefabLookup.TryGetValue(type, out var value))
		{
			throw new UnityException($"EffectController: EffectPrefab for {type} not found");
		}
		entity_particle_effect component = Object.Instantiate(value).GetComponent<entity_particle_effect>();
		if (!component)
		{
			throw new UnityException($"EffectController: EffectPrefab for {type} does not have entity_particle_effect component");
		}
		return component;
	}

	private void OnGetEffectFromPool(entity_particle_effect effect)
	{
		if ((bool)effect)
		{
			effect.gameObject.SetActive(value: true);
		}
	}

	private void OnReleaseEffectToPool(entity_particle_effect effect)
	{
		if ((bool)effect)
		{
			effect.gameObject.SetActive(value: false);
		}
	}

	private void OnDestroyPooledEffect(entity_particle_effect effect)
	{
		if ((bool)effect)
		{
			Object.Destroy(effect.gameObject);
		}
	}

	protected override void __initializeVariables()
	{
		base.__initializeVariables();
	}

	protected override void __initializeRpcs()
	{
		__registerRpc(1726284182u, __rpc_handler_1726284182, "PlayEffectRPC", RpcInvokePermission.Everyone);
		base.__initializeRpcs();
	}

	private static void __rpc_handler_1726284182(NetworkBehaviour target, FastBufferReader reader, __RpcParams rpcParams)
	{
		NetworkManager networkManager = target.NetworkManager;
		if ((object)networkManager != null && networkManager.IsListening)
		{
			reader.ReadValueSafe(out EffectType value, default(FastBufferWriter.ForEnums));
			reader.ReadValueSafe(out Vector3 value2);
			reader.ReadValueSafe(out EffectSettings value3, default(FastBufferWriter.ForNetworkSerializable));
			target.__rpc_exec_stage = __RpcExecStage.Execute;
			((EffectController)target).PlayEffectRPC(value, value2, value3);
			target.__rpc_exec_stage = __RpcExecStage.Send;
		}
	}

	protected internal override string __getTypeName()
	{
		return "EffectController";
	}
}
