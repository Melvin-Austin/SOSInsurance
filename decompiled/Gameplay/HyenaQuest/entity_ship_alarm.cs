using System;
using System.Collections.Generic;
using FailCake;
using UnityEngine;

namespace HyenaQuest;

public class entity_ship_alarm : MonoBehaviour
{
	public List<entity_led> warningLED = new List<entity_led>();

	public GameObject alarmLight;

	private util_timer _alarmTimer;

	private bool _active;

	public void Awake()
	{
		List<entity_led> list = warningLED;
		if (list == null || list.Count <= 0)
		{
			throw new UnityException("Missing warningLED");
		}
		if (!alarmLight)
		{
			throw new UnityException("Missing alarmLight");
		}
		alarmLight.SetActive(value: false);
		CoreController.WaitFor(delegate(IngameController ingameCtrl)
		{
			ingameCtrl.OnWorldTimerWarningUpdate += new Action<bool, bool>(OnWorldTimerWarningUpdate);
		});
	}

	public void OnDestroy()
	{
		_alarmTimer?.Stop();
		if ((bool)NetController<IngameController>.Instance)
		{
			NetController<IngameController>.Instance.OnWorldTimerWarningUpdate -= new Action<bool, bool>(OnWorldTimerWarningUpdate);
		}
	}

	private void OnWorldTimerWarningUpdate(bool detected, bool server)
	{
		if (server)
		{
			return;
		}
		_alarmTimer?.Stop();
		SetAlarm(detected);
		if (!detected)
		{
			return;
		}
		_alarmTimer = util_timer.Create(-1, 2f, delegate
		{
			IngameController instance = NetController<IngameController>.Instance;
			if ((object)instance == null || instance.IsTimerWarning())
			{
				SetAlarm(!_active);
			}
		});
	}

	private void SetAlarm(bool active)
	{
		if (!alarmLight || _active == active)
		{
			return;
		}
		_active = active;
		foreach (entity_led item in warningLED)
		{
			if ((bool)item)
			{
				item.SetActive(active);
			}
		}
		alarmLight.SetActive(active);
		if (active)
		{
			NetController<SoundController>.Instance?.Play3DSound("Ingame/Ship/detection_alarm.ogg", base.transform.position, new AudioData
			{
				distance = 10f,
				volume = 0.35f
			});
		}
	}
}
