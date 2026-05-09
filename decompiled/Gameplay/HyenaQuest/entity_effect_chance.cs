using System.Collections.Generic;
using FailCake;
using UnityEngine;
using UnityEngine.VFX;

namespace HyenaQuest;

public class entity_effect_chance : MonoBehaviour
{
	[Range(0f, 1f)]
	public float chance = 0.01f;

	[Range(0.1f, 2f)]
	public float check = 1f;

	public List<VisualEffect> effects = new List<VisualEffect>();

	public List<AudioClip> sounds = new List<AudioClip>();

	private AudioSource _audio;

	private util_timer _timer;

	public void Awake()
	{
		_audio = GetComponent<AudioSource>();
		if ((bool)_audio)
		{
			_audio.playOnAwake = false;
		}
		_timer?.Stop();
		_timer = util_timer.Create(-1, check, delegate
		{
			List<VisualEffect> list = effects;
			if (list != null && list.Count > 0 && !(Random.value > chance))
			{
				effects[Random.Range(0, effects.Count)].Play();
				if ((bool)_audio && _audio.enabled)
				{
					_audio.pitch = Random.Range(0.8f, 1.2f);
					_audio.PlayOneShot(sounds[Random.Range(0, sounds.Count)]);
				}
			}
		});
	}

	public void OnDestroy()
	{
		_timer?.Stop();
	}
}
