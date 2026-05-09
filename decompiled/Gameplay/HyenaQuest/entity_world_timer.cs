using System;
using UnityEngine;

namespace HyenaQuest;

public class entity_world_timer : MonoBehaviour
{
	private entity_split_flap_display _display;

	private uint _oldTime;

	public void Awake()
	{
		_display = GetComponent<entity_split_flap_display>();
		if (!_display)
		{
			throw new UnityException("Missing entity_split_flap_display");
		}
		CoreController.WaitFor(delegate(IngameController ingameCtrl)
		{
			ingameCtrl.OnWorldTimerUpdate += new Action<uint, bool>(OnWorldTimerUpdate);
			OnWorldTimerUpdate(0u, server: false);
		});
	}

	public void OnDestroy()
	{
		if ((bool)NetController<IngameController>.Instance)
		{
			NetController<IngameController>.Instance.OnWorldTimerUpdate -= new Action<uint, bool>(OnWorldTimerUpdate);
		}
	}

	private void OnWorldTimerUpdate(uint time, bool server)
	{
		if (!server)
		{
			if (time == 0)
			{
				_display.ResetText(SplitFlapMode.SHUFFLE);
			}
			else
			{
				bool flag = _oldTime == 0 && time >= _oldTime;
				_display.SetText(flag ? SplitFlapMode.SHUFFLE : SplitFlapMode.NORMAL, TimeUtils.SecondsToTime(time), flag ? 0.001f : 0.05f);
			}
			_oldTime = time;
		}
	}
}
