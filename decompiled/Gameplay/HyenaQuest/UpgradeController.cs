using System.Collections.Generic;
using FailCake;
using Unity.Netcode;
using UnityEngine;

namespace HyenaQuest;

[DisallowMultipleComponent]
[DefaultExecutionOrder(-70)]
[RequireComponent(typeof(NetworkObject))]
public class UpgradeController : NetController<UpgradeController>
{
	private readonly Dictionary<string, entity_ship_upgrade> _upgrades = new Dictionary<string, entity_ship_upgrade>();

	private util_timer _upgradingTimer;

	private readonly Dictionary<string, int> _boughtUpgrades = new Dictionary<string, int>();

	public override void OnNetworkDespawn()
	{
		base.OnNetworkDespawn();
		if (base.IsServer)
		{
			_upgradingTimer?.Stop();
		}
	}

	[Server]
	public void RegisterUpgrade(entity_ship_upgrade upgrade)
	{
		if (!upgrade)
		{
			throw new UnityException("Invalid upgrade");
		}
		string iD = upgrade.GetID();
		if (string.IsNullOrEmpty(iD))
		{
			throw new UnityException("Upgrade name is empty");
		}
		if (!_upgrades.TryAdd(iD, upgrade))
		{
			throw new UnityException("Upgrade already registered");
		}
		if (!NetController<PhoneController>.Instance)
		{
			throw new UnityException("Missing PhoneController");
		}
		NetController<PhoneController>.Instance.Register(upgrade.UpgradePhoneNumber, delegate
		{
			if (upgrade.IsDisabled())
			{
				return new List<string> { "ingame.phone.upgrade.already-owned" };
			}
			if (!NetController<IngameController>.Instance)
			{
				throw new UnityException("Missing IngameController");
			}
			if (NetController<IngameController>.Instance.Status() != 0)
			{
				return new List<string> { "ingame.phone.upgrade.disabled" };
			}
			if (!NetController<CurrencyController>.Instance)
			{
				throw new UnityException("Missing CurrencyController");
			}
			if (!NetController<CurrencyController>.Instance.Pay(upgrade.UpgradeCost))
			{
				return new List<string> { "ingame.phone.upgrade.no-money" };
			}
			ActivateUpgrade(upgrade, isLoad: false);
			_upgradingTimer?.Stop();
			_upgradingTimer = util_timer.Simple(2f, delegate
			{
				NetController<SoundController>.Instance?.Play3DSound($"Ingame/Upgrade/upgrade_{Random.Range(0, 3)}.ogg", NetController<IngameController>.Instance.shipPosition.position, new AudioData
				{
					mixer = SoundMixer.MUSIC,
					volume = 1f,
					distance = 8f
				}, broadcast: true);
				util_timer.Simple(1.26f, delegate
				{
					NetController<LightController>.Instance?.ExecuteAllLightCommand(LightCommand.FLICKER);
					NetController<ShakeController>.Instance?.ShakeRPC(ShakeMode.SHAKE_ALL, 0.4f, 0.05f);
				});
			});
			return new List<string> { "ingame.phone.upgrade.bought" };
		});
	}

	[Server]
	public void Load(Dictionary<string, int> saveData)
	{
		if (!base.IsServer)
		{
			throw new UnityException("Server only");
		}
		if (saveData == null || saveData.Count <= 0)
		{
			return;
		}
		foreach (KeyValuePair<string, entity_ship_upgrade> upgrade in GetUpgrades())
		{
			string iD = upgrade.Value.GetID();
			if (!string.IsNullOrEmpty(iD) && (bool)upgrade.Value && saveData.TryGetValue(iD, out var value) && value > 0)
			{
				upgrade.Value.ResetUpgrade();
				ActivateUpgrade(upgrade.Value, isLoad: true, value);
			}
		}
	}

	[Server]
	public void UnregisterUpgrade(entity_ship_upgrade upgrade)
	{
		if (!upgrade)
		{
			throw new UnityException("Invalid upgrade");
		}
		string iD = upgrade.GetID();
		if (string.IsNullOrEmpty(iD))
		{
			throw new UnityException("Upgrade name is empty");
		}
		if (!_upgrades.ContainsKey(iD))
		{
			throw new UnityException("Upgrade not registered");
		}
		_upgrades.Remove(upgrade.name);
		if ((bool)NetController<PhoneController>.Instance)
		{
			NetController<PhoneController>.Instance.Unregister(upgrade.UpgradePhoneNumber);
		}
	}

	public void ActivateUpgrade(entity_ship_upgrade upgrade, bool isLoad, int times = 1)
	{
		if (!upgrade)
		{
			throw new UnityException("Invalid upgrade");
		}
		string iD = upgrade.GetID();
		for (int i = 0; i < times; i++)
		{
			upgrade.OnUpgradeBought(isLoad);
			if (!_boughtUpgrades.TryAdd(iD, 1))
			{
				_boughtUpgrades[iD]++;
			}
			if (!upgrade.CanBuyAgain())
			{
				upgrade.DestroyUpgrade();
			}
		}
	}

	public Dictionary<string, int> GetBoughtUpgrades()
	{
		return _boughtUpgrades;
	}

	public Dictionary<string, entity_ship_upgrade> GetUpgrades()
	{
		return _upgrades;
	}

	public entity_ship_upgrade GetUpgrade(string id)
	{
		return _upgrades.GetValueOrDefault(id);
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
		return "UpgradeController";
	}
}
