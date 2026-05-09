using System;
using TMPro;
using UnityEngine;

namespace HyenaQuest;

public class entity_prop_delivery_eye : entity_prop_delivery_cursed
{
	public TextMeshPro directionText;

	private readonly NetVar<PlayerAction> _direction = new NetVar<PlayerAction>(PlayerAction.NONE);

	public override void OnNetworkSpawn()
	{
		base.OnNetworkSpawn();
		if (base.IsServer)
		{
			_direction.SetSpawnValue((PlayerAction)UnityEngine.Random.Range(1, 5));
		}
	}

	public void LateUpdate()
	{
		if (base.IsClient && (bool)PlayerController.LOCAL && (bool)directionText)
		{
			directionText.transform.LookAt(PlayerController.LOCAL.view);
		}
	}

	protected override void OnNetworkPostSpawn()
	{
		base.OnNetworkPostSpawn();
		if (!base.IsClient)
		{
			return;
		}
		_direction.RegisterOnValueChanged(delegate(PlayerAction _, PlayerAction newValue)
		{
			if ((bool)directionText)
			{
				switch (newValue)
				{
				case PlayerAction.FORWARD:
					directionText.text = MonoController<LocalizationController>.Instance.GetKeybindingText("Move", "up");
					break;
				case PlayerAction.BACKWARD:
					directionText.text = MonoController<LocalizationController>.Instance.GetKeybindingText("Move", "down");
					break;
				case PlayerAction.LEFT:
				case PlayerAction.RIGHT:
					directionText.text = MonoController<LocalizationController>.Instance.GetKeybindingText("Move", newValue.ToString());
					break;
				}
			}
		});
	}

	public override void OnNetworkPreDespawn()
	{
		base.OnNetworkPreDespawn();
		if (base.IsClient)
		{
			_direction.OnValueChanged = null;
		}
	}

	protected override void Init()
	{
		base.Init();
		if (!directionText)
		{
			throw new UnityException("Missing TextMeshPro directionText");
		}
	}

	protected override bool CanTakeDamage()
	{
		return false;
	}

	protected override object[] CurseParams()
	{
		return new object[1] { _direction.Value };
	}

	protected override void __initializeVariables()
	{
		if (_direction == null)
		{
			throw new Exception("entity_prop_delivery_eye._direction cannot be null. All NetworkVariableBase instances must be initialized.");
		}
		_direction.Initialize(this);
		__nameNetworkVariable(_direction, "_direction");
		NetworkVariableFields.Add(_direction);
		base.__initializeVariables();
	}

	protected override void __initializeRpcs()
	{
		base.__initializeRpcs();
	}

	protected internal override string __getTypeName()
	{
		return "entity_prop_delivery_eye";
	}
}
