using System;
using FailCake;
using UnityEngine;

namespace HyenaQuest;

public class entity_monster_redlight : entity_monster_ai
{
	private static readonly byte DAMAGE = 8;

	public float detectionDuration = 1f;

	public float offDuration = 2f;

	public float detectionDistance = 4f;

	public float force = 10f;

	private LineRenderer _lineRenderer;

	private entity_led _ledStatus;

	private int _obstacleLayerMask;

	private float _shootingCooldown;

	private util_timer _lightCycleTimer;

	private readonly NetVar<bool> _isRedLight = new NetVar<bool>(value: false);

	public new void Awake()
	{
		base.Awake();
		_lineRenderer = GetComponentInChildren<LineRenderer>(includeInactive: true);
		if (!_lineRenderer)
		{
			throw new UnityException("LineRenderer is not assigned in entity_monster_redlight");
		}
		_lineRenderer.useWorldSpace = true;
		_lineRenderer.positionCount = 2;
		_lineRenderer.enabled = false;
		_ledStatus = GetComponentInChildren<entity_led>(includeInactive: true);
		if (!_ledStatus)
		{
			throw new UnityException("Detecting LED is not assigned in entity_monster_redlight");
		}
		_obstacleLayerMask = LayerMask.GetMask("entity_ground");
	}

	protected override void OnNetworkPostSpawn()
	{
		base.OnNetworkPostSpawn();
		if (!base.IsClient)
		{
			return;
		}
		_isRedLight.RegisterOnValueChanged(delegate(bool _, bool newValue)
		{
			if ((bool)_ledStatus)
			{
				_ledStatus.SetActive(newValue);
				ResetTurret();
			}
		});
	}

	public override void OnNetworkPreDespawn()
	{
		base.OnNetworkPreDespawn();
		if (base.IsClient)
		{
			_isRedLight.OnValueChanged = null;
		}
	}

	public override void OnNetworkSpawn()
	{
		base.OnNetworkSpawn();
		if (base.IsServer)
		{
			_lightCycleTimer?.Stop();
			_lightCycleTimer = util_timer.Create(-1, detectionDuration, delegate
			{
				bool flag = !_isRedLight.Value;
				_isRedLight.Value = flag;
				_lightCycleTimer.SetDelay(flag ? detectionDuration : offDuration);
			});
		}
	}

	public override void OnNetworkDespawn()
	{
		base.OnNetworkDespawn();
		if (base.IsServer)
		{
			_lightCycleTimer?.Stop();
		}
	}

	public new void Update()
	{
		base.Update();
		if (base.IsClient && (bool)PlayerController.LOCAL && _isRedLight.Value)
		{
			_lineRenderer.enabled = false;
			if (!PlayerController.LOCAL.IsDead() && !(Time.time < _shootingCooldown) && !(Vector3.Distance(PlayerController.LOCAL.transform.position, base.transform.position) > detectionDistance) && PlayerController.LOCAL.IsPressingAnyKey() && HasLineOfSight(PlayerController.LOCAL))
			{
				_lineRenderer.enabled = true;
				_lineRenderer.SetPosition(0, _lineRenderer.transform.position);
				_lineRenderer.SetPosition(1, PlayerController.LOCAL.chest.transform.position);
				PlayerController.LOCAL.TakeHealth(DAMAGE);
				Vector3 normalized = (PlayerController.LOCAL.transform.position - base.transform.position).normalized;
				PlayerController.LOCAL.Shove(normalized, force);
				NetController<SoundController>.Instance?.PlaySound($"Ingame/Monsters/RedLight/shoot_{UnityEngine.Random.Range(0, 2)}.ogg", new AudioData
				{
					pitch = UnityEngine.Random.Range(0.9f, 1.2f),
					volume = UnityEngine.Random.Range(0.9f, 1f)
				});
				_shootingCooldown = Time.time + 0.2f;
			}
		}
	}

	private void ResetTurret()
	{
		if ((bool)_lineRenderer)
		{
			_lineRenderer.enabled = false;
			_shootingCooldown = 0f;
		}
	}

	private bool HasLineOfSight(entity_player player)
	{
		Vector3 position = _lineRenderer.transform.position;
		Vector3 position2 = player.head.position;
		return !Physics.Linecast(position, position2, _obstacleLayerMask, QueryTriggerInteraction.Collide);
	}

	protected override void __initializeVariables()
	{
		if (_isRedLight == null)
		{
			throw new Exception("entity_monster_redlight._isRedLight cannot be null. All NetworkVariableBase instances must be initialized.");
		}
		_isRedLight.Initialize(this);
		__nameNetworkVariable(_isRedLight, "_isRedLight");
		NetworkVariableFields.Add(_isRedLight);
		base.__initializeVariables();
	}

	protected override void __initializeRpcs()
	{
		base.__initializeRpcs();
	}

	protected internal override string __getTypeName()
	{
		return "entity_monster_redlight";
	}
}
