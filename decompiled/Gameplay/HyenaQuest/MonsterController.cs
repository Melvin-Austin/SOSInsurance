using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Unity.Netcode;
using UnityEngine;
using ZLinq;

namespace HyenaQuest;

[DisallowMultipleComponent]
[DefaultExecutionOrder(-70)]
[RequireComponent(typeof(NetworkObject))]
public class MonsterController : NetController<MonsterController>
{
	private readonly List<entity_monster_ai> _spawned = new List<entity_monster_ai>();

	public override void OnNetworkSpawn()
	{
		base.OnNetworkSpawn();
		if (!base.IsServer)
		{
			return;
		}
		CoreController.WaitFor(delegate(IngameController ingameCtrl)
		{
			if ((bool)this && base.IsSpawned)
			{
				ingameCtrl.OnStatusUpdated += new Action<INGAME_STATUS, bool>(OnIngameStatusUpdated);
			}
		});
	}

	public override void OnNetworkDespawn()
	{
		base.OnNetworkDespawn();
		if (base.IsServer && (bool)NetController<IngameController>.Instance)
		{
			NetController<IngameController>.Instance.OnStatusUpdated -= new Action<INGAME_STATUS, bool>(OnIngameStatusUpdated);
		}
	}

	[Server]
	public void CleanupMonsters()
	{
		foreach (entity_monster_ai item in _spawned)
		{
			if ((bool)item && item.IsSpawned)
			{
				item.Destroy();
			}
		}
		_spawned.Clear();
	}

	[Server]
	public void DuplicateSpawnMonster(string monsterID, Transform pos, Action<entity_monster_ai> callback = null)
	{
		DuplicateSpawnMonster(monsterID, pos.position, callback);
	}

	[Server]
	public void DuplicateSpawnMonster(string monsterID, Vector3 pos, Action<entity_monster_ai> callback = null)
	{
		if (string.IsNullOrEmpty(monsterID))
		{
			throw new UnityException("Monster template is null");
		}
		GameObject monsterByID = GetMonsterByID(monsterID);
		if (!monsterByID)
		{
			throw new UnityException("Failed to get monster template");
		}
		InternalSpawnMonster(monsterByID, pos, callback).Forget();
	}

	[Server]
	public void SpawnMonster(GameObject template, Transform tPos, Action<entity_monster_ai> callback = null)
	{
		if ((bool)this && base.IsSpawned)
		{
			if (!template)
			{
				throw new UnityException("Monster template is null or missing a prefab reference");
			}
			InternalSpawnMonster(template, tPos.position, callback).Forget();
		}
	}

	[Server]
	public void SpawnMonster(GameObject template, Vector3 pos, Action<entity_monster_ai> callback = null)
	{
		if ((bool)this && base.IsSpawned)
		{
			if (!template)
			{
				throw new UnityException("Monster template is null or missing a prefab reference");
			}
			InternalSpawnMonster(template, pos, callback).Forget();
		}
	}

	[Server]
	public void SpawnMonster(string monsterID, bool far, Action<entity_monster_ai> callback = null)
	{
		if (!this || !base.IsSpawned)
		{
			return;
		}
		List<Transform> list = (far ? NetController<MapController>.Instance.GetRandomRoomSpawnPointsAwayFromPlayers() : NetController<MapController>.Instance.GetAllSpawnPoints());
		if (list == null || list.Count == 0)
		{
			Debug.LogError("No monster spawns found");
			return;
		}
		GameObject monsterByID = GetMonsterByID(monsterID);
		if (!monsterByID)
		{
			throw new UnityException("Failed to get monster template for ID: " + monsterID);
		}
		Transform transform = list[UnityEngine.Random.Range(0, list.Count)];
		InternalSpawnMonster(monsterByID, transform.position, callback).Forget();
	}

	public GameObject GetMonsterByID(string id)
	{
		if (string.IsNullOrEmpty(id))
		{
			return null;
		}
		if (!NetController<MapController>.Instance)
		{
			throw new UnityException("Missing MapController");
		}
		List<WorldSettings> worlds = NetController<MapController>.Instance.GetWorlds();
		if (worlds == null || worlds.Count == 0)
		{
			throw new UnityException("No world settings found");
		}
		foreach (WorldSettings item in worlds)
		{
			foreach (MonsterSpawn monster in item.monsters)
			{
				GameObject gameObject = monster.variants.AsValueEnumerable().FirstOrDefault((GameObject m) => string.Equals(m.name, id, StringComparison.CurrentCultureIgnoreCase));
				if ((bool)gameObject)
				{
					return gameObject;
				}
			}
		}
		return null;
	}

