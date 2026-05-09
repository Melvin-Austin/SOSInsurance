using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

namespace HyenaQuest;

public class entity_prop_delivery_gas : entity_prop_delivery
{
	public GameObject fireTemplate;

	private readonly List<NetworkObject> _spawnedFire = new List<NetworkObject>();

	protected override void OnDamage(byte newHealth)
	{
		base.OnDamage(newHealth);
		if (base.IsServer)
		{
			int num = Random.Range(1, 3);
			for (int i = 0; i < num; i++)
			{
				SpawnFire();
			}
		}
	}

	public override void OnNetworkDespawn()
	{
		base.OnNetworkDespawn();
		if (!base.IsServer)
		{
			return;
		}
		foreach (NetworkObject item in _spawnedFire)
		{
			if ((bool)item && item.IsSpawned)
			{
				item.Despawn();
			}
		}
	}

	[Server]
	private void SpawnFire()
	{
		if (!base.IsServer)
		{
			throw new UnityException("Not Server");
		}
		GameObject obj = Object.Instantiate(fireTemplate, base.transform.position + new Vector3(Random.value * 0.2f, 0f, Random.value * 0.2f), Quaternion.identity);
		if (!obj)
		{
			throw new UnityException("Failed to spawn fire");
		}
		entity_fire component = obj.GetComponent<entity_fire>();
		if (!component)
		{
			throw new UnityException("Failed to spawn fire");
		}
		component.fireSpeed = Random.Range(0.1f, 0.3f);
		component.transform.localScale = Vector3.one * Random.Range(0.8f, 1f);
		NetworkObject component2 = obj.GetComponent<NetworkObject>();
		if (!component2)
		{
			throw new UnityException("Failed to spawn fire");
		}
		component2.Spawn();
		_spawnedFire.Add(component2);
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
		return "entity_prop_delivery_gas";
	}
}
