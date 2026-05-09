using System;
using System.Collections.Generic;
using FailCake;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;
using UnityEngine.VFX;

namespace HyenaQuest;

[RequireComponent(typeof(NetworkObject), typeof(NetworkTransform), typeof(NetworkRigidbody))]
[RequireComponent(typeof(Rigidbody), typeof(BoxCollider))]
public class entity_fire : NetworkBehaviour
{
	public static List<entity_fire> fires = new List<entity_fire>();

	public GameObject fire;

	[Range(0f, 1f)]
	public float fireSpeed = 0.5f;

	private NetworkTransform _networkTransform;

	private Rigidbody _rigidbody;

	private VisualEffect _vfx;

	private AudioSource _sfx;

	private util_fade_timer _despawnTimer;

	private readonly NetVar<float> _fireSpeed = new NetVar<float>(0f);

	public void Awake()
	{
		_rigidbody = GetComponent<Rigidbody>();
		if (!_rigidbody)
		{
			throw new UnityException("entity_phys requires a rigidbody component to work.");
		}
		_rigidbody.interpolation = RigidbodyInterpolation.Interpolate;
		_networkTransform = GetComponent<NetworkTransform>();
		if (!_networkTransform)
		{
			throw new UnityException("NetworkTransform not found");
		}
		_vfx = GetComponentInChildren<VisualEffect>(includeInactive: true);
		if (!_vfx)
		{
			throw new UnityException("entity_phys requires a vfx component to work.");
		}
		_sfx = GetComponentInChildren<AudioSource>(includeInactive: true);
		if (!_sfx)
		{
			throw new UnityException("entity_phys requires a sfx component to work.");
		}
		fires?.Add(this);
	}

	public override void OnNetworkSpawn()
	{
		base.OnNetworkSpawn();
		if (base.IsServer)
		{
			_fireSpeed.SetSpawnValue(fireSpeed);
		}
	}

	public override void OnNetworkDespawn()
	{
		base.OnNetworkDespawn();
		_despawnTimer?.Stop();
	}

	public override void OnDestroy()
	{
		base.OnDestroy();
		fires?.Remove(this);
	}

	protected override void OnNetworkPostSpawn()
	{
		base.OnNetworkPostSpawn();
		if (!base.IsClient)
		{
			return;
		}
		_fireSpeed.RegisterOnValueChanged(delegate(float _, float newValue)
		{
			_despawnTimer?.Stop();
			if (!(newValue <= 0f))
			{
				_despawnTimer = util_fade_timer.Fade(_fireSpeed.Value, 1f, 0.05f, delegate(float f)
				{
					if ((bool)fire && (bool)_vfx)
					{
						fire.transform.localScale = Vector3.one * f;
						_vfx.SetFloat("ScaleWSP", f);
					}
				}, delegate
				{
					Extinguish();
				});
			}
		});
	}

	public override void OnNetworkPreDespawn()
	{
		base.OnNetworkPreDespawn();
		if (base.IsClient)
		{
			_fireSpeed.OnValueChanged = null;
		}
	}

	public Rigidbody GetBody()
	{
		return _rigidbody;
	}

	public void Extinguish()
	{
		_sfx?.Stop();
		if (base.IsServer)
		{
			NetController<SoundController>.Instance?.Play3DSound("Ingame/Fire/sizzle_0.ogg", _sfx?.transform.position ?? base.transform.position, new AudioData
			{
				distance = 2f,
				pitch = UnityEngine.Random.Range(0.8f, 1.2f),
				volume = UnityEngine.Random.Range(0.8f, 1.2f)
			}, broadcast: true);
			base.NetworkObject.Despawn();
		}
	}

	protected override void __initializeVariables()
	{
		if (_fireSpeed == null)
		{
			throw new Exception("entity_fire._fireSpeed cannot be null. All NetworkVariableBase instances must be initialized.");
		}
		_fireSpeed.Initialize(this);
		__nameNetworkVariable(_fireSpeed, "_fireSpeed");
		NetworkVariableFields.Add(_fireSpeed);
		base.__initializeVariables();
	}

	protected override void __initializeRpcs()
	{
		base.__initializeRpcs();
	}

	protected internal override string __getTypeName()
	{
		return "entity_fire";
	}
}
