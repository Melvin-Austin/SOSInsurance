using System;
using System.Collections.Generic;
using FailCake;
using Unity.Netcode;
using UnityEngine;

namespace HyenaQuest;

public class entity_monster_grinder : entity_monster_ai
{
	private static readonly float MIN_SPEED = 2f;

	private static readonly float PLAYER_AIM_CHANCE = 0.8f;

	private static readonly float PLAYER_AIM_COOLDOWN = 2f;

	private static readonly float SLEEP_COOLDOWN = 8f;

	private static readonly float WAKE_DISTANCE = 1.5f;

	private static readonly float DEACTIVATE_DISTANCE = 3f;

	private static readonly float STUCK_CHECK_INTERVAL = 0.5f;

	private static readonly float STUCK_DISTANCE_THRESHOLD = 0.1f;

	private static readonly float FORWARD_FORCE = 150f;

	private static readonly int Sleeping = Animator.StringToHash("Sleeping");

	private Rigidbody _rigidbody;

	private entity_client_usable _fakeUsable;

	private float _lastPlayerReflect;

	private float _lastAudio;

	private int _layerMask;

	private int _playerLayerMask;

	private Vector3 _awakeDirection;

	private float _lastNearby;

	private readonly Collider[] _colliderHits = new Collider[NETController.MAX_PLAYERS];

	private readonly RaycastHit[] _hits = new RaycastHit[1];

	private float _stuckCheckTime;

	private readonly NetVar<bool> _sleeping = new NetVar<bool>(value: true);

	public new void Awake()
	{
		base.Awake();
		_fakeUsable = GetComponentInChildren<entity_client_usable>(includeInactive: true);
		if (!_fakeUsable)
		{
			throw new UnityException("Missing entity_client_usable");
		}
		_rigidbody = GetComponent<Rigidbody>();
		if (!_rigidbody)
		{
			throw new UnityException("Rigidbody is not assigned in entity_monster_grinder.");
		}
		_layerMask = LayerMask.GetMask("entity_ground", "entity_player", "entity_blocker", "entity_phys");
		_playerLayerMask = LayerMask.GetMask("entity_player");
	}

	public override void OnNetworkSpawn()
	{
		base.OnNetworkSpawn();
		if (base.IsServer)
		{
			ReplaceWorldBin();
		}
	}

	protected override void OnNetworkPostSpawn()
	{
		base.OnNetworkPostSpawn();
		if (!base.IsClient)
		{
			return;
		}
		_sleeping.RegisterOnValueChanged(delegate(bool _, bool newValue)
		{
			if ((bool)_animator && (bool)_fakeUsable)
			{
				NetController<SoundController>.Instance?.Play3DSound(newValue ? "Ingame/Monsters/Grinder/spike_in.ogg" : "Ingame/Monsters/Grinder/spike_out.ogg", GetPosition(), new AudioData
				{
					distance = 5f,
					pitch = UnityEngine.Random.Range(0.8f, 1.2f),
					volume = 0.8f
				});
				_fakeUsable.SetLocked(!newValue);
				_animator.SetBool(Sleeping, newValue);
			}
		});
	}

	public override void OnNetworkPreDespawn()
	{
		base.OnNetworkPreDespawn();
		if (base.IsClient)
		{
			_sleeping.OnValueChanged = null;
		}
	}

	protected void FixedUpdate()
	{
		if (!base.IsServer || !_rigidbody || _rigidbody.isKinematic)
		{
			return;
		}
		Vector3 position = GetPosition();
		int num = Physics.OverlapSphereNonAlloc(position, _sleeping.Value ? WAKE_DISTANCE : DEACTIVATE_DISTANCE, _colliderHits, _playerLayerMask, QueryTriggerInteraction.Ignore);
		if (_sleeping.Value)
		{
			if (num > 0)
			{
				Quaternion quaternion = Quaternion.LookRotation((_colliderHits[0].transform.position - position).normalized);
				base.transform.eulerAngles = new Vector3(0f, quaternion.eulerAngles.y, 0f);
				SetSleepStatus(sleep: false);
			}
			return;
		}
		if (num <= 0)
		{
			if (Time.time > _lastNearby)
			{
				SetSleepStatus(sleep: true);
				return;
			}
		}
		else
		{
			_lastNearby = Time.time + SLEEP_COOLDOWN;
		}
		CheckIfStuck();
		if (_rigidbody.linearVelocity.magnitude < MIN_SPEED)
		{
			_rigidbody.linearVelocity = base.transform.forward * MIN_SPEED;
		}
		_rigidbody.AddForce(base.transform.forward * (FORWARD_FORCE * Time.fixedDeltaTime), ForceMode.Force);
		CheckForWallCollision();
	}

