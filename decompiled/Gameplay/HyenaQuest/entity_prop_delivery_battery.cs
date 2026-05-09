using FailCake;
using UnityEngine;

namespace HyenaQuest;

public class entity_prop_delivery_battery : entity_prop_delivery
{
	public GameObject sparkDamage;

	private util_timer _sparkTimer;

	public override void OnNetworkDespawn()
	{
		base.OnNetworkDespawn();
		_sparkTimer?.Stop();
	}

	protected override void Init()
	{
		base.Init();
		if (!sparkDamage)
		{
			throw new UnityException("Missing sparkDamage gameobject");
		}
		sparkDamage.SetActive(value: false);
	}

	protected override void OnDamage(byte newHealth)
	{
		base.OnDamage(newHealth);
		if (!sparkDamage)
		{
			return;
		}
		sparkDamage.SetActive(value: true);
		NetController<SoundController>.Instance?.Play3DSound($"General/Entities/Effects/Spark/spark_{Random.Range(0, 3)}.ogg", base.transform.position, new AudioData
		{
			pitch = Random.Range(0.8f, 1.2f),
			volume = 0.7f
		});
		if (newHealth <= 0)
		{
			return;
		}
		_sparkTimer?.Stop();
		_sparkTimer = util_timer.Simple(0.8f, delegate
		{
			if ((bool)sparkDamage)
			{
				sparkDamage.SetActive(value: false);
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
		return "entity_prop_delivery_battery";
	}
}
