using System;
using System.Linq;
using FailCake;
using TMPro;
using Unity.Netcode;
using UnityEngine;

namespace HyenaQuest;

[DisallowMultipleComponent]
[RequireComponent(typeof(NetworkObject))]
public class entity_ship_upgrade : NetworkBehaviour
{
	[Range(1f, 2000f)]
	public int UpgradeCost;

	public string UpgradePhoneNumber;

	public GameObject canvas;

	public TextMeshPro PhoneNumberText;

	public TextMeshPro UpgradeCostText;

	public TextMeshPro CallNameText;

	private util_timer _blinkTimer;

	private bool _destroyed;

	private readonly NetVar<bool> _disabled = new NetVar<bool>(value: false);

	public void Awake()
	{
		if (string.IsNullOrEmpty(UpgradePhoneNumber))
		{
			throw new UnityException("Missing UpgradePhoneNumber");
		}
		if (!canvas)
		{
			throw new UnityException("Missing canvas");
		}
		if (!PhoneNumberText)
		{
			throw new UnityException("Missing PhoneNumberText");
		}
		if (!UpgradeCostText)
		{
			throw new UnityException("Missing UpgradeCostText");
		}
		if (!CallNameText)
		{
			throw new UnityException("Missing CallNameText");
		}
		UpdateTexts();
	}

	public override void OnNetworkSpawn()
	{
		base.OnNetworkSpawn();
		if (base.IsServer)
		{
			CoreController.WaitFor(delegate(UpgradeController upgradeCtrl)
			{
				upgradeCtrl.RegisterUpgrade(this);
			});
			CoreController.WaitFor(delegate(IngameController ingameCtrl)
			{
				ingameCtrl.OnStatusUpdated += new Action<INGAME_STATUS, bool>(OnIngameStatusUpdated);
			});
		}
		_blinkTimer?.Stop();
		_blinkTimer = util_timer.Create(-1, 0.5f, delegate
		{
			if ((bool)CallNameText)
			{
				CallNameText.enabled = !CallNameText.enabled;
			}
		});
	}

	public override void OnNetworkDespawn()
	{
		base.OnNetworkDespawn();
		if (base.IsServer)
		{
			if ((bool)NetController<UpgradeController>.Instance)
			{
				NetController<UpgradeController>.Instance.UnregisterUpgrade(this);
			}
			if ((bool)NetController<IngameController>.Instance)
			{
				NetController<IngameController>.Instance.OnStatusUpdated -= new Action<INGAME_STATUS, bool>(OnIngameStatusUpdated);
			}
		}
		_blinkTimer?.Stop();
		_blinkTimer = null;
	}

	public void DestroyUpgrade()
	{
		if (!base.IsServer)
		{
			throw new UnityException("DestroyUpgrade can only be called on the server");
		}
		_destroyed = true;
		SetDisabled(disabled: true);
	}

	[Server]
	public void SetDisabled(bool disabled)
	{
		if (!base.IsServer)
		{
			throw new UnityException("SetDisabled can only be called on the server");
		}
		if (!_destroyed || disabled)
		{
			_disabled.Value = disabled;
		}
	}

	public bool IsDisabled()
	{
		return _disabled.Value;
	}

	protected override void OnNetworkPostSpawn()
	{
		base.OnNetworkPostSpawn();
		if (base.IsClient)
		{
			_disabled.RegisterOnValueChanged(delegate(bool _, bool newValue)
			{
				UpdateCanvas(newValue);
			});
		}
	}

	public override void OnNetworkPreDespawn()
	{
		base.OnNetworkPreDespawn();
		if (base.IsClient)
		{
			_disabled.OnValueChanged = null;
		}
	}

	public virtual void OnUpgradeBought(bool isLoad)
	{
		throw new NotImplementedException();
	}

	public virtual bool CanBuyAgain()
	{
		throw new NotImplementedException();
	}

	public virtual string GetID()
	{
		throw new NotImplementedException();
	}

	public virtual void ResetUpgrade()
	{
	}

	private void UpdateCanvas(bool disabled)
	{
		if ((bool)canvas)
		{
			canvas.SetActive(!disabled);
			if (disabled)
			{
				NetController<SoundController>.Instance.Play3DSound("Ingame/Entities/Tv/138115__snakebarney__tv-off-short.ogg", canvas.transform.position, new AudioData
				{
					distance = 3f,
					volume = 0.6f
				});
			}
			else
			{
				NetController<SoundController>.Instance.Play3DSound("Ingame/Entities/Tv/693860__fission9__crt-tv-switches-on.ogg", canvas.transform.position, new AudioData
				{
					distance = 3f,
					volume = 0.6f
				});
			}
		}
	}

	private void OnIngameStatusUpdated(INGAME_STATUS status, bool server)
	{
		if (server && !_destroyed)
		{
			SetDisabled(status != INGAME_STATUS.IDLE);
		}
	}

	private void UpdateTexts()
	{
		if ((bool)PhoneNumberText && (bool)UpgradeCostText && !string.IsNullOrEmpty(UpgradePhoneNumber))
		{
			PhoneNumberText.text = string.Join("-", from i in Enumerable.Range(0, UpgradePhoneNumber.Length / 3 + ((UpgradePhoneNumber.Length % 3 != 0) ? 1 : 0))
				select UpgradePhoneNumber.Substring(i * 3, Math.Min(3, UpgradePhoneNumber.Length - i * 3)));
			UpgradeCostText.text = "<rotate=-90>€ </rotate>" + UpgradeCost;
		}
	}

	protected override void __initializeVariables()
	{
		if (_disabled == null)
		{
			throw new Exception("entity_ship_upgrade._disabled cannot be null. All NetworkVariableBase instances must be initialized.");
		}
		_disabled.Initialize(this);
		__nameNetworkVariable(_disabled, "_disabled");
		NetworkVariableFields.Add(_disabled);
		base.__initializeVariables();
	}

	protected override void __initializeRpcs()
	{
		base.__initializeRpcs();
	}

	protected internal override string __getTypeName()
	{
		return "entity_ship_upgrade";
	}
}
