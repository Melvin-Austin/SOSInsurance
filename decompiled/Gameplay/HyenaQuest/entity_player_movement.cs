using ECM2;
using UnityEngine;
using UnityEngine.InputSystem;

namespace HyenaQuest;

public class entity_player_movement : Character
{
	private static readonly int VelX = Animator.StringToHash("VelX");

	private static readonly int VelZ = Animator.StringToHash("VelZ");

	private static readonly int Crouching = Animator.StringToHash("Crouching");

	private static readonly int Swimming = Animator.StringToHash("Swimming");

	private static readonly int Falling = Animator.StringToHash("Falling");

	private static readonly float GEL_ACCELERATION = 8f;

	private static readonly float GEL_MAX_SPEED_MULTIPLIER = 2.5f;

	private static readonly float GEL_FRICTION = 0.25f;

	private static readonly float GEL_MOMENTUM_DECAY = 0.3f;

	public InputActionReference jumpAction;

	public InputActionReference crouchAction;

	public InputActionReference moveAction;

	private bool _jumping;

	private bool _crouching;

	private bool _moving;

	private int _gelLayer;

	private entity_player _owner;

	private entity_vehicle_seat _vehicle;

	private bool _onSpeedGel;

	private float _gelMomentum;

	public new void Awake()
	{
		base.Awake();
		_gelLayer = LayerMask.GetMask("entity_gel");
	}

	public void SetOwner(entity_player owner)
	{
		if ((bool)_owner)
		{
			throw new UnityException("Owner already set");
		}
		_owner = owner;
		SetupControls();
	}

	public void Update()
	{
		if ((bool)_owner)
		{
			if ((bool)_vehicle)
			{
				HandleVehicleMode();
				return;
			}
			HandleMove();
			HandleSpeedGel();
		}
	}

	public void LateUpdate()
	{
		if ((bool)_owner)
		{
			HandleAnimation();
		}
	}

	public bool IsMoving()
	{
		return _moving;
	}

	public void SetVehicle(entity_vehicle_seat vehicleSeat)
	{
		_vehicle = vehicleSeat;
	}

	public override float GetMaxSpeed()
	{
		if (!_owner)
		{
			return base.GetMaxSpeed();
		}
		float num = base.GetMaxSpeed();
		int curseCount = _owner.GetCurseCount(CURSE_TYPE.SLOW);
		if (curseCount > 0)
		{
			num *= Mathf.Pow(0.87f, curseCount);
		}
		float num2 = (HasItem("item_yeenskates") ? 2.8f : 0f);
		num *= 1f + num2 * 0.1f;
		if (_onSpeedGel || _gelMomentum > 0.1f)
		{
			num *= Mathf.Lerp(1f, GEL_MAX_SPEED_MULTIPLIER, Mathf.Clamp01(_gelMomentum));
		}
		return num;
	}

	public override Vector3 CalcVelocity(Vector3 vel, Vector3 desiredVelocity, float friction, bool isFluid, float deltaTime)
	{
		if (!_owner)
		{
			return base.CalcVelocity(vel, desiredVelocity, friction, isFluid, deltaTime);
		}
		if (GetVolumeType() == VolumeType.WORLD_FROZEN)
		{
			friction = 0.3f;
		}
		float num = (HasItem("item_yeenskates") ? 0.3f : 1f);
		if (_onSpeedGel)
		{
			num = GEL_FRICTION;
		}
		return base.CalcVelocity(vel, desiredVelocity, Mathf.Max(friction * num, 0f), isFluid, deltaTime);
	}

	public override float GetMaxBrakingDeceleration()
	{
		if (!_owner)
		{
			return base.GetMaxBrakingDeceleration();
		}
		if (_onSpeedGel)
		{
			return 0.1f;
		}
		if (GetMovementMode() == MovementMode.Walking)
		{
			return (GetVolumeType() != VolumeType.WORLD_FROZEN && !HasItem("item_yeenskates")) ? base.GetMaxBrakingDeceleration() : (_crouching ? 0.4f : 0f);
		}
		return base.GetMaxBrakingDeceleration();
	}

	public bool IsCrouching()
	{
		return _isCrouched;
	}

	public void Shove(Vector3 dir, float force)
	{
		AddForce(dir * force, ForceMode.VelocityChange);
	}

	public void OnDestroy()
	{
		if ((bool)_owner)
		{
			jumpAction.action.performed -= OnJumpStart;
			jumpAction.action.canceled -= OnJumpStop;
			crouchAction.action.performed -= OnCrouchStart;
			crouchAction.action.canceled -= OnCrouchStop;
		}
	}

