using System;
using FailCake;
using Unity.Netcode;
using UnityEngine;

namespace HyenaQuest;

[RequireComponent(typeof(NetworkObject))]
public class entity_zipper : NetworkBehaviour
{
	public GameObject zipperPrefab;

	[Range(1f, 10f)]
	public float startDelay = 1.5f;

	[Range(1f, 10f)]
	public float speed = 5f;

	[Range(1f, 10f)]
	public float maxDangleDistance = 1f;

	public int startPoint;

	private readonly int _ropeSegments = 4;

	private bool _complete;

	private float _currentSwingMomentum;

	private entity_movement_networked _zipperMovement;

	private entity_vehicle_seat _vehicle;

	private entity_button _zippUsable;

	private LineRenderer _rope;

	private SpringJoint _springJoint;

	private Rigidbody _vehicleBody;

	private util_timer _startTimer;

	private entity_player _currentRider;

	private readonly NetVar<NetworkBehaviourReference> _zipperVehicle = new NetVar<NetworkBehaviourReference>(null);

	public void Awake()
	{
		if (!zipperPrefab)
		{
			throw new UnityException("Zipper prefab is not assigned!");
		}
		_zipperMovement = GetComponent<entity_movement_networked>();
		if (!_zipperMovement)
		{
			throw new UnityException("Zipper prefab does not have a movement component!");
		}
		_zipperMovement.speed = speed;
		_rope = _zipperMovement.obj.GetComponent<LineRenderer>();
		if (!_rope)
		{
			throw new UnityException("Zipper prefab does not have a line renderer component!");
		}
		_rope.useWorldSpace = false;
		_rope.positionCount = _ropeSegments;
		_rope.numCapVertices = 2;
	}

	protected override void OnNetworkPostSpawn()
	{
		base.OnNetworkPostSpawn();
		if (!base.IsClient)
		{
			return;
		}
		_zipperVehicle.RegisterOnValueChanged(delegate(NetworkBehaviourReference oldValue, NetworkBehaviourReference newValue)
		{
			if (!base.IsServer)
			{
				entity_vehicle_seat entity_vehicle_seat2 = NETController.Get<entity_vehicle_seat>(newValue);
				if (!entity_vehicle_seat2)
				{
					throw new UnityException("Networked vehicle could not be found!");
				}
				_vehicle = entity_vehicle_seat2;
			}
		});
	}

	public override void OnNetworkPreDespawn()
	{
		base.OnNetworkPreDespawn();
		if (base.IsClient)
		{
			_zipperVehicle.OnValueChanged = null;
		}
	}

	public override void OnNetworkSpawn()
	{
		base.OnNetworkSpawn();
		if (base.IsServer)
		{
			CoreController.WaitFor(delegate(PlayerController plyCtrl)
			{
				plyCtrl.OnPlayerRemoved += new Action<entity_player, bool>(OnPlayerRemoved);
			});
			SetupZipper();
		}
	}

	private void OnPlayerRemoved(entity_player ply, bool server)
	{
		if (server && !(ply != _currentRider))
		{
			ResetPlayer(force: true);
		}
	}

	public override void OnNetworkDespawn()
	{
		base.OnNetworkDespawn();
		if (base.IsServer)
		{
			if (_startTimer != null)
			{
				_startTimer.Stop();
			}
			if ((bool)_zipperMovement)
			{
				_zipperMovement.StopMovement();
			}
			if ((bool)_zippUsable)
			{
				_zippUsable.OnUSE -= new Action<entity_player>(OnZipperUse);
			}
			if ((bool)_vehicle && _vehicle.IsSpawned)
			{
				_vehicle.NetworkObject.Despawn();
			}
			if ((bool)MonoController<PlayerController>.Instance)
			{
				MonoController<PlayerController>.Instance.OnPlayerRemoved -= new Action<entity_player, bool>(OnPlayerRemoved);
			}
		}
	}

	[Client]
	public void Update()
	{
		if (base.IsServer)
		{
			entity_player currentRider = _currentRider;
			if ((object)currentRider != null && currentRider.IsDead())
			{
				ResetPlayer(force: true);
			}
		}
		UpdateRope();
	}

	[Client]
	private void UpdateRope()
	{
		if ((bool)_vehicle && (bool)_rope && (bool)_zipperMovement)
		{
			Point point = _zipperMovement.GetPoint(startPoint);
			Vector3 a = base.transform.TransformPoint(point.pos);
			Vector3 position = _vehicle.transform.position;
			for (int i = 0; i < _ropeSegments; i++)
			{
				float t = (float)i / (float)(_ropeSegments - 1);
				Vector3 position2 = Vector3.Lerp(a, position, t);
				_rope.SetPosition(i, _rope.transform.InverseTransformPoint(position2));
			}
		}
	}

