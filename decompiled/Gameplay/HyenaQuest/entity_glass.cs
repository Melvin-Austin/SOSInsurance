using System;
using DinoFracture;
using FailCake;
using Pathfinding;
using Unity.Netcode;
using UnityEngine;

namespace HyenaQuest;

[DisallowMultipleComponent]
[RequireComponent(typeof(NetworkObject))]
public class entity_glass : NetworkBehaviour
{
	public float breakForce = 8f;

	public bool destroyable = true;

	public NodeLink2 nodeLink;

	private RuntimeFracturedGeometry _fracture;

	private entity_phys_shard[] _shards;

	private util_timer _breakTimer;

	private float _hitCooldown;

	private Collider _collision;

	private Rigidbody _rigidbody;

	private bool _shatterStarted;

	private Action _onShatterComplete;

	private static readonly float HIT_COOLDOWN = 0.035f;

	private readonly NetVar<GLASS_HEALTH> _health = new NetVar<GLASS_HEALTH>();

	public void Awake()
	{
		_fracture = GetComponentInChildren<RuntimeFracturedGeometry>(includeInactive: true);
		if (!_fracture)
		{
			throw new UnityException("Missing RuntimeFracturedGeometry");
		}
		_collision = GetComponent<Collider>();
		if (!_collision)
		{
			throw new UnityException("Missing Collider");
		}
		_rigidbody = GetComponent<Rigidbody>();
		if (!_rigidbody)
		{
			throw new UnityException("Missing Rigidbody");
		}
	}

	protected virtual bool IsBreakDamage(float impactForce)
	{
		if (destroyable)
		{
			return impactForce > breakForce;
		}
		return false;
	}

	public void OnCollisionEnter(Collision collision)
	{
		ContactPoint[] contacts = collision.contacts;
		if (contacts != null && contacts.Length > 0 && !(Time.time < _hitCooldown))
		{
			_hitCooldown = Time.time + HIT_COOLDOWN;
			Vector3 point = collision.contacts[0].point;
			float magnitude = collision.relativeVelocity.magnitude;
			if (IsBreakDamage(magnitude) && _health.Value.status == GLASS_STATUS.NONE)
			{
				OnBreakRPC(point);
			}
			NetController<SoundController>.Instance.Play3DSound($"Ingame/Props/Glass/glass bottle hit {UnityEngine.Random.Range(1, 8)}.ogg", point, new AudioData
			{
				pitch = UnityEngine.Random.Range(0.85f, 1.15f),
				distance = 4f,
				volume = 0.3f
			});
		}
	}

	public override void OnNetworkSpawn()
	{
		base.OnNetworkSpawn();
		if (base.IsServer)
		{
			_breakTimer?.Stop();
			if ((bool)(UnityEngine.Object)(object)nodeLink)
			{
				((Component)(object)nodeLink).gameObject.SetActive(value: false);
			}
		}
	}

	protected override void OnNetworkPostSpawn()
	{
		base.OnNetworkPostSpawn();
		_health.RegisterOnValueChanged(delegate(GLASS_HEALTH oldValue, GLASS_HEALTH newValue)
		{
			if ((int)newValue.status >= (int)oldValue.status)
			{
				switch (newValue.status)
				{
				case GLASS_STATUS.NONE:
					break;
				case GLASS_STATUS.SHATTER:
					ShatterGlass(newValue.hitPosition);
					break;
				case GLASS_STATUS.BROKEN:
					AttemptBreakGlass(newValue.hitPosition);
					break;
				}
			}
		});
	}

	public override void OnNetworkPreDespawn()
	{
		base.OnNetworkPreDespawn();
		if (base.IsOwner)
		{
			_health.OnValueChanged = null;
		}
	}