	private bool HasItem(string itemID)
	{
		if (!_owner)
		{
			return false;
		}
		return _owner.GetInventory()?.HasItem(itemID) ?? false;
	}

	private void SetupControls()
	{
		if (!_owner)
		{
			throw new UnityException("Failed to call 'SetupControls', not owner!");
		}
		jumpAction.action.performed += OnJumpStart;
		jumpAction.action.canceled += OnJumpStop;
		crouchAction.action.performed += OnCrouchStart;
		crouchAction.action.canceled += OnCrouchStop;
	}

	private bool CheckSpeedGel()
	{
		if (!IsGrounded())
		{
			return false;
		}
		if (Physics.Raycast(base.transform.position + Vector3.up * 0.1f, Vector3.down, out var hitInfo, 0.5f, _gelLayer, QueryTriggerInteraction.Collide))
		{
			return hitInfo.collider.CompareTag("SPRAY/SPEED");
		}
		return false;
	}

	private void HandleSpeedGel()
	{
		_onSpeedGel = CheckSpeedGel();
		if (_onSpeedGel)
		{
			_gelMomentum = Mathf.MoveTowards(_gelMomentum, 1f, Time.deltaTime * 1.5f);
			Vector3 vector = Vector3.ProjectOnPlane(base.characterMovement.velocity, GetUpVector());
			if (_moving && vector.sqrMagnitude > 0.5f)
			{
				AddForce(vector.normalized * (GEL_ACCELERATION * _gelMomentum * Time.deltaTime), ForceMode.VelocityChange);
			}
		}
		else
		{
			_gelMomentum *= GEL_MOMENTUM_DECAY;
			if (_gelMomentum < 0.01f)
			{
				_gelMomentum = 0f;
			}
		}
	}

	private void HandleAnimation()
	{
		if (!_owner)
		{
			return;
		}
		Animator animator = _owner.GetAnimator();
		if ((bool)animator)
		{
			if (_owner.IsDead())
			{
				animator.SetBool(Swimming, value: false);
				animator.SetBool(Falling, value: false);
				animator.SetBool(Crouching, value: false);
				return;
			}
			float maxSpeed = GetMaxSpeed();
			bool flag = GetVolumeType() == VolumeType.WATER;
			bool flag2 = GetVolumeType() == VolumeType.QUICKSAND;
			bool flag3 = flag || flag2;
			animator.SetFloat(VelX, base.characterMovement.sidewaysSpeed / maxSpeed);
			animator.SetFloat(VelZ, base.characterMovement.forwardSpeed / maxSpeed);
			animator.SetBool(Crouching, _isCrouched);
			animator.SetBool(Swimming, flag);
			animator.SetBool(Falling, !flag3 && !IsGrounded());
		}
	}

	private void HandleVehicleMode()
	{
		if ((bool)_owner && (bool)_vehicle)
		{
			Vector3 vector = _vehicle.GetSeatPos() + _vehicle.GetSeatOffsetPos();
			SetPosition(vector);
		}
	}

	private void HandleMove()
	{
		if (!_owner || _owner.IsDead() || base.isPaused || !SDK.MainCamera)
		{
			return;
		}
		Vector2 vector = moveAction.action.ReadValue<Vector2>();
		Vector3 normalized = (base.transform.forward * vector.y + base.transform.right * vector.x).normalized;
		normalized = Vector3.ClampMagnitude(normalized, 1f);
		switch (GetVolumeType())
		{
		case VolumeType.WATER:
		{
			entity_movement_volume_water entity_movement_volume_water2 = base.physicsVolume as entity_movement_volume_water;
			Transform transform = SDK.MainCamera.transform;
			normalized = (transform.forward * vector.y + transform.right * vector.x).normalized;
			normalized = Vector3.ClampMagnitude(normalized, 1f);
			if (_jumping)
			{
				if (CalcImmersionDepth() > 0.65f)
				{
					normalized += GetUpVector();
				}
				else
				{
					LaunchCharacter(GetUpVector() * 5f, overrideVerticalVelocity: true);
				}
			}
			else if (_crouching && ((object)entity_movement_volume_water2 == null || entity_movement_volume_water2.canSwim))
			{
				normalized -= GetUpVector();
			}
			break;
		}
		case VolumeType.QUICKSAND:
			normalized -= GetUpVector();
			break;
		}
		entity_player_inventory inventory = _owner.GetInventory();
		if ((bool)inventory)
		{
			entity_item_lowgrav entity_item_lowgrav2 = inventory.FindItemByID("item_lowgrav") as entity_item_lowgrav;
			base.gravity = (((object)entity_item_lowgrav2 != null && entity_item_lowgrav2.IsActive()) ? (Physics.gravity * 0.35f) : Physics.gravity);
		}
		_moving = normalized.magnitude > 0f;
		SetMovementDirection(normalized);
	}

