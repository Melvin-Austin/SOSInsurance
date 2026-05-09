using System;
using FailCake;
using UnityEngine;

namespace HyenaQuest;

public class entity_button_confirm : entity_usable
{
	public float defaultDistance = -0.045f;

	public float moveDistance = -0.03f;

	public float resetCooldown = 2f;

	public GameObject glass;

	public GameEvent OnUSE = new GameEvent();

	private util_timer _timer;

	private util_fade_timer _glassAnimation;

	private util_timer _resetTimer;

	private Quaternion _openRotation;

	private Quaternion _closedRotation;

	private readonly NetVar<bool> _isOpen = new NetVar<bool>(value: false);

	public new void Awake()
	{
		base.Awake();
		if (!glass)
		{
			throw new UnityException("entity_button_confirm requires glass to be set");
		}
		if (!target)
		{
			throw new UnityException("entity_button_confirm requires target to be set");
		}
		_openRotation = Quaternion.Euler(-90f, 110f, 0f);
		_closedRotation = Quaternion.Euler(-90f, 0f, 0f);
	}

	protected override void OnNetworkPostSpawn()
	{
		base.OnNetworkPostSpawn();
		if (!base.IsClient)
		{
			return;
		}
		locked.RegisterOnValueChanged(delegate(bool oldValue, bool newValue)
		{
			if (oldValue != newValue)
			{
				Animate(newValue);
			}
		});
		_isOpen.RegisterOnValueChanged(delegate(bool oldValue, bool newValue)
		{
			if (oldValue != newValue)
			{
				AnimateGlass(newValue);
			}
		});
	}

	public override void OnNetworkPreDespawn()
	{
		base.OnNetworkPreDespawn();
		if (base.IsClient)
		{
			locked.OnValueChanged = null;
			_isOpen.OnValueChanged = null;
		}
	}

	public override void OnNetworkDespawn()
	{
		if (base.IsServer)
		{
			_resetTimer?.Stop();
			_glassAnimation?.Stop();
		}
	}

	public override bool OnUseDown(entity_player ply, bool server)
	{
		if (!base.OnUseDown(ply, server))
		{
			return false;
		}
		if (!server)
		{
			return true;
		}
		if (_isOpen.Value)
		{
			OnUSE?.Invoke();
			SetLocked(newVal: true);
			_resetTimer?.Stop();
			if (resetCooldown > 0f)
			{
				_resetTimer = util_timer.Simple(resetCooldown, delegate
				{
					SetLocked(newVal: false);
					SetGlassStatus(open: false);
				});
			}
		}
		else
		{
			SetGlassStatus(open: true);
		}
		return true;
	}

	[Server]
	public void SetGlassStatus(bool open)
	{
		if (base.IsServer && _isOpen.Value != open)
		{
			_isOpen.Value = open;
		}
	}

	private void AnimateGlass(bool open)
	{
		if ((bool)glass)
		{
			if (_glassAnimation != null)
			{
				_glassAnimation.Stop();
			}
			_glassAnimation = util_fade_timer.Fade(5f, open ? 0f : 1f, open ? 1f : 0f, delegate(float f)
			{
				glass.transform.localRotation = Quaternion.Lerp(_closedRotation, _openRotation, f);
			});
		}
	}

	[Client]
	protected override void Animate(bool pressed)
	{
		if ((bool)target)
		{
			base.Animate(pressed);
			Vector3 localPosition = target.transform.localPosition;
			localPosition.z = (pressed ? moveDistance : defaultDistance);
			target.transform.localPosition = localPosition;
		}
	}

	protected override void __initializeVariables()
	{
		if (_isOpen == null)
		{
			throw new Exception("entity_button_confirm._isOpen cannot be null. All NetworkVariableBase instances must be initialized.");
		}
		_isOpen.Initialize(this);
		__nameNetworkVariable(_isOpen, "_isOpen");
		NetworkVariableFields.Add(_isOpen);
		base.__initializeVariables();
	}

	protected override void __initializeRpcs()
	{
		base.__initializeRpcs();
	}

	protected internal override string __getTypeName()
	{
		return "entity_button_confirm";
	}
}
