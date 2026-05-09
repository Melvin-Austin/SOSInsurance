using System;
using System.Collections.Generic;
using FailCake;
using Unity.Netcode;
using UnityEngine;

namespace HyenaQuest;

[RequireComponent(typeof(NetworkObject))]
public class entity_entity_spawner : NetworkBehaviour
{
	public GameObject prefab;

	public Transform spawnPoint;

	public float spawnDelay = 0.25f;

	private util_timer _spawnTimer;

	private readonly List<NetworkObject> _spawnedItems = new List<NetworkObject>();

	public void Awake()
	{
		if (!prefab)
		{
			throw new UnityException("Item prefab not set");
		}
	}

	public override void OnNetworkSpawn()
	{
		base.OnNetworkSpawn();
		if (base.IsServer)
		{
			CoreController.WaitFor(delegate(IngameController ingameController)
			{
				ingameController.OnStatusUpdated += new Action<INGAME_STATUS, bool>(OnIngameStatusUpdated);
				OnIngameStatusUpdated(ingameController.Status(), server: true);
			});
		}
	}

	public override void OnNetworkDespawn()
	{
		base.OnNetworkDespawn();
		if (base.IsServer)
		{
			_spawnTimer?.Stop();
			if ((bool)NetController<IngameController>.Instance)
			{
				NetController<IngameController>.Instance.OnStatusUpdated -= new Action<INGAME_STATUS, bool>(OnIngameStatusUpdated);
			}
		}
	}

	private void OnIngameStatusUpdated(INGAME_STATUS status, bool server)
	{
		if (server)
		{
			switch (status)
			{
			case INGAME_STATUS.IDLE:
				SpawnItems();
				break;
			case INGAME_STATUS.ROUND_END:
			case INGAME_STATUS.GAMEOVER:
				ClearItems();
				break;
			}
		}
	}

	[Server]
	private void SpawnItems()
	{
		if (!base.IsServer)
		{
			throw new UnityException("SpawnItems called on client");
		}
		ClearItems();
		_spawnTimer?.Stop();
		_spawnTimer = util_timer.Create(Mathf.Max(NETController.MAX_PLAYERS, NETController.DEFAULT_MAX_PLAYERS) + 1, spawnDelay, SpawnTemplates);
	}

	[Server]
	private void ClearItems()
	{
		if (!base.IsServer)
		{
			throw new UnityException("ClearItems called on client");
		}
		_spawnTimer?.Stop();
		_spawnTimer = null;
		if (_spawnedItems.Count == 0)
		{
			return;
		}
		foreach (NetworkObject spawnedItem in _spawnedItems)
		{
			if ((bool)spawnedItem && spawnedItem.IsSpawned)
			{
				spawnedItem.Despawn();
			}
		}
		_spawnedItems.Clear();
	}

	[Server]
	private void SpawnTemplates(int index)
	{
		if (!base.IsServer)
		{
			throw new UnityException("SpawnFlashlight called on client");
		}
		if (!prefab)
		{
			throw new UnityException("Item prefab not set");
		}
		if (index == 1 && UnityEngine.Random.Range(0, 3) == 1)
		{
			_spawnTimer.SetDelay(1.5f);
		}
		DoSpawn(index);
	}

	[Server]
	private void DoSpawn(int index)
	{
		Vector3 position = (spawnPoint ? spawnPoint.position : base.transform.position);
		Quaternion rotation = (spawnPoint ? spawnPoint.rotation : base.transform.rotation) * Quaternion.Euler(UnityEngine.Random.Range(-45, 45), UnityEngine.Random.Range(0f, 360f), UnityEngine.Random.Range(-45, 45));
		GameObject obj = UnityEngine.Object.Instantiate(prefab, position, rotation);
		if (!obj)
		{
			throw new UnityException("Failed to spawn item prefab");
		}
		NetworkObject component = obj.GetComponent<NetworkObject>();
		if (!component)
		{
			throw new UnityException("Failed to get NetworkObject component");
		}
		component.Spawn();
		_spawnedItems.Add(component);
		NetController<SoundController>.Instance.Play3DSound("Ingame/Store/store_fwomp.ogg", position, new AudioData
		{
			distance = 1.5f,
			volume = 0.1f,
			pitch = 0.8f * (float)index + 1f
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
		return "entity_entity_spawner";
	}
}
