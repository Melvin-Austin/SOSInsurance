using FailCake;
using TMPro;
using UnityEngine;

namespace HyenaQuest;

[RequireComponent(typeof(TextMeshProUGUI))]
public class ui_player_chat : MonoBehaviour
{
	private TextMeshProUGUI _text;

	private util_fade_timer _fadeTimer;

	private util_timer _delayTimer;

	private bool _isFresh;

	private static bool _logFilterRegistered;

	public void Awake()
	{
		_text = GetComponent<TextMeshProUGUI>();
		if (!_text)
		{
			throw new UnityException("Missing TextMeshProUGUI");
		}
		base.name = "ui_player_chat";
	}

	public void OnDestroy()
	{
		_fadeTimer?.Stop();
		_delayTimer?.Stop();
	}

	public void SetText(string text)
	{
		if ((bool)_text)
		{
			_text.text = text;
			_text.ForceMeshUpdate();
		}
	}

	public void Show()
	{
		_fadeTimer?.Stop();
		_delayTimer?.Stop();
		if ((bool)_text)
		{
			_text.alpha = 1f;
		}
	}

	public void Hide()
	{
		_fadeTimer?.Stop();
		_delayTimer?.Stop();
		if ((bool)_text)
		{
			_text.alpha = 0f;
		}
		_isFresh = false;
	}

	public void FadeOut()
	{
		_fadeTimer?.Stop();
		_delayTimer?.Stop();
		if ((bool)_text)
		{
			_text.alpha = 1f;
		}
		_isFresh = true;
		_delayTimer = util_timer.Simple(2f, delegate
		{
			if ((bool)_text)
			{
				_fadeTimer = util_fade_timer.Fade(0.5f, 1f, 0f, delegate(float f)
				{
					if ((bool)_text)
					{
						_text.alpha = f;
					}
				}, delegate
				{
					_isFresh = false;
				});
			}
		});
	}

	public bool IsFresh()
	{
		return _isFresh;
	}
}
