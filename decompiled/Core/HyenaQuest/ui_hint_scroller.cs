using TMPro;
using UnityEngine;

namespace HyenaQuest;

public class ui_hint_scroller : MonoBehaviour
{
	[Header("Hint settings")]
	public string text = "";

	public float speed = 0.2f;

	public bool isEnabled = true;

	private TextMeshPro _text;

	private TextMeshPro _cloneText;

	private RectTransform _rectTransform;

	private Vector2 _startPos;

	private float _sizeW;

	private float _scrollPos;

	public void Awake()
	{
		_text = GetComponentInChildren<TextMeshPro>();
		_text.SetText(text);
		_text.SetLayoutDirty();
		_rectTransform = _text.GetComponent<RectTransform>();
		_cloneText = Object.Instantiate(_text);
		_cloneText.SetText(text);
		RectTransform component = _cloneText.GetComponent<RectTransform>();
		component.SetParent(_rectTransform);
		component.anchorMin = new Vector2(1f, 0.5f);
		component.localScale = new Vector3(1f, 1f, 1f);
		component.transform.localPosition = Vector3.zero;
		component.anchoredPosition = new Vector2(10f, 0f);
		_sizeW = _text.preferredWidth + component.transform.localPosition.x;
		_startPos = _rectTransform.anchoredPosition;
		_scrollPos = 0f;
	}

	public void Update()
	{
		if (isEnabled)
		{
			_rectTransform.anchoredPosition = new Vector2(_startPos.x - _scrollPos, _startPos.y);
			_scrollPos = _scrollPos % _sizeW + speed;
		}
	}
}
