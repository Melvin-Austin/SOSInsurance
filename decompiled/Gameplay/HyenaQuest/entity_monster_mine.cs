using System;
using FailCake;
using UnityEngine;

namespace HyenaQuest;

public class entity_monster_mine : entity_monster_ai
{
	private entity_led _led;

	private util_timer _timer;

	private int _layer;

	private bool _destroying;

	private readonly NetVar<bool> _active = new NetVar<bool>(value: false);

	public new void Awake()
	{
		base.Awake();
		_led = GetComponent<entity_led>();
		if (!_led)
		{
			throw new UnityException("Missing LED");
		}
		_layer = LayerMask.GetMask("entity_phys", "entity_phys_item", "entity_player");
	}

	public override void OnNetworkSpawn()
	{
		base.OnNetworkSpawn();
		if (!base.IsServer)
		{
			return;
		}
		_active.SetSpawnValue(UnityEngine.Random.value > 0.5f);
		_timer?.Stop();
		_timer = util_timer.Create(-1, 1.5f, delegate
		{
			if (base.IsSpawned)
			{
				_active.SetSpawnValue(!_active.Value);
			}
		});
	}

	public override void OnNetworkDespawn()
	{
		base.OnNetworkDespawn();
		if (base.IsServer)
		{
			_timer?.Stop();
		}
	}

	public new void Update()
	{
		base.Update();
		if (!base.IsServer || _destroying || !_active.Value)
		{
			return;
		}
		Vector3 minePos = GetMinePos();
		if (Physics.CheckSphere(minePos, GetMineRange(), _layer, QueryTriggerInteraction.Ignore))
		{
			NetController<ExplosionController>.Instance?.Explode(minePos, 2.5f, 250);
			if (base.IsSpawned)
			{
				base.NetworkObject.Despawn();
			}
			_destroying = true;
		}
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
			if ((bool)_led && (bool)NetController<SoundController>.Instance)
			{
				_led.SetActive(newValue);
				NetController<SoundController>.Instance?.Play3DSound("Ingame/Entities/Mine/mine_beep.ogg", GetMinePos(), new AudioData
				{
					distance = 0.2f,
					volume = UnityEngine.Random.Range(0.35f, 0.5f),
					pitch = 1f
				});
			}
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

	protected virtual Vector3 GetMinePos()
	{
		return base.transform.position + Vector3.up * 0.17f;
	}

	protected virtual float GetMineRange()
	{
		return 0.27f;
	}

	protected override void __initializeVariables()
	{
		if (_active == null)
		{
			throw new Exception("entity_monster_mine._active cannot be null. All NetworkVariableBase instances must be initialized.");
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
		return "entity_monster_mine";
	}
}
