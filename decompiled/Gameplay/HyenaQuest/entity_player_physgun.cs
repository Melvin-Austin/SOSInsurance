using System;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

namespace HyenaQuest;

public class entity_player_physgun : NetworkBehaviour
{
	private static readonly float PHYS_MAX_GRAB_DISTANCE = 2.2f;

	private static readonly float PHYS_MIN_GRAB_DISTANCE = 0.6f;

	private static readonly float PHYS_ROTATION_LERP_SPEED = 85f;

	public LineRenderer physLine;

	public Transform MidPoint;

	public InputActionReference rotatePropAction;

	public InputActionReference throwAction;

	public InputActionReference cycleAction;

	private static readonly int Color = Shader.PropertyToID("_Color");

	private Quaternion _rotationDifference;

	private Quaternion _desiredRotation;

	private Vector3 _grabMouseCycle;

	private float _currentGrabDistance;

	private entity_player _owner;

	private bool _unlockedAchievement;

	private bool _throwPressed;

	private float _throwCharge;

	private readonly Vector3[] _cachedDirections = new Vector3[6];

	private readonly NetVar<NetworkBehaviourReference> _grabbingObject = new NetVar<NetworkBehaviourReference>(default(NetworkBehaviourReference), NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

	private readonly NetVar<Vector3> _grabbingObjectPoint = new NetVar<Vector3>(default(Vector3), NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

	private readonly NetVar<bool> _isProtected = new NetVar<bool>(value: false);

	public void Awake()
	{
		if (!physLine)
		{
			throw new UnityException("Missing line renderer");
		}
		if (!MidPoint)
		{
			throw new UnityException("Missing MidPoint transform");
		}
		if (!rotatePropAction)
		{
			throw new UnityException("Missing rotatePropAction InputActionReference");
		}
		if (!throwAction)
		{
			throw new UnityException("Missing throwAction InputActionReference");
		}
		if (!cycleAction)
		{
			throw new UnityException("Missing cycleAction InputActionReference");
		}
		_owner = GetComponent<entity_player>();
		if (!_owner)
		{
			throw new UnityException("entity_player_physgun requires entity_player component");
		}
		physLine.enabled = false;
		physLine.positionCount = 16;
	}

	public override void OnNetworkSpawn()
	{
		base.OnNetworkSpawn();
		if (base.IsClient && base.IsOwner)
		{
			SetupControls();
			_owner.OnHealthUpdate += new Action<int, bool>(OnTakeDamage);
			_owner.OnHealthStatusUpdate += new Action<bool>(OnHealthStatusUpdate);
		}
	}

	public override void OnNetworkDespawn()
	{
		base.OnNetworkDespawn();
		if (base.IsClient && base.IsOwner)
		{
			if ((bool)rotatePropAction)
			{
				rotatePropAction.action.performed -= OnReload;
				rotatePropAction.action.canceled -= OnReloadCancel;
			}
			if ((bool)throwAction)
			{
				throwAction.action.performed -= OnThrowStart;
				throwAction.action.canceled -= OnThrowEnd;
			}
			if ((bool)_owner)
			{
				_owner.OnHealthUpdate -= new Action<int, bool>(OnTakeDamage);
				_owner.OnHealthStatusUpdate -= new Action<bool>(OnHealthStatusUpdate);
			}
		}
	}

	public void LateUpdate()
	{
		if ((bool)_owner)
		{
			entity_phys grabbingObject = GetGrabbingObject();
			physLine.enabled = grabbingObject;
			if ((bool)grabbingObject)
			{
				Vector3 c = grabbingObject.transform.TransformPoint(_grabbingObjectPoint.Value);
				UpdateArcPoints(physLine, physLine.transform.position, MidPoint.position, c);
			}
		}
	}

	public Vector3 GetGrabbingPoint(bool world = false)
	{
		entity_phys grabbingObject = GetGrabbingObject();
		if ((bool)grabbingObject)
		{
			if (!world)
			{
				return _grabbingObjectPoint.Value;
			}
			return grabbingObject.transform.TransformPoint(_grabbingObjectPoint.Value);
		}
		return Vector3.zero;
	}

	public void FixedUpdate()
	{
		entity_phys grabbingObject = GetGrabbingObject();
		if (!grabbingObject || !base.IsOwner || !grabbingObject.IsOwner || !SDK.MainCamera || !_owner)
		{
			return;
		}
		if (grabbingObject.GetVelocity().sqrMagnitude > 3f && !_unlockedAchievement && Physics.Raycast(_owner.transform.position, Vector3.down, out var hitInfo, 2f) && (bool)hitInfo.collider && hitInfo.collider.transform == grabbingObject.transform)
		{
			_unlockedAchievement = true;
			NetController<StatsController>.Instance?.UnlockAchievementRPC(STEAM_ACHIEVEMENTS.ACHIEVEMENT_SURFER, base.RpcTarget.Server);
		}
		if (_owner.IsDead() || grabbingObject.IsLocked() || (grabbingObject.IsBeingGrabbed() && grabbingObject.GetGrabbingOwner() != _owner) || (grabbingObject is entity_item_pickable entity_item_pickable2 && entity_item_pickable2.HasOwner()))
		{
			SetGrabbingObject(null);
			return;
		}
		if (Vector3.Distance(grabbingObject.transform.position, _owner.view.transform.position) > PHYS_MAX_GRAB_DISTANCE * 2f)
		{
			SetGrabbingObject(null);
			return;
		}
		Vector3 up = Vector3.zero;
		Vector3 forward = Vector3.zero;
		Vector3 right = Vector3.zero;
		NearestTransformDirection(grabbingObject.transform, base.transform, ref up, ref forward, ref right);
		Vector2 vector = cycleAction.action.ReadValue<Vector2>();
		if (vector.y != 0f)
		{
			_currentGrabDistance = Mathf.Clamp(_currentGrabDistance + vector.y * 0.1f, PHYS_MIN_GRAB_DISTANCE, PHYS_MAX_GRAB_DISTANCE);
		}
		bool num = rotatePropAction.action.ReadValue<float>() >= 1f;
		Vector3 zero = Vector3.zero;
		PlayerSettings currentSettings = MonoController<SettingsController>.Instance.CurrentSettings;
		if (num)
		{
			Vector2 vector2 = Mouse.current.delta.ReadValue();
			float num2 = currentSettings.physgunRotateSensitivity * 0.15f;
			if (Mathf.Abs(vector2.x) > 0.8f)
			{
				zero.x = vector2.x * num2;
			}
			if (Mathf.Abs(vector2.y) > 0.8f)
			{
				zero.y = vector2.y * num2;
			}
		}
		Quaternion quaternion = Quaternion.AngleAxis(currentSettings.invertPhysRotationY ? (0f - zero.y) : zero.y, right) * Quaternion.AngleAxis(currentSettings.invertPhysRotationX ? (0f - zero.x) : zero.x, up) * _desiredRotation;
		Quaternion quaternion2 = base.transform.rotation * _rotationDifference;
		_desiredRotation = ((zero != Vector3.zero) ? quaternion : quaternion2);
		_rotationDifference = Quaternion.Inverse(base.transform.rotation) * _desiredRotation;
		Vector3 point = SDK.MainCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f)).GetPoint(_currentGrabDistance);
		Vector3 vector3 = grabbingObject.transform.TransformVector(_grabbingObjectPoint.Value);
		Vector3 velocity = SimulatePhysics(point - vector3, grabbingObject.transform.position);
		if (_throwPressed)
		{
			_throwCharge = Mathf.Clamp(_throwCharge + Time.fixedDeltaTime, 0f, 5f);
			velocity += UnityEngine.Random.insideUnitSphere * (_throwCharge * 0.35f);
			velocity += (_owner.view.transform.position - grabbingObject.transform.position).normalized * (_throwCharge * 0.5f);
		}
		grabbingObject.SetVelocity(velocity);
		grabbingObject.SetRotation(Quaternion.Lerp(grabbingObject.transform.rotation, _desiredRotation, Time.fixedDeltaTime * PHYS_ROTATION_LERP_SPEED));
		MidPoint.position = point;
	}

