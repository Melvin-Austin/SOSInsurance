using System;

namespace HyenaQuest;

[Serializable]
public class KofiMember
{
	public string Name { get; set; }

	public string DiscordUserId { get; set; }

	public KofiMember(string name, string discordUserId)
	{
		Name = name;
		DiscordUserId = discordUserId;
	}
}
