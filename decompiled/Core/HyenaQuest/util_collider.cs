using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using UnityEngine;
using ZLinq;

namespace HyenaQuest;

public static class util_collider
{
	private static readonly ConcurrentDictionary<GameObject, ConcurrentDictionary<Type, (Component component, DateTime lastAccessed)>> GameObjectCache = new ConcurrentDictionary<GameObject, ConcurrentDictionary<Type, (Component, DateTime)>>();

	private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(1.0);

	public static void Clear()
	{
		GameObjectCache.Clear();
	}

	public static void Tick()
	{
		DateTime utcNow = DateTime.UtcNow;
		foreach (KeyValuePair<GameObject, ConcurrentDictionary<Type, (Component, DateTime)>> item in GameObjectCache.AsValueEnumerable().ToList())
		{
			foreach (KeyValuePair<Type, (Component, DateTime)> item2 in item.Value.AsValueEnumerable().ToList())
			{
				if (utcNow - item2.Value.Item2 > CacheDuration)
				{
					item.Value.TryRemove(item2.Key, out var _);
				}
			}
			if (item.Value.IsEmpty)
			{
				GameObjectCache.TryRemove(item.Key, out ConcurrentDictionary<Type, (Component, DateTime)> _);
			}
		}
	}

	public static bool TryGetComponentInChildren<T>(this Collider collider, out T component) where T : Component
	{
		if (!collider || !collider.gameObject)
		{
			component = null;
			return false;
		}
		component = collider.gameObject.GetComponentInChildren<T>();
		return component;
	}

	public static bool TryGetComponent<T>(this Collider collider, out T component, int maxLevels = -1) where T : Component
	{
		if (GameObjectCache.TryGetValue(collider.gameObject, out ConcurrentDictionary<Type, (Component, DateTime)> value) && value.TryGetValue(typeof(T), out var value2))
		{
			if (!value2.Item1)
			{
				value.TryRemove(typeof(T), out var _);
				component = null;
				return false;
			}
			component = (T)value2.Item1;
			value[typeof(T)] = (value2.Item1, DateTime.UtcNow);
			return true;
		}
		if (!collider.gameObject.TryGetComponent<T>(out component, maxLevels))
		{
			return false;
		}
		GameObjectCache.GetOrAdd(collider.gameObject, (GameObject _) => new ConcurrentDictionary<Type, (Component, DateTime)>())[typeof(T)] = (component, DateTime.UtcNow);
		return true;
	}

	public static bool TryGetComponent<T>(this GameObject gameObject, out T component, int maxLevels = -1) where T : Component
	{
		if (GameObjectCache.TryGetValue(gameObject, out ConcurrentDictionary<Type, (Component, DateTime)> value) && value.TryGetValue(typeof(T), out var value2))
		{
			if (!value2.Item1)
			{
				value.TryRemove(typeof(T), out var _);
				component = null;
				return false;
			}
			component = (T)value2.Item1;
			value[typeof(T)] = (value2.Item1, DateTime.UtcNow);
			return true;
		}
		component = gameObject.GetComponent<T>();
		if ((bool)component)
		{
			GameObjectCache.GetOrAdd(gameObject, (GameObject _) => new ConcurrentDictionary<Type, (Component, DateTime)>())[typeof(T)] = (component, DateTime.UtcNow);
			return true;
		}
		if (maxLevels == 0)
		{
			return false;
		}
		Transform parent = gameObject.transform.parent;
		int num = 0;
		while ((bool)parent && (maxLevels < 0 || num < maxLevels))
		{
			component = parent.GetComponent<T>();
			if ((bool)component)
			{
				GameObjectCache.GetOrAdd(gameObject, (GameObject _) => new ConcurrentDictionary<Type, (Component, DateTime)>())[typeof(T)] = (component, DateTime.UtcNow);
				return true;
			}
			parent = parent.parent;
			num++;
		}
		return false;
	}
}
