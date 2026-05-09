using System;
using System.Collections.Generic;
using UnityEngine;

namespace HyenaQuest;

[DisallowMultipleComponent]
[DefaultExecutionOrder(-70)]
public class PurgeController : MonoController<PurgeController>
{
	public entity_area_purger OutsidePurger;

	private readonly List<entity_area_purger_safezone> _safeZones = new List<entity_area_purger_safezone>();

	public new void Awake()
	{
		base.Awake();
		if (!OutsidePurger)
		{
			throw new UnityException("Missing OutsidePurger");
		}
	}

	public void RegisterSafeArea(entity_area_purger_safezone area)
	{
		if (_safeZones.Contains(area))
		{
			throw new UnityException("Safe area already registered");
		}
		_safeZones.Add(area);
	}

	public void UnRegisterSafeArea(entity_area_purger_safezone area)
	{
		if (!_safeZones.Contains(area))
		{
			throw new UnityException("Safe area not registered");
		}
		_safeZones.Remove(area);
	}

	public List<entity_area_purger_safezone> GetSafeZones()
	{
		return _safeZones;
	}

	public void Purge(Action onComplete = null)
	{
		StartCoroutine(OutsidePurger.Purge(new PurgeSettings
		{
			outside = true
		}, onComplete));
	}
}