	public bool OnUse(RaycastHit? aimingHit)
	{
		if (!base.IsOwner || !base.IsClient)
		{
			return false;
		}
		if ((bool)GetGrabbingObject())
		{
			SetGrabbingObject(null);
			return true;
		}
		if (!aimingHit.HasValue || !(aimingHit?.collider))
		{
			return false;
		}
		if (aimingHit.Value.collider.TryGetComponent<entity_phys>(out var component, 2))
		{
			if (!component.IsSpawned || !component.CanGrab())
			{
				return false;
			}
			_rotationDifference = Quaternion.Inverse(base.transform.rotation) * component.transform.rotation;
			_currentGrabDistance = Mathf.Clamp(aimingHit.Value.distance, PHYS_MIN_GRAB_DISTANCE, PHYS_MAX_GRAB_DISTANCE);
			_grabbingObjectPoint.Value = aimingHit.Value.transform.InverseTransformVector(aimingHit.Value.point - aimingHit.Value.transform.position);
			SetGrabbingObject(component);
		}
		return false;
	}

	protected override void OnNetworkPostSpawn()
	{
		base.OnNetworkPostSpawn();
		if (base.IsOwner)
		{
			_grabbingObject.RegisterOnValueChanged(delegate(NetworkBehaviourReference prevValue, NetworkBehaviourReference newValue)
			{
				entity_phys entity_phys2 = NETController.Get<entity_phys>(prevValue);
				entity_phys entity_phys3 = NETController.Get<entity_phys>(newValue);
				if (!(entity_phys2 == entity_phys3))
				{
					if ((bool)entity_phys2)
					{
						entity_phys2.SetGrabbing(grabbing: false);
					}
					if ((bool)entity_phys3)
					{
						entity_phys3.SetGrabbing(grabbing: true);
						NetController<ShakeController>.Instance?.LocalShake(ShakeMode.SHAKE_ALL, 0.02f);
					}
					else
					{
						OnReloadCancel(default(InputAction.CallbackContext));
					}
				}
			});
		}
		_isProtected.RegisterOnValueChanged(delegate(bool _, bool newValue)
		{
			if ((bool)physLine)
			{
				physLine.material.SetColor(Color, (newValue ? UnityEngine.Color.orange : UnityEngine.Color.white) * 16f);
			}
		});
	}

