using UnityEngine;

namespace HyenaQuest;

public class entity_item_warn_light : entity_item_pickable
{
	public entity_led led;

	public override string GetID()
	{
		return "entity_item_warn_light";
	}

	public new void Update()
	{
		base.Update();
		if (base.IsClient && (bool)led)
		{
			led.SetActive(HasOwner());
		}
	}

	protected override void Init()
	{
		base.Init();
		if (!led)
		{
			throw new UnityException("Missing LightSource component!");
		}
		led.SetActive(enable: false);
	}

	[Server]
	protected override void SetInventoryOwner(Player owner, bool revokeOwner)
	{
		if (!base.IsServer)
		{
			throw new UnityException("Server only");
		}
		if (HasOwner())
		{
			Player player = MonoController<PlayerController>.Instance.GetPlayer(_owner.Value);
			if (player != null && (bool)player.player)
			{
				player.player.GetPhysgun().SetProtected(protect: false);
			}
		}
		if (owner != null && (bool)owner.player)
		{
			owner.player.GetPhysgun().SetProtected(protect: true);
		}
		base.SetInventoryOwner(owner, revokeOwner);
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
		return "entity_item_warn_light";
	}
}
