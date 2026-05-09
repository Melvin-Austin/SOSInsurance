using System;
using Unity.Netcode;
using UnityEngine;

namespace HyenaQuest;

[DisallowMultipleComponent]
[DefaultExecutionOrder(-81)]
[RequireComponent(typeof(NetworkObject))]
public class PowerController : NetController<PowerController>
{
	public GameEvent<PowerGrid, bool, bool> OnGridUpdate = new GameEvent<PowerGrid, bool, bool>();

	public GameEvent OnGridWarning = new GameEvent();

	private bool _flickerWarning;

	private readonly NetVar<bool> _basePowered = new NetVar<bool>(value: true);

	private readonly NetVar<bool> _mapPowered = new NetVar<bool>(value: true);

	protected override void OnNetworkPostSpawn()
	{
		base.OnNetworkPostSpawn();
		if (!base.IsClient)
		{
			return;
		}
		_basePowered.RegisterOnValueChanged(delegate(bool oldValue, bool newValue)
		{
			if (oldValue != newValue)
			{
				OnGridUpdate.Invoke(PowerGrid.BASE, newValue, param3: false);
			}
		});
		_mapPowered.RegisterOnValueChanged(delegate(bool oldValue, bool newValue)
		{
			if (oldValue != newValue)
			{
				OnGridUpdate.Invoke(PowerGrid.MAP, newValue, param3: false);
			}
		});
	}

	public override void OnNetworkPreDespawn()
	{
		base.OnNetworkPreDespawn();
		if (base.IsClient)
		{
			_basePowered.OnValueChanged = null;
			_mapPowered.OnValueChanged = null;
		}
	}

	[Server]
	public bool IsAreaPowered(PowerGrid grid)
	{
		return grid switch
		{
			PowerGrid.UNCONTROLLED => throw new ArgumentException("Invalid grid"), 
			PowerGrid.BASE => _basePowered.Value, 
			_ => _mapPowered.Value, 
		};
	}

	[Server]
	public void SetPoweredArea(PowerGrid grid, bool hasPower)
	{
		switch (grid)
		{
		case PowerGrid.UNCONTROLLED:
			throw new ArgumentException("Invalid grid");
		case PowerGrid.BASE:
			_basePowered.Value = hasPower;
			break;
		default:
			_mapPowered.Value = hasPower;
			break;
		}
		if (!hasPower && grid == PowerGrid.MAP)
		{
			SDK.Play2DSound?.Invoke("Ingame/Cycle/power_off.ogg", new AudioData
			{
				volume = 0.1f
			}, arg3: true);
		}
		OnGridUpdate.Invoke(grid, hasPower, param3: true);
	}

	[Server]
	public void SetPoweredArea(bool hasPower)
	{
		foreach (PowerGrid value in Enum.GetValues(typeof(PowerGrid)))
		{
			if (value != PowerGrid.UNCONTROLLED)
			{
				SetPoweredArea(value, hasPower);
			}
		}
	}

	protected override void __initializeVariables()
	{
		if (_basePowered == null)
		{
			throw new Exception("PowerController._basePowered cannot be null. All NetworkVariableBase instances must be initialized.");
		}
		_basePowered.Initialize(this);
		__nameNetworkVariable(_basePowered, "_basePowered");
		NetworkVariableFields.Add(_basePowered);
		if (_mapPowered == null)
		{
			throw new Exception("PowerController._mapPowered cannot be null. All NetworkVariableBase instances must be initialized.");
		}
		_mapPowered.Initialize(this);
		__nameNetworkVariable(_mapPowered, "_mapPowered");
		NetworkVariableFields.Add(_mapPowered);
		base.__initializeVariables();
	}

	protected override void __initializeRpcs()
	{
		base.__initializeRpcs();
	}

	protected internal override string __getTypeName()
	{
		return "PowerController";
	}
}
