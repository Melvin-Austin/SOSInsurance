using System.Collections.Generic;
using FailCake;
using UnityEngine;

namespace HyenaQuest;

public class entity_warning_led : MonoBehaviour
{
	public List<entity_led> leds = new List<entity_led>();

	public float delay;

	public float interval = 1f;

	public float beepPitch = 1f;

	public bool ledActive;

	public GameEvent<bool> OnStatusChange = new GameEvent<bool>();

	private util_timer _tick;

	private util_timer _delay;

	private bool _active;

	public void Awake()
	{
		SetActive(ledActive);
	}

	public void SetActive(bool active)
	{
		ledActive = active;
		_delay?.Stop();
		_tick?.Stop();
		if (active)
		{
			_delay = util_timer.Simple(delay, delegate
			{
				_tick = util_timer.Create(-1, interval, delegate
				{
					_active = !_active;
					OnStatusChange.Invoke(_active);
					if (_active && beepPitch > 0f && (bool)NetController<SoundController>.Instance)
					{
						NetController<SoundController>.Instance.Play3DSound("General/Entities/LED/688248__monyker__noisygreencreativeled.ogg", base.transform, new AudioData
						{
							pitch = beepPitch,
							distance = 3f,
							volume = 0.05f
						});
					}
					SetLEDs(_active);
				});
			});
		}
		else
		{
			SetLEDs(active: false);
			OnStatusChange.Invoke(param1: false);
		}
	}

	public void OnDestroy()
	{
		_tick?.Stop();
		_delay?.Stop();
	}

	private void SetLEDs(bool active)
	{
		foreach (entity_led led in leds)
		{
			if ((bool)led)
			{
				led.SetActive(active);
			}
		}
	}
}
