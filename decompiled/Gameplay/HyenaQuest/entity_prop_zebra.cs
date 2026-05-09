using UnityEngine;

namespace HyenaQuest;

public class entity_prop_zebra : entity_phys
{
	public Transform kissPoint;

	public static entity_prop_zebra AllMightyZebra;

	public override void OnNetworkSpawn()
	{
		base.OnNetworkSpawn();
		AllMightyZebra = this;
	}

	public override void OnNetworkDespawn()
	{
		base.OnNetworkDespawn();
		AllMightyZebra = null;
	}

	protected override void Init()
	{
		base.Init();
		if (!kissPoint)
		{
			throw new UnityException("Missing kiss point");
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
		return "entity_prop_zebra";
	}
}
