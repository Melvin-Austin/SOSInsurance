using System;
using FailCake;
using Opsive.GraphDesigner.Runtime.Variables;
using Opsive.Shared.Events;
using UnityEngine;

namespace HyenaQuest;

public class entity_monster_stalker : entity_monster_ai
{
	private static readonly int POSE = Animator.StringToHash("POSE");

	private static readonly int INTEREST = 20;

	private Collider _collider;

	private SharedVariable<GameObject> _targetStalk;

	private SharedVariable<bool> _angry;

	private SkinnedMeshRenderer[] _meshRenderers;

	private bool _wasLooking;

	private float _lastJumpScare;

	private float _lastBigJumpScare;

	private int _interestCounter;

	private entity_player _stalkingPlayer;

	private util_timer _interestTick;

	private readonly NetVar<byte> _stalkingPlayerID = new NetVar<byte>(byte.MaxValue);

	public new void Awake()
	{
		base.Awake();
		if (!_animator)
		{
			throw new UnityException("Missing Animator");
		}
		_collider = GetComponent<Collider>();
		if (!_collider)
		{
			throw new UnityException("Missing Collider");
		}
		_meshRenderers = GetComponentsInChildren<SkinnedMeshRenderer>(includeInactive: true);
		if (_meshRenderers == null || _meshRenderers.Length == 0)
		{
			throw new UnityException("Missing MeshRenderer");
		}
	}

	public override void OnNetworkSpawn()
	{
		base.OnNetworkSpawn();
		if (!MonoController<PlayerController>.Instance)
		{
			throw new UnityException("PlayerController is null");
		}
		if (base.IsServer)
		{
			_targetStalk = _behavior.GetVariable<GameObject>("TARGET");
			_angry = _behavior.GetVariable<bool>("ANGRY");
			Opsive.Shared.Events.EventHandler.RegisterEvent(_behavior, "STALK", OnMonsterStalk);
			Opsive.Shared.Events.EventHandler.RegisterEvent(_behavior, "LOOK", OnMonsterLook);
			MonoController<PlayerController>.Instance.OnPlayerRemoved += new Action<entity_player, bool>(OnPlayerRemoved);
			_interestTick?.Stop();
			_interestTick = util_timer.Create(-1, 1f, delegate
			{
				if ((bool)_stalkingPlayer)
				{
					_interestCounter = Mathf.Clamp(_interestCounter - 1, 0, INTEREST);
					if (_interestCounter <= 0)
					{
						OnMonsterReset();
					}
				}
			});
		}
		MonoController<PlayerController>.Instance.OnPlayerDeath += new Action<entity_player, bool>(OnPlayerDeath);
		base.OnNetworkSpawn();
	}

	public override void OnNetworkDespawn()
	{
		base.OnNetworkDespawn();
		if (base.IsServer)
		{
			_interestTick?.Stop();
			if ((bool)MonoController<PlayerController>.Instance)
			{
				MonoController<PlayerController>.Instance.OnPlayerRemoved -= new Action<entity_player, bool>(OnPlayerRemoved);
			}
			_angry.OnValueChange = null;
			Opsive.Shared.Events.EventHandler.UnregisterEvent(_behavior, "STALK", OnMonsterStalk);
			Opsive.Shared.Events.EventHandler.UnregisterEvent(_behavior, "LOOK", OnMonsterLook);
		}
		if ((bool)MonoController<PlayerController>.Instance)
		{
			MonoController<PlayerController>.Instance.OnPlayerDeath -= new Action<entity_player, bool>(OnPlayerDeath);
		}
		base.OnNetworkDespawn();
	}

	protected override void OnNetworkPostSpawn()
	{
		base.OnNetworkPostSpawn();
		if (!base.IsClient)
		{
			return;
		}
		_stalkingPlayerID.RegisterOnValueChanged(delegate(byte oldValue, byte newValue)
		{
			if (oldValue != newValue && (bool)PlayerController.LOCAL)
			{
				if (newValue == byte.MaxValue)
				{
					SetRenderers(enable: true);
				}
				else
				{
					SetRenderers(PlayerController.LOCAL.IsDead() || PlayerController.LOCAL.GetPlayerID() == newValue);
				}
			}
		});
	}

	public override void OnNetworkPreDespawn()
	{
		base.OnNetworkPreDespawn();
		if (base.IsClient)
		{
			_stalkingPlayerID.OnValueChanged = null;
		}
	}

	public new void Update()
	{
		base.Update();
		if (base.IsServer && (bool)_stalkingPlayer)
		{
			if (_stalkingPlayer.IsDead())
			{
				OnMonsterReset();
			}
			else if (_angry.Value && Vector3.Distance(_targetStalk.Value.transform.position, base.transform.position) <= 1.8f)
			{
				_stalkingPlayer.Kill(DamageType.NECK_SNAP);
				NetController<MonsterController>.Instance?.DuplicateSpawnMonster("entity_monster_stalker", base.transform.position);
				OnMonsterReset();
			}
		}
		if (base.IsClient && _stalkingPlayerID.Value == PlayerController.LOCAL?.GetPlayerID())
		{
			if (BehaviorUtils.IsPlayerLookingAtMonster(PlayerController.LOCAL, base.transform.position, _collider))
			{
				OnMonsterPose();
			}
			else
			{
				_wasLooking = false;
			}
		}
	}

