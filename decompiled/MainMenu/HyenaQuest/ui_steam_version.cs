using Steamworks;
using UnityEngine;
using UnityEngine.Localization.Components;
using UnityEngine.Localization.SmartFormat.PersistentVariables;

namespace HyenaQuest;

public class ui_steam_version : MonoBehaviour
{
	private void Awake()
	{
		LocalizeStringEvent component = GetComponent<LocalizeStringEvent>();
		if (!component)
		{
			throw new UnityException("Missing LocalizeStringEvent component");
		}
		string text = "????";
		if (SteamworksController.IsSteamRunning)
		{
			text = SteamApps.GetAppBuildId().ToString();
			if (SteamApps.GetCurrentBetaName(out var pchName, 128))
			{
				text = text + " - " + pchName;
			}
		}
		((StringVariable)component.StringReference["version"]).Value = text;
	}
}
