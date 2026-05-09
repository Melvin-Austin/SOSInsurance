using FailCake;
using Febucci.TextAnimatorForUnity.TextMeshPro;
using TMPro;
using UnityEngine;

namespace HyenaQuest;

[RequireComponent(typeof(TextMeshProUGUI))]
[RequireComponent(typeof(TextAnimator_TMP))]
public class ui_notification : ui_notification_base
{
	protected TextAnimator_TMP _text;

	protected util_timer _destroyTimer;

	public void Awake()
	{
		_text = GetComponent<TextAnimator_TMP>();
		if (!_text)
		{
			throw new UnityException("Missing TextMeshPro component");
		}
	}

	public new void OnDestroy()
	{
		_destroyTimer?.Stop();
		base.OnDestroy();
	}

	public virtual void SetText(string text, float duration)
	{
		if (!_text)
		{
			return;
		}
		_text.SetText(text);
		if (AutoScale())
		{
			Vector2 renderedValues = _text.TMProComponent.GetRenderedValues(onlyVisibleCharacters: true);
			if (base.transform is RectTransform rectTransform)
			{
				rectTransform.sizeDelta = new Vector2(rectTransform.sizeDelta.x, renderedValues.y + 4f);
			}
		}
		if (!(duration <= 0f))
		{
			_destroyTimer?.Stop();
			_destroyTimer = util_timer.Simple(duration, base.Destroy);
		}
	}

	protected virtual bool AutoScale()
	{
		return true;
	}
}
