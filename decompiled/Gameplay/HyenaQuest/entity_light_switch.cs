using System;
using Unity.Netcode;
using UnityEngine;

namespace HyenaQuest;

public class entity_light_switch : NetworkBehaviour
{
	private entity_button_toggle _btn;

	private entity_led _led;

	public void Awake()
	{
		_btn = GetComponent<entity_button_toggle>();
		if (!_btn)
		{
			throw new UnityException("Missing entity_button_toggle");
		}
		_led = GetComponentInChildren<entity_led>(includeInactive: true);
		if (!_led)
		{
			throw new UnityException("Missing entity_led");
		}
	}

	public override void OnNetworkSpawn()
	{
		base.OnNetworkSpawn();
		if ((bool)_btn)
		{
			if (base.IsServer)
			{
				_btn.OnUSE += new Action<byte>(OnUSE);
			}
			_btn.OnAnimate += new Action<bool>(OnAnimate);
		}
	}

	public override void OnNetworkDespawn()
	{
		base.OnNetworkDespawn();
		if ((bool)_btn)
		{
			if (base.IsServer)
			{
				_btn.OnUSE -= new Action<byte>(OnUSE);
			}
			_btn.OnAnimate -= new Action<bool>(OnAnimate);
		}
	}

	private void OnAnimate(bool active)
	{
		_led?.SetActive(active);
	}

	[Server]
	private void OnUSE(byte indx)
	{
		if (!base.IsServer)
		{
			throw new UnityException("Server only");
		}
		NetController<PowerController>.Instance?.SetPoweredArea(PowerGrid.BASE, indx == 0);
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
		return "entity_light_switch";
	}
}
