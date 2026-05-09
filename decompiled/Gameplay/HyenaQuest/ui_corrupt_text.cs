using System.Collections;
using TMPro;
using UnityEngine;

namespace HyenaQuest;

public class ui_corrupt_text : MonoBehaviour
{
	private TextMeshProUGUI _text;

	private string _targetText;

	private readonly string _corruptChars = "█▓▒░";

	public void Awake()
	{
		_text = GetComponent<TextMeshProUGUI>();
		if (!_text)
		{
			throw new UnityException("TextMeshProUGUI missing!");
		}
	}

	public void SetText(string text)
	{
		_targetText = text;
		_text.text = "";
		AnimateCorruption();
	}

	private void AnimateCorruption()
	{
		if (!string.IsNullOrEmpty(_targetText))
		{
			char[] array = new char[_targetText.Length];
			for (int i = 0; i < array.Length; i++)
			{
				array[i] = _corruptChars[Random.Range(0, _corruptChars.Length)];
			}
			_text.text = new string(array);
			StartCoroutine(CorruptTextCoroutine(array));
		}
	}

	private IEnumerator CorruptTextCoroutine(char[] currentText)
	{
		int shuffleCount = Random.Range(5, 8);
		for (int i = 0; i < _targetText.Length; i++)
		{
			for (int j = 0; j < shuffleCount; j++)
			{
				yield return new WaitForSeconds(0.01f);
				currentText[i] = _corruptChars[Random.Range(0, _corruptChars.Length)];
				_text.text = new string(currentText);
			}
			yield return new WaitForSeconds(0.01f);
			currentText[i] = _targetText[i];
			_text.text = new string(currentText);
		}
	}
}
