using System;
using UnityEngine;

namespace HyenaQuest;

public class entity_currency_display : MonoBehaviour
{
	private entity_split_flap_display _display;

	private int _oldCurrency;

	public void Awake()
	{
		_display = GetComponent<entity_split_flap_display>();
		if (!_display)
		{
			throw new UnityException("entity_timer_cycle requires entity_split_flap_display component!");
		}
		CoreController.WaitFor(delegate(CurrencyController currCtrl)
		{
			currCtrl.OnCurrencyChanged += new Action<int, bool>(OnCurrencyChanged);
			OnCurrencyChanged(currCtrl.GetCurrency(), server: false);
		});
	}

	public void OnDestroy()
	{
		if ((bool)NetController<CurrencyController>.Instance)
		{
			NetController<CurrencyController>.Instance.OnCurrencyChanged -= new Action<int, bool>(OnCurrencyChanged);
		}
	}

	private void OnCurrencyChanged(int currency, bool server)
	{
		if (!server)
		{
			bool flag = _oldCurrency == 0 && currency >= _oldCurrency;
			_display.SetText(flag ? SplitFlapMode.SHUFFLE : SplitFlapMode.NORMAL, Mathf.Abs(currency).ToString().PadLeft(6, ' '), flag ? 0.001f : 0.05f);
			_oldCurrency = currency;
		}
	}
}
