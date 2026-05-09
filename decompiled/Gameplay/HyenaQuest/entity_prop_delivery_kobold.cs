using System;
using FailCake;
using UnityEngine;

namespace HyenaQuest;

public class entity_prop_delivery_kobold : entity_prop_delivery
{
	public GameObject windup;

	private float _lastOwnerCD;

	private util_timer _selfDestructTimer;

	private readonly NetVar<bool> _selfDestruct = new NetVar<bool>(value: false);

	protected override void OnNetworkPostSpawn()
	{
		base.OnNetworkPostSpawn();
		if (!base.IsClient)
		{
			return;
		}
		_grabbingOwnerId.RegisterOnValueChanged(delegate(byte _, byte newValue)
		{
			if (!(Time.time < _lastOwnerCD) && !_selfDestruct.Value)
			{
				_lastOwnerCD = Time.time + 4f;
				bool flag = newValue != byte.MaxValue;
				NetController<SoundController>.Instance.Play3DSound(flag ? $"Ingame/Props/Special/Bratty/picked_{UnityEngine.Random.Range(0, 5)}.ogg" : $"Ingame/Props/Special/Bratty/drop_{UnityEngine.Random.Range(0, 4)}.ogg", base.transform.position, new AudioData
				{
					distance = 5f,
					volume = 1f,
					parent = this
				});
			}
		});
	}

	public override void OnNetworkDespawn()
	{
		base.OnNetworkDespawn();
		if (base.IsServer)
		{
			_selfDestructTimer?.Stop();
		}
	}

	public new void Update()
	{
		base.Update();
		if (base.IsClient && (bool)windup)
		{
			bool flag = IsBeingGrabbed();
			windup.transform.localEulerAngles = new Vector3(flag ? (Time.time * 100f) : 0f, 0f, 0f);
		}
	}

	protected override void Init()
	{
		base.Init();
		if (!windup)
		{
			throw new UnityException("Missing windup");
		}
	}

	protected override bool CanTakeDamage()
	{
		if (base.CanTakeDamage())
		{
			return !_selfDestruct.Value;
		}
		return false;
	}

	protected override void OnPreCollision(Collision collision)
	{
		base.OnPreCollision(collision);
		if (_selfDestruct.Value || !base.IsServer || health.Value > 0 || UnityEngine.Random.value > 0.25f)
		{
			return;
		}
		NetController<SoundController>.Instance.Play3DSound("Ingame/Props/Special/Bratty/explode.ogg", base.transform.position, new AudioData
		{
			distance = 5f,
			volume = 1f,
			parent = this
		}, broadcast: true);
		_selfDestruct.Value = true;
		_selfDestructTimer?.Stop();
		_selfDestructTimer = util_timer.Simple(8f, delegate
		{
			if ((bool)NetController<ExplosionController>.Instance)
			{
				NetController<ExplosionController>.Instance.Explode(base.transform.position, 4f, 255);
				Destroy();
			}
		});
	}

	protected override void __initializeVariables()
	{
		if (_selfDestruct == null)
		{
			throw new Exception("entity_prop_delivery_kobold._selfDestruct cannot be null. All NetworkVariableBase instances must be initialized.");
		}
		_selfDestruct.Initialize(this);
		__nameNetworkVariable(_selfDestruct, "_selfDestruct");
		NetworkVariableFields.Add(_selfDestruct);
		base.__initializeVariables();
	}

	protected override void __initializeRpcs()
	{
		base.__initializeRpcs();
	}

	protected internal override string __getTypeName()
	{
		return "entity_prop_delivery_kobold";
	}
}
