using System;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;

namespace HyenaQuest;

[RequireComponent(typeof(NetworkObject))]
public class entity_movement_train : NetworkBehaviour
{
	public Point startPoint;

	public Point endPoint;

	public float speed = 1f;

	public bool startActive;

	public bool loop;

	public bool shuffleLength = true;

	public bool shuffleModules = true;

	private entity_movement_train_module[] _trainModules;

	private entity_ambient_sound _soundCollider;

	private NetworkTransform _rootTransform;

	private float _nextMovementStartTime;

	private float _journeyLength;

	private float _totalTrackDistance;

	private readonly NetVar<byte> _maxTrains = new NetVar<byte>(0);

	private readonly NetVar<bool> _active = new NetVar<bool>(value: false);

	public void Awake()
	{
		_trainModules = GetComponentsInChildren<entity_movement_train_module>(includeInactive: true);
		entity_movement_train_module[] trainModules = _trainModules;
		if (trainModules == null || trainModules.Length <= 0)
		{
			throw new UnityException("Train needs at least 1 module");
		}
		_rootTransform = GetComponent<NetworkTransform>();
		if (!_rootTransform)
		{
			_rootTransform = GetComponentInChildren<NetworkTransform>(includeInactive: true);
		}
		if (!_rootTransform)
		{
			throw new UnityException("Missing train root");
		}
		_soundCollider = GetComponentInChildren<entity_ambient_sound>(includeInactive: true);
	}

	public override void OnNetworkSpawn()
	{
		base.OnNetworkSpawn();
		_totalTrackDistance = Vector3.Distance(startPoint.pos, endPoint.pos);
		float num = speed * ((endPoint.speedModifier > 0f) ? endPoint.speedModifier : 1f);
		if (_totalTrackDistance < 0.001f)
		{
			_journeyLength = Quaternion.Angle(Quaternion.Euler(startPoint.angle), Quaternion.Euler(endPoint.angle)) / (num * 90f);
		}
		else
		{
			_journeyLength = _totalTrackDistance / num;
		}
		if (base.IsServer && startActive)
		{
			StartMovement();
		}
	}

	[Server]
	public void StartMovement()
	{
		if (!base.IsServer)
		{
			throw new UnityException("Server Only");
		}
		if (!_active.Value)
		{
			_active.SetSpawnValue(value: true);
			OnLoopComplete();
		}
	}

	[Server]
	public void StopMovement()
	{
		if (!base.IsServer)
		{
			throw new UnityException("Server Only");
		}
		if (_active.Value)
		{
			_active.SetSpawnValue(value: false);
			ResetTrain();
		}
	}

	protected override void OnNetworkPostSpawn()
	{
		base.OnNetworkPostSpawn();
		if (!base.IsClient)
		{
			return;
		}
		_active.RegisterOnValueChanged(delegate(bool _, bool newValue)
		{
			if (!newValue)
			{
				entity_movement_train_module[] trainModules = _trainModules;
				foreach (entity_movement_train_module entity_movement_train_module2 in trainModules)
				{
					if (entity_movement_train_module2.gameObject.activeSelf)
					{
						entity_movement_train_module2.gameObject.SetActive(value: false);
					}
				}
			}
		});
		UpdateMovement();
	}

	public override void OnNetworkPreDespawn()
	{
		base.OnNetworkPreDespawn();
		if (base.IsClient)
		{
			_active.OnValueChanged = null;
		}
	}

	[Server]
	private void OnLoopComplete()
	{
		_nextMovementStartTime = ((endPoint.delay != Vector2.zero) ? (Time.time + UnityEngine.Random.Range(endPoint.delay.x, endPoint.delay.y)) : Time.time);
		int num = (shuffleLength ? UnityEngine.Random.Range(1, _trainModules.Length + 1) : _trainModules.Length);
		_maxTrains.SetSpawnValue((byte)num);
		if (shuffleModules)
		{
			_trainModules.ShuffleExcept(0);
		}
		ResetTrain();
	}

	private void UpdateSoundCollider()
	{
		if (!_soundCollider || !_active.Value)
		{
			return;
		}
		Collider[] area = _soundCollider.GetArea();
		if (area == null || area.Length <= 0 || !(area[0] is BoxCollider boxCollider))
		{
			return;
		}
		Bounds bounds = default(Bounds);
		bool flag = false;
		entity_movement_train_module[] trainModules = _trainModules;
		foreach (entity_movement_train_module entity_movement_train_module2 in trainModules)
		{
			GameObject obj = entity_movement_train_module2.gameObject;
			if ((object)obj != null && !obj.activeSelf)
			{
				continue;
			}
			Bounds bounds2 = entity_movement_train_module2.GetBounds();
			if (!(bounds2.size == Vector3.zero))
			{
				if (!flag)
				{
					bounds = bounds2;
					flag = true;
				}
				else
				{
					bounds.Encapsulate(bounds2);
				}
			}
		}
		if (flag)
		{
			Vector3 lhs = base.transform.InverseTransformPoint(bounds.min);
			Vector3 rhs = base.transform.InverseTransformPoint(bounds.max);
			Vector3 vector = Vector3.Min(lhs, rhs);
			Vector3 vector2 = Vector3.Max(lhs, rhs);
			Vector3 size = vector2 - vector;
			Vector3 center = (vector + vector2) * 0.5f;
			boxCollider.size = size;
			boxCollider.center = center;
		}
		else
		{
			boxCollider.size = Vector3.one * 0.1f;
			boxCollider.center = Vector3.zero;
		}
		_soundCollider.gameObject.SetActive(flag);
	}

