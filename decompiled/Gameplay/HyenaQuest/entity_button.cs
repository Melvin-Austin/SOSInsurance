using FailCake;
using UnityEngine;

namespace HyenaQuest;

public class entity_button : entity_usable
{
	public float defaultDistance = -0.045f;

	public float moveDistance = -0.03f;

	public float resetCooldown = 2f;

	public GameEvent<entity_player> OnUSE = new GameEvent<entity_player>();

	private util_timer _lockTimer;

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
	}

	public override void OnNetworkPreDespawn()
	{
		base.OnNetworkPreDespawn();
		if (base.IsClient)
		{
			locked.OnValueChanged = null;
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
		OnUSE?.Invoke(ply);
		SetLocked(newVal: true);
		if (resetCooldown > 0f)
		{
			_lockTimer?.Stop();
			_lockTimer = util_timer.Simple(resetCooldown, delegate
			{
				SetLocked(newVal: false);
			});
		}
		return true;
	}

	[Server]
	public override void SetLocked(bool newVal)
	{
		_lockTimer?.Stop();
		base.SetLocked(newVal);
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
		base.__initializeVariables();
	}

	protected override void __initializeRpcs()
	{
		base.__initializeRpcs();
	}

	protected internal override string __getTypeName()
	{
		return "entity_button";
	}
}
