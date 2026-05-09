using System;
using System.Collections.Generic;
using UnityEngine;

namespace HyenaQuest;

public class entity_phys_prop_scrap_layered : entity_phys_prop_scrap
{
	public List<GameObject> layers;

	private readonly NetVar<byte> _layer = new NetVar<byte>(byte.MaxValue);

	public override void OnNetworkSpawn()
	{
		base.OnNetworkSpawn();
		if (base.IsServer)
		{
			_layer.SetSpawnValue((byte)UnityEngine.Random.Range(0, layers.Count));
		}
	}

	protected override void OnNetworkPostSpawn()
	{
		base.OnNetworkPostSpawn();
		if (!base.IsClient)
		{
			return;
		}
		_layer.RegisterOnValueChanged(delegate(byte oldValue, byte newValue)
		{
			if (oldValue != newValue)
			{
				ProcessLayer(newValue);
			}
		});
	}

	public override void OnNetworkPreDespawn()
	{
		base.OnNetworkPreDespawn();
		if (base.IsClient)
		{
			_layer.OnValueChanged = null;
		}
	}

	private void ProcessLayer(byte layer)
	{
		if (layers == null || layers.Count == 0)
		{
			return;
		}
		for (int i = 0; i < layers.Count; i++)
		{
			if (i == layer)
			{
				layers[i].SetActive(value: true);
			}
			else
			{
				UnityEngine.Object.Destroy(layers[i]);
			}
		}
	}

	protected override void __initializeVariables()
	{
		if (_layer == null)
		{
			throw new Exception("entity_phys_prop_scrap_layered._layer cannot be null. All NetworkVariableBase instances must be initialized.");
		}
		_layer.Initialize(this);
		__nameNetworkVariable(_layer, "_layer");
		NetworkVariableFields.Add(_layer);
		base.__initializeVariables();
	}

	protected override void __initializeRpcs()
	{
		base.__initializeRpcs();
	}

	protected internal override string __getTypeName()
	{
		return "entity_phys_prop_scrap_layered";
	}
}
