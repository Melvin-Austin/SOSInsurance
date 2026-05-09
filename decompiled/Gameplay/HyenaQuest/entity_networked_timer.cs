using System;
using FailCake;
using Unity.Netcode;
using UnityEngine;

namespace HyenaQuest;

[DisallowMultipleComponent]
[RequireComponent(typeof(NetworkObject))]
public class entity_networked_timer : NetworkBehaviour
{
	public Vector2 timerActive;

	public Vector2 timerDisabled;

	public GameObject mdl;

	private util_timer _timer;

	private readonly NetVar<bool> _active = new NetVar<bool>(value: false);

	public void Awake()
	{
		if (!mdl)
		{
			throw new UnityException("Missing GameObject mdl");
		}
	}

	public void OnEnable()
	{
		if (base.IsServer && _timer != null)
		{
			_timer.SetPaused(pause: false);
		}
	}

	public void OnDisable()
	{
		if (base.IsServer && _timer != null)
		{
			_timer.SetPaused(pause: true);
		}
	}

	public override void OnNetworkSpawn()
	{
		base.OnNetworkSpawn();
		if (!base.IsServer)
		{
			return;
		}
		_timer?.Stop();
		_timer = util_timer.Create(-1, UnityEngine.Random.Range(timerActive.x, timerActive.y), delegate
		{
			if (base.IsSpawned)
			{
				_active.Value = !_active.Value;
				_timer.SetDelay(_active.Value ? UnityEngine.Random.Range(timerActive.x, timerActive.y) : UnityEngine.Random.Range(timerDisabled.x, timerDisabled.y));
			}
		});
	}

	public virtual void OnUpdate(bool active)
	{
	}

	protected override void OnNetworkPostSpawn()
	{
		base.OnNetworkPostSpawn();
		if (!base.IsClient)
		{
			return;
		}
		_active.RegisterOnValueChanged(delegate(bool _, bool newValue)
		{
			if ((bool)mdl)
			{
				mdl.SetActive(newValue);
			}
			OnUpdate(newValue);
		});
	}

	public override void OnNetworkPreDespawn()
	{
		base.OnNetworkPreDespawn();
		if (base.IsClient)
		{
			_active.OnValueChanged = null;
		}
	}

	public override void OnDestroy()
	{
		_timer?.Stop();
		_timer = null;
		base.OnDestroy();
	}

	protected override void __initializeVariables()
	{
		if (_active == null)
		{
			throw new Exception("entity_networked_timer._active cannot be null. All NetworkVariableBase instances must be initialized.");
		}
		_active.Initialize(this);
		__nameNetworkVariable(_active, "_active");
		NetworkVariableFields.Add(_active);
		base.__initializeVariables();
	}

	protected override void __initializeRpcs()
	{
		base.__initializeRpcs();
	}

	protected internal override string __getTypeName()
	{
		return "entity_networked_timer";
	}
}
