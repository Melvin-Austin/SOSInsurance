using System;
using TMPro;
using UnityEngine;

namespace HyenaQuest;

[ExecuteInEditMode]
[RequireComponent(typeof(TextMeshPro))]
public class entity_curved_text : MonoBehaviour
{
	public float radius = 10f;

	public float rotationSpeed = 30f;

	public bool scaleTextToRadius = true;

	public float textSizeRatio = 1.5f;

	public bool splitChars;

	private TextMeshPro _text;

	private float _currentRotation;

	public void Awake()
	{
		InitializeText();
	}

	public void OnEnable()
	{
		if (!_text)
		{
			InitializeText();
		}
		_text.OnPreRenderText += OnCurveText;
		_text.ForceMeshUpdate();
	}

	public void OnDisable()
	{
		if ((bool)_text)
		{
			_text.OnPreRenderText -= OnCurveText;
		}
	}

	public void Update()
	{
		if (!_text)
		{
			return;
		}
		if (scaleTextToRadius)
		{
			float num = radius * textSizeRatio;
			if (Mathf.Abs(_text.fontSize - num) > 0.01f)
			{
				_text.fontSize = Mathf.Max(0.1f, num);
			}
		}
		if (Application.isPlaying)
		{
			if (rotationSpeed != 0f)
			{
				_currentRotation += rotationSpeed * Time.deltaTime;
				_text.ForceMeshUpdate();
			}
		}
		else if (base.transform.hasChanged)
		{
			_text.ForceMeshUpdate();
			base.transform.hasChanged = false;
		}
	}

	public void SetText(string text, Color color)
	{
		if ((bool)_text)
		{
			_text.text = text;
			_text.color = color;
		}
	}

	private void InitializeText()
	{
		_text = GetComponent<TextMeshPro>();
		if (!_text)
		{
			throw new UnityException("Missing TextMeshPro Component");
		}
		_text.alignment = TextAlignmentOptions.Center;
		_text.verticalAlignment = VerticalAlignmentOptions.Baseline;
	}

	private void OnCurveText(TMP_TextInfo textInfo)
	{
		int num = 0;
		for (int i = 0; i < textInfo.characterCount; i++)
		{
			if (textInfo.characterInfo[i].isVisible)
			{
				num++;
			}
		}
		if (num == 0)
		{
			return;
		}
		float angleStep = (splitChars ? (360f / (float)num) : 0f);
		int num2 = 0;
		TMP_CharacterInfo[] characterInfo = textInfo.characterInfo;
		for (int j = 0; j < characterInfo.Length; j++)
		{
			TMP_CharacterInfo charInfo = characterInfo[j];
			if (charInfo.isVisible)
			{
				int vertexIndex = charInfo.vertexIndex;
				Vector3[] vertices = textInfo.meshInfo[charInfo.materialReferenceIndex].vertices;
				Vector3 vector = new Vector3((vertices[vertexIndex].x + vertices[vertexIndex + 2].x) * 0.5f, charInfo.baseLine, 0f);
				vertices[vertexIndex] -= vector;
				vertices[vertexIndex + 1] -= vector;
				vertices[vertexIndex + 2] -= vector;
				vertices[vertexIndex + 3] -= vector;
				Matrix4x4 matrix4x = ComputeTransformationMatrix(vector, charInfo, textInfo, num2, angleStep);
				vertices[vertexIndex] = matrix4x.MultiplyPoint3x4(vertices[vertexIndex]);
				vertices[vertexIndex + 1] = matrix4x.MultiplyPoint3x4(vertices[vertexIndex + 1]);
				vertices[vertexIndex + 2] = matrix4x.MultiplyPoint3x4(vertices[vertexIndex + 2]);
				vertices[vertexIndex + 3] = matrix4x.MultiplyPoint3x4(vertices[vertexIndex + 3]);
				num2++;
			}
		}
	}

	private Matrix4x4 ComputeTransformationMatrix(Vector3 charMidBaselinePos, TMP_CharacterInfo charInfo, TMP_TextInfo textInfo, int charIndex, float angleStep)
	{
		float num = ((charInfo.lineNumber >= 0 && textInfo.lineInfo != null) ? textInfo.lineInfo[charInfo.lineNumber].baseline : 0f);
		float num2 = Mathf.Max(0.001f, radius - num);
		float num3 = ((splitChars ? ((float)charIndex * angleStep) : ((0f - charMidBaselinePos.x / num2) * 57.29578f)) + _currentRotation) * (MathF.PI / 180f);
		Vector3 pos = new Vector3(Mathf.Sin(num3) * num2, charMidBaselinePos.y, Mathf.Cos(num3) * num2);
		Quaternion q = Quaternion.Euler(0f, num3 * 57.29578f + 180f, 0f);
		return Matrix4x4.TRS(pos, q, Vector3.one);
	}
}
