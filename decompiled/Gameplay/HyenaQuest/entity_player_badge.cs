using System;
using SaintsField;
using TMPro;
using UnityEngine;
using ZLinq;
using ZLinq.Linq;

namespace HyenaQuest;

public class entity_player_badge : MonoBehaviour
{
	private static readonly int EmissionMap = Shader.PropertyToID("_EmissionMap");

	public TextMeshPro nameTag;

	public TextMeshPro deathStatsText;

	public TextMeshPro deliveryStatsText;

	public TextMeshPro scrapStatsText;

	public TextMeshPro rankText;

	public SpriteRenderer healthBar;

	public MeshRenderer playerIconRenderer;

	public SaintsDictionary<STEAM_ACHIEVEMENTS, GameObject> playerBadges = new SaintsDictionary<STEAM_ACHIEVEMENTS, GameObject>();

	private entity_player _owner;

	private Vector3 _startPos;

	private int _id;

	private int _scrap;

	private int _deliveries;

	private int _deaths;

	public void Awake()
	{
		if (!nameTag)
		{
			throw new UnityException("Invalid entity_player, missing name");
		}
		if (!deathStatsText)
		{
			throw new UnityException("Invalid entity_player, missing death stats text");
		}
		if (!deliveryStatsText)
		{
			throw new UnityException("Invalid entity_player, missing delivery stats text");
		}
		if (!scrapStatsText)
		{
			throw new UnityException("Invalid entity_player, missing scrap stats text");
		}
		if (!rankText)
		{
			throw new UnityException("Invalid entity_player, missing rank text");
		}
		if (!healthBar)
		{
			throw new UnityException("Invalid entity_player, missing health sprite renderer");
		}
		if (!playerIconRenderer)
		{
			throw new UnityException("Invalid entity_player, missing playerIconRenderer object");
		}
		_startPos = healthBar.transform.localPosition;
	}

	public void SetPlayerID(int id)
	{
		_id = id;
		MonoController<LocalizationController>.Instance?.Cleanup($"stats-rank-{_id}");
	}

	public void SetPlayerName(string plyName, bool dev)
	{
		if (!nameTag)
		{
			throw new UnityException("Invalid entity_player, missing name tag");
		}
		nameTag.text = plyName;
		nameTag.color = (dev ? Color.orange : Color.white);
	}

	public void SetPlayerIcon(Texture2D avatar)
	{
		if (!playerIconRenderer)
		{
			throw new UnityException("Invalid entity_player, missing playerIconRenderer object");
		}
		if ((bool)avatar)
		{
			playerIconRenderer.material.mainTexture = avatar;
			playerIconRenderer.material.SetTexture(EmissionMap, avatar);
		}
	}

	public void SetDeathStats(int deaths)
	{
		if (!deathStatsText)
		{
			throw new UnityException("Invalid entity_player, missing death stats text");
		}
		_deaths = deaths;
		deathStatsText.text = deaths.ToString().PadLeft(5, '0');
	}

	public void SetDeliveryStats(int deliveries)
	{
		if (!deliveryStatsText)
		{
			throw new UnityException("Invalid entity_player, missing delivery stats text");
		}
		_deliveries = deliveries;
		deliveryStatsText.text = deliveries.ToString().PadLeft(5, '0');
		UpdateRank();
	}

	public void SetScrapStats(int scrap)
	{
		if (!scrapStatsText)
		{
			throw new UnityException("Invalid entity_player, missing scrap stats text");
		}
		_scrap = scrap;
		scrapStatsText.text = scrap.ToString().PadLeft(9, '0');
		UpdateRank();
	}

	public void SetOwner(entity_player owner)
	{
		if (!owner)
		{
			throw new UnityException("Invalid owner object");
		}
		_owner = owner;
	}

	public void SetBadges(int badgeData)
	{
		int num = 0;
		foreach (STEAM_ACHIEVEMENTS value2 in Enum.GetValues(typeof(STEAM_ACHIEVEMENTS)))
		{
			if (playerBadges.TryGetValue(value2, out var value) && (bool)value)
			{
				bool active = value2 switch
				{
					STEAM_ACHIEVEMENTS.ACHIEVEMENT_DEV => _owner.IsDeveloperOrFriend(), 
					STEAM_ACHIEVEMENTS.ACHIEVEMENT_KOFI => false, 
					_ => (badgeData & (1 << num)) != 0, 
				};
				value.SetActive(active);
			}
			num++;
		}
	}

	public void SetHealth(byte health)
	{
		if (!healthBar)
		{
			throw new UnityException("Invalid entity_player, missing health sprite renderer");
		}
		float num = (float)(int)health / 100f;
		healthBar.transform.localScale = new Vector3(0.9f, num, 1f);
		healthBar.transform.localPosition = new Vector3(_startPos.x, _startPos.y + num * 0.5f, _startPos.z);
	}

	public void OnDestroy()
	{
		MonoController<LocalizationController>.Instance?.Cleanup($"stats-rank-{_id}");
	}

	private void UpdateRank()
	{
		if (!MonoController<LocalizationController>.Instance)
		{
			throw new UnityException("Missing LocalizationController");
		}
		MonoController<LocalizationController>.Instance.Cleanup($"stats-rank-{_id}");
		MonoController<LocalizationController>.Instance.Get($"stats-rank-{_id}", "ingame.stats.ranks", delegate(string s)
		{
			if ((bool)rankText)
			{
				int num = _deliveries * 100 + _scrap / 5;
				ValueEnumerable<FromArray<string>, string> source = s.Split(new string[1] { "<##>" }, StringSplitOptions.None).AsValueEnumerable();
				TextMeshPro textMeshPro = rankText;
				string text = ((num < 21000) ? ((num < 3000) ? ((num < 750) ? (source.ElementAtOrDefault(0) ?? "UNPAID INTERN") : ((num >= 1500) ? (source.ElementAtOrDefault(2) ?? "EXPENDABLE ASSET") : (source.ElementAtOrDefault(1) ?? "INTERN"))) : ((num < 9000) ? ((num >= 5500) ? (source.ElementAtOrDefault(4) ?? "EMPLOYEEN") : (source.ElementAtOrDefault(3) ?? "JUNIOR SCAVENGER")) : ((num >= 14000) ? (source.ElementAtOrDefault(6) ?? "DELIVERY CLERK") : (source.ElementAtOrDefault(5) ?? "ASSOCIATE HYENA")))) : ((num < 100000) ? ((num < 48000) ? ((num >= 32000) ? (source.ElementAtOrDefault(8) ?? "MIDDLE MANAGEYENA") : (source.ElementAtOrDefault(7) ?? "LOGISTICS SPECIALIST")) : ((num >= 70000) ? (source.ElementAtOrDefault(10) ?? "PACK DIRECTOR") : (source.ElementAtOrDefault(9) ?? "SPOTTED SUPERVISOR"))) : ((num < 220000) ? ((num >= 150000) ? (source.ElementAtOrDefault(12) ?? "EXECUTIVE PREDATOR") : (source.ElementAtOrDefault(11) ?? "VP OF CARCASS")) : ((num >= 320000) ? (source.ElementAtOrDefault(14) ?? "ALPHA HYENA") : (source.ElementAtOrDefault(13) ?? "BOARD HYENA")))));
				textMeshPro.text = text;
			}
		});
	}
}
