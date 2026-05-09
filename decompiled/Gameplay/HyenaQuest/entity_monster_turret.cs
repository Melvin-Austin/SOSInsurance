using System;
using Opsive.GraphDesigner.Runtime.Variables;
using UnityEngine;

namespace HyenaQuest;

public class entity_monster_turret : entity_monster_ai
{
	private readonly byte BULLET_DAMAGE = 2;

	public GameObject turretObj;

	public Transform shootPosition;

	public float rotationSpeed = 5f;

	public float scanAngle = 45f;

	public float shootCooldown = 1.5f;

	public float scanCooldown = 2f;

	private AudioSource _shootSFX;

	private ParticleSystem _shootVFX;

	private SharedVariable<GameObject> _target;

	private SharedVariable<float> _distance;

	private SharedVariable<Vector2> _fov;

	private float _scanCooldown;

	private float _shootCooldown;

	private float _bulletCooldown;

	private int _playerLayerMask;

	private readonly NetVar<bool> _shooting = new NetVar<bool>(value: false);

	private readonly NetVar<bool> _detected = new NetVar<bool>(value: false);

	public new void Awake()
	{
		base.Awake();
		if (!turretObj)
		{
			throw new UnityException("Turret object is not assigned in entity_monster_turret.");
		}
		if (!shootPosition)
		{
			throw new UnityException("Shoot position is not assigned in entity_monster_turret.");
		}
		_shootVFX = GetComponentInChildren<ParticleSystem>(includeInactive: true);
		if (!_shootVFX)
		{
			throw new UnityException("Shoot VFX is not assigned in entity_monster_turret.");
		}
		_shootSFX = GetComponentInChildren<AudioSource>(includeInactive: true);
		if (!_shootSFX)
		{
			throw new UnityException("Shoot SFX is not assigned in entity_monster_turret.");
		}
		_playerLayerMask = LayerMask.GetMask("entity_player", "entity_ground", "entity_phys");
	}

	public override void OnNetworkSpawn()
	{
		base.OnNetworkSpawn();
		if (base.IsServer)
		{
			if (!_behavior)
			{
				throw new UnityException("Missing Behavior");
			}
			_target = _behavior.GetVariable<GameObject>("TARGET");
			_distance = _behavior.GetVariable<float>("DISTANCE");
			_fov = _behavior.GetVariable<Vector2>("FOV");
		}
	}

	protected override void OnNetworkPostSpawn()
	{
		base.OnNetworkPostSpawn();
		if (!base.IsClient)
		{
			return;
		}
		_shooting.RegisterOnValueChanged(delegate(bool _, bool newValue)
		{
			if (newValue)
			{
				_shootVFX.Play(withChildren: true);
				_shootSFX.Play();
			}
			else
			{
				_shootVFX.Stop(withChildren: true);
				_shootSFX.Stop();
			}
		});
		_detected.RegisterOnValueChanged(delegate(bool _, bool newValue)
		{
			NetController<SoundController>.Instance.Play3DSound(newValue ? $"Ingame/Monsters/Turret/reload_{UnityEngine.Random.Range(0, 2)}.ogg" : $"Ingame/Monsters/Turret/shutdown_{UnityEngine.Random.Range(0, 1)}.ogg", shootPosition.position, new AudioData
			{
				pitch = UnityEngine.Random.Range(0.9f, 1.1f),
				volume = UnityEngine.Random.Range(0.3f, 0.5f),
				distance = 8f
			});
		});
	}

	public override void OnNetworkPreDespawn()
	{
		base.OnNetworkPreDespawn();
		if (base.IsClient)
		{
			_shooting.OnValueChanged = null;
			_detected.OnValueChanged = null;
		}
	}

	public new void Update()
	{
		base.Update();
		if (!base.IsServer)
		{
			return;
		}
		if (_target != null && (bool)_target.Value && _target.Value.CompareTag("Player"))
		{
			_detected.Value = true;
			Vector3 position = _target.Value.transform.position;
			position.y = shootPosition.position.y;
			Vector3 forward = position - shootPosition.position;
			if (forward.sqrMagnitude > 0.01f)
			{
				Quaternion b = Quaternion.LookRotation(forward);
				turretObj.transform.rotation = Quaternion.Slerp(turretObj.transform.rotation, b, Time.deltaTime * 2f);
			}
			_scanCooldown = Time.time + scanCooldown;
			if (!(Time.time > _shootCooldown))
			{
				return;
			}
			if (Time.time > _bulletCooldown)
			{
				_bulletCooldown = Time.time + 0.005f;
				entity_player entity_player2 = ShootRay();
				if ((bool)entity_player2)
				{
					entity_player2.TakeHealthRPC(BULLET_DAMAGE, DamageType.CUT);
				}
			}
			_shooting.Value = true;
		}
		else if (!(Time.time < _scanCooldown))
		{
			if (rotationSpeed > 0f)
			{
				float y = Mathf.PingPong(Time.time * rotationSpeed, scanAngle * 2f) - scanAngle;
				Quaternion b2 = Quaternion.Euler(0f, y, 0f);
				turretObj.transform.localRotation = Quaternion.Slerp(turretObj.transform.localRotation, b2, Time.deltaTime * rotationSpeed);
			}
			_shootCooldown = Time.time + shootCooldown;
			_shooting.Value = false;
			_detected.Value = false;
		}
	}

	[Server]
	private entity_player ShootRay()
	{
		float num = _fov?.Value.x ?? 45f;
		float num2 = _fov?.Value.y ?? 30f;
		float maxDistance = _distance?.Value ?? 10f;
		float angle = UnityEngine.Random.Range((0f - num) / 2f, num / 2f);
		float angle2 = UnityEngine.Random.Range((0f - num2) / 2f, num2 / 2f);
		Quaternion quaternion = Quaternion.AngleAxis(angle, shootPosition.up);
		Vector3 direction = Quaternion.AngleAxis(angle2, shootPosition.right) * quaternion * shootPosition.forward;
		if (Physics.Raycast(shootPosition.position, direction, out var hitInfo, maxDistance, _playerLayerMask) && hitInfo.collider.CompareTag("Player") && hitInfo.collider.TryGetComponent<entity_player>(out var component))
		{
			return component;
		}
		return null;
	}

	protected override void __initializeVariables()
	{
		if (_shooting == null)
		{
			throw new Exception("entity_monster_turret._shooting cannot be null. All NetworkVariableBase instances must be initialized.");
		}
		_shooting.Initialize(this);
		__nameNetworkVariable(_shooting, "_shooting");
		NetworkVariableFields.Add(_shooting);
		if (_detected == null)
		{
			throw new Exception("entity_monster_turret._detected cannot be null. All NetworkVariableBase instances must be initialized.");
		}
		_detected.Initialize(this);
		__nameNetworkVariable(_detected, "_detected");
		NetworkVariableFields.Add(_detected);
		base.__initializeVariables();
	}

	protected override void __initializeRpcs()
	{
		base.__initializeRpcs();
	}

	protected internal override string __getTypeName()
	{
		return "entity_monster_turret";
	}
}
