using System;
using FailCake;
using UnityEngine;
using UnityEngine.UI;

namespace HyenaQuest;

[RequireComponent(typeof(Image))]
public class ui_fade : MonoBehaviour
{
	public bool fadeIn;

	public float fadeSpeed;

	public bool playOnAwake;

	private util_fade_timer _fadeTimer;

	private Image _sprite;

	public void Awake()
	{
		_sprite = GetComponent<Image>();
		if (!_sprite)
		{
			throw new UnityException("Missing Image component");
		}
		SetAlpha(fadeIn ? 0f : 1f);
		if (playOnAwake)
		{
			Play();
		}
	}

	public void Play(Action<bool> callback = null)
	{
		SetAlpha(fadeIn ? 0f : 1f);
		if (_fadeTimer != null)
		{
			_fadeTimer.Stop();
		}
		_fadeTimer = util_fade_timer.Fade(fadeSpeed, fadeIn ? 0f : 1f, fadeIn ? 1f : 0f, SetAlpha, delegate(float alpha)
		{
			SetAlpha(alpha);
			callback?.Invoke(fadeIn);
		});
	}

	public void SetColor(Color color)
	{
		if ((bool)_sprite)
		{
			_sprite.color = color;
		}
	}

	public void OnDestroy()
	{
		Stop();
	}

	public void Stop()
	{
		_fadeTimer?.Stop();
	}

	public void SetAlpha(float alpha)
	{
		Color color = _sprite.color;
		color.a = alpha;
		_sprite.color = color;
	}
}
