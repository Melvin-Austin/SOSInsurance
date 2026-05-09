using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

namespace HyenaQuest;

[RequireComponent(typeof(BoxCollider))]
public class entity_area_purger : MonoBehaviour
{
	private BoxCollider _collider;

	private static readonly WaitForSeconds PurgeDelay = new WaitForSeconds(1f);

	public void Awake()
	{
		_collider = GetComponent<BoxCollider>();
		if (!_collider)
		{
			throw new UnityException("Missing BoxCollider");
		}
	}

	[Server]
	public IEnumerator Purge(PurgeSettings settings, Action onComplete)
	{
		if (!NetworkManager.Singleton || !NetworkManager.Singleton.IsServer)
		{
			throw new UnityException("Purge can only be called on the server");
		}
		PurgeEntities(settings.outside, MonoController<PlayerController>.Instance.GetAlivePlayers(), delegate(entity_player player)
		{
			player.Kill(DamageType.INSTANT);
		}, settings.safeAreaCheck);
		yield return PurgeDelay;
		PurgeEntityType(settings, delegate(entity_phys phys)
		{
			phys.Destroy();
		});
		PurgeEntityType(settings, delegate(entity_monster_ai monster)
		{
			monster.NetworkObject.Despawn();
		});
		PurgeEntityType(settings, delegate(entity_store_item storeItem)
		{
			storeItem.NetworkObject.Despawn();
		});
		onComplete?.Invoke();
	}

	[Server]
	private void PurgeEntityType<T>(PurgeSettings settings, Action<T> purgeAction) where T : NetworkBehaviour
	{
		PurgeEntities(settings.outside, UnityEngine.Object.FindObjectsByType<T>(FindObjectsSortMode.None), purgeAction, settings.safeAreaCheck);
	}

	private void PurgeEntities<T>(bool outside, IEnumerable<T> entities, Action<T> purgeAction, bool checkIfSafe = false) where T : NetworkBehaviour
	{
		List<entity_area_purger_safezone> list = (checkIfSafe ? MonoController<PurgeController>.Instance.GetSafeZones() : null);
		foreach (T entity in entities)
		{
			if (!entity || !entity.IsSpawned)
			{
				continue;
			}
			Vector3 position = entity.gameObject.transform.position;
			if (entity is entity_player entity_player2)
			{
				position = entity_player2.neck.transform.position;
			}
			bool flag = false;
			if (checkIfSafe && list != null)
			{
				foreach (entity_area_purger_safezone item in list)
				{
					if (item.HasEntity(position))
					{
						flag = true;
						break;
					}
				}
			}
			if (!flag)
			{
				bool flag2 = IsEntityInside(position);
				if ((outside && !flag2) || (!outside && flag2))
				{
					purgeAction(entity);
				}
			}
		}
	}

	private bool IsEntityInside(Vector3 pos)
	{
		return _collider.bounds.Contains(pos);
	}
}
