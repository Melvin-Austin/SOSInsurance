using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

namespace HyenaQuest;

public class entity_ambient_sound_mixer_controller : MonoBehaviour
{
	[Header("Settings")]
	public AudioMixer mixer;

	public string volumeParameter;

	private readonly List<entity_ambient_sound_mixer> _ambientAreas = new List<entity_ambient_sound_mixer>();

	private float _currentVolume = -80f;

	public void Awake()
	{
		if (!mixer)
		{
			throw new UnityException("Mixer missing!");
		}
		if (string.IsNullOrEmpty(volumeParameter))
		{
			throw new UnityException("Volume parameter missing!");
		}
		_currentVolume = -80f;
	}

	public void Register(entity_ambient_sound_mixer ambient)
	{
		if ((bool)ambient && !_ambientAreas.Contains(ambient))
		{
			_ambientAreas.Add(ambient);
		}
	}

	public void Unregister(entity_ambient_sound_mixer ambient)
	{
		if ((bool)ambient && _ambientAreas.Contains(ambient))
		{
			_ambientAreas.Remove(ambient);
		}
	}

	public void OnDestroy()
	{
		_ambientAreas.Clear();
		if ((bool)mixer)
		{
			mixer.SetFloat(volumeParameter, -80f);
		}
	}

	public void Update()
	{
		if (!PlayerController.LOCAL || !mixer)
		{
			return;
		}
		float b = -80f;
		float num = float.MaxValue;
		if (!PlayerController.LOCAL.IsDead())
		{
			foreach (entity_ambient_sound_mixer ambientArea in _ambientAreas)
			{
				if (!ambientArea || !ambientArea.isActiveAndEnabled)
				{
					continue;
				}
				Bounds? bounds = ambientArea.GetBounds();
				if (bounds.HasValue)
				{
					float num2 = Vector3.Distance(PlayerController.LOCAL.view.position, ambientArea.transform.position);
					float magnitude = bounds.Value.extents.magnitude;
					if (!(num2 > magnitude) && num2 < num)
					{
						num = num2;
						b = (ambientArea.fullDistance ? 0f : Mathf.Lerp(-80f, 0f, 1f - num2 / magnitude));
					}
				}
				else if (Mathf.Approximately(num, float.MaxValue))
				{
					b = 0f;
				}
			}
		}
		_currentVolume = Mathf.Lerp(_currentVolume, b, Time.deltaTime * 10f);
		mixer.SetFloat(volumeParameter, _currentVolume);
	}
}
