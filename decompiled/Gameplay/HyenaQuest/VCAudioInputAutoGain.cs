using MetaVoiceChat.Input;
using UnityEngine;

namespace HyenaQuest;

public class VCAudioInputAutoGain : VcInputFilter
{
	[Range(0.01f, 0.5f)]
	public float targetLevel = 0.15f;

	[Range(1f, 50f)]
	public float maxGain = 20f;

	[Range(0.1f, 1f)]
	public float minGain = 0.5f;

	[Range(0.001f, 0.1f)]
	public float attackSpeed = 0.1f;

	[Range(0.01f, 0.5f)]
	public float releaseSpeed = 0.01f;

	[Range(0.3f, 0.95f)]
	public float softClipThreshold = 0.7f;

	[Range(0.0001f, 0.01f)]
	public float noiseFloor = 0.001f;

	private static readonly int REFERENCE_BUFFER_SIZE = 128;

	private float _currentGain = 1f;

	protected override void Filter(int index, ref float[] samples)
	{
		if (samples != null && samples.Length != 0)
		{
			float rms = CalculateRms(samples);
			UpdateGain(rms, samples.Length);
			for (int i = 0; i < samples.Length; i++)
			{
				float sample = samples[i] * _currentGain;
				samples[i] = ApplySoftClip(sample);
			}
		}
	}

	private float CalculateRms(float[] samples)
	{
		float num = 0f;
		for (int i = 0; i < samples.Length; i++)
		{
			num += samples[i] * samples[i];
		}
		return Mathf.Sqrt(num / (float)samples.Length);
	}

	private void UpdateGain(float rms, int bufferSize)
	{
		if (!(rms < noiseFloor))
		{
			float num = Mathf.Clamp(targetLevel / rms, minGain, maxGain);
			float num2 = ((num < _currentGain) ? attackSpeed : releaseSpeed);
			float p = (float)bufferSize / (float)REFERENCE_BUFFER_SIZE;
			float t = 1f - Mathf.Pow(1f - num2, p);
			_currentGain = Mathf.Lerp(_currentGain, num, t);
		}
	}

	private float ApplySoftClip(float sample)
	{
		float num = Mathf.Abs(sample);
		if (num <= softClipThreshold)
		{
			return sample;
		}
		float num2 = Mathf.Sign(sample);
		float num3 = 1f - softClipThreshold;
		float x = (num - softClipThreshold) / num3;
		float num4 = num3 * TanhApprox(x);
		return num2 * Mathf.Min(softClipThreshold + num4, 0.99f);
	}

	private float TanhApprox(float x)
	{
		if (x > 3f)
		{
			return 1f;
		}
		if (x < -3f)
		{
			return -1f;
		}
		float num = x * x;
		return x * (27f + num) / (27f + 9f * num);
	}
}
