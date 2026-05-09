namespace HyenaQuest;

public static class util_string
{
	public static string BytesToString(int bytes)
	{
		string[] array = new string[5] { "B", "KB", "MB", "GB", "TB" };
		double num = bytes;
		int num2 = 0;
		while (num >= 1024.0 && num2 < array.Length - 1)
		{
			num2++;
			num /= 1024.0;
		}
		return $"{num:0.###} {array[num2]}";
	}

	public static string? Truncate(this string? value, int maxLength, string truncationSuffix = "…")
	{
		if (value == null || value.Length <= maxLength)
		{
			return value;
		}
		return value.Substring(0, maxLength) + truncationSuffix;
	}
}
