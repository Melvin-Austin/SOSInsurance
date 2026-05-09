using FailCake;
using UnityEngine;
using UnityEngine.UI;

namespace HyenaQuest;

[DisallowMultipleComponent]
public class ui_fade_delete : MonoBehaviour
{
	[Range(0.01f, 10f)]
	public float fadeOutSpeed = 1f;

	[Range(0f, 10f)]
	public float fadeInSpeed;

	private util_fade_timer _fade;

	private Image _image;

	public void Awake()
	{
		_image = GetComponent<Image>();
		if (!_image)
		{
			throw new UnityException("Missing image");
		}
		_fade?.Stop();
		if (fadeInSpeed > 0f)
		{
			_fade = util_fade_timer.Fade(fadeInSpeed, 0f, 1f, SetAlpha, delegate
			{
				_fade = util_fade_timer.Fade(fadeOutSpeed, 1f, 0f, SetAlpha, OnComplete);
			});
		}
		else
		{
			_fade = util_fade_timer.Fade(fadeOutSpeed, 1f, 0f, SetAlpha, OnComplete);
		}
	}

	public void OnDestroy()
	{
		_fade?.Stop();
	}

	private void SetAlpha(float alpha)
	{
		if ((bool)_image)
		{
			_image.color = new Color(_image.color.r, _image.color.g, _image.color.b, alpha);
		}
	}

	private void OnComplete(float alpha)
	{
		SetAlpha(alpha);
		Object.Destroy(base.gameObject, 0.05f);
	}
}
