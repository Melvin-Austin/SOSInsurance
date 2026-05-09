using System;

namespace HyenaQuest;

public class TimeUtils
{
	public static string SecondsToTime(uint seconds)
	{
		TimeSpan timeSpan = TimeSpan.FromSeconds(seconds);
		return $"{timeSpan.Hours:D2}:{timeSpan.Minutes:D2}:{timeSpan.Seconds:D2}";
	}

	public static string SecondsToMsTime(uint msSeconds)
	{
		TimeSpan timeSpan = TimeSpan.FromMilliseconds(msSeconds);
		return $"{timeSpan.Minutes:D2}:{timeSpan.Seconds:D2}.{timeSpan.Milliseconds:D3}";
	}
}
