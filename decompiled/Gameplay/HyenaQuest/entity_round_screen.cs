using System;
using TMPro;
using UnityEngine;

namespace HyenaQuest;

public class entity_round_screen : MonoBehaviour
{
	[Header("Settings")]
	public TextMeshPro roundText;

	public void Awake()
	{
		if (!roundText)
		{
			throw new UnityException("RoundText not found");
		}
		CoreController.WaitFor(delegate(IngameController ingameCtrl)
		{
			ingameCtrl.OnRoundUpdate += new Action<byte, bool>(OnRoundUpdate);
		});
	}

	public void OnDestroy()
	{
		if ((bool)NetController<IngameController>.Instance)
		{
			NetController<IngameController>.Instance.OnRoundUpdate -= new Action<byte, bool>(OnRoundUpdate);
		}
	}

	private void OnRoundUpdate(byte round, bool server)
	{
		if (!server && (bool)roundText)
		{
			roundText.text = round.ToString();
		}
	}
}
