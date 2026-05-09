using System;
using System.Net.Http;
using System.Text.RegularExpressions;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;

namespace HyenaQuest;

public class ui_steam_news : MonoBehaviour
{
	public TextMeshProUGUI titleText;

	public TextMeshProUGUI contentText;

	public void Awake()
	{
		if (!titleText)
		{
			throw new UnityException("Missing TextMeshProUGUI component for titleText");
		}
		if (!contentText)
		{
			throw new UnityException("Missing TextMeshProUGUI component for contentText");
		}
		GatherSteamNews();
	}

	private void GatherSteamNews()
	{
		UnityWebRequest www = UnityWebRequest.Get($"https://api.steampowered.com/ISteamNews/GetNewsForApp/v2/?appid=3376480&feeds=steam_community_announcements&cachebuster={DateTimeOffset.UtcNow.ToUnixTimeSeconds()}");
		www.SendWebRequest().completed += delegate
		{
			try
			{
				if (www.result != UnityWebRequest.Result.Success)
				{
					throw new HttpRequestException("Failed to get latest game Steam news: " + www.error);
				}
				string text = www.downloadHandler.text;
				if (string.IsNullOrEmpty(text))
				{
					throw new HttpRequestException("Received empty response from Steam API");
				}
				SteamNewsResponse steamNewsResponse = JsonUtility.FromJson<SteamNewsResponse>(text);
				if (steamNewsResponse == null)
				{
					throw new HttpRequestException("Failed to parse Steam news data");
				}
				if (steamNewsResponse.appnews.newsitems == null || steamNewsResponse.appnews.newsitems.Length == 0)
				{
					throw new HttpRequestException("No news items found or failed to parse news data");
				}
				SteamAppNews.SteamNewsItem steamNewsItem = steamNewsResponse.appnews.newsitems[0];
				titleText.text = steamNewsItem.title;
				string[] array = steamNewsItem.contents.Split("[hr][/hr]");
				if (array == null || array.Length == 0)
				{
					throw new UnityException("Failed to split news content into summary");
				}
				string text2 = Regex.Replace(array[0].Replace("[hr]", "").Replace("[/hr]", "").Replace("[/p]", "\n\n")
					.Replace("[b]", "<wave a=0.1>")
					.Replace("[/b]", "</wave>"), "\\[p[^\\]]*\\]", "");
				contentText.text = text2;
			}
			catch (Exception ex)
			{
				Debug.LogError(ex.Message ?? "");
				contentText.text = "FAILED TO GET LATEST GAME NEWS";
			}
			finally
			{
				www.Dispose();
			}
		};
	}
}
