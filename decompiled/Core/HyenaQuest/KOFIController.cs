using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using Newtonsoft.Json.Linq;
using UnityEngine;
using UnityEngine.Networking;

namespace HyenaQuest;

[DefaultExecutionOrder(-120)]
[DisallowMultipleComponent]
public class KOFIController : MonoController<KOFIController>
{
	public const int MAX_NAME_LENGTH = 20;

	public GameEvent OnPatronsLoaded = new GameEvent();

	private static readonly Dictionary<string, TIERS> _tierNameMapping = new Dictionary<string, TIERS> { 
	{
		"Merchant Guild",
		TIERS.MERCHANT_GUILD
	} };

	private readonly Dictionary<TIERS, string> _tierColors = new Dictionary<TIERS, string>
	{
		{
			TIERS.MERCHANT_GUILD,
			"#f2a191"
		},
		{
			TIERS.TESTERS,
			"#91f2ad"
		}
	};

	private readonly Dictionary<TIERS, List<KofiMember>> _tierMembers = new Dictionary<TIERS, List<KofiMember>>
	{
		{
			TIERS.MERCHANT_GUILD,
			new List<KofiMember>()
		},
		{
			TIERS.TESTERS,
			new List<KofiMember>
			{
				new KofiMember("D3lta", "188733543793295360"),
				new KofiMember("Revolving DCON", "352262029903396865"),
				new KofiMember("Neo Te Aika", "97137117393612800"),
				new KofiMember("Altaris", "787971061731688448"),
				new KofiMember("AvalonJay", "151067236520427520"),
				new KofiMember("Loweys Litsman", "112937145684987904"),
				new KofiMember("JustSumToast", "335203650957737994"),
				new KofiMember("Bromvlieg", "137297118166646784"),
				new KofiMember("Sionzee", "182914681311068160"),
				new KofiMember("OneBrattyBold", "271740767796592643"),
				new KofiMember("ScottyFoxArt", "148261204140228609"),
				new KofiMember("OtterMakeGames", "1206670450311299193"),
				new KofiMember("Mastofpu", "183862977638825985"),
				new KofiMember("Kinjry", "86368821966835712"),
				new KofiMember("C.J. The Magpie", "591061974696198170")
			}
		}
	};

	public new void Awake()
	{
		base.Awake();
		FetchPatronsData();
		UnityEngine.Object.DontDestroyOnLoad(base.gameObject);
	}

	public bool HasKofiBadge(string discordUserId)
	{
		if (string.IsNullOrEmpty(discordUserId))
		{
			return false;
		}
		foreach (List<KofiMember> value in _tierMembers.Values)
		{
			if (value.Any((KofiMember m) => m.DiscordUserId == discordUserId))
			{
				return true;
			}
		}
		return false;
	}

	public string GetIngameCredits()
	{
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.AppendLine();
		stringBuilder.AppendLine();
		stringBuilder.AppendLine();
		stringBuilder.AppendLine();
		foreach (TIERS item in from t in _tierMembers.Keys
			orderby (t == TIERS.TESTERS) ? 1 : 0, (int)t
			select t)
		{
			List<KofiMember> list = _tierMembers[item];
			if (list.Count == 0)
			{
				continue;
			}
			string valueOrDefault = _tierColors.GetValueOrDefault(item, "#555");
			string text = item.ToString().Replace("_", " ");
			stringBuilder.AppendLine("<b><size=190><align=\"right\"><color=" + valueOrDefault + ">" + text + "</align></size></color></b>");
			foreach (KofiMember item2 in list)
			{
				string text2 = item2.Name?.Trim();
				if (!string.IsNullOrEmpty(text2))
				{
					if (text2.Length > 20)
					{
						text2 = text2.Substring(0, 20) + "...";
					}
					stringBuilder.AppendLine(text2);
				}
			}
			stringBuilder.AppendLine();
			stringBuilder.AppendLine();
		}
		stringBuilder.AppendLine();
		stringBuilder.AppendLine();
		stringBuilder.AppendLine(" ");
		return stringBuilder.ToString();
	}

	private void FetchPatronsData()
	{
		UnityWebRequest www = UnityWebRequest.Get("https://api.hyena.quest/api/v1/patreons");
		www.SendWebRequest().completed += delegate
		{
			try
			{
				if (www.result != UnityWebRequest.Result.Success)
				{
					throw new HttpRequestException("Failed to fetch patrons: " + www.error);
				}
				string text = www.downloadHandler.text;
				if (string.IsNullOrEmpty(text))
				{
					throw new HttpRequestException("Received empty response from patrons API");
				}
				ParseAndProcessResponse(text);
			}
			catch (Exception ex)
			{
				Debug.LogError("[KOFIController] " + ex.Message);
			}
			finally
			{
				www.Dispose();
			}
		};
	}

	private void ParseAndProcessResponse(string json)
	{
		JObject jObject = JObject.Parse(json);
		string[] credits = jObject["credits"]?.ToObject<string[]>() ?? Array.Empty<string>();
		CreatePatronsFile(credits);
		if (jObject["members"] is JObject jObject2)
		{
			foreach (JProperty item in jObject2.Properties())
			{
				string text = item.Name;
				if (!_tierNameMapping.TryGetValue(text, out var value))
				{
					Debug.LogWarning("Unknown tier: " + text);
					continue;
				}
				if (value != TIERS.TESTERS)
				{
					_tierMembers[value].Clear();
				}
				if (!(item.Value is JArray jArray))
				{
					continue;
				}
				foreach (JObject item2 in jArray)
				{
					string value2 = item2["name"]?.ToString()?.Trim();
					string discordUserId = item2["discord_userid"]?.ToString()?.Trim();
					if (!string.IsNullOrEmpty(value2))
					{
						_tierMembers[value].Add(new KofiMember(value2, discordUserId));
					}
				}
			}
		}
		OnPatronsLoaded?.Invoke();
	}

	private void CreatePatronsFile(string[] credits)
	{
		try
		{
			string path = Path.Combine(Application.dataPath, "..", "PATRONS.md");
			StringBuilder stringBuilder = new StringBuilder();
			stringBuilder.AppendLine("# Patrons");
			stringBuilder.AppendLine("## Want to be on the list? Visit https://hyena.quest/kofi and help support my projects :D");
			stringBuilder.AppendLine("---------------------------------------------------------");
			stringBuilder.AppendLine();
			stringBuilder.AppendLine();
			foreach (string text in credits)
			{
				if (!string.IsNullOrEmpty(text))
				{
					stringBuilder.AppendLine("- " + text);
				}
			}
			stringBuilder.AppendLine();
			stringBuilder.AppendLine("---------------------------------------------------------");
			File.WriteAllText(path, stringBuilder.ToString());
		}
		catch (Exception ex)
		{
			Debug.LogError("Failed to write PATRONS.md: " + ex.Message);
		}
	}
}
