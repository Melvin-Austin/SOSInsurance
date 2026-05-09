using System;
using TMPro;
using Unity.Netcode;
using UnityEngine;

namespace HyenaQuest;

[RequireComponent(typeof(NetworkObject))]
public class entity_delivery_spot : NetworkBehaviour
{
	public entity_trigger trigger;

	private TextMeshPro _deliveryText;

	private readonly NetVar<int> _deliveryAddress = new NetVar<int>(-1);

	public void Awake()
	{
		if (!trigger)
		{
			throw new UnityException("entity_delivery_spot requires Collider component");
		}
		_deliveryText = GetComponentInChildren<TextMeshPro>(includeInactive: true);
		if (!(UnityEngine.Object)(object)_deliveryText)
		{
			throw new UnityException("entity_delivery_spot requires TextMeshPro component");
		}
		_deliveryText.text = "";
	}

	protected override void OnNetworkPostSpawn()
	{
		base.OnNetworkPostSpawn();
		if (base.IsClient)
		{
			_deliveryAddress.RegisterOnValueChanged(delegate(int _, int newValue)
			{
				_deliveryText.text = ((newValue == -1) ? "" : newValue.ToString());
			});
		}
	}

	public override void OnNetworkPreDespawn()
	{
		base.OnNetworkPreDespawn();
		if (base.IsClient)
		{
			_deliveryAddress.OnValueChanged = null;
		}
	}

	public override void OnNetworkSpawn()
	{
		base.OnNetworkSpawn();
		SDK.OnDeliverySpotRegister?.Invoke(this);
		if (base.IsServer)
		{
			trigger.OnEnter += new Action<Collider>(OnEnter);
			trigger.OnExit += new Action<Collider>(OnExit);
		}
	}

	public override void OnNetworkDespawn()
	{
		base.OnNetworkDespawn();
		SDK.OnDeliverySpotUnregister?.Invoke(this);
		if (base.IsServer && (bool)trigger)
		{
			trigger.OnEnter -= new Action<Collider>(OnEnter);
			trigger.OnExit -= new Action<Collider>(OnExit);
		}
	}

	[Server]
	public void SetDeliveryAddress(int address)
	{
		if (base.IsSpawned && !base.IsServer)
		{
			throw new UnityException("SetDeliveryAddress can only be called on the server");
		}
		_deliveryAddress.SetSpawnValue(address);
	}

	public int GetDeliveryAddress()
	{
		return _deliveryAddress.Value;
	}

	private void OnEnter(Collider other)
	{
		if (base.IsServer && (bool)other)
		{
			other.SendMessageUpwards("OnDeliverySpotEnter", _deliveryAddress.Value, SendMessageOptions.DontRequireReceiver);
		}
	}

	private void OnExit(Collider other)
	{
		if (base.IsServer && (bool)other)
		{
			other.SendMessageUpwards("OnDeliverySpotExit", _deliveryAddress.Value, SendMessageOptions.DontRequireReceiver);
		}
	}

	protected override void __initializeVariables()
	{
		if (_deliveryAddress == null)
		{
			throw new Exception("entity_delivery_spot._deliveryAddress cannot be null. All NetworkVariableBase instances must be initialized.");
		}
		_deliveryAddress.Initialize(this);
		__nameNetworkVariable(_deliveryAddress, "_deliveryAddress");
		NetworkVariableFields.Add(_deliveryAddress);
		base.__initializeVariables();
	}

	protected override void __initializeRpcs()
	{
		base.__initializeRpcs();
	}

	protected internal override string __getTypeName()
	{
		return "entity_delivery_spot";
	}
}
