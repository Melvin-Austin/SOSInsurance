using System;
using System.Collections.Generic;
using FailCake;
using Opsive.GraphDesigner.Runtime.Variables;
using Opsive.Shared.Events;
using UnityEngine;

namespace HyenaQuest;

public class entity_monster_ram : entity_monster_ai
{
	private static readonly int Status = Animator.StringToHash("MODE");

	private static readonly int MaskTexture = Shader.PropertyToID("_Mask");

	private static readonly int CoatColor = Shader.PropertyToID("_CoatColor");

	private static readonly float RAM_SPEED = 30f;

	private static readonly float RAM_ACCELERATION = 60f;

	public List<Texture2D> masks = new List<Texture2D>();

	[ColorUsage(true, true)]
	public List<Color> colors = new List<Color>();

	public SkinnedMeshRenderer body;

	private Rigidbody _rigidbody;

	private entity_led _led;

	private entity_animator_event _animatorEvent;

	private SharedVariable<bool> _disabled;

	private SharedVariable<GameObject> _target;

	private util_timer _ramTimer;

	private util_timer _blinkTimer;

	private bool _isRamming;

	private int _layerMask;

	private readonly RaycastHit[] _hits = new RaycastHit[4];

	private readonly NetVar<int> _skin = new NetVar<int>(0);

	public new void Awake()
	{
		base.Awake();
		if (!_animator)
		{
			throw new UnityException("entity_monster_ram requires Animator component");
		}
		_rigidbody = GetComponent<Rigidbody>();
		if (!_rigidbody)
		{
			throw new UnityException("entity_monster_ram requires Rigidbody component");
		}
		_rigidbody.isKinematic = true;
		if (!body)
		{
			throw new UnityException("Missing body SkinnedMeshRenderer");
		}
		_led = GetComponentInChildren<entity_led>(includeInactive: true);
		if (!_led)
		{
			throw new UnityException("entity_monster_ram requires entity_led component");
		}
		_animatorEvent = GetComponentInChildren<entity_animator_event>(includeInactive: true);
		if (!_animatorEvent)
		{
			throw new UnityException("entity_monster_ram requires entity_animator_event component");
		}
		_layerMask = LayerMask.GetMask("entity_ground", "entity_player");
	}

	public override void OnNetworkSpawn()
	{
		base.OnNetworkSpawn();
		_animatorEvent.OnAnimationEvent += new Action<string>(OnAnimationEvent);
		if (base.IsServer)
		{
			_skin.Value = EncodeSkin(UnityEngine.Random.Range(0, masks.Count), UnityEngine.Random.Range(0, colors.Count), UnityEngine.Random.Range(0, 3), UnityEngine.Random.Range(30, 100));
			_target = _behavior.GetVariable<GameObject>("TARGET");
			_disabled = _behavior.GetVariable<bool>("DISABLED");
			_disabled.Value = false;
			Opsive.Shared.Events.EventHandler.RegisterEvent(_behavior, "BAWBAW", OnBAWBAW);
		}
	}

	public override void OnNetworkDespawn()
	{
		base.OnNetworkDespawn();
		_blinkTimer?.Stop();
		if ((bool)_animatorEvent)
		{
			_animatorEvent.OnAnimationEvent -= new Action<string>(OnAnimationEvent);
		}
		if (base.IsServer)
		{
			_ramTimer?.Stop();
			Opsive.Shared.Events.EventHandler.UnregisterEvent(_behavior, "BAWBAW", OnBAWBAW);
		}
	}

	protected override void OnNetworkPostSpawn()
	{
		base.OnNetworkPostSpawn();
		if (base.IsClient)
		{
			_skin.RegisterOnValueChanged(delegate(int _, int newValue)
			{
				ApplySkin(newValue);
			});
		}
	}

	public override void OnNetworkPreDespawn()
	{
		base.OnNetworkPreDespawn();
		if (base.IsClient)
		{
			_skin.OnValueChanged = null;
		}
	}

	public new void Update()
	{
		base.Update();
	}