	[Server]
	private void SetupZipper()
	{
		if (!_zipperMovement)
		{
			throw new UnityException("Zipper prefab does not have a movement component!");
		}
		if (_zipperMovement.points.Count < 2)
		{
			throw new UnityException("Zipper prefab does not have enough points!");
		}
		GameObject gameObject = UnityEngine.Object.Instantiate(zipperPrefab, _zipperMovement.transform.position, Quaternion.identity);
		if (!gameObject)
		{
			throw new UnityException("Zipper prefab could not be instantiated!");
		}
		entity_vehicle_seat component = gameObject.GetComponent<entity_vehicle_seat>();
		if (!component)
		{
			throw new UnityException("Zipper prefab does not have a vehicle seat component!");
		}
		if (!component.NetworkObject)
		{
			throw new UnityException("Zipper prefab does not have a network object component!");
		}
		component.NetworkObject.Spawn(destroyWithScene: true);
		_zippUsable = gameObject.GetComponent<entity_button>();
		if (!_zippUsable)
		{
			throw new UnityException("Zipper prefab does not have a usable component!");
		}
		_zippUsable.OnUSE += new Action<entity_player>(OnZipperUse);
		_vehicle = component;
		Rigidbody rigidbody = _zipperMovement.obj.AddComponent<Rigidbody>();
		rigidbody.isKinematic = true;
		rigidbody.useGravity = false;
		rigidbody.interpolation = RigidbodyInterpolation.Interpolate;
		rigidbody.collisionDetectionMode = CollisionDetectionMode.Discrete;
		rigidbody.freezeRotation = false;
		rigidbody.mass = 10f;
		_vehicleBody = _vehicle.gameObject.AddComponent<Rigidbody>();
		_vehicleBody.isKinematic = false;
		_vehicleBody.useGravity = true;
		_vehicleBody.interpolation = RigidbodyInterpolation.Interpolate;
		_vehicleBody.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
		_vehicleBody.mass = 1f;
		_vehicleBody.linearDamping = 1f;
		_springJoint = _vehicle.gameObject.AddComponent<SpringJoint>();
		_springJoint.autoConfigureConnectedAnchor = false;
		_springJoint.maxDistance = maxDangleDistance;
		_springJoint.spring = 20f;
		_springJoint.damper = 5f;
		_springJoint.massScale = 20f;
		_springJoint.connectedBody = rigidbody;
		_zipperVehicle.Value = component;
	}

	[Server]
	private void OnZipperUse(entity_player ply)
	{
		if (!base.IsServer)
		{
			throw new UnityException("OnZipperUse called on client!");
		}
		if (!_vehicle)
		{
			throw new UnityException("Zipper prefab does not have a vehicle seat component!");
		}
		if (!ply || (bool)_currentRider)
		{
			return;
		}
		_zippUsable.SetLocked(newVal: true);
		_complete = false;
		NetController<SoundController>.Instance?.Play3DSound("Ingame/Entities/Zipper/latch_close.ogg", _vehicle.transform.position, new AudioData
		{
			pitch = UnityEngine.Random.Range(0.8f, 1.2f)
		}, broadcast: true);
		NetController<NotificationController>.Instance?.BroadcastRPC(new NotificationData
		{
			id = "entity_zipper",
			duration = -1f,
			text = "Press SPACE to unlatch"
		}, ply.GetConnectionID());
		ply.OnPlayerAction += new Action<PlayerAction, bool>(OnPlayerAction);
		ply.SetVehicle(_vehicle);
		_currentRider = ply;
		_startTimer?.Stop();
		_startTimer = util_timer.Simple(startDelay, delegate
		{
			_zipperMovement.reverse = false;
			_zipperMovement.StartMovement(reset: true, delegate
			{
				_complete = true;
				if (!_currentRider)
				{
					ResetHook();
				}
			});
		});
	}

	[Server]
	private void ResetPlayer(bool force = false)
	{
		if (base.IsServer && (bool)_currentRider)
		{
			_currentRider.SetVehicle(null);
			_currentRider.ShoveRPC(_currentRider.transform.forward, 3f);
			NetController<NotificationController>.Instance?.BroadcastRemoveRPC("entity_zipper", _currentRider.GetConnectionID());
			_currentRider.OnPlayerAction -= new Action<PlayerAction, bool>(OnPlayerAction);
			_currentRider = null;
			if (_complete || force)
			{
				ResetHook();
			}
		}
	}

	[Server]
	private void ResetHook()
	{
		_startTimer?.Stop();
		_complete = false;
		_zipperMovement.reverse = true;
		_zipperMovement.StartMovement(reset: true, delegate
		{
			_zippUsable.SetLocked(newVal: false);
		});
	}

	private void OnPlayerAction(PlayerAction action, bool server)
	{
		if (server && (bool)_currentRider && action == PlayerAction.JUMP)
		{
			NetController<SoundController>.Instance?.Play3DSound("Ingame/Entities/Zipper/latch_open.ogg", _currentRider.transform.position, new AudioData
			{
				pitch = UnityEngine.Random.Range(0.8f, 1.2f)
			}, broadcast: true);
			ResetPlayer();
		}
	}

	protected override void __initializeVariables()
	{
		if (_zipperVehicle == null)
		{
			throw new Exception("entity_zipper._zipperVehicle cannot be null. All NetworkVariableBase instances must be initialized.");
		}
		_zipperVehicle.Initialize(this);
		__nameNetworkVariable(_zipperVehicle, "_zipperVehicle");
		NetworkVariableFields.Add(_zipperVehicle);
		base.__initializeVariables();
	}

	protected override void __initializeRpcs()
	{
		base.__initializeRpcs();
	}

	protected internal override string __getTypeName()
	{
		return "entity_zipper";
	}
}
