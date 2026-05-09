using System;
using UnityEngine;

namespace HyenaQuest;

public class entity_kill : entity_trigger
{
	public DamageType damageType = DamageType.GENERIC;

	public new void Awake()
	{
		base.Awake();
		OnStay += new Action<Collider>(Kill);
	}

	public void OnDestroy()
	{
		OnStay -= new Action<Collider>(Kill);
	}

	private void Kill(Collider col)
	{
		if ((bool)this && base.gameObject.activeInHierarchy && (bool)col && (bool)col.gameObject && !col.transform.IsChildOf(base.transform) && !(col.gameObject == base.gameObject))
		{
			SDK.OnKillRequest?.Invoke(damageType, col);
		}
	}
}
