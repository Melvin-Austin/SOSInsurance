using MetaVoiceChat.Output;
using UnityEngine;

namespace HyenaQuest;

public class VCAudioOutputAmplitude : VcOutputFilter
{
	[HideInInspector]
	public float amplitude;

	protected override void Filter(int index, float[] samples, float targetLatency)
	{
		amplitude = GetRms(samples);
	}

	private float GetRms(float[] samples)
	{
		if (samples == null || samples.Length == 0)
		{
			return 0f;
		}
		float num = 0f;
		foreach (float num2 in samples)
		{
			num += num2 * num2;
		}
		return Mathf.Sqrt(num / (float)samples.Length);
	}
}