	[Server]
	private void ReplaceWorldBin()
	{
		if (!base.IsServer)
		{
			throw new UnityException("Not server");
		}
		IList<entity_phys_prop_scrap_trashbin> list = UnityEngine.Object.FindObjectsByType<entity_phys_prop_scrap_trashbin>(FindObjectsInactive.Exclude, FindObjectsSortMode.None).Shuffle();
		if (list == null || list.Count <= 0)
		{
			return;
		}
		entity_phys_prop_scrap_trashbin entity_phys_prop_scrap_trashbin2 = list[UnityEngine.Random.Range(0, list.Count)];
		if ((bool)entity_phys_prop_scrap_trashbin2)
		{
			Vector3 newPos = entity_phys_prop_scrap_trashbin2.transform.position;
			Quaternion newRotation = entity_phys_prop_scrap_trashbin2.transform.rotation;
			NetworkObject networkObject = entity_phys_prop_scrap_trashbin2.NetworkObject;
			if (!networkObject)
			{
				throw new UnityException("Network object is not assigned in entity_phys_prop_scrap_trashbin.");
			}
			networkObject.Despawn();
			UnityEngine.Object.DestroyImmediate(entity_phys_prop_scrap_trashbin2.gameObject);
			util_timer.Simple(0.1f, delegate
			{
				_networkTransform?.SetState(newPos, newRotation, base.transform.lossyScale, teleportDisabled: false);
			});
		}
	}

	[Server]
	private void SetSleepStatus(bool sleep)
	{
		if (!base.IsServer)
		{
			throw new UnityException("Not server");
		}
		_sleeping.Value = sleep;
	}

	private Vector3 GetPosition()
	{
		return base.transform.position + Vector3.up * 0.6f;
	}

	private void CheckIfStuck()
	{
		if (base.IsServer && (bool)_rigidbody && Time.time >= _stuckCheckTime)
		{
			if (Vector3.Distance(base.transform.position, _lastPosition) < STUCK_DISTANCE_THRESHOLD)
			{
				Vector3 normalized = new Vector3(UnityEngine.Random.Range(-1f, 1f), 0f, UnityEngine.Random.Range(-1f, 1f)).normalized;
				Quaternion rotation = Quaternion.LookRotation(normalized);
				rotation.eulerAngles = new Vector3(0f, rotation.eulerAngles.y, 0f);
				base.transform.rotation = rotation;
				_rigidbody.linearVelocity = normalized * MIN_SPEED;
			}
			_stuckCheckTime = Time.time + STUCK_CHECK_INTERVAL;
		}
	}

	private void CheckForWallCollision()
	{
		if (!base.IsServer || !_rigidbody)
		{
			return;
		}
		float maxDistance = 0.12f;
		Vector3 vector = new Vector3(0.5f, 0.35f, 0.1f);
		Vector3 normalized = _rigidbody.linearVelocity.normalized;
		if (Physics.BoxCastNonAlloc(GetPosition() + normalized * 0.2f, vector * 0.5f, normalized, _hits, base.transform.rotation, maxDistance, _layerMask, QueryTriggerInteraction.Ignore) > 0)
		{
			Vector3 vector2 = normalized;
			Vector3 vector3 = _hits[0].normal;
			if (Vector3.Dot(vector3, vector2) > 0f)
			{
				vector3 = -vector3;
			}
			Vector3 vector4;
			if (UnityEngine.Random.Range(0f, 1f) < PLAYER_AIM_CHANCE && Time.time > _lastPlayerReflect)
			{
				_lastPlayerReflect = Time.time + PLAYER_AIM_COOLDOWN;
				entity_player entity_player2 = MonoController<PlayerController>.Instance.FindNearbyPlayer(base.transform.position, 4f);
				vector4 = ((!entity_player2) ? Vector3.Reflect(vector2, vector3) : (entity_player2.transform.position - base.transform.position).normalized);
			}
			else
			{
				vector4 = ((Mathf.Abs(Vector3.Dot(vector2, vector3)) < 0.3f) ? Vector3.ProjectOnPlane(vector2, vector3).normalized : Vector3.Reflect(vector2, vector3));
			}
			Vector3 vector5 = new Vector3(UnityEngine.Random.Range(-0.2f, 0.2f), 0f, UnityEngine.Random.Range(-0.2f, 0.2f));
			vector4 = (vector4 + vector5).normalized;
			if (vector4.magnitude < 0.1f)
			{
				vector4 = new Vector3(UnityEngine.Random.Range(-1f, 1f), 0f, UnityEngine.Random.Range(-1f, 1f)).normalized;
			}
			Quaternion rotation = Quaternion.LookRotation(vector4);
			rotation.eulerAngles = new Vector3(0f, rotation.eulerAngles.y, 0f);
			base.transform.rotation = rotation;
			float num = Mathf.Max(_rigidbody.linearVelocity.magnitude, 2f);
			_rigidbody.linearVelocity = vector4 * num;
			if (Time.time > _lastAudio)
			{
				_lastAudio = Time.time + 0.25f;
				NetController<SoundController>.Instance?.Play3DSound($"Ingame/Props/Concrete/concrete_block_impact_hard{UnityEngine.Random.Range(1, 4)}.ogg", GetPosition(), new AudioData
				{
					distance = 4f,
					pitch = UnityEngine.Random.Range(0.85f, 1.2f),
					volume = UnityEngine.Random.Range(0.35f, 0.5f)
				}, broadcast: true);
			}
		}
	}

	protected override void __initializeVariables()
	{
		if (_sleeping == null)
		{
			throw new Exception("entity_monster_grinder._sleeping cannot be null. All NetworkVariableBase instances must be initialized.");
		}
		_sleeping.Initialize(this);
		__nameNetworkVariable(_sleeping, "_sleeping");
		NetworkVariableFields.Add(_sleeping);
		base.__initializeVariables();
	}

	protected override void __initializeRpcs()
	{
		base.__initializeRpcs();
	}

	protected internal override string __getTypeName()
	{
		return "entity_monster_grinder";
	}
}
