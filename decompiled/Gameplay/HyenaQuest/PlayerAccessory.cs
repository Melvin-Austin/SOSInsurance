using System;
using UnityEngine;
using UnityEngine.Scripting;

namespace HyenaQuest;

[Serializable]
[Preserve]
[CreateAssetMenu(menuName = "HyenaQuest/Accessory")]
public class PlayerAccessory : ScriptableObject
{
	public ACCESSORY_TYPE type;

	public STEAM_ACHIEVEMENTS achievement;

	public bool hideGoogles;

	public bool hideHair;

	public bool hideHat;

	public GameObject obj;

	public Sprite preview;
}
