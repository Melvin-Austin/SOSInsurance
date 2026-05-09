using System;
using UnityEngine;

namespace HyenaQuest;

public class entity_damage : entity_trigger
{
	[Range(0f, 100f)]
	public byte damage = 1;

	[Range(0f, 10f)]
	public float damageCooldown = 0.4f;

	public DamageType damageType = DamageType.GENERIC;

	public bool damageOnMove;

	public new void Awake()
	{
		base.Awake();
		OnStay += new Action<Collider>(Damage);
	}

	public void OnDestroy()
	{
		OnStay -= new Action<Collider>(Damage);
	}

	private void Damage(Collider col)
	{
		if ((bool)this && base.gameObject.activeInHierarchy && (bool)col && (bool)col.gameObject && !col.transform.IsChildOf(base.transform) && !(col.gameObject == base.gameObject))
		{
			SDK.OnDamageRequest?.Invoke(damageType, damage, damageCooldown, damageOnMove, col);
		}
	}
}
