using FailCake;
using UnityEngine;

namespace HyenaQuest;

[RequireComponent(typeof(AudioSource))]
public class entity_sound_tester : MonoBehaviour
{
	private entity_led _locator;

	private Light _light;

	private AudioSource _audio;

	private util_timer _bleep;

	private util_timer _lightTimer;

	public void Awake()
	{
		_locator = GetComponentInChildren<entity_led>(includeInactive: true);
		if (!_locator)
		{
			throw new UnityException("Missing entity_led component");
		}
		_light = GetComponentInChildren<Light>(includeInactive: true);
		if (!_light)
		{
			throw new UnityException("Missing Light component");
		}
		_audio = GetComponent<AudioSource>();
		if (!_audio)
		{
			throw new UnityException("Missing AudioSource component");
		}
		_audio.playOnAwake = false;
	}

	public void OnEnable()
	{
		_lightTimer?.Stop();
		_bleep?.Stop();
		_bleep = util_timer.Create(-1, 3f, delegate
		{
			Beep();
		});
	}

	public void OnDisable()
	{
		Disable();
	}

	public void OnDestroy()
	{
		Disable();
	}

	private void Disable()
	{
		_bleep?.Stop();
		_lightTimer?.Stop();
		_light.enabled = false;
		_locator.SetActive(enable: false);
	}

	private void Beep()
	{
		if (!_light || !_locator)
		{
			return;
		}
		_light.enabled = true;
		_locator.SetActive(enable: true);
		_audio.Stop();
		_audio.Play();
		_lightTimer?.Stop();
		_lightTimer = util_timer.Simple(0.35f, delegate
		{
			if ((bool)_light)
			{
				_light.enabled = false;
				_locator.SetActive(enable: false);
			}
		});
	}
}
