using UnityEngine;

namespace HyenaQuest;

public class entity_prop_delivery_cage : entity_prop_delivery
{
	private float _cooldown;

	public new void Update()
	{
		base.Update();
		if (!base.IsOwner)
		{
			return;
		}
		entity_player grabbingOwner = GetGrabbingOwner();
		if (IsBeingGrabbed() && !(Time.time < _cooldown))
		{
			_cooldown = Time.time + (float)Random.Range(1, 3);
			_rigidbody.AddExplosionForce((grabbingOwner ? Random.Range(2, 5) : Random.Range(1, 2)) * 2, base.transform.position + Random.insideUnitSphere, 2f, 0f, ForceMode.VelocityChange);
			if ((bool)grabbingOwner)
			{
				grabbingOwner.Shove(Random.insideUnitSphere, 5f);
			}
		}
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
		return "entity_prop_delivery_cage";
	}
}