	private void OnJumpStart(InputAction.CallbackContext ctx)
	{
		_jumping = true;
		if (!IsFloating())
		{
			Jump();
		}
	}

	private void OnJumpStop(InputAction.CallbackContext ctx)
	{
		_jumping = false;
		StopJumping();
	}

	private void OnCrouchStart(InputAction.CallbackContext ctx)
	{
		_crouching = true;
		if (!IsFloating())
		{
			Crouch();
		}
	}

	private void OnCrouchStop(InputAction.CallbackContext ctx)
	{
		_crouching = false;
		UnCrouch();
	}

	private bool IsFloating()
	{
		VolumeType volumeType = GetVolumeType();
		return volumeType == VolumeType.WATER || volumeType == VolumeType.QUICKSAND;
	}

	private VolumeType GetVolumeType()
	{
		if (!base.physicsVolume)
		{
			return VolumeType.NONE;
		}
		if (base.physicsVolume is entity_movement_volume entity_movement_volume2)
		{
			return entity_movement_volume2.GetVolumeType();
		}
		throw new UnityException($"Invalid volume type {base.physicsVolume.GetType()}!");
	}

	protected override void UpdatePhysicsVolume(PhysicsVolume newPhysicsVolume)
	{
		if (newPhysicsVolume is entity_movement_volume entity_movement_volume2 && (bool)entity_movement_volume2.ignoreArea && entity_movement_volume2.ignoreArea.bounds.Contains(base.characterMovement.worldCenter))
		{
			newPhysicsVolume = null;
		}
		if ((bool)newPhysicsVolume)
		{
			Vector3 worldCenter = base.characterMovement.worldCenter;
			SetPhysicsVolume(newPhysicsVolume.boxCollider.bounds.Contains(worldCenter) ? newPhysicsVolume : null);
		}
		else
		{
			SetPhysicsVolume(null);
		}
	}

	public new void Pause(bool pause, bool clearState = true)
	{
		base.Pause(pause, clearState);
		base.characterMovement.collider.enabled = true;
		base.characterMovement.collider.isTrigger = pause;
	}

	public void ResetMovementState()
	{
		_jumping = false;
		_crouching = false;
		_onSpeedGel = false;
		_gelMomentum = 0f;
		base.physicsVolume = null;
		OnPhysicsVolumeChanged(null);
		SetMovementMode(MovementMode.Walking);
		UnCrouch();
		StopJumping();
	}

	protected override bool DoJump()
	{
		if (!_owner)
		{
			return base.DoJump();
		}
		Vector3 vector = -GetGravityDirection();
		if (base.characterMovement.isConstrainedToPlane && Mathf.Approximately(Vector3.Dot(base.characterMovement.GetPlaneConstraintNormal(), vector), 1f))
		{
			return false;
		}
		float num = base.jumpImpulse;
		entity_item_pickable entity_item_pickable2 = _owner.GetInventory()?.FindItemByID("item_yeenspring") ?? null;
		if ((bool)entity_item_pickable2)
		{
			num += 1.3f;
			NetController<SoundController>.Instance?.Play3DSound("Ingame/Items/Spring/spring_0.ogg", entity_item_pickable2.transform.position, new AudioData
			{
				pitch = Random.Range(0.7f, 1.3f),
				volume = 0.1f
			}, broadcast: true);
		}
		if (Physics.Raycast(base.transform.position + Vector3.up * 0.1f, Vector3.down, out var hitInfo, 0.5f, _gelLayer, QueryTriggerInteraction.Collide) && hitInfo.collider.CompareTag("SPRAY/JUMP"))
		{
			num += 1.8f;
			NetController<SoundController>.Instance?.Play3DSound("Ingame/Items/Gel/jump_0.ogg", base.transform.position, new AudioData
			{
				pitch = Random.Range(0.7f, 1.3f),
				volume = 0.4f
			}, broadcast: true);
		}
		float num2 = Mathf.Max(Vector3.Dot(base.characterMovement.velocity, vector), num);
		base.characterMovement.velocity = Vector3.ProjectOnPlane(base.characterMovement.velocity, vector) + vector * num2;
		return true;
	}
}
