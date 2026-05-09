using System.Collections.Generic;
using UnityEngine;

namespace HyenaQuest;

public class entity_sound_playback : MonoBehaviour
{
	public bool random;

	public float playback;

	public List<AudioClip> clips = new List<AudioClip>();

	private AudioSource _source;

	public void Awake()
	{
		_source = GetComponent<AudioSource>();
		if (!_source)
		{
			throw new UnityException("Missing AudioSource");
		}
	}

	public void OnEnable()
	{
		if ((bool)_source && (bool)_source.clip)
		{
			List<AudioClip> list = clips;
			if (list != null && list.Count > 0)
			{
				_source.clip = clips[Random.Range(0, clips.Count)];
			}
			_source.time = (random ? Random.Range(0f, _source.clip.length) : playback);
			_source.Play();
		}
	}
}
