using System;
using System.Collections.Generic;
using UnityEngine;

namespace HyenaQuest;

public class entity_switch : entity_usable
{
	[Header("Settings")]
	public List<Point> points = new List<Point>();

	public GameEvent<bool> OnUSE = new GameEvent<bool>();

	private readonly NetVar<byte> _pressed = new NetVar<byte>(byte.MaxValue);

	private bool _isDown;

	protected override void OnNetworkPostSpawn()
	{
		base.OnNetworkPostSpawn();
		if (!base.IsClient)
		{
			return;
		}
		_pressed.RegisterOnValueChanged(delegate(byte oldValue, byte newValue)
		{
			if (oldValue != newValue)
			{
				Animate(newValue != byte.MaxValue);
			}
		});
	}

	public override void OnNetworkPreDespawn()
	{
		base.OnNetworkPreDespawn();
		if (base.IsClient)
		{
			_pressed.OnValueChanged = null;
		}
	}

	public override bool OnUseDown(entity_player ply, bool server)
	{
		if (!base.OnUseDown(ply, server) || _pressed.Value != byte.MaxValue)
		{
			return false;
		}
		if (!server)
		{
			return true;
		}
		OnUSE?.Invoke(param1: true);
		_pressed.Value = ply.GetPlayerID();
		return true;
	}

	public override bool OnUseUP(entity_player ply, bool server)
	{
		if (!base.OnUseUP(ply, server))
		{
			return false;
		}
		if (!server)
		{
			return true;
		}
		OnUSE?.Invoke(param1: false);
		_pressed.Value = byte.MaxValue;
		return true;
	}

	[Server]
	public override void SetLocked(bool locks)
	{
		if (IsLocked() != locks)
		{
			locked.Value = locks;
			_pressed.Value = (locks ? byte.MaxValue : _pressed.Value);
		}
	}

	[Client]
	protected override void Animate(bool isDown)
	{
		if (_isDown != isDown)
		{
			_isDown = isDown;
			Point point = points[isDown ? 1 : 0];
			target.transform.localPosition = point.pos;
			target.transform.localEulerAngles = point.angle;
			base.Animate(isDown);
		}
	}

	protected override void __initializeVariables()
	{
		if (_pressed == null)
		{
			throw new Exception("entity_switch._pressed cannot be null. All NetworkVariableBase instances must be initialized.");
		}
		_pressed.Initialize(this);
		__nameNetworkVariable(_pressed, "_pressed");
		NetworkVariableFields.Add(_pressed);
		base.__initializeVariables();
	}

	protected override void __initializeRpcs()
	{
		base.__initializeRpcs();
	}

	protected internal override string __getTypeName()
	{
		return "entity_switch";
	}
}
