using System;
using System.Collections.Generic;
using Steamworks;

namespace HyenaQuest;

[Serializable]
public struct WorkshopItem
{
	public PublishedFileId_t fileId;

	public CSteamID creator;

	public string title;

	public string description;

	public HashSet<string> tags;

	public EItemState status;

	public string installPath;
}
