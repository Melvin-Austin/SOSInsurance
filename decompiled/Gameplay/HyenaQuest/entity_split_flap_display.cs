using System.Collections.Generic;
using System.Linq;
using FailCake;
using UnityEngine;

namespace HyenaQuest;

[RequireComponent(typeof(AudioSource))]
public class entity_split_flap_display : MonoBehaviour
{
	public string ID;

	public int size = 10;

	public string defaultText = "";

	public string defaultMatrix = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789 ?!.";

	[Header("Templates")]
	public GameObject flapTemplate;

	public AudioClip flip;

	public AudioClip shuffle;

	private List<entity_split_flap_counter> _counters = new List<entity_split_flap_counter>();

	private readonly List<util_timer> _timers = new List<util_timer>();

	private AudioSource _soundEffect;

	private SplitFlapText _text;

	private bool ValidateID()
	{
		return !string.IsNullOrEmpty(ID);
	}

	public void Awake()
	{
		if (!flapTemplate)
		{
			throw new UnityException("Missing flap template");
		}
		_soundEffect = GetComponent<AudioSource>();
		if (!_soundEffect)
		{
			throw new UnityException("Missing AudioSource component");
		}
		_soundEffect.spatialBlend = 1f;
		_soundEffect.maxDistance = 2.5f;
		_soundEffect.minDistance = 0f;
		_soundEffect.rolloffMode = AudioRolloffMode.Linear;
		Prepare();
	}

	public void OnDestroy()
	{
		if (_timers.Count <= 0)
		{
			return;
		}
		foreach (util_timer timer in _timers)
		{
			timer?.Stop();
		}
		_timers.Clear();
	}

	public void ResetText(SplitFlapMode mode)
	{
		SetText(mode, defaultText, defaultMatrix);
	}

	public void SetText(SplitFlapMode mode, string text, float speed = 0.05f, int attempts = 0)
	{
		_text = new SplitFlapText
		{
			text = text,
			speed = speed,
			mode = mode,
			matrix = defaultMatrix,
			attempts = attempts
		};
		AnimateText(_text);
	}

	public void SetText(SplitFlapMode mode, string text, string matrix, float speed = 0.05f, int attempts = 0)
	{
		_text = new SplitFlapText
		{
			text = text,
			speed = speed,
			mode = mode,
			matrix = (string.IsNullOrEmpty(matrix) ? defaultMatrix : matrix),
			attempts = attempts
		};
		AnimateText(_text);
	}

	public void SetMatrix(string matrix)
	{
		if (string.IsNullOrEmpty(matrix))
		{
			throw new UnityException("Invalid matrix");
		}
		_text = new SplitFlapText
		{
			text = _text.text,
			speed = _text.speed,
			mode = _text.mode,
			matrix = matrix,
			attempts = _text.attempts
		};
		AnimateText(_text);
	}

	[Client]
	private void Prepare()
	{
		_counters = GetComponentsInChildren<entity_split_flap_counter>(includeInactive: true).ToList();
		if (_counters == null || _counters.Count != size)
		{
			throw new UnityException("Invalid counter template, size mismatch");
		}
		for (int i = 0; i < _counters.Count; i++)
		{
			if ((bool)_counters[i])
			{
				_counters[i].SetMatrix(defaultMatrix);
				_counters[i].SetDefault(defaultText[i]);
			}
		}
	}

	[Client]
	private void PlaySoundEffect()
	{
		if ((bool)flip && (bool)_soundEffect && _soundEffect.enabled)
		{
			_soundEffect.clip = flip;
			_soundEffect.volume = 0.1f;
			_soundEffect.pitch = Random.Range(0.95f, 1.05f);
			_soundEffect.Play();
		}
	}

	[Client]
	private void PlayShuffleSound()
	{
		if ((bool)shuffle && (bool)_soundEffect && _soundEffect.enabled)
		{
			_soundEffect.clip = shuffle;
			_soundEffect.volume = 0.2f;
			_soundEffect.pitch = Random.Range(0.8f, 1.2f);
			_soundEffect.Play();
		}
	}

	[Client]
	private void AnimateText(SplitFlapText data)
	{
		List<entity_split_flap_counter> counters = _counters;
		if (counters == null || counters.Count <= 0)
		{
			Prepare();
		}
		counters = _counters;
		if (counters == null || counters.Count <= 0)
		{
			throw new UnityException("Failed to generate counters");
		}
		if (string.IsNullOrEmpty(data.text))
		{
			data.text = "";
		}
		if (_timers.Count > 0)
		{
			foreach (util_timer timer in _timers)
			{
				timer?.Stop();
			}
			_timers.Clear();
		}
		foreach (entity_split_flap_counter counter2 in _counters)
		{
			if ((bool)counter2)
			{
				counter2.SetMatrix(data.matrix);
			}
		}
		if (data.mode == SplitFlapMode.INSTANT)
		{
			for (int i = 0; i < size; i++)
			{
				if (i < data.text.Length && (bool)_counters[i])
				{
					_counters[i].SetCharacter(data.text.ToUpper()[i]);
				}
			}
			if (data.speed > 0.02f)
			{
				PlaySoundEffect();
			}
			return;
		}
		if (data.mode == SplitFlapMode.SHUFFLE)
		{
			foreach (entity_split_flap_counter counter3 in _counters)
			{
				if ((bool)counter3)
				{
					counter3.SetCharacter(data.matrix[Random.Range(0, data.matrix.Length)]);
				}
			}
			PlayShuffleSound();
		}
		for (int j = 0; j < size; j++)
		{
			if (j >= data.text.Length)
			{
				continue;
			}
			entity_split_flap_counter counter = _counters[j];
			char letter = data.text.ToUpper()[j];
			_timers.Add(util_timer.Simple((float)j * 0.08f, delegate
			{
				counter.MoveToLetter(letter, data.speed, data.attempts, delegate
				{
					if (data.speed > 0.02f)
					{
						PlaySoundEffect();
					}
				});
			}));
		}
	}
}
