using FailCake;
using Unity.Netcode;
using UnityEngine;

namespace HyenaQuest;

public class entity_monster_cycle : NetworkBehaviour
{
	public float cooldown;

	[Range(0f, 1f)]
	public float chance;

	[Range(0f, 10f)]
	public float check = 1f;

	private entity_movement_networked _movement;

	private bool _isTravelling;

	private float _cooldown;

	private util_timer _timer;

	public void Awake()
	{
		_movement = GetComponent<entity_movement_networked>();
		if (!_movement)
		{
			_movement = GetComponentInChildren<entity_movement_networked>(includeInactive: true);
		}
		if (!_movement)
		{
			throw new UnityException("Missing entity_movement_networked");
		}
		_movement.loop = false;
	}

	public override void OnNetworkDespawn()
	{
		base.OnNetworkDespawn();
		if (base.IsServer)
		{
			_timer?.Stop();
			_movement?.StopMovement();
		}
	}

	public override void OnNetworkSpawn()
	{
		base.OnNetworkSpawn();
		if (!base.IsServer)
		{
			return;
		}
		_timer?.Stop();
		_timer = util_timer.Create(-1, check, delegate
		{
			if (!_isTravelling && !(Time.time < _cooldown) && !(Random.value > chance))
			{
				_isTravelling = true;
				_movement.StartMovement(reset: true, delegate
				{
					_cooldown = Time.time + cooldown;
					_isTravelling = false;
				});
			}
		});
	}

	protected override void __initializeVariables()
	{
		base.__initializeVariables();
	}

	protected override void __initializeRpcs()
	{
		base.__initializeRpcs();
	}

	protected internal override string __getTypeName()
	{
		return "entity_monster_cycle";
	}
}
