using System;
using SaintsField;
using UnityEngine;

namespace HyenaQuest;

[DisallowMultipleComponent]
[DefaultExecutionOrder(-70)]
public class CurseScreenController : MonoController<CurseScreenController>
{
	[Header("UI")]
	public SaintsDictionary<CURSE_TYPE, GameObject> curseScreens = new SaintsDictionary<CURSE_TYPE, GameObject>();

	public new void Awake()
	{
		base.Awake();
		PlayerController.OnLocalPlayerSet += new Action(OnLocalPlayerSet);
	}

	private void OnLocalPlayerSet()
	{
		if (!PlayerController.LOCAL)
		{
			throw new UnityException("PlayerController.LOCAL is null!");
		}
		PlayerController.LOCAL.OnPlayerCurse += new Action<CURSE_TYPE, bool, bool>(OnPlayerCurse);
	}

	private void OnPlayerCurse(CURSE_TYPE type, bool active, bool server)
	{
		if (server || !PlayerController.LOCAL || !curseScreens.TryGetValue(type, out var value) || value.activeSelf == active)
		{
			return;
		}
		if (!PlayerController.LOCAL.IsDead() && type == CURSE_TYPE.ABYSS)
		{
			NetController<ShakeController>.Instance.LocalShake(ShakeMode.SHAKE_ALL);
			if (active)
			{
				MonoController<UIController>.Instance.SetFade(fadeIn: false, 1.8f);
			}
			else
			{
				NetController<SoundController>.Instance.PlaySound("Ingame/Player/Damage/Curse/curse.ogg", new AudioData
				{
					pitch = UnityEngine.Random.Range(0.8f, 1.2f),
					volume = 0.5f
				});
				MonoController<UIController>.Instance.SetFade(fadeIn: false, 1.8f);
			}
		}
		value.SetActive(active);
	}

	public new void OnDestroy()
	{
		PlayerController.OnLocalPlayerSet -= new Action(OnLocalPlayerSet);
		if ((bool)PlayerController.LOCAL)
		{
			PlayerController.LOCAL.OnPlayerCurse -= new Action<CURSE_TYPE, bool, bool>(OnPlayerCurse);
		}
		base.OnDestroy();
	}
}
