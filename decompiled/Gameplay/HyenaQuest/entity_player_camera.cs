using System;
using FailCake;
using UnityEngine;
using UnityEngine.InputSystem;

namespace HyenaQuest;

public class entity_player_camera : MonoBehaviour
{
	public InputActionReference lookAction;

	public InputActionReference zoomAction;

	private static readonly float MIN_PITCH = -75f;

	private static readonly float MAX_PITCH = 75f;

	private static readonly float ZOOM_SPEED = 5.5f;

	private static readonly float SPECTATE_DISTANCE = 1.2f;

	private static readonly float SPECTATE_BODY_DISTANCE = 1.95f;

	private float _yawInput;

	private float _pitchInput;

	private float _sensitivity;

	private bool _locked;

	private entity_vehicle_seat _vehicle;

	private int _groundMask;

	private Vector3? _forcedPosition;

	private Quaternion? _forcedRotation;

	private LayerMask _originalCullingMask;

	private util_fade_timer _zoomTimer;

	private entity_player_movement _characterMovement;

	private readonly RaycastHit[] _results = new RaycastHit[5];

	private readonly Collider[] _overlapResults = new Collider[5];

	private Transform _spectateTarget;

	public void Awake()
	{
		if (!lookAction)
		{
			throw new UnityException("Missing lookAction InputActionReference");
		}
		if (!zoomAction)
		{
			throw new UnityException("Missing zoomAction InputActionReference");
		}
		_characterMovement = GetComponentInParent<entity_player_movement>(includeInactive: true);
		if (!_characterMovement)
		{
			throw new UnityException("Invalid entity_player, missing entity_player_character_movement object");
		}
		_groundMask = LayerMask.GetMask("entity_ground");
	}

	public void Setup()
	{
		if (!PlayerController.LOCAL)
		{
			throw new Exception("Invalid entity_player, missing PlayerController.LOCAL object");
		}
		if (!SDK.MainCamera)
		{
			throw new Exception("Invalid entity_player, missing Camera object");
		}
		SDK.MainCamera.fieldOfView = GetFOV();
		SDK.MainCamera.transform.parent = PlayerController.LOCAL.view;
		SDK.MainCamera.transform.localEulerAngles = Vector3.zero;
		SDK.MainCamera.transform.localPosition = Vector3.zero;
		_originalCullingMask = SDK.MainCamera.cullingMask;
		_yawInput = PlayerController.LOCAL.transform.eulerAngles.y;
		if ((bool)MonoController<UIController>.Instance)
		{
			MonoController<UIController>.Instance.SetFade(fadeIn: false);
		}
		zoomAction.action.canceled += OnZoomCanceled;
		zoomAction.action.performed += OnZoomPerformed;
		CoreController.WaitFor(delegate(SettingsController settingsCtrl)
		{
			settingsCtrl.OnSettingsUpdated += new Action(OnSettingsUpdated);
		});
	}

	public void LateUpdate()
	{
		if (!base.isActiveAndEnabled || !SDK.MainCamera)
		{
			return;
		}
		if (IsBeingForced())
		{
			SDK.MainCamera.transform.position = _forcedPosition.GetValueOrDefault();
			SDK.MainCamera.transform.rotation = _forcedRotation.GetValueOrDefault();
			SDK.MainCamera.fieldOfView = 60f;
			return;
		}
		HandleView();
		entity_player lOCAL = PlayerController.LOCAL;
		if ((object)lOCAL == null || lOCAL.IsDead())
		{
			UpdateSpectateView();
		}
		else
		{
			UpdateView();
		}
	}

	public void Update()
	{
		if (base.isActiveAndEnabled && !_forcedPosition.HasValue && !_forcedRotation.HasValue)
		{
			CheckIfCameraInsideGround();
		}
	}

	public void RenderPlayerOnly(bool render)
	{
		if ((bool)SDK.MainCamera)
		{
			SDK.MainCamera.cullingMask = (render ? LayerMask.GetMask("entity_player", "UI") : ((int)_originalCullingMask));
		}
	}

	public void OnDestroy()
	{
		_zoomTimer?.Stop();
		if ((bool)MonoController<SettingsController>.Instance)
		{
			MonoController<SettingsController>.Instance.OnSettingsUpdated -= new Action(OnSettingsUpdated);
		}
		if ((bool)zoomAction)
		{
			zoomAction.action.canceled -= OnZoomCanceled;
			zoomAction.action.performed -= OnZoomPerformed;
		}
	}

