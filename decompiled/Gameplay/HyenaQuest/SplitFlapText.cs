using UnityEngine;

namespace HyenaQuest;

public struct SplitFlapText
{
	public SplitFlapMode mode;

	public string text;

	public string matrix;

	public float speed;

	public int attempts;

	public override bool Equals(object obj)
	{
		if (!(obj is SplitFlapText splitFlapText))
		{
			return false;
		}
		if (mode == splitFlapText.mode && text == splitFlapText.text && matrix == splitFlapText.matrix && Mathf.Approximately(speed, splitFlapText.speed))
		{
			return attempts == splitFlapText.attempts;
		}
		return false;
	}

	public override int GetHashCode()
	{
		return (mode, text, matrix, speed, attempts).GetHashCode();
	}

	public static bool operator ==(SplitFlapText a, SplitFlapText b)
	{
		if (a.mode == b.mode && a.text == b.text && a.matrix == b.matrix && Mathf.Approximately(a.speed, b.speed))
		{
			return a.attempts == b.attempts;
		}
		return false;
	}

	public static bool operator !=(SplitFlapText a, SplitFlapText b)
	{
		return !(a == b);
	}
}