	public override void OnNetworkPreDespawn()
	{
		base.OnNetworkPreDespawn();
		if (base.IsOwner)
		{
			_grabbingObject.OnValueChanged = null;
			_isProtected.OnValueChanged = null;
		}
	}

	[Client]
	public void SetGrabbingObject(entity_phys obj)
	{
		if (!base.IsOwner)
		{
			throw new UnityException("Owner only");
		}
		if (!obj || (obj.IsSpawned && obj.CanGrab()))
		{
			_grabbingObject.Value = obj;
		}
	}

	public entity_phys GetGrabbingObject()
	{
		return NETController.Get<entity_phys>(_grabbingObject.Value);
	}

	public bool IsGrabbing()
	{
		return GetGrabbingObject();
	}

	[Server]
	public void SetProtected(bool protect)
	{
		if (!base.IsServer)
		{
			throw new UnityException("Server only");
		}
		_isProtected.Value = protect;
	}

	public bool IsProtected()
	{
		return _isProtected.Value;
	}

	private static Vector3 SimulatePhysics(Vector3 dest, Vector3 physPos)
	{
		return (dest - physPos) / Time.fixedDeltaTime * 0.3f / 2f;
	}

	[Client]
	private void OnTakeDamage(int health, bool server)
	{
		if (!server && (bool)GetGrabbingObject() && UnityEngine.Random.Range(0, 100) > 70)
		{
			SetGrabbingObject(null);
		}
	}

	private void OnHealthStatusUpdate(bool dead)
	{
		SetGrabbingObject(null);
	}

	private void SetupControls()
	{
		if (base.IsOwner)
		{
			rotatePropAction.action.performed += OnReload;
			rotatePropAction.action.canceled += OnReloadCancel;
			throwAction.action.performed += OnThrowStart;
			throwAction.action.canceled += OnThrowEnd;
		}
	}

	private void OnThrowStart(InputAction.CallbackContext obj)
	{
		if (base.IsOwner && (bool)_owner)
		{
			_throwPressed = true;
		}
	}

	private void OnThrowEnd(InputAction.CallbackContext obj)
	{
		if (base.IsOwner && (bool)_owner)
		{
			entity_phys grabbingObject = GetGrabbingObject();
			if ((bool)grabbingObject && _throwCharge > 0.35f)
			{
				SetGrabbingObject(null);
				grabbingObject.SetVelocity(_owner.view.transform.forward * (_throwCharge * 3f));
				grabbingObject.OnThrow();
			}
			_throwPressed = false;
			_throwCharge = 0f;
		}
	}

