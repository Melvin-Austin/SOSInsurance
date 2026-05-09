using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

namespace HyenaQuest;

[RequireComponent(typeof(NetworkObject))]
public class entity_gate : NetworkBehaviour
{
	public List<Sprite> statusSprites = new List<Sprite>();

	private SpriteRenderer _status;

	private entity_door _door;

	private entity_trigger _trigger;

	private readonly NetVar<bool> _open = new NetVar<bool>(value: false);

	public void Awake()
	{
		_status = GetComponentInChildren<SpriteRenderer>(includeInactive: true);
		if (!_status)
		{
			throw new UnityException("Missing SpriteRenderer");
		}
		_door = GetComponentInChildren<entity_door>(includeInactive: true);
		if (!_door)
		{
			throw new UnityException("Missing entity_door");
		}
		_trigger = GetComponentInChildren<entity_trigger>(includeInactive: true);
		if (!_trigger)
		{
			throw new UnityException("Missing entity_trigger");
		}
	}

	public override void OnNetworkSpawn()
	{
		base.OnNetworkSpawn();
		if (base.IsServer && (bool)_trigger)
		{
			_trigger.OnEnter += new Action<Collider>(OnTriggerEnter);
		}
	}

	public override void OnNetworkDespawn()
	{
		base.OnNetworkDespawn();
		if (base.IsServer && (bool)_trigger)
		{
			_trigger.OnEnter -= new Action<Collider>(OnTriggerEnter);
		}
	}

	protected override void OnNetworkPostSpawn()
	{
		base.OnNetworkPostSpawn();
		if (!base.IsClient)
		{
			return;
		}
		_open.RegisterOnValueChanged(delegate(bool _, bool newValue)
		{
			if (newValue)
			{
				if ((bool)_status)
				{
					_status.sprite = statusSprites[1];
				}
				if ((bool)_door)
				{
					_door.SetOpen(newValue: true);
				}
			}
		});
	}

	public override void OnNetworkPreDespawn()
	{
		base.OnNetworkPreDespawn();
		if (base.IsClient)
		{
			_open.OnValueChanged = null;
		}
	}

	private void OnTriggerEnter(Collider obj)
	{
		if ((bool)obj && !_open.Value)
		{
			_open.Value = true;
			NetController<SoundController>.Instance?.Play3DSound($"Ingame/Props/Glass/glass_break_{UnityEngine.Random.Range(0, 3)}.ogg", _trigger.transform.position, new AudioData
			{
				distance = 4f
			}, broadcast: true);
			NetController<EffectController>.Instance?.PlayEffectRPC(EffectType.SPARKS, _trigger.transform.position, new EffectSettings
			{
				playSound = true
			});
		}
	}

	protected override void __initializeVariables()
	{
		if (_open == null)
		{
			throw new Exception("entity_gate._open cannot be null. All NetworkVariableBase instances must be initialized.");
		}
		_open.Initialize(this);
		__nameNetworkVariable(_open, "_open");
		NetworkVariableFields.Add(_open);
		base.__initializeVariables();
	}

	protected override void __initializeRpcs()
	{
		base.__initializeRpcs();
	}

	protected internal override string __getTypeName()
	{
		return "entity_gate";
	}
}
