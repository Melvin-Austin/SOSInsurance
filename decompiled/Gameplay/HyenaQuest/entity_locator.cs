using FailCake;
using UnityEngine;

namespace HyenaQuest;

public class entity_locator : MonoBehaviour
{
	[Range(0.1f, 10f)]
	public float offTimer = 2f;

	[Range(0.1f, 10f)]
	public float onTimer = 0.35f;

	public bool playOnAwake;

	public AudioClip clip;

	[Range(1f, 20f)]
	public float hearingRange = 5f;

	private entity_led _led;

	private Light _light;

	private util_timer _beepTimer;

	private util_timer _lightTimer;

	public void Awake()
	{
		_led = GetComponentInChildren<entity_led>(includeInactive: true);
		if (!_led)
		{
			throw new UnityException("entity_locator requires entity_led component");
		}
		_light = GetComponentInChildren<Light>(includeInactive: true);
		if (!_light)
		{
			throw new UnityException("entity_locator requires Light component");
		}
		_light.enabled = false;
		_light.color = _led.activeColor;
		_beepTimer?.Stop();
		_beepTimer = util_timer.Create(-1, offTimer, delegate
		{
			Beep();
		}, null, playOnAwake);
	}

	public void OnDestroy()
	{
		_beepTimer?.Stop();
		_lightTimer?.Stop();
	}

	public void SetActive(bool activated)
	{
		_lightTimer?.Stop();
		_beepTimer?.SetPaused(!activated, reset: true);
		_light.enabled = false;
		_led.SetActive(enable: false);
	}

	public void ManualBeep(float pitch = 1f, float volume = 0.7f)
	{
		Beep(pitch, volume);
	}

	private void Beep(float pitch = 1f, float volume = 0.7f)
	{
		if (!_light || !_led)
		{
			return;
		}
		_light.enabled = true;
		_led.SetActive(enable: true);
		NetController<SoundController>.Instance.Play3DSound(clip, base.transform.position, new AudioData
		{
			distance = hearingRange,
			pitch = pitch,
			volume = volume,
			mixer = SoundMixer.SFX
		});
		_lightTimer?.Stop();
		_lightTimer = util_timer.Simple(onTimer, delegate
		{
			if ((bool)_light)
			{
				_light.enabled = false;
				_led.SetActive(enable: false);
			}
		});
	}
}