	[Rpc(SendTo.Server, Delivery = RpcDelivery.Reliable)]
	private void OnBreakRPC(Vector3 hitPoint)
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
				Delivery = RpcDelivery.Reliable
			};
			RpcParams rpcParams = default(RpcParams);
			FastBufferWriter bufferWriter = __beginSendRpc(623964473u, rpcParams, attributeParams, SendTo.Server, RpcDelivery.Reliable);
			bufferWriter.WriteValueSafe(in hitPoint);
			__endSendRpc(ref bufferWriter, 623964473u, rpcParams, attributeParams, SendTo.Server, RpcDelivery.Reliable);
		}
		if (__rpc_exec_stage != __RpcExecStage.Execute)
		{
			return;
		}
		__rpc_exec_stage = __RpcExecStage.Send;
		Vector3 hitPoint2 = hitPoint;
		if (_health.Value.status == GLASS_STATUS.NONE)
		{
			_health.Value = new GLASS_HEALTH
			{
				hitPosition = hitPoint2,
				status = GLASS_STATUS.SHATTER
			};
			_breakTimer?.Stop();
			_breakTimer = util_timer.Simple(UnityEngine.Random.Range(0.35f, 0.7f), delegate
			{
				_health.Value = new GLASS_HEALTH
				{
					hitPosition = hitPoint2,
					status = GLASS_STATUS.BROKEN
				};
			});
		}
	}

	[Shared]
	private void AttemptBreakGlass(Vector3 hitPos)
	{
		NetController<SoundController>.Instance?.Play3DSound($"Ingame/Props/Glass/glass_break_{UnityEngine.Random.Range(0, 3)}.ogg", base.transform.position, new AudioData
		{
			pitch = UnityEngine.Random.Range(0.85f, 1.15f),
			distance = 4f,
			volume = 0.6f
		});
		if ((bool)_collision)
		{
			UnityEngine.Object.Destroy(_collision);
		}
		if ((bool)_rigidbody)
		{
			UnityEngine.Object.Destroy(_rigidbody);
		}
		if ((bool)(UnityEngine.Object)(object)nodeLink && base.IsServer)
		{
			((Component)(object)nodeLink).gameObject.SetActive(value: true);
		}
		entity_phys_shard[] shards = _shards;
		if (shards != null && shards.Length > 0)
		{
			BreakGlass(hitPos);
		}
		else if (_shatterStarted)
		{
			_onShatterComplete = delegate
			{
				BreakGlass(hitPos);
			};
		}
		else
		{
			ShatterGlass(hitPos, delegate
			{
				BreakGlass(hitPos);
			});
		}
	}

	[Shared]
	private void BreakGlass(Vector3 hitPos)
	{
		entity_phys_shard[] shards = _shards;
		if (shards == null || shards.Length <= 0)
		{
			return;
		}
		shards = _shards;
		foreach (entity_phys_shard entity_phys_shard2 in shards)
		{
			if ((bool)entity_phys_shard2)
			{
				entity_phys_shard2.Shred();
				Rigidbody body = entity_phys_shard2.GetBody();
				if ((bool)body)
				{
					Vector3 normalized = (entity_phys_shard2.transform.position - hitPos).normalized;
					body.AddForce(normalized * 0.12f, ForceMode.Impulse);
				}
			}
		}
	}

	[Shared]
	private void ShatterGlass(Vector3 pos, Action onComplete = null)
	{
		if (_shatterStarted)
		{
			return;
		}
		_shatterStarted = true;
		Vector3 localPos = _fracture.transform.InverseTransformPoint(pos);
		try
		{
			_fracture.Fracture(localPos).OnFractureComplete += delegate
			{
				util_timer.Simple(0.1f, delegate
				{
					_shards = GetComponentsInChildren<entity_phys_shard>(includeInactive: true);
					NetController<SoundController>.Instance?.Play3DSound($"Ingame/Props/Glass/glass_crack_{UnityEngine.Random.Range(0, 3)}.ogg", pos, new AudioData
					{
						pitch = UnityEngine.Random.Range(0.85f, 1.15f),
						distance = 4f,
						volume = 0.6f
					}, broadcast: true);
					onComplete?.Invoke();
					_onShatterComplete?.Invoke();
					_onShatterComplete = null;
				});
			};
		}
		catch
		{
			NetController<SoundController>.Instance?.Play3DSound($"Ingame/Props/Glass/glass_crack_{UnityEngine.Random.Range(0, 3)}.ogg", pos, new AudioData
			{
				pitch = UnityEngine.Random.Range(0.85f, 1.15f),
				distance = 4f,
				volume = 0.6f
			}, broadcast: true);
			onComplete?.Invoke();
			_onShatterComplete?.Invoke();
			_onShatterComplete = null;
		}
	}

	protected override void __initializeVariables()
	{
		if (_health == null)
		{
			throw new Exception("entity_glass._health cannot be null. All NetworkVariableBase instances must be initialized.");
		}
		_health.Initialize(this);
		__nameNetworkVariable(_health, "_health");
		NetworkVariableFields.Add(_health);
		base.__initializeVariables();
	}

	protected override void __initializeRpcs()
	{
		__registerRpc(623964473u, __rpc_handler_623964473, "OnBreakRPC", RpcInvokePermission.Everyone);
		base.__initializeRpcs();
	}

	private static void __rpc_handler_623964473(NetworkBehaviour target, FastBufferReader reader, __RpcParams rpcParams)
	{
		NetworkManager networkManager = target.NetworkManager;
		if ((object)networkManager != null && networkManager.IsListening)
		{
			reader.ReadValueSafe(out Vector3 value);
			target.__rpc_exec_stage = __RpcExecStage.Execute;
			((entity_glass)target).OnBreakRPC(value);
			target.__rpc_exec_stage = __RpcExecStage.Send;
		}
	}

	protected internal override string __getTypeName()
	{
		return "entity_glass";
	}
}
