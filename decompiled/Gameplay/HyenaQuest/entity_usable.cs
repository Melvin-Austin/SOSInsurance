using System;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

namespace HyenaQuest;

[RequireComponent(typeof(Collider))]
[RequireComponent(typeof(NetworkObject))]
public class entity_usable : NetworkBehaviour
{
	public AudioClip lockedSND;

	public AudioClip pressSND;

	public AudioClip unpressSND;

	[Header("GameObject")]
	public GameObject target;

	public float clickDistance = 1.65f;

	public NetVar<bool> locked = new NetVar<bool>(value: false);

	[SerializeField]
	private string hint;

	public GameEvent<bool> OnAnimate = new GameEvent<bool>();

	private Collider _collision;

	private MeshRenderer _renderer;

	private readonly NetVar<FixedString128Bytes> _hint = new NetVar<FixedString128Bytes>();

	public void Awake()
	{
		_collision = GetComponent<Collider>();
		if (!_collision)
		{
			throw new UnityException("entity_button requires a collider component to work.");
		}
		_collision.isTrigger = true;
		base.gameObject.layer = LayerMask.NameToLayer("entity_usable");
		if ((bool)target)
		{
			target.isStatic = false;
		}
	}

	public override void OnNetworkSpawn()
	{
		base.OnNetworkSpawn();
		if (base.IsServer && !string.IsNullOrEmpty(hint))
		{
			_hint.SetSpawnValue(hint);
		}
	}

	protected override void OnNetworkPostSpawn()
	{
		base.OnNetworkPostSpawn();
		if (!base.IsClient)
		{
			return;
		}
		locked.RegisterOnValueChanged(delegate(bool oldValue, bool newValue)
		{
			if (oldValue != newValue)
			{
				base.tag = (newValue ? "ENTITY/LOCKED" : "Untagged");
			}
		});
	}

	public override void OnNetworkPreDespawn()
	{
		base.OnNetworkPreDespawn();
		if (base.IsClient)
		{
			locked.OnValueChanged = null;
		}
	}

	[Client]
	public virtual InteractionData InteractionSelector(Collider obj)
	{
		string value = GetHint();
		if (!string.IsNullOrEmpty(value))
		{
			return new InteractionData(IsLocked() ? Interaction.INTERACT_LOCKED : Interaction.INTERACT, new Bounds[1] { obj.bounds }, value);
		}
		return null;
	}

	public virtual bool OnUseDown(entity_player ply, bool server)
	{
		if (!server && IsLocked())
		{
			NetController<SoundController>.Instance?.Play3DSound(lockedSND, base.transform, new AudioData
			{
				pitch = UnityEngine.Random.Range(0.8f, 1.1f),
				distance = 4f,
				volume = 0.7f
			});
			return false;
		}
		return !IsLocked();
	}

	public virtual bool OnUseUP(entity_player ply, bool server)
	{
		return true;
	}

	[Server]
	public virtual void SetLocked(bool newVal)
	{
		if (base.IsSpawned && !base.IsServer)
		{
			throw new UnityException("SetLocked can only be called on the server!");
		}
		locked.SetSpawnValue(newVal);
	}

	[Server]
	public void SetHint(string newHint)
	{
		if (base.IsSpawned && !base.IsServer)
		{
			throw new UnityException("SetHint can only be called on the server");
		}
		_hint.SetSpawnValue(newHint);
	}

	public string GetHint()
	{
		return _hint.Value.ToString();
	}

	public virtual bool IsLocked()
	{
		return locked.Value;
	}

	[Client]
	protected virtual void Animate(bool newVal)
	{
		if (!NetController<SoundController>.Instance)
		{
			return;
		}
		AudioData data = new AudioData
		{
			pitch = UnityEngine.Random.Range(0.8f, 1.1f),
			distance = 4f,
			volume = 0.7f
		};
		if (newVal)
		{
			if ((bool)pressSND)
			{
				NetController<SoundController>.Instance.Play3DSound(pressSND, target ? target.transform : base.transform, data);
			}
		}
		else if ((bool)unpressSND)
		{
			NetController<SoundController>.Instance.Play3DSound(unpressSND, target ? target.transform : base.transform, data);
		}
		OnAnimate?.Invoke(newVal);
	}

	protected override void __initializeVariables()
	{
		if (locked == null)
		{
			throw new Exception("entity_usable.locked cannot be null. All NetworkVariableBase instances must be initialized.");
		}
		locked.Initialize(this);
		__nameNetworkVariable(locked, "locked");
		NetworkVariableFields.Add(locked);
		if (_hint == null)
		{
			throw new Exception("entity_usable._hint cannot be null. All NetworkVariableBase instances must be initialized.");
		}
		_hint.Initialize(this);
		__nameNetworkVariable(_hint, "_hint");
		NetworkVariableFields.Add(_hint);
		base.__initializeVariables();
	}

	protected override void __initializeRpcs()
	{
		base.__initializeRpcs();
	}

	protected internal override string __getTypeName()
	{
		return "entity_usable";
	}
}