	private void OnPlayerDeath(entity_player ply, bool server)
	{
		if (!server && !(ply != PlayerController.LOCAL))
		{
			SetRenderers(enable: true);
		}
	}

	[Server]
	private void OnPlayerRemoved(entity_player ply, bool server)
	{
		if (server && ply == _stalkingPlayer)
		{
			OnMonsterReset();
		}
	}

	[Server]
	private void OnMonsterLook()
	{
		if (base.IsServer)
		{
			_interestCounter = INTEREST;
		}
	}

	[Server]
	private void OnMonsterStalk()
	{
		if (base.IsServer && _targetStalk != null && (bool)_targetStalk.Value && _targetStalk.Value.TryGetComponent<entity_player>(out var component))
		{
			if (!component)
			{
				throw new UnityException("Stalking player is null");
			}
			_stalkingPlayerID.Value = component.GetPlayerID();
			_stalkingPlayer = component;
			OnMonsterLook();
		}
	}

	[Server]
	private void OnMonsterReset()
	{
		if (base.IsServer)
		{
			if ((bool)_stalkingPlayer && (bool)NetController<MapController>.Instance && NetController<MapController>.Instance.IsGenerated())
			{
				Transform randomRoomSpawnPointAwayFromPlayers = NetController<MapController>.Instance.GetRandomRoomSpawnPointAwayFromPlayers();
				base.transform.position = randomRoomSpawnPointAwayFromPlayers.position;
			}
			_stalkingPlayerID.Value = byte.MaxValue;
			_stalkingPlayer = null;
			_angry.Value = false;
			_targetStalk.Value = null;
			_wasLooking = false;
			_lastJumpScare = 0f;
			_lastBigJumpScare = 0f;
			_interestCounter = INTEREST;
		}
	}

	[Client]
	private void OnMonsterPose()
	{
		if (!base.IsClient || !_animator || _wasLooking)
		{
			return;
		}
		_wasLooking = true;
		_animator.SetInteger(POSE, UnityEngine.Random.Range(0, 7));
		float num = Vector3.Distance(base.transform.position, PlayerController.LOCAL.transform.position);
		float pitch = Mathf.Lerp(0.8f, 1.3f, Mathf.Clamp01(1f - num / 5f));
		NetController<SoundController>.Instance?.Play3DSound($"Ingame/Monsters/Stalker/squeak_{UnityEngine.Random.Range(0, 4)}.ogg", base.transform.position, new AudioData
		{
			distance = 5f,
			pitch = pitch
		});
		if (num >= 2f)
		{
			if (Time.time >= _lastJumpScare)
			{
				_lastJumpScare = Time.time + 8f;
				NetController<SoundController>.Instance?.PlaySound($"Ingame/Monsters/Stalker/eerie_{UnityEngine.Random.Range(0, 4)}.ogg", new AudioData
				{
					volume = 0.4f,
					pitch = UnityEngine.Random.Range(0.9f, 1.1f)
				});
			}
		}
		else if (!(Time.time < _lastBigJumpScare))
		{
			_lastBigJumpScare = Time.time + 8f;
			float volume = Mathf.Lerp(0.8f, 1.2f, Mathf.Clamp01(1f - num / 2f));
			NetController<SoundController>.Instance?.PlaySound($"Ingame/Monsters/Stalker/jumpscare_{UnityEngine.Random.Range(0, 6)}.ogg", new AudioData
			{
				pitch = UnityEngine.Random.Range(0.7f, 1.1f),
				volume = volume
			});
			float intensity = Mathf.Lerp(0.1f, 0.3f, (2f - num) / 2f);
			NetController<ShakeController>.Instance.LocalShake(ShakeMode.SHAKE_ALL, 0.4f, intensity);
		}
	}

	[Client]
	private void SetRenderers(bool enable)
	{
		if (_meshRenderers == null || _meshRenderers.Length == 0)
		{
			return;
		}
		SkinnedMeshRenderer[] meshRenderers = _meshRenderers;
		foreach (SkinnedMeshRenderer skinnedMeshRenderer in meshRenderers)
		{
			if ((bool)skinnedMeshRenderer)
			{
				skinnedMeshRenderer.enabled = enable;
			}
		}
	}

	protected override void __initializeVariables()
	{
		if (_stalkingPlayerID == null)
		{
			throw new Exception("entity_monster_stalker._stalkingPlayerID cannot be null. All NetworkVariableBase instances must be initialized.");
		}
		_stalkingPlayerID.Initialize(this);
		__nameNetworkVariable(_stalkingPlayerID, "_stalkingPlayerID");
		NetworkVariableFields.Add(_stalkingPlayerID);
		base.__initializeVariables();
	}

	protected override void __initializeRpcs()
	{
		base.__initializeRpcs();
	}

	protected internal override string __getTypeName()
	{
		return "entity_monster_stalker";
	}
}
