using System;
using UnityEngine;

namespace HyenaQuest;

public class entity_phys_prop_scrap_urchin : entity_phys_prop_scrap
{
	private static readonly int MIN_PLAYER_ATTACH = 2;

	private CapsuleCollider _collider;

	private Rigidbody _body;

	private entity_player _attachedPlayer;

	private int _playerLayer;

	private float _lastDamageCD;

	private Vector3 _attachLocalPos;

	private Quaternion _attachLocalRot;

	private readonly Collider[] _hitBuffer = new Collider[8];

	private readonly NetVar<bool> _attached = new NetVar<bool>(value: false);

	public override void OnNetworkSpawn()
	{
		base.OnNetworkSpawn();
		if (base.IsServer)
		{
			_body.isKinematic = true;
			SetLocked(LOCK_TYPE.LOCKED);
			CoreController.WaitFor(delegate(PlayerController ctr)
			{
				ctr.OnPlayerDeath += new Action<entity_player, bool>(OnPlayerRemovedOrDeath);
				ctr.OnPlayerRemoved += new Action<entity_player, bool>(OnPlayerRemovedOrDeath);
			});
		}
	}

	public override void OnNetworkDespawn()
	{
		base.OnNetworkDespawn();
		if (base.IsServer)
		{
			if ((bool)_attachedPlayer)
			{
				NetController<CurseController>.Instance?.RemoveCurse(_attachedPlayer, CURSE_TYPE.SLOW);
			}
			if ((bool)MonoController<PlayerController>.Instance)
			{
				MonoController<PlayerController>.Instance.OnPlayerDeath -= new Action<entity_player, bool>(OnPlayerRemovedOrDeath);
				MonoController<PlayerController>.Instance.OnPlayerRemoved -= new Action<entity_player, bool>(OnPlayerRemovedOrDeath);
			}
		}
	}

	[Client]
	public override InteractionData InteractionSelector(Collider obj)
	{
		if (_attached.Value)
		{
			return base.InteractionSelector(obj);
		}
		return null;
	}

	protected override void OnNetworkPostSpawn()
	{
		base.OnNetworkPostSpawn();
		if (!base.IsClient)
		{
			return;
		}
		_attached.RegisterOnValueChanged(delegate(bool _, bool newValue)
		{
			if ((bool)_collider)
			{
				_collider.isTrigger = newValue;
			}
		});
	}

	public override void OnNetworkPreDespawn()
	{
		base.OnNetworkPreDespawn();
		if (base.IsClient)
		{
			_attached.OnValueChanged = null;
		}
	}

	public void LateUpdate()
	{
		if (base.IsServer && (bool)_attachedPlayer)
		{
			Transform transform = _attachedPlayer.transform;
			base.transform.position = transform.TransformPoint(_attachLocalPos);
			base.transform.rotation = transform.rotation * _attachLocalRot;
		}
	}

	public entity_player GetAttachedPlayer()
	{
		return _attachedPlayer;
	}

	public void FixedUpdate()
	{
		if (!base.IsServer || (bool)_attachedPlayer || !_collider)
		{
			return;
		}
		float radius = _collider.radius;
		int num = Physics.OverlapSphereNonAlloc(base.transform.position, radius, _hitBuffer, _playerLayer);
		if (num == 0)
		{
			return;
		}
		int num2 = NETController.Instance?.ConnectedClientsIds.Count ?? 1;
		for (int i = 0; i < num; i++)
		{
			if (!_hitBuffer[i] || !_hitBuffer[i].CompareTag("Player"))
			{
				continue;
			}
			entity_player component = _hitBuffer[i].GetComponent<entity_player>();
			if (!component || component.IsDead())
			{
				continue;
			}
			if (num2 < MIN_PLAYER_ATTACH)
			{
				if (Time.time > _lastDamageCD)
				{
					_lastDamageCD = Time.time + 1f;
					component.TakeHealthRPC(5, DamageType.CUT);
				}
			}
			else
			{
				Attach(component, base.transform.position);
			}
			break;
		}
	}

	public override bool CanScrap(entity_player scrapper)
	{
		if (base.CanScrap(scrapper))
		{
			return _attached.Value;
		}
		return false;
	}

	protected override void Init()
	{
		base.Init();
		_body = GetComponent<Rigidbody>();
		if (!_body)
		{
			throw new UnityException("Missing RigidBody");
		}
		_collider = GetComponentInChildren<CapsuleCollider>(includeInactive: true);
		if (!_collider)
		{
			throw new UnityException("Missing CapsuleCollider");
		}
		_playerLayer = LayerMask.GetMask("entity_player");
	}

	private void Attach(entity_player player, Vector3 attachPos)
	{
		if ((bool)player && !player.IsDead())
		{
			player.TakeHealthRPC(5, DamageType.CUT);
			if (!player.IsDead())
			{
				_attachedPlayer = player;
				_attached.SetSpawnValue(value: true);
				Transform transform = player.transform;
				_attachLocalPos = transform.InverseTransformPoint(attachPos);
				_attachLocalRot = Quaternion.Inverse(transform.rotation) * base.transform.rotation;
				NetController<CurseController>.Instance?.AddCurse(CURSE_TYPE.SLOW, _attachedPlayer);
			}
		}
	}

	private void Detach()
	{
		if ((bool)_attachedPlayer)
		{
			NetController<CurseController>.Instance?.RemoveCurse(_attachedPlayer, CURSE_TYPE.SLOW);
			_attached.SetSpawnValue(value: false);
			_attachedPlayer = null;
			_body.isKinematic = false;
		}
	}

	private void OnPlayerRemovedOrDeath(entity_player ply, bool server)
	{
		if (server && !(ply != _attachedPlayer))
		{
			Detach();
		}
	}

	protected override void __initializeVariables()
	{
		if (_attached == null)
		{
			throw new Exception("entity_phys_prop_scrap_urchin._attached cannot be null. All NetworkVariableBase instances must be initialized.");
		}
		_attached.Initialize(this);
		__nameNetworkVariable(_attached, "_attached");
		NetworkVariableFields.Add(_attached);
		base.__initializeVariables();
	}

	protected override void __initializeRpcs()
	{
		base.__initializeRpcs();
	}

	protected internal override string __getTypeName()
	{
		return "entity_phys_prop_scrap_urchin";
	}
}
