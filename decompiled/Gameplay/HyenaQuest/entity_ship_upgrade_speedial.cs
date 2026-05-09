using System;
using System.Collections.Generic;
using UnityEngine;

namespace HyenaQuest;

public class entity_ship_upgrade_speedial : entity_ship_upgrade
{
	public entity_button speedDial;

	private bool _hasSpeedDial;

	public new void Awake()
	{
		base.Awake();
		if (!speedDial)
		{
			throw new UnityException("Missing speed dial gameobject");
		}
	}

	public override void OnNetworkSpawn()
	{
		base.OnNetworkSpawn();
		if (base.IsServer)
		{
			CoreController.WaitFor(delegate(IngameController ingameCtrl)
			{
				ingameCtrl.OnStatusUpdated += new Action<INGAME_STATUS, bool>(OnStatusUpdated);
			});
			CoreController.WaitFor(delegate(ContractController contractCtrl)
			{
				contractCtrl.OnTasksUpdated += new Action<bool>(OnTasksUpdated);
			});
			CoreController.WaitFor(delegate(PhoneController phoneCtrl)
			{
				phoneCtrl.OnStatusUpdated += new Action<PHONE_STATUS, bool>(OnPhoneStatusUpdated);
			});
			CoreController.WaitFor(delegate(ScrapController scrapCtrl)
			{
				scrapCtrl.OnShipScrapUpdate += new Action<int, bool>(OnShipScrapUpdate);
			});
			speedDial.OnUSE += new Action<entity_player>(OnUSE);
			UpdateSpeedDialStatus();
		}
	}

	public override void OnNetworkDespawn()
	{
		base.OnNetworkDespawn();
		if (base.IsServer)
		{
			if ((bool)NetController<IngameController>.Instance)
			{
				NetController<IngameController>.Instance.OnStatusUpdated -= new Action<INGAME_STATUS, bool>(OnStatusUpdated);
			}
			if ((bool)NetController<ContractController>.Instance)
			{
				NetController<ContractController>.Instance.OnTasksUpdated -= new Action<bool>(OnTasksUpdated);
			}
			if ((bool)NetController<PhoneController>.Instance)
			{
				NetController<PhoneController>.Instance.OnStatusUpdated -= new Action<PHONE_STATUS, bool>(OnPhoneStatusUpdated);
			}
			if ((bool)NetController<ScrapController>.Instance)
			{
				NetController<ScrapController>.Instance.OnShipScrapUpdate -= new Action<int, bool>(OnShipScrapUpdate);
			}
			if ((bool)speedDial)
			{
				speedDial.OnUSE -= new Action<entity_player>(OnUSE);
			}
		}
	}

	[Server]
	public override void OnUpgradeBought(bool isLoad)
	{
		if (!base.IsServer)
		{
			throw new UnityException("Server only");
		}
		_hasSpeedDial = true;
		UpdateSpeedDialStatus();
	}

	public override bool CanBuyAgain()
	{
		return false;
	}

	public override string GetID()
	{
		return "ship_upgrade_speedial";
	}

	[Shared]
	private void OnShipScrapUpdate(int amount, bool server)
	{
		if (server)
		{
			UpdateSpeedDialStatus();
		}
	}

	[Server]
	private void OnUSE(entity_player ply)
	{
		if (!base.IsServer)
		{
			throw new UnityException("Server only");
		}
		if (!NetController<ContractController>.Instance)
		{
			throw new UnityException("Missing ContractController");
		}
		if (!NetController<ScrapController>.Instance)
		{
			throw new UnityException("Missing ScrapController");
		}
		if (!NetController<PhoneController>.Instance)
		{
			throw new UnityException("Missing PhoneController");
		}
		int claimedScrap = NetController<ScrapController>.Instance.GetClaimedScrap();
		List<Task> affordableTasks = NetController<ContractController>.Instance.GetAffordableTasks(claimedScrap, sort: false);
		if (affordableTasks.Count == 0)
		{
			NetController<SoundController>.Instance?.Play3DSound("Ingame/Entities/Terminal/142608__autistic-lucario__error.ogg", speedDial.transform, new AudioData
			{
				pitch = UnityEngine.Random.Range(0.85f, 1.2f),
				distance = 6f
			}, broadcast: true);
			return;
		}
		Task task = affordableTasks[UnityEngine.Random.Range(0, affordableTasks.Count)];
		if (!NetController<PhoneController>.Instance.AutoType(task.Address.ToString(), 0.2f, 0.2f, null))
		{
			NetController<SoundController>.Instance?.Play3DSound("Ingame/Entities/Terminal/142608__autistic-lucario__error.ogg", speedDial.transform, new AudioData
			{
				pitch = UnityEngine.Random.Range(0.85f, 1.2f),
				distance = 6f
			}, broadcast: true);
		}
		UpdateSpeedDialStatus();
	}

	[Server]
	private void OnPhoneStatusUpdated(PHONE_STATUS status, bool server)
	{
		if (server)
		{
			UpdateSpeedDialStatus();
		}
	}

	[Server]
	private void OnTasksUpdated(bool server)
	{
		if (server)
		{
			UpdateSpeedDialStatus();
		}
	}

	[Server]
	private void OnStatusUpdated(INGAME_STATUS status, bool server)
	{
		if (server)
		{
			UpdateSpeedDialStatus();
		}
	}

	[Server]
	private void UpdateSpeedDialStatus()
	{
		if (!base.IsServer)
		{
			throw new UnityException("Server only");
		}
		if (!speedDial)
		{
			throw new UnityException("Missing speed dial gameobject");
		}
		if (!NetController<IngameController>.Instance)
		{
			throw new UnityException("Missing IngameController");
		}
		if (!NetController<ScrapController>.Instance)
		{
			throw new UnityException("Missing ScrapController");
		}
		if (!NetController<ContractController>.Instance)
		{
			throw new UnityException("Missing ContractController");
		}
		if (!NetController<PhoneController>.Instance)
		{
			throw new UnityException("Missing PhoneController");
		}
		if (!_hasSpeedDial)
		{
			speedDial.SetLocked(newVal: true);
			return;
		}
		bool flag = NetController<IngameController>.Instance.Status() != INGAME_STATUS.PLAYING;
		int claimedScrap = NetController<ScrapController>.Instance.GetClaimedScrap();
		bool flag2 = NetController<ContractController>.Instance.GetAffordableTasks(claimedScrap).Count <= 0;
		bool flag3 = NetController<PhoneController>.Instance.Status() != PHONE_STATUS.IDLE;
		speedDial.SetLocked(flag || flag2 || flag3);
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
		return "entity_ship_upgrade_speedial";
	}
}
