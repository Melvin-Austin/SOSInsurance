using FailCake;
using Febucci.TextAnimatorForUnity.TextMeshPro;
using TMPro;
using UnityEngine;

namespace HyenaQuest;

[RequireComponent(typeof(TextMeshPro))]
[RequireComponent(typeof(TextAnimator_TMP))]
public class ui_notification_3d : MonoBehaviour
{
	private TextMeshPro _text;

	private util_fade_timer _fadeTimer;

	private Vector3 _position;

	public void Awake()
	{
		_text = GetComponent<TextMeshPro>();
		if (!_text)
		{
			throw new UnityException("Missing TextMeshPro");
		}
		_position = base.transform.position;
	}

	public void OnDestroy()
	{
		_fadeTimer?.Stop();
	}

	public void SetText(string text, float fadeSpeed, float size = 1f, Color startColor = default(Color), Color endColor = default(Color))
	{
		if (startColor == default(Color))
		{
			startColor = Color.white;
		}
		if (endColor == default(Color))
		{
			endColor = startColor;
		}
		startColor.a = 1f;
		endColor.a = 0f;
		_text.fontSize = size;
		_text.color = ((startColor == default(Color)) ? Color.white : startColor);
		_text.SetText(text);
		_fadeTimer?.Stop();
		_fadeTimer = util_fade_timer.Fade(fadeSpeed, 0f, 1f, delegate(float alpha)
		{
			_text.color = Color.Lerp(startColor, endColor, alpha);
		}, delegate
		{
			Object.Destroy(base.gameObject);
		});
	}
}
