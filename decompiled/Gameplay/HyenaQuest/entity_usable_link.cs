using UnityEngine;

namespace HyenaQuest;

public class entity_usable_link : entity_client_usable
{
	public string url = "https://";

	[Client]
	public override void OnPlayerUse(entity_player player)
	{
		if ((bool)player && !IsLocked())
		{
			Application.OpenURL(url);
		}
	}
}
