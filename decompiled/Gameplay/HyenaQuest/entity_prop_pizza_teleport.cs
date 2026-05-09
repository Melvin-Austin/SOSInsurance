using System;
using System.Collections;
using UnityEngine;

namespace HyenaQuest;

public class entity_prop_pizza_teleport : entity_phys_painter
{
	private Coroutine _teleportRoutine;

	public override void OnNetworkSpawn()
	{
		base.OnNetworkSpawn();
		if (base.IsServer)
		{
			_teleportRoutine = StartCoroutine(Teleport());
		}
	}

	public override void OnNetworkDespawn()
	{
		base.OnNetworkDespawn();
		if (base.IsServer && _teleportRoutine != null)
		{
			StopCoroutine(_teleportRoutine);
		}
	}

	protected virtual IEnumerator Teleport()
	{
		throw new NotImplementedException();
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
		return "entity_prop_pizza_teleport";
	}
}
