using System;
using UnityEngine;

namespace HyenaQuest;

[Serializable]
public struct InstancedAccessory
{
	public ACCESSORY_TYPE type;

	public SkinnedMeshRenderer renderer;

	public PlayerAccessory data;
}
