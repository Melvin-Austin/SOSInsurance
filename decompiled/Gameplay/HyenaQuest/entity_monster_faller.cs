using System;
using FailCake;
using UnityEngine;

namespace HyenaQuest;

public class entity_monster_faller : entity_monster_ai
{
	private Rigidbody _body;

	private LineRenderer _lineRenderer;

	private int _layerMask;

	private util_timer _timer;

	private readonly NetVar<bool> _activated = new NetVar<bool>(value: false);

	public new void Awake()
	{
		_body = GetComponent<Rigidbody>();
		if (!_body)
		{
			throw new UnityException("entity_monster_faller requires a Rigidbody component to work.");
		}
		_lineRenderer = GetComponentInChildren<LineRenderer>(includeInactive: true);
		if (!_lineRenderer)
		{
			throw new UnityException("entity_monster_faller requires a LineRenderer component to work.");
		}
		_lineRenderer.useWorldSpace = true;
		_lineRenderer.positionCount = 2;
		_layerMask = LayerMask.GetMask("entity_phys", "entity_ground", "entity_player", "entity_phys_item");
	}

	protected override void OnNetworkPostSpawn()
	{
		base.OnNetworkPostSpawn();
		if (!base.IsClient)
		{
			return;
		}
		_activated.RegisterOnValueChanged(delegate(bool oldValue, bool newValue)
		{
			if (oldValue != newValue)
			{
				_lineRenderer.enabled = !newValue;
			}
		});
	}

	public override void OnNetworkPreDespawn()
	{
		base.OnNetworkPreDespawn();
		if (base.IsClient)
		{
			_activated.OnValueChanged = null;
		}
	}

	public new void Update()
	{
		base.Update();
		if (!_lineRenderer || _activated.Value)
		{
			return;
		}
		Vector3 position = base.transform.position;
		Vector3 position2 = base.transform.up * 1000f;
		if (Physics.Raycast(position, base.transform.up, out var hitInfo, 20f, _layerMask, QueryTriggerInteraction.Ignore))
		{
			if (base.IsServer && (bool)hitInfo.rigidbody)
			{
				ActivateMine();
			}
			Vector3 normalized = (hitInfo.point - position).normalized;
			position2 = position + normalized * Vector3.Distance(position, hitInfo.point);
		}
		_lineRenderer.SetPosition(0, position);
		_lineRenderer.SetPosition(1, position2);
	}

	public override void OnNetworkDespawn()
	{
		base.OnNetworkDespawn();
		if (base.IsServer)
		{
			_timer?.Stop();
		}
	}

	[Server]
	private void ActivateMine()
	{
		if (!base.IsServer)
		{
			throw new UnityException("Server only");
		}
		_activated.Value = true;
		_body.isKinematic = false;
		NetController<SoundController>.Instance?.Play3DSound("Ingame/Monsters/Faller/activate.ogg", base.transform.position, new AudioData
		{
			distance = 4f,
			pitch = UnityEngine.Random.Range(0.8f, 1.2f)
		}, broadcast: true);
		_timer?.Stop();
		_timer = util_timer.Simple(0.8f + UnityEngine.Random.Range(0f, 0.8f), delegate
		{
			NetController<ExplosionController>.Instance?.Explode(base.transform.position, 4f, 180);
			if (base.IsSpawned)
			{
				base.NetworkObject.Despawn();
			}
		});
	}

	protected override void __initializeVariables()
	{
		if (_activated == null)
		{
			throw new Exception("entity_monster_faller._activated cannot be null. All NetworkVariableBase instances must be initialized.");
		}
		_activated.Initialize(this);
		__nameNetworkVariable(_activated, "_activated");
		NetworkVariableFields.Add(_activated);
		base.__initializeVariables();
	}

	protected override void __initializeRpcs()
	{
		base.__initializeRpcs();
	}

	protected internal override string __getTypeName()
	{
		return "entity_monster_faller";
	}
}
