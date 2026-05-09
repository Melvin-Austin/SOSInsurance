using UnityEngine;

namespace HyenaQuest;

public class entity_monster_chicken : entity_monster_chaser
{
	protected override bool OnUpdate()
	{
		if (!base.OnUpdate())
		{
			return false;
		}
		if (Vector3.Distance(_targetPlayer.transform.position, base.transform.position) <= 0.8f && Time.time > _attackCD)
		{
			_attackCD = Time.time + 1.2f;
			_networkAnimator?.SetTrigger("ATTACK");
			_targetPlayer.TakeHealthRPC(8, DamageType.CUT);
			NetController<SoundController>.Instance?.Play3DSound($"Ingame/Monsters/Chicken/alert_{Random.Range(0, 3)}.ogg", base.transform.position, new AudioData
			{
				distance = 5f,
				volume = 1f,
				pitch = Random.Range(0.8f, 1.2f)
			}, broadcast: true);
		}
		return true;
	}

	[Server]
	protected override void OnIdle()
	{
		NetController<SoundController>.Instance?.Play3DSound(_targetPlayer ? $"Ingame/Monsters/Chicken/angrybawk{Random.Range(1, 9)}.ogg" : $"Ingame/Monsters/Chicken/bawk{Random.Range(1, 15)}.ogg", base.transform.position, new AudioData
		{
			distance = 5f,
			volume = 0.15f,
			pitch = Random.Range(0.8f, 1.2f)
		}, broadcast: true);
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
		return "entity_monster_chicken";
	}
}
