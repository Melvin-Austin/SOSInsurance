using System;
using System.Collections.Generic;
using Opsive.Shared.Utility;
using Unity.Netcode;
using UnityEngine;
using ZLinq;

namespace HyenaQuest;

[DisallowMultipleComponent]
public class entity_phys_breakable : entity_phys
{
	public NetVar<byte> health = new NetVar<byte>(3);

	public bool destroyable = true;

	public float breakForce = 5f;

	public List<entity_phys_shard> pieces = new List<entity_phys_shard>();

	public List<AudioClip> damageSounds = new List<AudioClip>();

	protected byte _maxHealth;

	protected float _noDamageCD;

	protected override void OnNetworkPostSpawn()
	{
		base.OnNetworkPostSpawn();
		_maxHealth = health.Value;
		if (!base.IsClient)
		{
			return;
		}
		health.RegisterOnValueChanged(delegate(byte oldValue, byte newValue)
		{
			if (oldValue != newValue)
			{
				if (newValue > oldValue)
				{
					_maxHealth = newValue;
				}
				else if (oldValue <= _maxHealth)
				{
					OnDamage(newValue);
				}
			}
		});
	}

	public override void OnNetworkPreDespawn()
	{
		base.OnNetworkPreDespawn();
		if (base.IsClient)
		{
			health.OnValueChanged = null;
		}
	}

	[Server]
	public int GetHealth()
	{
		return health.Value;
	}

	[Server]
	public virtual void Damage(byte damage, Vector3? point = null)
	{
		if (!base.IsServer)
		{
			throw new UnityException("Not server");
		}
		if (CanTakeDamage())
		{
			_noDamageCD = Time.time + 0.25f;
			if (damageSounds.Count > 0)
			{
				NetController<SoundController>.Instance.Play3DSound(damageSounds[UnityEngine.Random.Range(0, damageSounds.Count)], base.transform, new AudioData
				{
					pitch = UnityEngine.Random.Range(0.85f, 1.15f),
					distance = 4f,
					volume = 0.3f
				}, broadcast: true);
			}
			NetController<NotificationController>.Instance.BroadcastAll3DRPC(new NotificationData3D
			{
				position = point.GetValueOrDefault(base.transform.position),
				message = "ingame.ui.notification.damaged-item",
				fadeSpeed = 1f,
				scale = 0.45f,
				startColor = Color.white,
				endColor = new Color(0.7f, 0f, 0f, 1f)
			});
			int value = GetHealth() - damage;
			SetHealth((byte)Mathf.Clamp(value, 0, _maxHealth));
		}
	}

	[Rpc(SendTo.Server)]
	private void DamageRPC(Vector3 point)
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
			FastBufferWriter bufferWriter = __beginSendRpc(968518156u, rpcParams, attributeParams, SendTo.Server, RpcDelivery.Reliable);
			bufferWriter.WriteValueSafe(in point);
			__endSendRpc(ref bufferWriter, 968518156u, rpcParams, attributeParams, SendTo.Server, RpcDelivery.Reliable);
		}
		if (__rpc_exec_stage == __RpcExecStage.Execute)
		{
			__rpc_exec_stage = __RpcExecStage.Send;
			Damage(1, point);
		}
	}

	[Server]
	protected virtual void SetHealth(byte newHealth)
	{
		if (!base.IsServer)
		{
			throw new UnityException("Not server");
		}
		health.Value = (byte)Mathf.Clamp(newHealth, 0, _maxHealth);
		if (health.Value <= 0)
		{
			OnBreak();
		}
	}

	[Server]
	protected virtual void OnBreak()
	{
		if (destroyable)
		{
			Destroy();
		}
	}

	public override void OnThrow()
	{
		if (base.IsOwner)
		{
			_noDamageCD = Time.time + 0.8f;
		}
	}

	protected virtual bool CanTakeDamage()
	{
		if (IsLocked() || health.Value <= 0 || health.Value == byte.MaxValue || Time.time < _noDamageCD)
		{
			return false;
		}
		return !entity_phys_breakable_safe_area.NO_BREAK_AREAS.AsValueEnumerable().Any((entity_phys_breakable_safe_area a) => a.IsInside(base.transform.position, GetBounds()));
	}

	protected virtual bool IsBreakDamage(float impactForce)
	{
		if (impactForce > breakForce)
		{
			return health.Value > 0;
		}
		return false;
	}

	protected override void OnCollision(Collision collision)
	{
		if (CanTakeDamage() && (!IsBeingGrabbed() || !(collision.gameObject == GetGrabbingOwner()?.gameObject)))
		{
			float magnitude = collision.relativeVelocity.magnitude;
			float impactForce = (collision.rigidbody ? (magnitude * collision.rigidbody.mass) : magnitude);
			if (IsBreakDamage(impactForce))
			{
				DamageRPC((collision.contactCount > 0) ? collision.GetContact(0).point : base.transform.position);
			}
		}
	}

	[Shared]
	protected virtual void OnDamage(byte newHealth)
	{
		if (IsBeingGrabbed() && base.IsOwner)
		{
			NetController<ShakeController>.Instance.LocalShake(ShakeMode.SHAKE_ALL, 0.1f, 0.05f);
		}
		List<entity_phys_shard> list = pieces;
		if (list == null || list.Count <= 0)
		{
			return;
		}
		int num = Mathf.Clamp(_maxHealth - newHealth, 0, pieces.Count);
		if (num == 0)
		{
			return;
		}
		for (int i = 0; i < num; i++)
		{
			if (pieces.Count != 0 && (bool)pieces[i])
			{
				entity_phys_shard obj = pieces[i];
				if (!obj)
				{
					throw new UnityException("Invalid piece prefab");
				}
				MeshRenderer renderer = obj.GetRenderer();
				if (!renderer)
				{
					throw new UnityException("Renderer missing");
				}
				RenderOutline(renderer, render: false);
				int num2 = _renderers.IndexOf(renderer);
				if (num2 != -1)
				{
					_renderers[num2] = null;
				}
				obj.Shred();
				pieces[i] = null;
			}
		}
	}

	protected override void __initializeVariables()
	{
		if (health == null)
		{
			throw new Exception("entity_phys_breakable.health cannot be null. All NetworkVariableBase instances must be initialized.");
		}
		health.Initialize(this);
		__nameNetworkVariable(health, "health");
		NetworkVariableFields.Add(health);
		base.__initializeVariables();
	}

	protected override void __initializeRpcs()
	{
		__registerRpc(968518156u, __rpc_handler_968518156, "DamageRPC", RpcInvokePermission.Everyone);
		base.__initializeRpcs();
	}

	private static void __rpc_handler_968518156(NetworkBehaviour target, FastBufferReader reader, __RpcParams rpcParams)
	{
		NetworkManager networkManager = target.NetworkManager;
		if ((object)networkManager != null && networkManager.IsListening)
		{
			reader.ReadValueSafe(out Vector3 value);
			target.__rpc_exec_stage = __RpcExecStage.Execute;
			((entity_phys_breakable)target).DamageRPC(value);
			target.__rpc_exec_stage = __RpcExecStage.Send;
		}
	}

	protected internal override string __getTypeName()
	{
		return "entity_phys_breakable";
	}
}
