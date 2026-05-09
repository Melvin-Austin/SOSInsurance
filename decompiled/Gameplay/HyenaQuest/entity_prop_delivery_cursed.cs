using UnityEngine;

namespace HyenaQuest;

public class entity_prop_delivery_cursed : entity_prop_delivery
{
	public CURSE_TYPE curse;

	private float _lastDamageCD;

	public override void OnNetworkDespawn()
	{
		base.OnNetworkDespawn();
		if (base.IsServer)
		{
			entity_player entity_player2 = MonoController<PlayerController>.Instance?.GetPlayerEntityByID(_grabbingOwnerId.Value);
			if ((bool)entity_player2)
			{
				NetController<CurseController>.Instance?.RemoveCurse(entity_player2, curse);
			}
		}
	}

	protected override void OnNetworkPostSpawn()
	{
		base.OnNetworkPostSpawn();
		if (!base.IsServer)
		{
			return;
		}
		_grabbingOwnerId.RegisterOnValueChanged(delegate(byte oldValue, byte newValue)
		{
			entity_player playerEntityByID = MonoController<PlayerController>.Instance.GetPlayerEntityByID(newValue);
			entity_player playerEntityByID2 = MonoController<PlayerController>.Instance.GetPlayerEntityByID(oldValue);
			if ((bool)playerEntityByID)
			{
				NetController<CurseController>.Instance.AddCurse(curse, playerEntityByID, CurseParams());
			}
			if ((bool)playerEntityByID2)
			{
				NetController<CurseController>.Instance.RemoveCurse(playerEntityByID2, curse);
			}
		});
	}

	protected override bool CanTakeDamage()
	{
		return false;
	}

	protected override void OnCollision(Collision collision)
	{
		if (!base.IsOwner || !IsBeingGrabbed())
		{
			return;
		}
		entity_player grabbingOwner = GetGrabbingOwner();
		if (!grabbingOwner)
		{
			return;
		}
		entity_player_inventory inventory = grabbingOwner.GetInventory();
		if ((bool)inventory && !inventory.HasItem("entity_item_warn_light"))
		{
			float magnitude = collision.relativeVelocity.magnitude;
			float impactForce = (collision.rigidbody ? (magnitude * collision.rigidbody.mass) : magnitude);
			if (!(Time.time < _lastDamageCD) && IsBreakDamage(impactForce))
			{
				_lastDamageCD = Time.time + 1f;
				grabbingOwner.TakeHealth((byte)Random.Range(17, 22), DamageType.CURSE);
			}
		}
	}

	protected virtual object[] CurseParams()
	{
		return null;
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
		return "entity_prop_delivery_cursed";
	}
}
