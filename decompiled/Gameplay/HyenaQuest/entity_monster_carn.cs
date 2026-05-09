using System;
using UnityEngine;

namespace HyenaQuest;

public class entity_monster_carn : entity_monster_ai
{
	public entity_trigger trigger;

	public new void Awake()
	{
		base.Awake();
		if (!_networkAnimator)
		{
			throw new UnityException("entity_monster_carn requires NetworkAnimator component");
		}
	}

	public override void OnNetworkSpawn()
	{
		base.OnNetworkSpawn();
		if (base.IsServer)
		{
			trigger.OnEnter += new Action<Collider>(OnTriggerEnter);
		}
	}

	public override void OnNetworkDespawn()
	{
		base.OnNetworkDespawn();
		if (base.IsServer)
		{
			trigger.OnEnter -= new Action<Collider>(OnTriggerEnter);
		}
	}

	private void OnTriggerEnter(Collider obj)
	{
		if (base.IsServer && obj.CompareTag("Player"))
		{
			entity_player component = obj.GetComponent<entity_player>();
			if ((bool)component)
			{
				_networkAnimator.SetTrigger("ATTACK");
				component.Kill(DamageType.CUT);
			}
		}
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
		return "entity_monster_carn";
	}
}