	[Server]
	private void ResetTrain()
	{
		if (!base.IsServer)
		{
			throw new UnityException("Server only");
		}
		NetworkTransform rootTransform = _rootTransform;
		if ((object)rootTransform != null && rootTransform.IsSpawned)
		{
			_rootTransform.SetState(startPoint.pos, Quaternion.Euler(startPoint.angle), Vector3.one, teleportDisabled: false);
		}
	}

	private void UpdateMovement()
	{
		if (!_active.Value)
		{
			return;
		}
		int value = _maxTrains.Value;
		if (value == 0)
		{
			return;
		}
		float[] array = new float[value];
		for (int i = 1; i < value; i++)
		{
			array[i] = array[i - 1] + _trainModules[i - 1].moduleLength;
		}
		if (base.IsServer)
		{
			if (Time.time < _nextMovementStartTime)
			{
				return;
			}
			float num = (Time.time - _nextMovementStartTime) / _journeyLength * _totalTrackDistance;
			Vector3 normalized = (endPoint.pos - startPoint.pos).normalized;
			Vector3 localPosition;
			Quaternion localRotation;
			if (num <= _totalTrackDistance)
			{
				float t = num / _totalTrackDistance;
				localPosition = (endPoint.smoothPos ? Vector3.Lerp(startPoint.pos, endPoint.pos, t) : endPoint.pos);
				localRotation = (endPoint.smoothAngle ? Quaternion.Slerp(Quaternion.Euler(startPoint.angle), Quaternion.Euler(endPoint.angle), t) : Quaternion.Euler(endPoint.angle));
			}
			else
			{
				float num2 = num - _totalTrackDistance;
				localPosition = endPoint.pos + normalized * num2;
				localRotation = Quaternion.Euler(endPoint.angle);
			}
			if ((bool)_rootTransform && _rootTransform.IsSpawned)
			{
				_rootTransform.transform.localPosition = localPosition;
				_rootTransform.transform.localRotation = localRotation;
			}
		}
		Vector3 vector = endPoint.pos - startPoint.pos;
		Vector3 localPosition2 = _rootTransform.transform.localPosition;
		float num3 = Mathf.Max(0f, Vector3.Dot(localPosition2 - startPoint.pos, vector.normalized));
		bool flag = value > 0;
		for (int j = 0; j < _trainModules.Length; j++)
		{
			bool flag2 = false;
			float num4 = 0f;
			if (j < value)
			{
				num4 = num3 - array[j];
				if (num4 < _totalTrackDistance - 0.01f)
				{
					flag = false;
				}
				flag2 = num4 > 0f && num4 < _totalTrackDistance - 0.01f;
			}
			if (_trainModules[j].gameObject.activeSelf != flag2)
			{
				_trainModules[j].gameObject.SetActive(flag2);
			}
			if (!flag2)
			{
				continue;
			}
			float num5 = num4 / _totalTrackDistance;
			if (num5 >= 1f)
			{
				_trainModules[j].transform.localPosition = endPoint.pos;
				_trainModules[j].transform.localRotation = Quaternion.Euler(endPoint.angle);
				continue;
			}
			_trainModules[j].transform.localPosition = (endPoint.smoothPos ? Vector3.Lerp(startPoint.pos, endPoint.pos, num5) : endPoint.pos);
			if (endPoint.smoothAngle)
			{
				_trainModules[j].transform.localRotation = Quaternion.Slerp(Quaternion.Euler(startPoint.angle), Quaternion.Euler(endPoint.angle), num5);
			}
			else
			{
				_trainModules[j].transform.localRotation = Quaternion.Euler(endPoint.angle);
			}
		}
		if (flag && base.IsServer)
		{
			if (!loop)
			{
				StopMovement();
			}
			else
			{
				OnLoopComplete();
			}
		}
	}

	private void Update()
	{
		UpdateMovement();
	}

	private void LateUpdate()
	{
		UpdateSoundCollider();
	}

	protected override void __initializeVariables()
	{
		if (_maxTrains == null)
		{
			throw new Exception("entity_movement_train._maxTrains cannot be null. All NetworkVariableBase instances must be initialized.");
		}
		_maxTrains.Initialize(this);
		__nameNetworkVariable(_maxTrains, "_maxTrains");
		NetworkVariableFields.Add(_maxTrains);
		if (_active == null)
		{
			throw new Exception("entity_movement_train._active cannot be null. All NetworkVariableBase instances must be initialized.");
		}
		_active.Initialize(this);
		__nameNetworkVariable(_active, "_active");
		NetworkVariableFields.Add(_active);
		base.__initializeVariables();
	}

	protected override void __initializeRpcs()
	{
		base.__initializeRpcs();
	}

	protected internal override string __getTypeName()
	{
		return "entity_movement_train";
	}
}
