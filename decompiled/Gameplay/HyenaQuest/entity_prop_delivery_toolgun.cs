using FailCake;
using Unity.Netcode;
using UnityEngine;

namespace HyenaQuest;

public class entity_prop_delivery_toolgun : entity_prop_delivery
{
	public LineRenderer line;

	public ParticleSystem deleteEffect;

	private util_timer _beamTimer;

	private int _layerMask;

	public override void OnNetworkSpawn()
	{
		base.OnNetworkSpawn();
		_beamTimer?.Stop();
	}

	[Server]
	public override void Damage(byte damage, Vector3? point)
	{
		if (!base.IsServer)
		{
			throw new UnityException("ShootBeam called on client, but should be called on server!");
		}
		base.Damage(damage, point);
		if (!Physics.Raycast(new Ray(line.transform.position + Vector3.forward * 0.2f, line.transform.forward), out var hitInfo, 100f, _layerMask) || !hitInfo.collider || hitInfo.rigidbody == _rigidbody)
		{
			return;
		}
		Vector3 rigidPos = Vector3.zero;
		if ((bool)hitInfo.rigidbody)
		{
			rigidPos = hitInfo.rigidbody.transform.position;
			entity_monster_ai component2;
			entity_phys_breakable component3;
			if (hitInfo.rigidbody.TryGetComponent<entity_player>(out var component))
			{
				rigidPos = component.chest.position;
				component.Kill(DamageType.INSTANT);
			}
			else if (hitInfo.rigidbody.TryGetComponent<entity_monster_ai>(out component2))
			{
				component2.Kill();
			}
			else if (hitInfo.rigidbody.TryGetComponent<entity_phys_breakable>(out component3))
			{
				component3.Destroy();
			}
		}
		BeamEffectRPC(hitInfo.point, rigidPos);
	}

	protected override void Init()
	{
		base.Init();
		if (!line)
		{
			throw new UnityException("LineRenderer is not assigned!");
		}
		line.enabled = false;
		line.useWorldSpace = false;
		line.positionCount = 2;
		_layerMask = LayerMask.GetMask("entity_player", "entity_enemy", "entity_ground", "entity_phys");
	}

	[Rpc(SendTo.ClientsAndHost)]
	private void BeamEffectRPC(Vector3 endPoint, Vector3 rigidPos)
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
			FastBufferWriter bufferWriter = __beginSendRpc(733695803u, rpcParams, attributeParams, SendTo.ClientsAndHost, RpcDelivery.Reliable);
			bufferWriter.WriteValueSafe(in endPoint);
			bufferWriter.WriteValueSafe(in rigidPos);
			__endSendRpc(ref bufferWriter, 733695803u, rpcParams, attributeParams, SendTo.ClientsAndHost, RpcDelivery.Reliable);
		}
		if (__rpc_exec_stage != __RpcExecStage.Execute)
		{
			return;
		}
		__rpc_exec_stage = __RpcExecStage.Send;
		if (!line)
		{
			return;
		}
		line.enabled = true;
		line.SetPosition(0, line.transform.localPosition);
		line.SetPosition(1, line.transform.InverseTransformPoint(endPoint));
		if (rigidPos != Vector3.zero)
		{
			deleteEffect.transform.position = rigidPos;
			deleteEffect.Play();
		}
		NetController<SoundController>.Instance.Play3DSound($"Ingame/Props/Special/Toolgun/airboat_gun_lastshot{Random.Range(1, 3)}.ogg", base.transform.position, new AudioData
		{
			pitch = Random.Range(0.95f, 1.05f),
			volume = 1f
		});
		_beamTimer?.Stop();
		_beamTimer = util_timer.Simple(0.05f, delegate
		{
			if ((bool)line)
			{
				line.enabled = false;
			}
		});
	}

	protected override void __initializeVariables()
	{
		base.__initializeVariables();
	}

	protected override void __initializeRpcs()
	{
		__registerRpc(733695803u, __rpc_handler_733695803, "BeamEffectRPC", RpcInvokePermission.Everyone);
		base.__initializeRpcs();
	}

	private static void __rpc_handler_733695803(NetworkBehaviour target, FastBufferReader reader, __RpcParams rpcParams)
	{
		NetworkManager networkManager = target.NetworkManager;
		if ((object)networkManager != null && networkManager.IsListening)
		{
			reader.ReadValueSafe(out Vector3 value);
			reader.ReadValueSafe(out Vector3 value2);
			target.__rpc_exec_stage = __RpcExecStage.Execute;
			((entity_prop_delivery_toolgun)target).BeamEffectRPC(value, value2);
			target.__rpc_exec_stage = __RpcExecStage.Send;
		}
	}

	protected internal override string __getTypeName()
	{
		return "entity_prop_delivery_toolgun";
	}
}