	public void Spectate(Transform target)
	{
		if ((bool)PlayerController.LOCAL && (bool)SDK.MainCamera && !IsBeingForced())
		{
			_locked = false;
			_spectateTarget = target;
			SDK.MainCamera.fieldOfView = GetFOV();
			SDK.MainCamera.transform.localEulerAngles = Vector3.zero;
			SDK.MainCamera.transform.localPosition = Vector3.zero;
			_pitchInput = ((!_spectateTarget) ? (-45f) : _spectateTarget.transform.eulerAngles.x);
			_yawInput = ((!_spectateTarget) ? PlayerController.LOCAL.transform.eulerAngles.y : _spectateTarget.transform.eulerAngles.y);
		}
	}

	public void LockCamera(bool locked)
	{
		_locked = locked;
	}

	public bool IsCameraLocked()
	{
		return _locked;
	}

	public float GetPitch()
	{
		return _pitchInput;
	}

	public void SetVehicle(entity_vehicle_seat vehicleSeat)
	{
		_vehicle = vehicleSeat;
	}

	public void ForceLookAt(Vector3 position, Quaternion rotation)
	{
		_forcedPosition = position;
		_forcedRotation = rotation;
	}

	public void ResetCamera()
	{
		if ((bool)SDK.MainCamera)
		{
			SDK.MainCamera.transform.localPosition = Vector3.zero;
			SDK.MainCamera.transform.localRotation = Quaternion.identity;
			SDK.MainCamera.fieldOfView = GetFOV();
			_yawInput = PlayerController.LOCAL.transform.eulerAngles.y;
			_pitchInput = PlayerController.LOCAL.transform.eulerAngles.x;
			_forcedPosition = null;
			_forcedRotation = null;
			RenderPlayerOnly(render: false);
		}
	}

	public void LookAt(Transform target, float forceLookSpeed)
	{
		if ((bool)target && (bool)SDK.MainCamera)
		{
			Vector3 position = new Vector3(0.5f, 0.5f, SDK.MainCamera.nearClipPlane);
			Vector3 eulerAngles = Quaternion.LookRotation(target.position - SDK.MainCamera.ViewportToWorldPoint(position)).eulerAngles;
			_pitchInput = Mathf.LerpAngle(_pitchInput, 0f - eulerAngles.x, forceLookSpeed * Time.deltaTime);
			_yawInput = Mathf.LerpAngle(_yawInput, eulerAngles.y, forceLookSpeed * Time.deltaTime);
			LimitRotation();
		}
	}

	private bool IsBeingForced()
	{
		if (_forcedPosition.HasValue)
		{
			return _forcedRotation.HasValue;
		}
		return false;
	}

	private void LimitRotation()
	{
		_pitchInput = MathUtils.ClampAngle(_pitchInput, MIN_PITCH, MAX_PITCH);
		_yawInput = MathUtils.ClampAngle(_yawInput, 0f, 360f);
		if ((bool)_vehicle && _vehicle.limit)
		{
			_pitchInput = MathUtils.ClampAngle(_pitchInput, 0f - _vehicle.pitchLimit, _vehicle.pitchLimit);
			_yawInput = MathUtils.ClampAngle(_yawInput, 0f - _vehicle.yawLimit, _vehicle.yawLimit);
		}
	}

	private void OnSettingsUpdated()
	{
		if ((bool)MonoController<SettingsController>.Instance && (bool)SDK.MainCamera)
		{
			_sensitivity = MonoController<SettingsController>.Instance.CurrentSettings.mouseSensitivity;
			SDK.MainCamera.fieldOfView = GetFOV();
		}
	}

	private float GetFOV()
	{
		if (!MonoController<SettingsController>.Instance)
		{
			return 60f;
		}
		return Mathf.Clamp(MonoController<SettingsController>.Instance.CurrentSettings.fov, 60f, 120f);
	}

	private void UpdateSpectateView()
	{
		if ((bool)SDK.MainCamera && (bool)PlayerController.LOCAL)
		{
			Transform obj = SDK.MainCamera.transform;
			Vector3 vector = new Vector3(0f, 0f, _spectateTarget ? (0f - SPECTATE_DISTANCE) : (0f - SPECTATE_BODY_DISTANCE));
			Quaternion quaternion = Quaternion.Euler(0f - _pitchInput, _yawInput, 0f);
			Vector3 vector2 = (_spectateTarget ? _spectateTarget.position : PlayerController.LOCAL.GetDeathLocation());
			obj.position = vector2 + quaternion * vector;
			obj.LookAt(vector2);
		}
	}

