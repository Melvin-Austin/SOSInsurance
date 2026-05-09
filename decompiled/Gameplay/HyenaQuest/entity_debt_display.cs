using System;
using UnityEngine;

namespace HyenaQuest;

public class entity_debt_display : MonoBehaviour
{
	private entity_split_flap_display _display;

	public void Awake()
	{
		_display = GetComponent<entity_split_flap_display>();
		if (!_display)
		{
			throw new UnityException("entity_timer_cycle requires entity_split_flap_display component!");
		}
		CoreController.WaitFor(delegate(CurrencyController currCtrl)
		{
			currCtrl.OnDebtChanged += new Action<int, bool, bool>(OnDebtChanged);
			OnDebtChanged(currCtrl.GetDebt(), server: false, set: true);
		});
	}

	public void OnDestroy()
	{
		if ((bool)NetController<CurrencyController>.Instance)
		{
			NetController<CurrencyController>.Instance.OnDebtChanged -= new Action<int, bool, bool>(OnDebtChanged);
		}
	}

	private void OnDebtChanged(int debt, bool server, bool set)
	{
		if (!server)
		{
			_display.SetText(set ? SplitFlapMode.SHUFFLE : SplitFlapMode.NORMAL, Mathf.Abs(debt).ToString().PadLeft(5, ' '), set ? 0.001f : 0.05f);
		}
	}
}
