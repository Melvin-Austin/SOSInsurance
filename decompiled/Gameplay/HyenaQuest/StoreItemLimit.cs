using System;
using Pathfinding.Util;
using UnityEngine;

namespace HyenaQuest;

[Serializable]
[Preserve]
public struct StoreItemLimit
{
	public string itemID;

	[Range(0f, 5f)]
	public byte limit;
}
