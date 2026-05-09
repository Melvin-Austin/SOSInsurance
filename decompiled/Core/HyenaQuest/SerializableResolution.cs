using System;
using UnityEngine;

namespace HyenaQuest;

[Serializable]
public struct SerializableResolution
{
	public int width;

	public int height;

	public uint refreshRateNumerator;

	public uint refreshRateDenominator;

	public static implicit operator SerializableResolution(Resolution resolution)
	{
		SerializableResolution result = default(SerializableResolution);
		result.width = resolution.width;
		result.height = resolution.height;
		result.refreshRateNumerator = resolution.refreshRateRatio.numerator;
		result.refreshRateDenominator = resolution.refreshRateRatio.denominator;
		return result;
	}

	public Resolution ToResolution()
	{
		Resolution result = default(Resolution);
		result.width = width;
		result.height = height;
		result.refreshRateRatio = new RefreshRate
		{
			numerator = refreshRateNumerator,
			denominator = refreshRateDenominator
		};
		return result;
	}
}