	public void FixedUpdate()
	{
		if (!base.IsServer || !_rigidbody || !_isRamming || _rigidbody.isKinematic)
		{
			return;
		}
		float magnitude = _rigidbody.linearVelocity.magnitude;
		if (magnitude < RAM_SPEED)
		{
			float num = Mathf.Min(RAM_ACCELERATION * Time.fixedDeltaTime, RAM_SPEED - magnitude);
			_rigidbody.linearVelocity += base.transform.forward * num;
		}
		Vector3 vector = base.transform.position + Vector3.up * 0.5f;
		Vector3 halfExtents = new Vector3(0.5f, 0.4f, 0.1f) / 2f;
		float maxDistance = Mathf.Max(0.05f, _rigidbody.linearVelocity.magnitude * Time.fixedDeltaTime + 0.3f);
		int num2 = Physics.BoxCastNonAlloc(vector, halfExtents, base.transform.forward, _hits, base.transform.rotation, maxDistance, _layerMask, QueryTriggerInteraction.Ignore);
		if (num2 <= 0)
		{
			return;
		}
		bool flag = false;
		for (int i = 0; i < num2; i++)
		{
			RaycastHit raycastHit = _hits[i];
			if ((bool)raycastHit.collider)
			{
				if (raycastHit.collider.CompareTag("Player") && raycastHit.collider.TryGetComponent<entity_player>(out var component))
				{
					component.TakeHealthRPC(30, DamageType.CUT);
					flag = true;
				}
				else
				{
					flag = true;
				}
			}
		}
		if (flag)
		{
			NetController<ShakeController>.Instance?.Shake3DRPC(vector, ShakeMode.SHAKE_ALL, 0.25f, 0.05f, ShakeSoundMode.OFF, 5f);
			NetController<EffectController>.Instance?.PlayEffectRPC(EffectType.SMOKE, vector, new EffectSettings
			{
				playSound = false
			});
			StopRamming();
		}
	}

	[Shared]
	private void OnAnimationEvent(string eventName)
	{
		if (!_led || !string.Equals(eventName, "BawLight", StringComparison.InvariantCultureIgnoreCase))
		{
			return;
		}
		_led.SetActive(enable: true);
		_blinkTimer?.Stop();
		_blinkTimer = util_timer.Simple(0.25f, delegate
		{
			if ((bool)_led)
			{
				_led.SetActive(enable: false);
			}
		});
	}

	[Server]
	private void StopRamming()
	{
		_isRamming = false;
		_rigidbody.linearVelocity = Vector3.zero;
		_rigidbody.angularVelocity = Vector3.zero;
		_rigidbody.isKinematic = true;
		_agent.isStopped = false;
		ResetPath();
		SetSpeed(2f);
		_ramTimer?.Stop();
		_ramTimer = util_timer.Simple(3f, delegate
		{
			_animator.SetInteger(Status, 0);
			_ramTimer = util_timer.Simple(0.5f, delegate
			{
				_target.Value = null;
				_disabled.Value = false;
			});
		});
	}

	[Server]
	private void OnBAWBAW()
	{
		if (!base.IsServer)
		{
			throw new UnityException("OnBAWBAW called on client");
		}
		if (_agent.isTraversingOffMeshLink || !_target.Value)
		{
			return;
		}
		_animator.SetInteger(Status, 1);
		_disabled.Value = true;
		_agent.isStopped = true;
		_ramTimer?.Stop();
		_ramTimer = util_timer.Simple(0.5f, delegate
		{
			_ramTimer?.Stop();
			_ramTimer = util_timer.Simple(0.15f, delegate
			{
				SetSpeed(0.001f);
				SetPath(_target.Value.transform.position);
				_ramTimer?.Stop();
				_ramTimer = util_timer.Simple(0.85f, delegate
				{
					ResetPath();
					_isRamming = true;
					_rigidbody.isKinematic = false;
					_ramTimer?.Stop();
					_ramTimer = util_timer.Simple(10f, StopRamming);
				});
			});
		});
	}

	private int EncodeSkin(int maskIndex, int colorIndex, int headIndex, int headWeight)
	{
		return (maskIndex & 0xFF) | ((colorIndex & 0xFF) << 8) | ((headIndex & 0xFF) << 16) | ((headWeight & 0xFF) << 24);
	}

	[Shared]
	private void ApplySkin(int encoded)
	{
		if ((bool)body)
		{
			int num = encoded & 0xFF;
			int num2 = (encoded >> 8) & 0xFF;
			int num3 = (encoded >> 16) & 0xFF;
			int num4 = (encoded >> 24) & 0xFF;
			Material material = body.material;
			if (masks.Count > 0 && num >= 0 && num < masks.Count)
			{
				material.SetTexture(MaskTexture, masks[num]);
			}
			if (colors.Count > 0 && num2 >= 0 && num2 < colors.Count)
			{
				material.SetColor(CoatColor, colors[num2]);
			}
			for (int i = 0; i < 3; i++)
			{
				body.SetBlendShapeWeight(i, (i == num3) ? num4 : 0);
			}
		}
	}

	protected override void __initializeVariables()
	{
		if (_skin == null)
		{
			throw new Exception("entity_monster_ram._skin cannot be null. All NetworkVariableBase instances must be initialized.");
		}
		_skin.Initialize(this);
		__nameNetworkVariable(_skin, "_skin");
		NetworkVariableFields.Add(_skin);
		base.__initializeVariables();
	}

	protected override void __initializeRpcs()
	{
		base.__initializeRpcs();
	}

	protected internal override string __getTypeName()
	{
		return "entity_monster_ram";
	}
}
