using System;
using FailCake;
using UnityEngine;

namespace HyenaQuest;

public class entity_loading_screen : MonoBehaviour
{
	public GameObject loadingScreen;

	private util_timer _loadingTimer;

	public void Awake()
	{
		if (!loadingScreen)
		{
			throw new UnityException("Missing loadingScreen");
		}
		loadingScreen.SetActive(value: false);
		CoreController.WaitFor(delegate(IngameController ingameCtrl)
		{
			ingameCtrl.OnStatusUpdated += new Action<INGAME_STATUS, bool>(OnIngameStatusUpdated);
		});
	}

	public void OnDestroy()
	{
		_loadingTimer?.Stop();
		if ((bool)NetController<IngameController>.Instance)
		{
			NetController<IngameController>.Instance.OnStatusUpdated -= new Action<INGAME_STATUS, bool>(OnIngameStatusUpdated);
		}
	}

	private void OnIngameStatusUpdated(INGAME_STATUS status, bool server)
	{
		if (server)
		{
			return;
		}
		_loadingTimer?.Stop();
		if (status == INGAME_STATUS.GENERATE)
		{
			_loadingTimer = util_timer.Simple(4.4f, delegate
			{
				if ((bool)loadingScreen)
				{
					loadingScreen.SetActive(value: true);
				}
			});
		}
		else
		{
			loadingScreen.SetActive(value: false);
		}
	}
}
