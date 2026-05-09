using System;
using System.Collections.Generic;
using FailCake;
using UnityEngine;
using ZLinq;

namespace HyenaQuest;

[RequireComponent(typeof(MeshRenderer), typeof(MeshFilter))]
public class entity_split_flap_counter : MonoBehaviour
{
	private static readonly List<char> ATLAS_MATRIX = new List<char>
	{
		':', 'Z', 'Y', 'X', 'W', 'V', 'U', 'T', 'S', 'R',
		'?', 'Q', 'P', 'O', 'N', 'M', 'L', 'K', 'J', 'I',
		'H', 'G', 'F', 'E', 'D', 'C', '9', '8', '7', '6',
		'5', '4', '3', '2', '1', '0', '.', ' ', 'A', 'B'
	};

	private static readonly int Index = Shader.PropertyToID("_Index");

	private char _currentChar = ' ';

	private Dictionary<char, int> _matrix;

	private string _matrixStr;

	private util_timer _timer;

	private MeshRenderer _meshRenderer;

	public void Awake()
	{
		_meshRenderer = GetComponent<MeshRenderer>();
		if (!_meshRenderer)
		{
			throw new UnityException("Missing MeshRenderer");
		}
	}

	public void SetMatrix(string matrix)
	{
		if (!string.IsNullOrEmpty(matrix) && !(_matrixStr == matrix))
		{
			_matrixStr = matrix;
			_matrix = (from c in matrix.AsValueEnumerable()
				select new
				{
					Char = c,
					Index = ATLAS_MATRIX.IndexOf(c)
				} into x
				where x.Index != -1
				select x).ToDictionary(x => x.Char, x => x.Index);
		}
	}

	public void SetDefault(char letter)
	{
		if (CharToSprite(letter) == -1)
		{
			throw new UnityException($"Char '{letter}' not available");
		}
		_timer?.Stop();
		SetCharacter(letter);
	}

	public void OnDestroy()
	{
		_timer?.Stop();
	}

	public void MoveToLetter(char letter, float speed, int attempts, Action onCharMove = null, Action onDone = null)
	{
		if (_matrixStr.IndexOf(letter) == -1)
		{
			throw new UnityException($"Letter '{letter}' not available in matrix!");
		}
		_timer?.Stop();
		if (_currentChar == letter)
		{
			onDone?.Invoke();
			return;
		}
		int num = _matrixStr.IndexOf(_currentChar);
		int num2 = Mathf.Abs(_matrixStr.IndexOf(letter) - num);
		if (num2 <= 0)
		{
			onDone?.Invoke();
			return;
		}
		int currentAttempt = 0;
		_timer = util_timer.Create(num2 * attempts, speed, delegate
		{
			int index = (_matrixStr.IndexOf(_currentChar) + 1) % _matrixStr.Length;
			SetCharacter(_matrixStr[index]);
			onCharMove?.Invoke();
			if (_currentChar == letter)
			{
				currentAttempt++;
				if (currentAttempt >= attempts)
				{
					_timer?.Stop();
					onDone?.Invoke();
				}
			}
		});
	}

	public void SetCharacter(char c)
	{
		_currentChar = c;
		SetCharacter(CharToSprite(c));
	}

	public void SetCharacter(int index)
	{
		if (index == -1)
		{
			throw new UnityException("Invalid character index");
		}
		if ((bool)_meshRenderer)
		{
			_meshRenderer.material.SetFloat(Index, index);
		}
	}

	private int CharToSprite(char c)
	{
		return _matrix.GetValueOrDefault(c, -1);
	}
}