	private void OnReloadCancel(InputAction.CallbackContext obj)
	{
		if (base.IsOwner && (bool)_owner)
		{
			_owner.GetCamera().LockCamera(locked: false);
		}
	}

	private void OnReload(InputAction.CallbackContext obj)
	{
		if (base.IsOwner && (bool)_owner && !_owner.IsDead())
		{
			entity_phys grabbingObject = GetGrabbingObject();
			if ((bool)grabbingObject)
			{
				_desiredRotation = grabbingObject.transform.rotation;
				_owner.GetCamera().LockCamera(locked: true);
			}
		}
	}

	private static void UpdateArcPoints(LineRenderer physLine, Vector3 a, Vector3 b, Vector3 c)
	{
		if (!physLine)
		{
			throw new UnityException("Missing line renderer");
		}
		b = Vector3.Lerp(a, b, 0.5f);
		for (int i = 1; i < physLine.positionCount - 1; i++)
		{
			float t = (float)i / (float)(physLine.positionCount - 1);
			physLine.SetPosition(i, Vector3.Lerp(Vector3.Lerp(a, b, t), Vector3.Lerp(b, c, t), t));
		}
		physLine.SetPosition(0, a);
		physLine.SetPosition(physLine.positionCount - 1, c);
	}

	private void NearestTransformDirection(Transform transformToCheck, Transform referenceTransform, ref Vector3 up, ref Vector3 forward, ref Vector3 right)
	{
		_cachedDirections[0] = transformToCheck.forward;
		_cachedDirections[1] = -transformToCheck.forward;
		_cachedDirections[2] = transformToCheck.up;
		_cachedDirections[3] = -transformToCheck.up;
		_cachedDirections[4] = transformToCheck.right;
		_cachedDirections[5] = -transformToCheck.right;
		up = GetBestDirection(_cachedDirections, referenceTransform.up, 0);
		forward = GetBestDirection(_cachedDirections, referenceTransform.forward, 1, up);
		right = GetBestDirection(_cachedDirections, referenceTransform.right, 2, up, forward);
	}

	private Vector3 GetBestDirection(Vector3[] directions, Vector3 target, int excludeCount, Vector3 exclude1 = default(Vector3), Vector3 exclude2 = default(Vector3))
	{
		float num = float.NegativeInfinity;
		Vector3 result = Vector3.zero;
		foreach (Vector3 vector in directions)
		{
			if ((excludeCount < 1 || (!(vector == exclude1) && !(vector == -exclude1))) && (excludeCount < 2 || (!(vector == exclude2) && !(vector == -exclude2))))
			{
				float num2 = Vector3.Dot(target, vector);
				if (num2 > num)
				{
					num = num2;
					result = vector;
				}
			}
		}
		return result;
	}

	protected override void __initializeVariables()
	{
		if (_grabbingObject == null)
		{
			throw new Exception("entity_player_physgun._grabbingObject cannot be null. All NetworkVariableBase instances must be initialized.");
		}
		_grabbingObject.Initialize(this);
		__nameNetworkVariable(_grabbingObject, "_grabbingObject");
		NetworkVariableFields.Add(_grabbingObject);
		if (_grabbingObjectPoint == null)
		{
			throw new Exception("entity_player_physgun._grabbingObjectPoint cannot be null. All NetworkVariableBase instances must be initialized.");
		}
		_grabbingObjectPoint.Initialize(this);
		__nameNetworkVariable(_grabbingObjectPoint, "_grabbingObjectPoint");
		NetworkVariableFields.Add(_grabbingObjectPoint);
		if (_isProtected == null)
		{
			throw new Exception("entity_player_physgun._isProtected cannot be null. All NetworkVariableBase instances must be initialized.");
		}
		_isProtected.Initialize(this);
		__nameNetworkVariable(_isProtected, "_isProtected");
		NetworkVariableFields.Add(_isProtected);
		base.__initializeVariables();
	}

	protected override void __initializeRpcs()
	{
		base.__initializeRpcs();
	}

	protected internal override string __getTypeName()
	{
		return "entity_player_physgun";
	}
}
