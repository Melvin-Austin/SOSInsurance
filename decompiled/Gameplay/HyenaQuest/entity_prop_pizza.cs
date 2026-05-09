using FailCake;
using Unity.Netcode;
using UnityEngine;

namespace HyenaQuest;

public class entity_prop_pizza : entity_phys_painter
{
	public static Vector3? TUNA_POSITION;

	public static Vector3? PEPPERONI_POSITION;

	private float _holdTime;

	private bool _ritualComplete;

	private bool _tunaComplete;

	private bool _pepperoniComplete;

	private bool _veggieComplete;

	private float _grassTimer;

	public new void Update()
	{
		base.Update();
		if (!base.IsServer)
		{
			return;
		}
		if (!_ritualComplete)
		{
			IngameController instance = NetController<IngameController>.Instance;
			if ((object)instance != null && instance.Status() == INGAME_STATUS.IDLE)
			{
				if (IsOnVacuumTower())
				{
					_holdTime += Time.deltaTime;
					if (_holdTime > 2f)
					{
						_ritualComplete = true;
						StartRitual();
					}
				}
				else
				{
					_holdTime = 0f;
				}
			}
		}
		if (!_pepperoniComplete && entity_fire.fires != null)
		{
			foreach (entity_fire fire in entity_fire.fires)
			{
				if ((bool)fire && Vector3.Distance(fire.transform.position, base.transform.position) <= 0.3f)
				{
					ReleaseThePepperoni();
					break;
				}
			}
		}
		if (_veggieComplete || !(NetController<IngameController>.Instance?.veggieArea))
		{
			return;
		}
		if (NetController<IngameController>.Instance.veggieArea.bounds.Contains(base.transform.position))
		{
			if (_grassTimer <= 0f)
			{
				_grassTimer = Time.time + 100f;
			}
			else if (Time.time > _grassTimer)
			{
				ReleaseTheVeggies();
			}
		}
		else
		{
			_grassTimer = 0f;
		}
	}

	public override void OnNetworkDespawn()
	{
		base.OnNetworkDespawn();
		if (base.IsServer)
		{
			TUNA_POSITION = null;
			PEPPERONI_POSITION = null;
		}
	}

	protected override void OnCollision(Collision col)
	{
		base.OnCollision(col);
		if (base.IsOwner && !_tunaComplete && col != null && (bool)col.gameObject && (bool)col.rigidbody && col.gameObject.name.StartsWith("fish_mx_1"))
		{
			entity_phys_prop_scrap_tuna component = col.gameObject.GetComponent<entity_phys_prop_scrap_tuna>();
			if ((bool)component)
			{
				ReleaseTheTunaRPC(component);
			}
		}
	}

	[Server]
	private void StartRitual()
	{
		if (!base.IsServer)
		{
			throw new UnityException("Server only");
		}
		if (!NetController<SoundController>.Instance)
		{
			throw new UnityException("SoundController instance not found");
		}
		if (!NetController<ShakeController>.Instance)
		{
			throw new UnityException("ShakeController instance not found");
		}
		if (!NetController<LightController>.Instance)
		{
			throw new UnityException("LightController instance not found");
		}
		NetController<SoundController>.Instance.Play3DSound("Ingame/Entities/Pizza/summon.ogg", base.transform.position, new AudioData
		{
			distance = 10f
		}, broadcast: true);
		NetController<ShakeController>.Instance.Shake3DRPC(base.transform.position, ShakeMode.SHAKE_ALL, 0.25f, 0.05f, ShakeSoundMode.OFF, 5f);
		NetController<PowerController>.Instance.SetPoweredArea(PowerGrid.BASE, hasPower: false);
		util_timer.Simple(1f, delegate
		{
			if ((bool)NetController<PhoneController>.Instance)
			{
				NetController<PhoneController>.Instance.AutoType("74627753 ", 0.25f, 1f, delegate
				{
					NetController<PowerController>.Instance.SetPoweredArea(PowerGrid.BASE, hasPower: true);
				});
			}
		});
	}

