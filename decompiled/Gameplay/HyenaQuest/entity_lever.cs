using System;
using FailCake;
using UnityEngine;

namespace HyenaQuest;

public class entity_lever : entity_usable
{
	public NetVar<bool> activated = new NetVar<bool>(value: false);

	public float resetCooldown = 2f;

	[Header("Movement")]
	public float speed = 5f;

	[Header("Rotation")]
	public Vector3 rotateEnd = new Vector3(0f, 0f, 90f);

	[Header("Position")]
	public Vector3 positionEnd;

	[Header("Effects")]
	public bool spark;

	public GameEvent<entity_player, bool> OnUSE = new GameEvent<entity_player, bool>();

	private util_timer _timer;

	private util_fade_timer _rotationTimer;

	private entity_spark _spark;

	public new void Awake()
	{
		base.Awake();
		if (spark)
		{
			_spark = GetComponentInChildren<entity_spark>(includeInactive: true);
			if (!_spark)
			{
				throw new UnityException("entity_lever requires entity_spark component");
			}
		}
	}

	protected override void OnNetworkPostSpawn()
	{
		base.OnNetworkPostSpawn();
		if (!base.IsClient)
		{
			return;
		}
		activated.RegisterOnValueChanged(delegate(bool oldValue, bool newValue)
		{
			if (oldValue != newValue)
			{
				Animate(newValue);
			}
		});
	}

	public override void OnNetworkPreDespawn()
	{
		base.OnNetworkPreDespawn();
		if (base.IsClient)
		{
			activated.OnValueChanged = null;
		}
	}

	public override void OnNetworkDespawn()
	{
		base.OnNetworkDespawn();
		if (base.IsServer)
		{
			_timer?.Stop();
			_rotationTimer?.Stop();
		}
	}

	public override bool OnUseDown(entity_player ply, bool server)
	{
		if (_rotationTimer != null)
		{
			return false;
		}
		if (!base.OnUseDown(ply, server))
		{
			return false;
		}
		if (!server)
		{
			return true;
		}
		activated.Value = !activated.Value;
		OnUSE?.Invoke(ply, activated.Value);
		SetLocked(newVal: true);
		if (resetCooldown > 0f)
		{
			_timer?.Stop();
			_timer = util_timer.Simple(resetCooldown, delegate
			{
				SetLocked(newVal: false);
			});
		}
		return true;
	}

	[Server]
	public void SetActive(bool active)
	{
		if (!base.IsServer)
		{
			throw new UnityException("SetActive can only be called on server");
		}
		activated.Value = active;
	}

	[Client]
	protected override void Animate(bool active)
	{
		base.Animate(active);
		_rotationTimer?.Stop();
		_rotationTimer = util_fade_timer.Fade(speed, (!active) ? 1 : 0, active ? 1 : 0, SetLeverPosition, delegate
		{
			_rotationTimer = null;
			if (spark && active)
			{
				_spark.Play();
			}
		});
	}

	private void SetLeverPosition(float lerpT)
	{
		if ((bool)target)
		{
			target.transform.localEulerAngles = Vector3.Lerp(Vector3.zero, rotateEnd, lerpT);
			target.transform.localPosition = Vector3.Lerp(Vector3.zero, positionEnd, lerpT);
		}
	}

	protected override void __initializeVariables()
	{
		if (activated == null)
		{
			throw new Exception("entity_lever.activated cannot be null. All NetworkVariableBase instances must be initialized.");
		}
		activated.Initialize(this);
		__nameNetworkVariable(activated, "activated");
		NetworkVariableFields.Add(activated);
		base.__initializeVariables();
	}

	protected override void __initializeRpcs()
	{
		base.__initializeRpcs();
	}

	protected internal override string __getTypeName()
	{
		return "entity_lever";
	}
}
