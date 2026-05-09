using UnityEngine;
using UnityEngine.Audio;

namespace HyenaQuest;

[RequireComponent(typeof(AudioSource))]
public class entity_sound : MonoBehaviour
{
	private AudioSource _speaker;

	private AudioLowPassFilter _volumeFilter;

	private float _timeRemaining = -1f;

	private bool _playOnce;

	public void Awake()
	{
		_speaker = GetComponent<AudioSource>();
		if (!_speaker)
		{
			throw new UnityException("entity_sound requires AudioSource component");
		}
		_volumeFilter = GetComponent<AudioLowPassFilter>();
		if (!_volumeFilter)
		{
			throw new UnityException("entity_sound requires AudioLowPassFilter component");
		}
		_volumeFilter.enabled = false;
		_speaker.loop = false;
		_speaker.playOnAwake = false;
		_speaker.enabled = false;
		_speaker.spatialize = false;
		_speaker.spatializePostEffects = false;
	}

	public void SetMixer(AudioMixerGroup mixer)
	{
		_speaker.outputAudioMixerGroup = mixer;
	}

	public void SetClip(AudioClip clip)
	{
		Stop();
		_speaker.clip = clip;
	}

	public void Set3DTarget(Transform tr, float maxDistance = 10f)
	{
		Set3DTarget(tr.position, maxDistance);
	}

	public void Set3DTarget(Vector3 position, float maxDistance = 10f)
	{
		_speaker.spatialBlend = 1f;
		_speaker.maxDistance = maxDistance;
		_speaker.minDistance = 0f;
		_speaker.rolloffMode = AudioRolloffMode.Custom;
		_speaker.spread = 180f;
		_speaker.spatialize = false;
		_speaker.spatializePostEffects = false;
		_speaker.transform.position = position;
	}

	public void Set2D()
	{
		_speaker.spatialBlend = 0f;
		_speaker.spatialize = false;
		_speaker.spatializePostEffects = false;
	}

	public void SetPitch(float pitch)
	{
		_speaker.pitch = pitch;
	}

	public void SetVolume(float volume)
	{
		_speaker.volume = volume;
	}

	public void PlayOnce()
	{
		if ((bool)_speaker.clip)
		{
			_timeRemaining = Time.time + _speaker.clip.length;
			_playOnce = true;
			Stop();
			Play();
		}
	}

	public void Stop()
	{
		_speaker.Stop();
		_speaker.enabled = false;
		_volumeFilter.enabled = false;
	}

	public void Pause()
	{
		_speaker.Pause();
	}

	public void Play()
	{
		if (Mathf.Approximately(_speaker.spatialBlend, 1f))
		{
			_volumeFilter.enabled = IsInsideVolume();
		}
		_speaker.enabled = true;
		_speaker.Play();
	}

	public void Destroy()
	{
		Stop();
		Set2D();
		SetClip(null);
		_playOnce = false;
		NetController<SoundController>.Instance.QueueSound(this);
	}

	public void Update()
	{
		if ((bool)_speaker && (bool)_speaker.clip && _playOnce && !(Time.time < _timeRemaining))
		{
			Destroy();
		}
	}

	private bool IsInsideVolume()
	{
		entity_movement_volume[] array = Object.FindObjectsByType<entity_movement_volume>();
		if (array == null || array.Length == 0)
		{
			return false;
		}
		entity_movement_volume[] array2 = array;
		foreach (entity_movement_volume entity_movement_volume2 in array2)
		{
			if (entity_movement_volume2.isActiveAndEnabled && entity_movement_volume2.waterVolume && entity_movement_volume2.IsInsideVolume(base.transform.position))
			{
				return true;
			}
		}
		return false;
	}
}
