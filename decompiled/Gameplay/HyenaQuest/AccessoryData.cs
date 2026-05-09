using System;
using UnityEngine;

namespace HyenaQuest;

[Serializable]
public struct AccessoryData
{
	public int index;

	public ACCESSORY_TYPE type;

	public bool locked;

	public Sprite preview;
}