	[Server]
	private async UniTaskVoid InternalSpawnMonster(GameObject template, Vector3 pos, Action<entity_monster_ai> callback = null)
	{
		if (!template)
		{
			throw new UnityException("Monster template is null");
		}
		if (!this)
		{
			return;
		}
		string templateName = template.name;
		if (string.IsNullOrEmpty(templateName))
		{
			throw new UnityException("Invalid monster template name");
		}
		if (templateName.StartsWith("SDK-entity_monster", StringComparison.InvariantCultureIgnoreCase))
		{
			template = MonoController<SDKController>.Instance.monsters.AsValueEnumerable().SingleOrDefault((GameObject s) => string.Equals(s.name, templateName.Replace("SDK-", ""), StringComparison.InvariantCultureIgnoreCase));
		}
		if (!template)
		{
			Debug.LogWarning("Could not find SDK monster template: " + templateName);
			return;
		}
		AsyncInstantiateOperation<GameObject> op = UnityEngine.Object.InstantiateAsync(template, pos, Quaternion.identity);
		await op.ToUniTask();
		if ((bool)this)
		{
			GameObject obj = op.Result[0];
			if (!obj)
			{
				throw new UnityException("Failed to instantiate monster");
			}
			entity_monster_ai component = obj.GetComponent<entity_monster_ai>();
			if (!component)
			{
				throw new UnityException("Monster template missing entity_monster_ai component");
			}
			component.NetworkObject.Spawn(destroyWithScene: true);
			_spawned.Add(component);
			callback?.Invoke(component);
		}
	}

	[Server]
	private void SpawnMonsters()
	{
		if (!NetController<IngameController>.Instance)
		{
			throw new UnityException("Missing IngameController");
		}
		int num = CalculateMaxMonsters();
		if (num > 0 && _spawned.Count < num)
		{
			for (int i = 0; i < num; i++)
			{
				PickAndSpawnMonster();
			}
		}
	}

	[Server]
	private void PickAndSpawnMonster()
	{
		if (!NetController<MapController>.Instance)
		{
			throw new UnityException("Missing MapController");
		}
		WorldSettings generatedWorld = NetController<MapController>.Instance.GetGeneratedWorld();
		if (!generatedWorld)
		{
			throw new UnityException("Generated world type is null");
		}
		List<MonsterSpawn> monsters = generatedWorld.monsters;
		if (monsters == null || monsters.Count <= 0)
		{
			return;
		}
		List<MonsterSpawn> monsters2 = generatedWorld.monsters;
		if (monsters2.Count == 0)
		{
			Debug.LogError($"No eligible monsters found for world {generatedWorld}");
			return;
		}
		float num = 0f;
		foreach (MonsterSpawn item in monsters2)
		{
			num += item.chance;
		}
		float num2 = UnityEngine.Random.value * num;
		float num3 = 0f;
		MonsterSpawn monsterSpawn = monsters2[0];
		foreach (MonsterSpawn item2 in monsters2)
		{
			num3 += item2.chance;
			if (num2 <= num3)
			{
				monsterSpawn = item2;
				break;
			}
		}
		int num4 = UnityEngine.Random.Range(0, monsterSpawn.variants.Count);
		GameObject gameObject = monsterSpawn.variants[num4];
		if (!gameObject)
		{
			throw new UnityException($"Invalid variant {num4} on world {generatedWorld.name}");
		}
		List<Transform> allSpawnPoints = NetController<MapController>.Instance.GetAllSpawnPoints();
		if (allSpawnPoints == null || allSpawnPoints.Count == 0)
		{
			throw new UnityException("No spawn points available");
		}
		Transform spawnLocation = allSpawnPoints[UnityEngine.Random.Range(0, allSpawnPoints.Count)];
		if (!spawnLocation)
		{
			throw new UnityException("No spawn points available");
		}
		SpawnMonster(gameObject, spawnLocation.position, delegate(entity_monster_ai ai)
		{
			Debug.Log($"Created monster {ai.name} at {spawnLocation.position}");
		});
	}

	[Server]
	private void OnIngameStatusUpdated(INGAME_STATUS status, bool server)
	{
		if (server)
		{
			switch (status)
			{
			case INGAME_STATUS.WAITING_PLAY_CONFIRMATION:
				SpawnMonsters();
				break;
			case INGAME_STATUS.ROUND_END:
				CleanupMonsters();
				break;
			}
		}
	}

	[Server]
	private int CalculateMaxMonsters()
	{
		if (!base.IsServer)
		{
			throw new UnityException("Server only");
		}
		if (!NetController<MapController>.Instance)
		{
			throw new UnityException("Missing MapController");
		}
		if (!NetController<IngameController>.Instance)
		{
			throw new UnityException("Missing IngameController");
		}
		if (!NetController<MapController>.Instance.IsGenerated())
		{
			return 0;
		}
		WorldSettings generatedWorld = NetController<MapController>.Instance.GetGeneratedWorld();
		if (!generatedWorld)
		{
			throw new UnityException("Generated world type is null");
		}
		return generatedWorld.CalculateMaxMonsters(NetController<IngameController>.Instance.GetCurrentRound());
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
		return "MonsterController";
	}
}
