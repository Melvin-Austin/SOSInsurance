using System;
using TMPro;
using UnityEngine;

namespace HyenaQuest;

[DisallowMultipleComponent]
public class entity_prop_delivery : entity_phys_breakable
{
	public GameObject notificationPreview;

	private TextMeshPro _addressText;

	private int _taskID = -1;

	private readonly NetVar<bool> _onDeliverySpot = new NetVar<bool>(value: false);

	private readonly NetVar<int> _deliveryAddress = new NetVar<int>(-1);

	protected override void OnNetworkPostSpawn()
	{
		base.OnNetworkPostSpawn();
		if (base.IsClient)
		{
			_deliveryAddress.RegisterOnValueChanged(delegate(int _, int newValue)
			{
				UpdateModel(newValue);
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

	public void OnDeliverySpotEnter(int address)
	{
		if (address == _deliveryAddress.Value)
		{
			SetOnDeliverySpot(onSpot: true);
		}
	}

	public void OnDeliverySpotExit(int address)
	{
		if (address == _deliveryAddress.Value)
		{
			SetOnDeliverySpot(onSpot: false);
		}
	}

	public int GetAddress()
	{
		return _deliveryAddress.Value;
	}

	[Server]
	public void SetTask(int taskID)
	{
		if (!base.IsServer)
		{
			throw new UnityException("SetTask can only be called on the server.");
		}
		_taskID = taskID;
	}

	[Server]
	public void SetTask(int taskID, int address)
	{
		if (!base.IsServer)
		{
			throw new UnityException("SetTask can only be called on the server.");
		}
		_taskID = taskID;
		_deliveryAddress.Value = address;
	}

	[Server]
	public int GetTaskID()
	{
		return _taskID;
	}

	[Server]
	public void SetOnDeliverySpot(bool onSpot)
	{
		if (!base.IsServer)
		{
			throw new UnityException("SetOnDeliverySpot can only be called on the server.");
		}
		_onDeliverySpot.SetSpawnValue(onSpot);
	}

	public new void Update()
	{
		base.Update();
		if (!base.IsServer || _taskID == -1 || !IsOnDeliverySpot())
		{
			return;
		}
		if ((bool)NetController<TutorialController>.Instance)
		{
			NetController<NotificationController>.Instance?.BroadcastAll3DRPC(new NotificationData3D
			{
				position = base.transform.position,
				message = "ingame.ui.notification.delivered",
				fadeSpeed = 1f,
				scale = 0.45f,
				startColor = Color.white,
				endColor = Color.white
			});
			NetController<TutorialController>.Instance.OnDeliveryCompleted();
		}
		else
		{
			if (!NetController<ContractController>.Instance)
			{
				throw new UnityException("Missing ContractController");
			}
			if (NetController<ContractController>.Instance.CompleteTask(_taskID, CalculateBonus()))
			{
				NetController<StatsController>.Instance?.RegisterDelivery(GetLastOwnerID(), 1);
				NetController<NotificationController>.Instance?.BroadcastAll3DRPC(new NotificationData3D
				{
					position = base.transform.position,
					message = "ingame.ui.notification.delivered",
					fadeSpeed = 1f,
					scale = 0.45f,
					startColor = Color.white,
					endColor = Color.white
				});
				OnDelivery();
			}
		}
		SetLocked(LOCK_TYPE.LOCKED);
		_taskID = -1;
	}

	public override void Destroy()
	{
		if (_taskID == -1)
		{
			base.Destroy();
			return;
		}
		NetController<ContractController>.Instance?.FailTask(_taskID);
		_taskID = -1;
		base.Destroy();
	}

	public bool IsProtected()
	{
		entity_player grabbingOwner = GetGrabbingOwner();
		if (!grabbingOwner)
		{
			return false;
		}
		entity_player_physgun physgun = grabbingOwner.GetPhysgun();
		if ((bool)physgun)
		{
			return physgun.IsProtected();
		}
		return false;
	}

	protected override bool CanTakeDamage()
	{
		if (!base.CanTakeDamage())
		{
			return false;
		}
		if (IsBeingGrabbed())
		{
			return !IsProtected();
		}
		return true;
	}

	protected override void Init()
	{
		base.Init();
		if (!notificationPreview)
		{
			throw new UnityException("Missing notification preview");
		}
		_addressText = GetComponentInChildren<TextMeshPro>(includeInactive: true);
	}

	private bool IsOnDeliverySpot()
	{
		if (_onDeliverySpot.Value && !IsBeingGrabbed() && _rigidbody.linearVelocity.magnitude <= 0.1f)
		{
			return _lastLetgoTime + 1f < Time.time;
		}
		return false;
	}

	[Server]
	protected virtual void OnDelivery()
	{
	}

	[Server]
	private TaskBonus CalculateBonus()
	{
		float num = (float)(int)health.Value / (float)(int)_maxHealth;
		if (Mathf.Approximately(num, 1f))
		{
			return TaskBonus.FULL;
		}
		if (!(num >= 0.5f))
		{
			return TaskBonus.NONE;
		}
		return TaskBonus.HALF;
	}

	[Client]
	private void UpdateModel(int newValue)
	{
		if ((bool)_addressText)
		{
			_addressText.text = newValue.ToString();
		}
	}

	protected override void __initializeVariables()
	{
		if (_onDeliverySpot == null)
		{
			throw new Exception("entity_prop_delivery._onDeliverySpot cannot be null. All NetworkVariableBase instances must be initialized.");
		}
		_onDeliverySpot.Initialize(this);
		__nameNetworkVariable(_onDeliverySpot, "_onDeliverySpot");
		NetworkVariableFields.Add(_onDeliverySpot);
		if (_deliveryAddress == null)
		{
			throw new Exception("entity_prop_delivery._deliveryAddress cannot be null. All NetworkVariableBase instances must be initialized.");
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
		return "entity_prop_delivery";
	}
}