	[Server]
	[Rpc(SendTo.Server)]
	private void ReleaseTheTunaRPC(NetworkBehaviourReference objRef)
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
			FastBufferWriter bufferWriter = __beginSendRpc(4116872954u, rpcParams, attributeParams, SendTo.Server, RpcDelivery.Reliable);
			bufferWriter.WriteValueSafe(in objRef, default(FastBufferWriter.ForNetworkSerializable));
			__endSendRpc(ref bufferWriter, 4116872954u, rpcParams, attributeParams, SendTo.Server, RpcDelivery.Reliable);
		}
		if (__rpc_exec_stage != __RpcExecStage.Execute)
		{
			return;
		}
		__rpc_exec_stage = __RpcExecStage.Send;
		if (!base.IsServer)
		{
			throw new UnityException("Server only");
		}
		entity_phys_prop_scrap_tuna entity_phys_prop_scrap_tuna2 = NETController.Get<entity_phys_prop_scrap_tuna>(objRef);
		if ((bool)entity_phys_prop_scrap_tuna2)
		{
			if (!NetController<StatsController>.Instance)
			{
				throw new UnityException("StatsController instance not found");
			}
			if (!NetController<ExplosionController>.Instance)
			{
				throw new UnityException("ExplosionController instance not found");
			}
			_tunaComplete = true;
			TUNA_POSITION = entity_phys_prop_scrap_tuna2.transform.position;
			NetController<ExplosionController>.Instance.Explode(base.transform.position, 5f, 0);
			NetController<StatsController>.Instance.UnlockAchievementSV(STEAM_ACHIEVEMENTS.ACHIEVEMENT_PIZZA_TUNA, ulong.MaxValue);
			entity_phys_prop_scrap_tuna2.NetworkObject.Despawn();
		}
	}

	[Server]
	private void ReleaseThePepperoni()
	{
		if (!base.IsServer)
		{
			throw new UnityException("Server only");
		}
		if (!NetController<StatsController>.Instance)
		{
			throw new UnityException("StatsController instance not found");
		}
		if (!NetController<EffectController>.Instance)
		{
			throw new UnityException("EffectController instance not found");
		}
		_pepperoniComplete = true;
		PEPPERONI_POSITION = base.transform.position;
		NetController<EffectController>.Instance.PlayEffectRPC(EffectType.CONFETTI_SPHERE, base.transform.position, default(EffectSettings));
		NetController<StatsController>.Instance.UnlockAchievementSV(STEAM_ACHIEVEMENTS.ACHIEVEMENT_PIZZA_PEPPERONI, ulong.MaxValue);
	}

	[Server]
	private void ReleaseTheVeggies()
	{
		if (!base.IsServer)
		{
			throw new UnityException("Server only");
		}
		if (!NetController<StatsController>.Instance)
		{
			throw new UnityException("StatsController instance not found");
		}
		if (!NetController<EffectController>.Instance)
		{
			throw new UnityException("EffectController instance not found");
		}
		_veggieComplete = true;
		NetController<EffectController>.Instance.PlayEffectRPC(EffectType.SPARKS, base.transform.position, default(EffectSettings));
		NetController<StatsController>.Instance.UnlockAchievementSV(STEAM_ACHIEVEMENTS.ACHIEVEMENT_PIZZA_VEGGIE, ulong.MaxValue);
	}

	[Server]
	private bool IsOnVacuumTower()
	{
		if (!base.IsServer)
		{
			throw new UnityException("Server only");
		}
		if (IsBeingGrabbed() || GetVelocity().sqrMagnitude > 0.2f)
		{
			return false;
		}
		Transform transform = base.transform;
		for (int i = 0; i < 4; i++)
		{
			if (!Physics.Raycast(transform.position, Vector3.down, out var hitInfo, 0.25f))
			{
				return false;
			}
			if (!hitInfo.collider || !hitInfo.collider.gameObject.name.Contains("item_vacuum"))
			{
				return false;
			}
			transform = hitInfo.collider.transform;
		}
		return true;
	}

	protected override void __initializeVariables()
	{
		base.__initializeVariables();
	}

	protected override void __initializeRpcs()
	{
		__registerRpc(4116872954u, __rpc_handler_4116872954, "ReleaseTheTunaRPC", RpcInvokePermission.Everyone);
		base.__initializeRpcs();
	}

	private static void __rpc_handler_4116872954(NetworkBehaviour target, FastBufferReader reader, __RpcParams rpcParams)
	{
		NetworkManager networkManager = target.NetworkManager;
		if ((object)networkManager != null && networkManager.IsListening)
		{
			reader.ReadValueSafe(out NetworkBehaviourReference value, default(FastBufferWriter.ForNetworkSerializable));
			target.__rpc_exec_stage = __RpcExecStage.Execute;
			((entity_prop_pizza)target).ReleaseTheTunaRPC(value);
			target.__rpc_exec_stage = __RpcExecStage.Send;
		}
	}

	protected internal override string __getTypeName()
	{
		return "entity_prop_pizza";
	}
}
