using System;
using FailCake;
using UnityEngine;

namespace HyenaQuest;

public class entity_prop_grenade : entity_phys
{
	public float explosionRadius = 3f;

	[Range(1f, 50f)]
	public int timer = 5;

	private entity_locator _led;

	private util_timer _timer;

	private bool _destroying;

	private int _ticks;

	protected readonly NetVar<bool> _blink = new NetVar<bool>(value: false);

	public override void OnNetworkSpawn()
	{
		base.OnNetworkSpawn();
		if (base.IsServer)
		{
			if (_timer != null)
			{
				_timer.Stop();
			}
			_timer = util_timer.Create(timer, 0.5f, delegate
			{
				_blink.Value = !_blink.Value;
			}, Explode);
		}
	}

	public override void OnNetworkDespawn()
	{
		base.OnNetworkDespawn();
		if (_timer != null)
		{
			_timer.Stop();
		}
	}

	protected override void OnNetworkPostSpawn()
	{
		base.OnNetworkPostSpawn();
		if (!base.IsClient)
		{
			return;
		}
		_blink.RegisterOnValueChanged(delegate
		{
			if ((bool)_led)
			{
				_led.ManualBeep(1f + (float)_ticks * 0.35f);
				_ticks++;
			}
		});
	}

	public override void OnNetworkPreDespawn()
	{
		base.OnNetworkPreDespawn();
		if (base.IsClient)
		{
			_blink.OnValueChanged = null;
		}
	}

	protected override void Init()
	{
		base.Init();
		_led = GetComponentInChildren<entity_locator>(includeInactive: true);
		if (!_led)
		{
			throw new UnityException("entity_prop_grenade requires entity_locator component");
		}
	}

	private void Explode()
	{
		if (base.IsServer && !_destroying)
		{
			_destroying = true;
			NetController<ExplosionController>.Instance.Explode(base.transform.position, explosionRadius, 10000);
			base.NetworkObject.Despawn();
		}
	}

	protected override void __initializeVariables()
	{
		if (_blink == null)
		{
			throw new Exception("entity_prop_grenade._blink cannot be null. All NetworkVariableBase instances must be initialized.");
		}
		_blink.Initialize(this);
		__nameNetworkVariable(_blink, "_blink");
		NetworkVariableFields.Add(_blink);
		base.__initializeVariables();
	}

	protected override void __initializeRpcs()
	{
		base.__initializeRpcs();
	}

	protected internal override string __getTypeName()
	{
		return "entity_prop_grenade";
	}
}
