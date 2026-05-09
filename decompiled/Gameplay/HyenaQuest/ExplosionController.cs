using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

namespace HyenaQuest;

[DisallowMultipleComponent]
[DefaultExecutionOrder(-70)]
[RequireComponent(typeof(NetworkObject))]
public class ExplosionController : NetController<ExplosionController>
{
	[Header("Template")]
	public GameObject explosionTemplate;

	private int _mask;

	private int _wallMask;

	private readonly Collider[] _results = new Collider[128];

	public new void Awake()
	{
		base.Awake();
		_mask = LayerMask.GetMask("entity_player", "entity_enemy", "entity_phys");
		_wallMask = LayerMask.GetMask("entity_ground");
	}

	[Server]
	public void Explode(Vector3 pos, float range, int maxDamage)
	{
		if (!base.IsServer)
		{
			throw new UnityException("Server only");
		}
		if (range <= 0f)
		{
			throw new UnityException("Invalid strength value");
		}
		ExplodeRPC(pos, range);
		DamageEntities(pos, range, maxDamage);
	}

	[Client]
	public void ExplodeEffect(Vector3 pos, float range)
	{
		if (!base.IsClient)
		{
			throw new UnityException("Client only");
		}
		CreateExplosion(pos, range);
	}

	[Rpc(SendTo.ClientsAndHost)]
	private void ExplodeRPC(Vector3 pos, float distance)
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
			FastBufferWriter bufferWriter = __beginSendRpc(485760478u, rpcParams, attributeParams, SendTo.ClientsAndHost, RpcDelivery.Reliable);
			bufferWriter.WriteValueSafe(in pos);
			bufferWriter.WriteValueSafe(in distance, default(FastBufferWriter.ForPrimitives));
			__endSendRpc(ref bufferWriter, 485760478u, rpcParams, attributeParams, SendTo.ClientsAndHost, RpcDelivery.Reliable);
		}
		if (__rpc_exec_stage == __RpcExecStage.Execute)
		{
			__rpc_exec_stage = __RpcExecStage.Send;
			CreateExplosion(pos, distance);
		}
	}

	private void CreateExplosion(Vector3 pos, float distance)
	{
		if (distance <= 0f)
		{
			throw new UnityException("Invalid strength value");
		}
		GameObject obj = UnityEngine.Object.Instantiate(explosionTemplate, pos, Quaternion.identity);
		if (!obj)
		{
			throw new UnityException("Failed to instantiate explosion object");
		}
		entity_explosion component = obj.GetComponent<entity_explosion>();
		if (!component)
		{
			throw new UnityException("Missing entity_explosion");
		}
		component.SetDistance(distance);
		component.Explode();
	}

	[Server]
	private void DamageEntities(Vector3 pos, float distance, int maxDamage)
	{
		int num = Physics.OverlapSphereNonAlloc(pos, distance, _results, _mask);
		if (num == 0 || maxDamage <= 0)
		{
			return;
		}
		HashSet<entity_monster_ai> hashSet = new HashSet<entity_monster_ai>();
		for (int i = 0; i < num; i++)
		{
			Collider collider = _results[i];
			if (!collider || !collider.gameObject)
			{
				continue;
			}
			Vector3 b = collider.ClosestPoint(pos);
			float num2 = Vector3.Distance(pos, b);
			if (num2 > distance)
			{
				continue;
			}
			float num3 = CalculateVisibility(pos, collider);
			if (num3 <= 0f)
			{
				continue;
			}
			byte b2 = (byte)Math.Clamp(Mathf.RoundToInt((float)Mathf.Max(Mathf.RoundToInt((float)maxDamage * (1f - num2 / distance)), 0) * num3), 0, 100);
			entity_player component2;
			entity_phys_breakable component3;
			if (!collider.attachedRigidbody)
			{
				if (collider.TryGetComponent<entity_monster_ai>(out var component, 2) && hashSet.Add(component))
				{
					component.TakeHealth(b2);
				}
			}
			else if (collider.attachedRigidbody.TryGetComponent<entity_player>(out component2))
			{
				component2.TakeHealthRPC(b2);
			}
			else if (collider.attachedRigidbody.TryGetComponent<entity_phys_breakable>(out component3))
			{
				component3.Damage(b2, null);
			}
		}
	}

	private float CalculateVisibility(Vector3 explosionPos, Collider target)
	{
		Bounds bounds = target.bounds;
		Vector3 center = bounds.center;
		Vector3 extents = bounds.extents;
		Vector3[] array = new Vector3[9]
		{
			center,
			center + new Vector3(extents.x, extents.y, extents.z),
			center + new Vector3(extents.x, extents.y, 0f - extents.z),
			center + new Vector3(extents.x, 0f - extents.y, extents.z),
			center + new Vector3(extents.x, 0f - extents.y, 0f - extents.z),
			center + new Vector3(0f - extents.x, extents.y, extents.z),
			center + new Vector3(0f - extents.x, extents.y, 0f - extents.z),
			center + new Vector3(0f - extents.x, 0f - extents.y, extents.z),
			center + new Vector3(0f - extents.x, 0f - extents.y, 0f - extents.z)
		};
		int num = 0;
		int num2 = array.Length;
		for (int i = 0; i < num2; i++)
		{
			if (!Physics.Linecast(explosionPos, array[i], _wallMask, QueryTriggerInteraction.Ignore))
			{
				num++;
			}
		}
		return (float)num / (float)num2;
	}

	protected override void __initializeVariables()
	{
		base.__initializeVariables();
	}

	protected override void __initializeRpcs()
	{
		__registerRpc(485760478u, __rpc_handler_485760478, "ExplodeRPC", RpcInvokePermission.Everyone);
		base.__initializeRpcs();
	}

	private static void __rpc_handler_485760478(NetworkBehaviour target, FastBufferReader reader, __RpcParams rpcParams)
	{
		NetworkManager networkManager = target.NetworkManager;
		if ((object)networkManager != null && networkManager.IsListening)
		{
			reader.ReadValueSafe(out Vector3 value);
			reader.ReadValueSafe(out float value2, default(FastBufferWriter.ForPrimitives));
			target.__rpc_exec_stage = __RpcExecStage.Execute;
			((ExplosionController)target).ExplodeRPC(value, value2);
			target.__rpc_exec_stage = __RpcExecStage.Send;
		}
	}

	protected internal override string __getTypeName()
	{
		return "ExplosionController";
	}
}
