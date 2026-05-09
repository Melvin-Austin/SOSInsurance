using System.Collections;
using TMPro;
using UnityEngine;

namespace HyenaQuest;

public class ui_fade_in_out : MonoBehaviour
{
	public float fadeInDuration = 0.5f;

	public float fadeOutDuration = 0.5f;

	public float displayDuration = 2f;

	private TextMeshProUGUI _text;

	public void Awake()
	{
		_text = GetComponent<TextMeshProUGUI>();
		if (!_text)
		{
			throw new UnityException("Missing TextMeshProUGUI component");
		}
		SetAlpha(0f);
		StartCoroutine(FadeSequence());
	}

	private IEnumerator FadeSequence()
	{
		float timer = 0f;
		while (timer < fadeInDuration)
		{
			timer += Time.deltaTime;
			SetAlpha(Mathf.Lerp(0f, 1f, timer / fadeInDuration));
			yield return null;
		}
		yield return new WaitForSeconds(displayDuration);
		timer = 0f;
		while (timer < fadeOutDuration)
		{
			timer += Time.deltaTime;
			SetAlpha(Mathf.Lerp(1f, 0f, timer / fadeOutDuration));
			yield return null;
		}
	}

	private void SetAlpha(float alpha)
	{
		if ((bool)_text)
		{
			_text.color = new Color(_text.color.r, _text.color.g, _text.color.b, alpha);
		}
	}
}
