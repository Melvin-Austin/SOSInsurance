using System;
using Unity.Netcode;
using UnityEngine;

namespace HyenaQuest;

[DisallowMultipleComponent]
[RequireComponent(typeof(NetworkObject))]
[DefaultExecutionOrder(-70)]
public class CurrencyController : NetController<CurrencyController>
{
	public GameEvent<int, bool> OnCurrencyChanged = new GameEvent<int, bool>();

	public GameEvent<int, bool, bool> OnDebtChanged = new GameEvent<int, bool, bool>();

	private bool _wasWarnedDebtPaid;

	private readonly NetVar<int> _currency = new NetVar<int>(IngameController.STARTING_CURRENCY);

	private readonly NetVar<int> _debt = new NetVar<int>(0);

	private readonly NetVar<int> _initialDebt = new NetVar<int>(0);

	protected override void OnNetworkPostSpawn()
	{
		base.OnNetworkPostSpawn();
		if (base.IsClient)
		{
			_debt.RegisterOnValueChanged(delegate(int _, int newValue)
			{
				OnDebtChanged.Invoke(newValue, param2: false, newValue <= 0);
			});
			_currency.RegisterOnValueChanged(delegate(int _, int newValue)
			{
				OnCurrencyChanged?.Invoke(newValue, param2: false);
			});
		}
	}

	public override void OnNetworkPreDespawn()
	{
		base.OnNetworkPreDespawn();
		if (base.IsClient)
		{
			_debt.OnValueChanged = null;
			_currency.OnValueChanged = null;
		}
	}

	public override void OnNetworkSpawn()
	{
		base.OnNetworkSpawn();
		if (base.IsServer)
		{
			CoreController.WaitFor(delegate(IngameController ingameCtrl)
			{
				ingameCtrl.OnStatusUpdated += new Action<INGAME_STATUS, bool>(OnIngameStatusUpdated);
			});
		}
	}

	public override void OnNetworkDespawn()
	{
		base.OnNetworkDespawn();
		if (base.IsServer && (bool)NetController<IngameController>.Instance)
		{
			NetController<IngameController>.Instance.OnStatusUpdated -= new Action<INGAME_STATUS, bool>(OnIngameStatusUpdated);
		}
	}

	public bool CanPay(int amount)
	{
		return _currency.Value - amount >= 0;
	}

	[Server]
	public bool Pay(int amount)
	{
		if (!base.IsServer)
		{
			throw new UnityException("Server only");
		}
		int num = _currency.Value - amount;
		if (num < 0)
		{
			return false;
		}
		SetCurrency(num);
		return true;
	}

	[Server]
	public void AddCurrency(int amount)
	{
		if (!base.IsServer)
		{
			throw new UnityException("Server only");
		}
		SetCurrency(_currency.Value + amount);
	}

	[Server]
	public void SetCurrency(int amount)
	{
		if (!base.IsServer)
		{
			throw new UnityException("Server only");
		}
		_currency.Value = Math.Clamp(amount, 0, 99999);
		OnCurrencyChanged?.Invoke(_currency.Value, param2: true);
	}

	public int GetCurrency()
	{
		return _currency.Value;
	}

	[Server]
	public int PayDebt(int amount, TaskBonus bonus)
	{
		if (!base.IsServer)
		{
			throw new UnityException("Server only");
		}
		AddCurrency(GetBonusMultiplier(amount, bonus));
		int penaltyMultiplier = GetPenaltyMultiplier(amount, bonus);
		_debt.Value = Math.Clamp(_debt.Value - penaltyMultiplier, 0, 99999);
		if (_debt.Value <= 0 && !_wasWarnedDebtPaid)
		{
			_wasWarnedDebtPaid = true;
			NetController<NotificationController>.Instance?.BroadcastAllRPC(new NotificationData
			{
				id = "debt-paid",
				text = "ingame.ui.notification.debt-paid",
				duration = -1f,
				soundEffect = "Ingame/Ship/debt_paid.ogg"
			});
		}
		OnDebtChanged?.Invoke(_debt.Value, param2: true, param3: false);
		return penaltyMultiplier;
	}

	private int GetPenaltyMultiplier(int amount, TaskBonus bonus)
	{
		return bonus switch
		{
			TaskBonus.HALF => (int)((float)amount * 0.85f), 
			TaskBonus.FULL => amount, 
			_ => (int)((float)amount * 0.75f), 
		};
	}

	private int GetBonusMultiplier(int amount, TaskBonus bonus)
	{
		return bonus switch
		{
			TaskBonus.HALF => (int)((float)amount * 0.65f), 
			TaskBonus.FULL => (int)((float)amount * 0.75f), 
			_ => (int)((float)amount * 0.35f), 
		};
	}

	[Server]
	public void SetDebt(int amount)
	{
		if (!base.IsServer)
		{
			throw new UnityException("Server only");
		}
		_debt.Value = Math.Clamp(amount, 0, 99999);
		OnDebtChanged?.Invoke(_debt.Value, param2: true, param3: true);
	}

	public void SetInitialDebt(int amount)
	{
		if (!base.IsServer)
		{
			throw new UnityException("Server only");
		}
		_initialDebt.Value = Math.Clamp(amount, 0, 99999);
		SetDebt(amount);
	}

	public int GetInitialDebt()
	{
		return _initialDebt.Value;
	}

	[Server]
	public int GetDebt()
	{
		return _debt.Value;
	}

	public bool PaidDebt()
	{
		return _debt.Value <= 0;
	}

	private void OnIngameStatusUpdated(INGAME_STATUS status, bool server)
	{
		if (server)
		{
			NetController<NotificationController>.Instance?.BroadcastRemoveAllRPC("debt-paid");
			_wasWarnedDebtPaid = false;
		}
	}

	protected override void __initializeVariables()
	{
		if (_currency == null)
		{
			throw new Exception("CurrencyController._currency cannot be null. All NetworkVariableBase instances must be initialized.");
		}
		_currency.Initialize(this);
		__nameNetworkVariable(_currency, "_currency");
		NetworkVariableFields.Add(_currency);
		if (_debt == null)
		{
			throw new Exception("CurrencyController._debt cannot be null. All NetworkVariableBase instances must be initialized.");
		}
		_debt.Initialize(this);
		__nameNetworkVariable(_debt, "_debt");
		NetworkVariableFields.Add(_debt);
		if (_initialDebt == null)
		{
			throw new Exception("CurrencyController._initialDebt cannot be null. All NetworkVariableBase instances must be initialized.");
		}
		_initialDebt.Initialize(this);
		__nameNetworkVariable(_initialDebt, "_initialDebt");
		NetworkVariableFields.Add(_initialDebt);
		base.__initializeVariables();
	}

	protected override void __initializeRpcs()
	{
		base.__initializeRpcs();
	}

	protected internal override string __getTypeName()
	{
		return "CurrencyController";
	}
}
