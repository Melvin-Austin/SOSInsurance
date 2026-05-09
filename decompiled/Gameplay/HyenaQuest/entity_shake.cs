using System.Collections.Generic;
using UnityEngine;

namespace HyenaQuest;

public class entity_shake : MonoBehaviour
{
	[Header("Settings")]
	public bool active;

	public ShakeMode mode = ShakeMode.SHAKE_ALL;

	public float intensity = 0.5f;

	public float radius = 2f;

	public bool scaleIntensity = true;

	public ShakeSoundMode soundMode;

	public List<AudioClip> soundFX = new List<AudioClip>();

	private float _time;

	public void Awake()
	{
		CoreController.WaitFor(delegate(ShakeController shakeCtrl)
		{
			shakeCtrl.Register(this);
		});
	}

	public void OnDestroy()
	{
		NetController<ShakeController>.Instance?.Unregister(this);
	}

	[Client]
	public void SetActive(bool act, float time = 0f, float intense = 0f)
	{
		active = act;
		_time = ((act && time > 0f) ? (Time.time + time) : 0f);
		SetIntensity((intense > 0f) ? intense : intensity);
		List<AudioClip> list = soundFX;
		if (list != null && list.Count > 0 && act)
		{
			AudioData data = new AudioData
			{
				distance = radius + 1f,
				volume = 0.4f
			};
			switch (soundMode)
			{
			case ShakeSoundMode.LOCAL:
				NetController<SoundController>.Instance.Play3DSound(soundFX[Random.Range(0, soundFX.Count)], base.transform.position, data);
				break;
			case ShakeSoundMode.GLOBAL:
				NetController<SoundController>.Instance.PlaySound(soundFX[Random.Range(0, soundFX.Count)], data);
				break;
			}
		}
	}

	[Client]
	public void SetIntensity(float shakeIntensity)
	{
		intensity = shakeIntensity;
	}

	[Client]
	public float GetIntensity()
	{
		return intensity;
	}

	[Client]
	public void SetShakeMode(ShakeMode shakeMode)
	{
		mode = shakeMode;
	}

	[Client]
	public ShakeMode GetShakeMode()
	{
		return mode;
	}

	[Client]
	public void SetSoundMode(ShakeSoundMode soundMode)
	{
		this.soundMode = soundMode;
	}

	[Client]
	public void SetRadius(float rad)
	{
		radius = rad;
	}

	[Client]
	public float GetRadius()
	{
		return radius;
	}

	[Client]
	public bool ScaleWithIntensity()
	{
		return scaleIntensity;
	}

	[Client]
	public bool IsGlobal()
	{
		return radius <= 0f;
	}

	[Client]
	public bool IsActive()
	{
		return active;
	}

	private void Update()
	{
		if (!(_time <= 0f) && !(Time.time < _time))
		{
			SetActive(act: false);
		}
	}
}
