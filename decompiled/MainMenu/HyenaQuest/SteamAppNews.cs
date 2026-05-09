using System;

namespace HyenaQuest;

[Serializable]
public class SteamAppNews
{
	[Serializable]
	public class SteamNewsItem
	{
		public string title;

		public string contents;
	}

	public SteamNewsItem[] newsitems;
}
