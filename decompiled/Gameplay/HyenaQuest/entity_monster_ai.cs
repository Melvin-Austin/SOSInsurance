using System;
using Opsive.BehaviorDesigner.Runtime;
using Pathfinding;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;

namespace HyenaQuest;

[RequireComponent(typeof(NetworkObject))]
public class entity_monster_ai : NetworkBehaviour
{
	private static readonly int Velocity = Animator.StringToHash("Velocity");

	public NetVar<byte> health = new NetVar<byte>(3);

	protected BehaviorTree _behavior;

	protected NetworkTransform _networkTransform;

	protected Animator _animator;

	protected NetworkAnimator _networkAnimator;

	protected bool _hasVelocityParam;

	protected byte _maxHealth = byte.MaxValue;

	protected Vector3 _lastPosition;

	protected float _clientVelocity;

	protected FollowerEntity _agent;

	protected bool _hasGravityEnabled;

	protected float _baseSpeed;

	public void Awake()
	{
		_agent = GetComponent<FollowerEntity>();
		if (!(UnityEngine.Object)(object)_agent)
		{
			_agent = GetComponentInChildren<FollowerEntity>(includeInactive: true);
		}
		_networkAnimator = GetComponent<NetworkAnimator>();
		_animator = GetComponent<Animator>();
		if (!_animator)
		{
			_animator = GetComponentInChildren<Animator>(includeInactive: true);
		}
		if ((bool)_animator)
		{
			_hasVelocityParam = _animator.ContainsParam("Velocity");
		}
		_networkTransform = GetComponent<NetworkTransform>();
		if (!_networkTransform)
		{
			_networkTransform = GetComponentInChildren<NetworkTransform>(includeInactive: true);
		}
		_behavior = GetComponent<BehaviorTree>();
		if (!_behavior)
		{
			_behavior = GetComponentInChildren<BehaviorTree>(includeInactive: true);
		}
		if ((bool)_behavior)
		{
			_behavior.StartWhenEnabled = false;
			_behavior.StopBehavior();
		}
		_maxHealth = health.Value;
	}

	public override void OnNetworkSpawn()
	{
		base.OnNetworkSpawn();
		if (base.IsServer)
		{
			if ((bool)(UnityEngine.Object)(object)_agent)
			{
				_hasGravityEnabled = _agent.enableGravity;
				_baseSpeed = Mathf.Max(0.2f, _agent.maxSpeed);
			}
			_behavior?.StartBehavior();
			return;
		}
		if ((bool)(UnityEngine.Object)(object)_agent)
		{
			UnityEngine.Object.DestroyImmediate((UnityEngine.Object)(object)_agent);
			_agent = null;
		}
		if ((bool)_behavior)
		{
			UnityEngine.Object.DestroyImmediate(_behavior);
			_behavior = null;
		}
	}

	public override void OnNetworkDespawn()
	{
		base.OnNetworkDespawn();
		if (base.IsServer && (bool)_behavior)
		{
			_behavior.StopBehavior();
		}
	}

	public float GetVelocity()
	{
		if (!base.IsServer)
		{
			return _clientVelocity;
		}
		return _agent.velocity.magnitude;
	}

	public void SetSpeed(float speed)
	{
		if ((bool)(UnityEngine.Object)(object)_agent)
		{
			_agent.maxSpeed = speed;
			_baseSpeed = speed;
		}
	}

	public FollowerEntity GetAgent()
	{
		return _agent;
	}

	public void SetPath(Vector3 position)
	{
		if ((bool)(UnityEngine.Object)(object)_agent)
		{
			_agent.SetDestination(position);
			_agent.SearchPath();
			_agent.isStopped = false;
		}
	}

	public void SetPath(Transform t)
	{
		if ((bool)(UnityEngine.Object)(object)_agent)
		{
			SetPath(t.position);
		}
	}

	public void ResetPath()
	{
		if ((bool)(UnityEngine.Object)(object)_agent)
		{
			_agent.isStopped = true;
		}
	}

	public bool Arrived()
	{
		if (!(UnityEngine.Object)(object)_agent)
		{
			return false;
		}
		if (!_agent.isStopped)
		{
			return _agent.reachedEndOfPath;
		}
		return true;
	}

	public void Update()
	{
		if (!base.IsServer)
		{
			_clientVelocity = (base.transform.position - _lastPosition).magnitude / Time.deltaTime;
			_lastPosition = base.transform.position;
		}
		else if ((bool)(UnityEngine.Object)(object)_agent)
		{
			if (_hasGravityEnabled)
			{
				_agent.enableGravity = !_agent.isTraversingOffMeshLink;
			}
			_agent.maxSpeed = (_agent.isTraversingOffMeshLink ? (_baseSpeed * 8f) : _baseSpeed);
		}
	}

	public void LateUpdate()
	{
		if (base.IsClient && (bool)_animator && _hasVelocityParam)
		{
			_animator.SetFloat(Velocity, GetVelocity());
		}
	}

	[Server]
	public virtual void TakeHealth(byte damage)
	{
		if (!base.IsServer)
		{
			throw new UnityException("Server only");
		}
		int value = health.Value - damage;
		health.Value = (byte)Mathf.Clamp(value, 0, _maxHealth);
		if (health.Value <= 0)
		{
			Kill();
		}
	}

	[Server]
	public virtual void Heal(byte heal)
	{
		if (!base.IsServer)
		{
			throw new UnityException("Server only");
		}
		int value = health.Value + heal;
		health.Value = (byte)Mathf.Clamp(value, 0, _maxHealth);
	}

	[Server]
	public virtual void Kill()
	{
		if (!base.IsServer)
		{
			throw new UnityException("Server only");
		}
		Destroy();
	}

	[Server]
	public virtual void Destroy()
	{
		if (base.IsSpawned)
		{
			if (!base.IsServer)
			{
				throw new UnityException("Server only");
			}
			base.NetworkObject.Despawn();
		}
	}

	protected override void __initializeVariables()
	{
		if (health == null)
		{
			throw new Exception("entity_monster_ai.health cannot be null. All NetworkVariableBase instances must be initialized.");
		}
		health.Initialize(this);
		__nameNetworkVariable(health, "health");
		NetworkVariableFields.Add(health);
		base.__initializeVariables();
	}

	protected override void __initializeRpcs()
	{
		base.__initializeRpcs();
	}

	protected internal override string __getTypeName()
	{
		return "entity_monster_ai";
	}
}
