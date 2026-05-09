using System.Collections.Generic;
using FailCake;
using Unity.Netcode;
using UnityEngine;

namespace HyenaQuest;

public class entity_monster_cone : entity_monster_ai
{
	private enum ConeState
	{
		IDLE,
		PRE_STAB,
		WAITING,
		RECOVERING
	}

	private static readonly float WAKE_DISTANCE = 1.3f;

	private static readonly float PRE_STAB_COOLDOWN = 0.25f;

	private static readonly float RESET_COOLDOWN = 3f;

	private static readonly float STAB_FORCE = 8f;

	private static readonly float JUMP_FORCE = 7f;

	private static readonly float STAB_TILT_TORQUE = 8f;

	private static readonly float FLIP_JUMP_FORCE = 4f;

	private static readonly float FLIP_TORQUE = 0.6f;

	private static readonly float FLIP_COOLDOWN = 1f;

	private static readonly float STILL_THRESHOLD = 0.01f;

	private static readonly float UPRIGHT_THRESHOLD = 0.85f;

	private static readonly int Stab = Animator.StringToHash("STAB");

	private Rigidbody _rigidbody;

	private ConeState _state;

	private float _stabTime;

	private float _flipTime;

	private Vector3 _stabDirection;

	private int _playerLayerMask;

	private readonly Collider[] _colliderHits = new Collider[NETController.MAX_PLAYERS];

	public new void Awake()
	{
		base.Awake();
		_rigidbody = GetComponent<Rigidbody>();
		if (!_rigidbody)
		{
			throw new UnityException("Rigidbody is not assigned in entity_monster_cone.");
		}
		if (!_animator)
		{
			throw new UnityException("Animator is not assigned in entity_monster_cone.");
		}
		_playerLayerMask = LayerMask.GetMask("entity_player");
	}

	public override void OnNetworkSpawn()
	{
		base.OnNetworkSpawn();
		if (base.IsServer)
		{
			ReplaceWorldCone();
		}
	}

	protected new void Update()
	{
		base.Update();
		if (base.IsServer && (bool)_rigidbody && !_rigidbody.isKinematic)
		{
			switch (_state)
			{
			case ConeState.IDLE:
				UpdateIdle();
				break;
			case ConeState.PRE_STAB:
				UpdatePreStab();
				break;
			case ConeState.WAITING:
				UpdateWaiting();
				break;
			case ConeState.RECOVERING:
				UpdateRecovering();
				break;
			}
		}
	}

	[Server]
	private void ReplaceWorldCone()
	{
		if (!base.IsServer)
		{
			throw new UnityException("Not server");
		}
		IList<entity_phys_prop_scrap_cone> list = Object.FindObjectsByType<entity_phys_prop_scrap_cone>(FindObjectsInactive.Exclude, FindObjectsSortMode.None).Shuffle();
		if (list == null || list.Count <= 0)
		{
			return;
		}
		entity_phys_prop_scrap_cone entity_phys_prop_scrap_cone2 = list[Random.Range(0, list.Count)];
		if ((bool)entity_phys_prop_scrap_cone2)
		{
			Vector3 newPos = entity_phys_prop_scrap_cone2.transform.position;
			Quaternion newRotation = entity_phys_prop_scrap_cone2.transform.rotation;
			NetworkObject networkObject = entity_phys_prop_scrap_cone2.NetworkObject;
			if (!networkObject)
			{
				throw new UnityException("Network object is not assigned in entity_phys_prop_scrap_cone.");
			}
			networkObject.Despawn();
			Object.DestroyImmediate(entity_phys_prop_scrap_cone2.gameObject);
			util_timer.Simple(0.1f, delegate
			{
				_networkTransform?.SetState(newPos, newRotation, base.transform.lossyScale, teleportDisabled: false);
			});
		}
	}

	private void UpdateIdle()
	{
		Vector3 position = GetPosition();
		if (Physics.OverlapSphereNonAlloc(position, WAKE_DISTANCE, _colliderHits, _playerLayerMask, QueryTriggerInteraction.Ignore) > 0)
		{
			_animator.SetBool(Stab, value: true);
			Collider collider = _colliderHits[0];
			_stabDirection = collider.transform.position - position;
			_stabDirection.y = 0f;
			_stabDirection.Normalize();
			_state = ConeState.PRE_STAB;
			_stabTime = Time.time + PRE_STAB_COOLDOWN;
		}
	}

	private void UpdatePreStab()
	{
		if (!(Time.time < _stabTime))
		{
			Vector3 force = _stabDirection * STAB_FORCE + Vector3.up * JUMP_FORCE;
			_rigidbody.AddForce(force, ForceMode.VelocityChange);
			Vector3 vector = Vector3.Cross(Vector3.up, _stabDirection);
			_rigidbody.AddTorque(vector * STAB_TILT_TORQUE, ForceMode.VelocityChange);
			_animator.SetBool(Stab, value: true);
			_stabTime = Time.time + RESET_COOLDOWN;
			_state = ConeState.WAITING;
		}
	}

	private void UpdateWaiting()
	{
		if (!(Time.time < _stabTime))
		{
			_state = ConeState.RECOVERING;
		}
	}

	private void UpdateRecovering()
	{
		if (Vector3.Dot(base.transform.up, Vector3.up) > UPRIGHT_THRESHOLD && IsStill())
		{
			_animator.SetBool(Stab, value: false);
			_state = ConeState.IDLE;
		}
		else if (!(Time.time < _flipTime) && IsStill())
		{
			_rigidbody.AddForce(Vector3.up * FLIP_JUMP_FORCE, ForceMode.VelocityChange);
			Vector3 vector = Vector3.Cross(base.transform.up, Vector3.up);
			_rigidbody.AddTorque(vector * FLIP_TORQUE, ForceMode.Impulse);
			_networkAnimator.SetTrigger("FLIP");
			_flipTime = Time.time + FLIP_COOLDOWN;
		}
	}

	private Vector3 GetPosition()
	{
		return base.transform.position + Vector3.up * 0.05f;
	}

	private bool IsStill()
	{
		if (_rigidbody.linearVelocity.sqrMagnitude < STILL_THRESHOLD)
		{
			return _rigidbody.angularVelocity.sqrMagnitude < STILL_THRESHOLD;
		}
		return false;
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
		return "entity_monster_cone";
	}
}
