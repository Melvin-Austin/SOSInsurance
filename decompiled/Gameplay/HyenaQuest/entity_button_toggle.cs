using System;
using System.Collections.Generic;
using UnityEngine;

namespace HyenaQuest;

public class entity_button_toggle : entity_usable
{
	[Header("Settings")]
	public List<Point> points = new List<Point>();

	public GameEvent<byte> OnUSE = new GameEvent<byte>();

	private readonly NetVar<byte> _pressedIndex = new NetVar<byte>(0);

	protected override void OnNetworkPostSpawn()
	{
		base.OnNetworkPostSpawn();
		if (!base.IsClient)
		{
			return;
		}
		_pressedIndex.RegisterOnValueChanged(delegate(byte oldValue, byte newValue)
		{
			if (oldValue != newValue)
			{
				AnimateToggle(newValue);
			}
		});
	}

	public override void OnNetworkPreDespawn()
	{
		base.OnNetworkPreDespawn();
		if (base.IsClient)
		{
			_pressedIndex.OnValueChanged = null;
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
		_pressedIndex.Value = (byte)((_pressedIndex.Value + 1) % points.Count);
		OnUSE.Invoke(_pressedIndex.Value);
		return true;
	}

	private void AnimateToggle(byte index)
	{
		if (base.IsClient && (bool)target)
		{
			if (index >= points.Count)
			{
				throw new UnityException($"entity_button_toggle index out of range: {index} / {points.Count}");
			}
			target.transform.localPosition = points[index].pos;
			target.transform.localRotation = Quaternion.Euler(points[index].angle);
			Animate(index == (byte)points.Count - 1);
		}
	}

	protected override void __initializeVariables()
	{
		if (_pressedIndex == null)
		{
			throw new Exception("entity_button_toggle._pressedIndex cannot be null. All NetworkVariableBase instances must be initialized.");
		}
		_pressedIndex.Initialize(this);
		__nameNetworkVariable(_pressedIndex, "_pressedIndex");
		NetworkVariableFields.Add(_pressedIndex);
		base.__initializeVariables();
	}

	protected override void __initializeRpcs()
	{
		base.__initializeRpcs();
	}

	protected internal override string __getTypeName()
	{
		return "entity_button_toggle";
	}
}
