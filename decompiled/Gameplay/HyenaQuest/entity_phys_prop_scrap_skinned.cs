using System;
using System.Collections.Generic;
using UnityEngine;

namespace HyenaQuest;

public class entity_phys_prop_scrap_skinned : entity_phys_prop_scrap
{
	public List<Material> skins = new List<Material>();

	private readonly NetVar<byte> _skin = new NetVar<byte>(0);

	public override void OnNetworkSpawn()
	{
		base.OnNetworkSpawn();
		if (base.IsServer)
		{
			_skin.SetSpawnValue((byte)UnityEngine.Random.Range(0, skins.Count));
		}
	}

	protected override void OnNetworkPostSpawn()
	{
		base.OnNetworkPostSpawn();
		if (!base.IsClient)
		{
			return;
		}
		_skin.RegisterOnValueChanged(delegate(byte _, byte newValue)
		{
			Renderer[] renderers = _renderers;
			if (renderers != null && renderers.Length > 0)
			{
				_renderers[0].material = skins[newValue];
			}
		});
	}

	public override void OnNetworkPreDespawn()
	{
		base.OnNetworkPreDespawn();
		if (base.IsClient)
		{
			_skin.OnValueChanged = null;
		}
	}

	protected override void __initializeVariables()
	{
		if (_skin == null)
		{
			throw new Exception("entity_phys_prop_scrap_skinned._skin cannot be null. All NetworkVariableBase instances must be initialized.");
		}
		_skin.Initialize(this);
		__nameNetworkVariable(_skin, "_skin");
		NetworkVariableFields.Add(_skin);
		base.__initializeVariables();
	}

	protected override void __initializeRpcs()
	{
		base.__initializeRpcs();
	}

	protected internal override string __getTypeName()
	{
		return "entity_phys_prop_scrap_skinned";
	}
}