	private void UpdateView()
	{
		if (!SDK.MainCamera)
		{
			return;
		}
		entity_player lOCAL = PlayerController.LOCAL;
		if ((object)lOCAL == null || lOCAL.IsDead())
		{
			return;
		}
		entity_player_movement movement = PlayerController.LOCAL.GetMovement();
		if ((bool)movement)
		{
			SDK.MainCamera.transform.localPosition = NetController<ShakeController>.Instance.ApplyShakes(new Vector3(0f, 0.08f, 0f));
			Transform view = PlayerController.LOCAL.view;
			Transform head = PlayerController.LOCAL.head;
			float t = Mathf.Max(b: Vector3.Distance(view.position, head.position) * 3f, a: movement.IsCrouching() ? 12 : 4) * Time.deltaTime;
			view.localRotation = Quaternion.Euler(0f - _pitchInput, 0f, 0f);
			view.position = Vector3.Lerp(view.position, head.position + head.forward * 0.025f + head.up * 0.08f, t);
			float num = _yawInput;
			if ((bool)_vehicle && _vehicle.limit)
			{
				num += _vehicle.transform.eulerAngles.y;
			}
			_characterMovement.SetRotation(Quaternion.Euler(0f, num, 0f));
		}
	}

	private void CheckIfCameraInsideGround()
	{
		if (!SDK.MainCamera || !MonoController<UIController>.Instance)
		{
			return;
		}
		entity_player lOCAL = PlayerController.LOCAL;
		if ((object)lOCAL == null || lOCAL.IsDead())
		{
			MonoController<UIController>.Instance.SetViewBlocked(blocked: false);
			return;
		}
		Vector3 position = SDK.MainCamera.transform.position;
		Vector3 position2 = PlayerController.LOCAL.neck.position;
		Vector3 vector = position - position2;
		float magnitude = vector.magnitude;
		int num = Physics.RaycastNonAlloc(position2, vector.normalized, _results, magnitude, _groundMask);
		for (int i = 0; i < num; i++)
		{
			if ((bool)_results[i].collider && !_results[i].collider.isTrigger && !_results[i].collider.CompareTag("OCCLUDER/VIEW-IGNORE"))
			{
				MonoController<UIController>.Instance.SetViewBlocked(blocked: true);
				return;
			}
		}
		int num2 = Physics.OverlapSphereNonAlloc(position, 0.05f, _overlapResults, _groundMask, QueryTriggerInteraction.Ignore);
		for (int j = 0; j < num2; j++)
		{
			if ((bool)_overlapResults[j] && !_overlapResults[j].CompareTag("OCCLUDER/VIEW-IGNORE"))
			{
				MonoController<UIController>.Instance.SetViewBlocked(blocked: true);
				return;
			}
		}
		MonoController<UIController>.Instance.SetViewBlocked(blocked: false);
	}

	private void HandleView()
	{
		if (!IsCameraLocked() && Application.isFocused && (bool)MonoController<SettingsController>.Instance)
		{
			PlayerSettings currentSettings = MonoController<SettingsController>.Instance.CurrentSettings;
			Vector2 vector = lookAction.action.ReadValue<Vector2>();
			float num = _sensitivity * 0.25f;
			_pitchInput += (currentSettings.invertMouseY ? (0f - vector.y) : vector.y) * num;
			_yawInput += vector.x * num;
			LimitRotation();
		}
	}

	private void OnZoomPerformed(InputAction.CallbackContext ctx)
	{
		if (!SDK.MainCamera)
		{
			return;
		}
		entity_player lOCAL = PlayerController.LOCAL;
		if ((object)lOCAL == null || lOCAL.IsDead() || IsBeingForced())
		{
			return;
		}
		_zoomTimer?.Stop();
		_zoomTimer = util_fade_timer.Fade(ZOOM_SPEED, SDK.MainCamera.fieldOfView, GetFOV() - 35f, delegate(float val)
		{
			if ((bool)SDK.MainCamera)
			{
				SDK.MainCamera.fieldOfView = val;
			}
		});
	}

	private void OnZoomCanceled(InputAction.CallbackContext ctx)
	{
		if (!SDK.MainCamera)
		{
			return;
		}
		_zoomTimer?.Stop();
		_zoomTimer = util_fade_timer.Fade(ZOOM_SPEED, SDK.MainCamera.fieldOfView, GetFOV(), delegate(float val)
		{
			if ((bool)SDK.MainCamera)
			{
				SDK.MainCamera.fieldOfView = val;
			}
		});
	}
}
