using System;
using System.Collections.Generic;
using Opsive.GraphDesigner.Runtime.Variables;
using Opsive.Shared.Events;
using UnityEngine;

namespace HyenaQuest;

public class entity_monster_chaser : entity_monster_ai
{
	public List<Material> colors = new List<Material>();

	public SkinnedMeshRenderer render;

	protected SharedVariable<GameObject> _target;

	protected entity_player _targetPlayer;

	protected float _attackCD;

	protected float _idleTimer;

	private readonly NetVar<byte> _color = new NetVar<byte>(0);

	public new void Awake()
	{
		base.Awake();
		if (!render)
		{
			throw new UnityException("entity_monster_chaser requires SkinnedMeshRenderer component");
		}
	}

	public override void OnNetworkSpawn()
	{
		base.OnNetworkSpawn();
		if (base.IsServer)
		{
			_color.Value = (byte)UnityEngine.Random.Range(0, colors.Count);
			_target = _behavior.GetVariable<GameObject>("TARGET");
			Opsive.Shared.Events.EventHandler.RegisterEvent(_behavior, "SET_TARGET", OnTargetSet);
			Opsive.Shared.Events.EventHandler.RegisterEvent(_behavior, "FOUND_TARGET", OnTargetFound);
		}
	}

	public override void OnNetworkDespawn()
	{
		base.OnNetworkDespawn();
		if (base.IsServer)
		{
			_target = null;
			Opsive.Shared.Events.EventHandler.UnregisterEvent(_behavior, "SET_TARGET", OnTargetSet);
			Opsive.Shared.Events.EventHandler.UnregisterEvent(_behavior, "FOUND_TARGET", OnTargetFound);
		}
	}

	protected override void OnNetworkPostSpawn()
	{
		base.OnNetworkPostSpawn();
		if (!base.IsClient)
		{
			return;
		}
		_color.RegisterOnValueChanged(delegate(byte _, byte newValue)
		{
			if ((bool)render)
			{
				render.material = colors[newValue];
			}
		});
	}

	public override void OnNetworkPreDespawn()
	{
		base.OnNetworkPreDespawn();
		if (base.IsClient)
		{
			_color.OnValueChanged = null;
		}
	}

	public new void Update()
	{
		base.Update();
		OnUpdate();
	}

	protected virtual bool OnUpdate()
	{
		if (!base.IsServer)
		{
			return false;
		}
		if (Time.time >= _idleTimer)
		{
			_idleTimer = Time.time + (_targetPlayer ? UnityEngine.Random.Range(0.15f, 0.4f) : UnityEngine.Random.Range(0.15f, 1.3f));
			OnIdle();
		}
		if (!_targetPlayer)
		{
			return false;
		}
		if (_targetPlayer.IsDead() || ((bool)NetController<IngameController>.Instance && NetController<IngameController>.Instance.IsShipArea(_targetPlayer)))
		{
			_target.Value = null;
			_targetPlayer = null;
			return false;
		}
		return true;
	}

	protected virtual void OnIdle()
	{
	}

	[Server]
	protected virtual void OnTargetFound()
	{
		if (!base.IsServer)
		{
			throw new UnityException("OnTargetFound can only be called on server");
		}
		if ((bool)_networkAnimator)
		{
			_networkAnimator.SetTrigger("TARGET_FOUND");
		}
	}

	[Server]
	protected virtual void OnTargetSet()
	{
		if (!base.IsServer)
		{
			throw new UnityException("OnTargetSet can only be called on server");
		}
		_targetPlayer = (_target.Value ? _target.Value.GetComponent<entity_player>() : null);
	}

	protected override void __initializeVariables()
	{
		if (_color == null)
		{
			throw new Exception("entity_monster_chaser._color cannot be null. All NetworkVariableBase instances must be initialized.");
		}
		_color.Initialize(this);
		__nameNetworkVariable(_color, "_color");
		NetworkVariableFields.Add(_color);
		base.__initializeVariables();
	}

	protected override void __initializeRpcs()
	{
		base.__initializeRpcs();
	}

	protected internal override string __getTypeName()
	{
		return "entity_monster_chaser";
	}
}
