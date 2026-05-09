using System;
using Unity.Netcode;
using UnityEngine;

namespace HyenaQuest;

public class entity_tutorial_controller : MonoBehaviour
{
	public entity_item_tv_remote tvRemote;

	public entity_button_emission replayBtn;

	public void Awake()
	{
		if (!tvRemote)
		{
			throw new UnityException("entity_tutorial_controller requires tvController");
		}
		if (!replayBtn)
		{
			throw new UnityException("entity_tutorial_controller requires replayBtn");
		}
		if ((bool)NetworkManager.Singleton && NetworkManager.Singleton.IsServer)
		{
			CoreController.WaitFor(delegate(IngameController ingameCtrl)
			{
				ingameCtrl.OnStatusUpdated += new Action<INGAME_STATUS, bool>(OnStatusUpdated);
			});
			replayBtn.OnUSE += new Action<entity_player>(RequestReplayTutorial);
		}
	}

	public void OnDestroy()
	{
		if ((bool)NetworkManager.Singleton && NetworkManager.Singleton.IsServer)
		{
			if ((bool)NetController<IngameController>.Instance)
			{
				NetController<IngameController>.Instance.OnStatusUpdated -= new Action<INGAME_STATUS, bool>(OnStatusUpdated);
			}
			if ((bool)replayBtn)
			{
				replayBtn.OnUSE -= new Action<entity_player>(RequestReplayTutorial);
			}
		}
	}

	private void OnStatusUpdated(INGAME_STATUS status, bool server)
	{
		if (server && (bool)replayBtn)
		{
			replayBtn.SetLocked(status != INGAME_STATUS.IDLE);
		}
	}

	private void RequestReplayTutorial(entity_player ply)
	{
		if ((bool)ply && (bool)tvRemote)
		{
			tvRemote.RequestVideoRPC((!ply.IsCub() && UnityEngine.Random.Range(0, 100) < 5) ? ((byte)1) : ((byte)0));
		}
	}
}
