using System;
using System.Text;
using TMPro;
using UnityEngine;

namespace HyenaQuest;

public class entity_world_details : MonoBehaviour
{
	public TextMeshPro worldInfoText;

	public TextMeshPro dangerInfoText;

	private bool _inLocation;

	public void Awake()
	{
		if (!worldInfoText)
		{
			throw new UnityException("Missing worldInfoText component");
		}
		if (!dangerInfoText)
		{
			throw new UnityException("Missing dangerInfoText component");
		}
		CoreController.WaitFor(delegate(ScrapController scrapCtrl)
		{
			scrapCtrl.OnWorldScrapUpdate += new Action<int, bool>(OnWorldScrapUpdate);
			scrapCtrl.OnShipScrapUpdate += new Action<int, bool>(OnWorldScrapUpdate);
		});
		CoreController.WaitFor(delegate(MapController mapCtrl)
		{
			mapCtrl.OnMapGenerated += new Action<bool>(OnMapGenerated);
			mapCtrl.OnMapCleared += new Action<bool>(OnMapCleared);
		});
		CoreController.WaitFor(delegate(ContractController contCtrl)
		{
			contCtrl.OnContractUpdate += new Action<Contract, bool>(OnContractUpdate);
		});
		CoreController.WaitFor(delegate(IngameController ingameCtrl)
		{
			ingameCtrl.OnStatusUpdated += new Action<INGAME_STATUS, bool>(OnStatusUpdated);
		});
	}

	public void OnDestroy()
	{
		if ((bool)NetController<ScrapController>.Instance)
		{
			NetController<ScrapController>.Instance.OnWorldScrapUpdate -= new Action<int, bool>(OnWorldScrapUpdate);
			NetController<ScrapController>.Instance.OnShipScrapUpdate -= new Action<int, bool>(OnWorldScrapUpdate);
		}
		if ((bool)NetController<MapController>.Instance)
		{
			NetController<MapController>.Instance.OnMapGenerated -= new Action<bool>(OnMapGenerated);
			NetController<MapController>.Instance.OnMapCleared -= new Action<bool>(OnMapCleared);
		}
		if ((bool)NetController<ContractController>.Instance)
		{
			NetController<ContractController>.Instance.OnContractUpdate -= new Action<Contract, bool>(OnContractUpdate);
		}
		if ((bool)NetController<IngameController>.Instance)
		{
			NetController<IngameController>.Instance.OnStatusUpdated -= new Action<INGAME_STATUS, bool>(OnStatusUpdated);
		}
		if ((bool)MonoController<LocalizationController>.Instance)
		{
			MonoController<LocalizationController>.Instance.Cleanup("world.info.level");
			MonoController<LocalizationController>.Instance.Cleanup("world.info");
		}
	}

	private void OnMapCleared(bool server)
	{
		_inLocation = false;
		UpdateText();
	}

	private void OnMapGenerated(bool server)
	{
		_inLocation = true;
		UpdateText();
	}

	private void OnWorldScrapUpdate(int scrap, bool server)
	{
		UpdateText();
	}

	private void OnStatusUpdated(INGAME_STATUS status, bool server)
	{
		if ((bool)dangerInfoText)
		{
			dangerInfoText.enabled = status == INGAME_STATUS.WAITING_PLAY_CONFIRMATION || status == INGAME_STATUS.PLAYING;
		}
	}

	private void UpdateText()
	{
		if (!MonoController<LocalizationController>.Instance)
		{
			throw new UnityException("Missing LocalizationController");
		}
		if (!worldInfoText)
		{
			throw new UnityException("worldInfoText is not set");
		}
		MonoController<LocalizationController>.Instance.Get("world.info", "ingame.ui.hints.scrap", delegate(string t)
		{
			StringBuilder stringBuilder = new StringBuilder();
			if (_inLocation)
			{
				int worldScrap = NetController<ScrapController>.Instance.GetWorldScrap();
				stringBuilder.AppendLine("<align=\"center\">" + t);
				stringBuilder.AppendLine($"<size=80%>{worldScrap}</size>");
				stringBuilder.AppendLine("");
				stringBuilder.AppendLine(MonoController<LocalizationController>.Instance.Get("ingame.world.info.addresses") ?? "");
				stringBuilder.AppendLine($"<size=80%>{NetController<DeliveryController>.Instance?.GetTotalDeliverySpots()}</size>");
			}
			else
			{
				stringBuilder.AppendLine(MonoController<LocalizationController>.Instance.Get("ingame.world.info.no-location"));
			}
			worldInfoText.text = stringBuilder.ToString();
		});
	}

	private void OnContractUpdate(Contract contract, bool server)
	{
		if (!MonoController<LocalizationController>.Instance)
		{
			throw new UnityException("Missing LocalizationController");
		}
		if (!dangerInfoText)
		{
			throw new UnityException("dangerInfoText is not set");
		}
		string key = NetController<ContractController>.Instance.GetDangerLevel() switch
		{
			0 => "ingame.world.danger.safe", 
			1 => "ingame.world.danger.minor", 
			3 => "ingame.world.danger.major", 
			_ => "ingame.world.danger.extreme", 
		};
		MonoController<LocalizationController>.Instance.Get("world.info.level", key, delegate(string v)
		{
			if ((bool)dangerInfoText)
			{
				dangerInfoText.text = v;
			}
		});
	}
}
