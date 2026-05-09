using System;
using FailCake;
using UnityEngine;

namespace HyenaQuest;

public class entity_monster_popper : entity_monster_ai
{
	public GameObject balloon;

	public Transform ballonPosition;

	public float popDistance = 0.2f;

	public float popAffectDistance = 2f;

	[Range(0f, 255f)]
	public byte popDamage = 10;

	private entity_attractor _attractor;

	private int _layerMask;

	private util_timer _respawnTimer;

	private util_fade_timer _refillTimer;

	private float _explodeCooldown;

	private readonly NetVar<bool> _exploded = new NetVar<bool>(value: false);

	public new void Awake()
	{
		base.Awake();
		if (!balloon)
		{
			throw new UnityException("Missing balloon");
		}
		if (!ballonPosition)
		{
			throw new UnityException("Missing ballonPosition");
		}
		_attractor = GetComponentInChildren<entity_attractor>(includeInactive: true);
		if (!_attractor)
		{
			throw new UnityException("Missing entity_attractor");
		}
		_attractor.enabled = false;
		_animator = GetComponent<Animator>();
		if (!_animator)
		{
			throw new UnityException("entity_monster_thief requires Animator component");
		}
		_layerMask = LayerMask.GetMask("entity_player", "entity_phys", "entity_phys_item");
	}

	public override void OnNetworkDespawn()
	{
		base.OnNetworkDespawn();
		_refillTimer?.Stop();
		if (base.IsServer)
		{
			_respawnTimer?.Stop();
		}
	}

	protected override void OnNetworkPostSpawn()
	{
		base.OnNetworkPostSpawn();
		if (!base.IsClient)
		{
			return;
		}
		_exploded.RegisterOnValueChanged(delegate(bool oldValue, bool newValue)
		{
			if (oldValue != newValue)
			{
				if (newValue)
				{
					OnClientPop();
				}
				else
				{
					OnBallonRefill();
				}
			}
		});
	}

	public override void OnNetworkPreDespawn()
	{
		base.OnNetworkPreDespawn();
		if (base.IsClient)
		{
			_exploded.OnValueChanged = null;
		}
	}

	public new void Update()
	{
		base.Update();
		if (base.IsServer && !_exploded.Value && !(Time.time < _explodeCooldown) && Physics.CheckSphere(ballonPosition.position, popDistance, _layerMask, QueryTriggerInteraction.Ignore))
		{
			_exploded.Value = true;
			_respawnTimer?.Stop();
			_respawnTimer = util_timer.Simple(5f, delegate
			{
				_exploded.Value = false;
				_explodeCooldown = Time.time + 1.2f;
			});
		}
	}

	private void OnBallonRefill()
	{
		if (!balloon)
		{
			return;
		}
		NetController<SoundController>.Instance?.Play3DSound($"Ingame/Monsters/Popper/inflate_{UnityEngine.Random.Range(0, 4)}.ogg", ballonPosition.position, new AudioData
		{
			distance = 5f,
			volume = 0.3f
		});
		_refillTimer?.Stop();
		_refillTimer = util_fade_timer.Fade(1.2f, 0f, 1f, delegate(float f)
		{
			if ((bool)balloon && !_exploded.Value)
			{
				balloon.transform.localScale = Vector3.one * f;
			}
		});
	}

	private void OnClientPop()
	{
		if ((bool)balloon && (bool)_attractor && (bool)PlayerController.LOCAL)
		{
			_refillTimer?.Stop();
			balloon.transform.localScale = Vector3.zero;
			_attractor.enabled = false;
			_attractor.Push();
			NetController<EffectController>.Instance?.PlayEffect(EffectType.CONFETTI_SPHERE, ballonPosition.position, new EffectSettings
			{
				count = 60,
				playSound = false
			});
			NetController<SoundController>.Instance?.Play3DSound($"General/Entities/Effects/Confetti/tada_party_{UnityEngine.Random.Range(0, 4)}.ogg", ballonPosition.position, new AudioData
			{
				distance = 3f
			});
			if (!(Vector3.Distance(PlayerController.LOCAL.transform.position, ballonPosition.position) > popAffectDistance))
			{
				PlayerController.LOCAL.TakeHealth(popDamage);
			}
		}
	}

	protected override void __initializeVariables()
	{
		if (_exploded == null)
		{
			throw new Exception("entity_monster_popper._exploded cannot be null. All NetworkVariableBase instances must be initialized.");
		}
		_exploded.Initialize(this);
		__nameNetworkVariable(_exploded, "_exploded");
		NetworkVariableFields.Add(_exploded);
		base.__initializeVariables();
	}

	protected override void __initializeRpcs()
	{
		base.__initializeRpcs();
	}

	protected internal override string __getTypeName()
	{
		return "entity_monster_popper";
	}
}
