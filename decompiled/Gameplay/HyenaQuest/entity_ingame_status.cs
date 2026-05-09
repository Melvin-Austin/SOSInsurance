using System;
using UnityEngine;

namespace HyenaQuest;

public class entity_ingame_status : MonoBehaviour
{
	public INGAME_STATUS status;

	public GameObject content;

	public void Awake()
	{
		if (!content)
		{
			throw new UnityException("Missing content GameObject");
		}
		CoreController.WaitFor(delegate(IngameController ctrl)
		{
			ctrl.OnStatusUpdated += new Action<INGAME_STATUS, bool>(OnStatusUpdated);
		});
	}

	public void OnDestroy()
	{
		if ((bool)NetController<IngameController>.Instance)
		{
			NetController<IngameController>.Instance.OnStatusUpdated -= new Action<INGAME_STATUS, bool>(OnStatusUpdated);
		}
	}

	private void OnStatusUpdated(INGAME_STATUS newStatus, bool server)
	{
		if ((bool)content)
		{
			content.SetActive(newStatus == status);
		}
	}
}
