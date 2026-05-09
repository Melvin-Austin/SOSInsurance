using System;
using UnityEngine;

namespace HyenaQuest;

[Serializable]
public struct RoomLayer
{
	public GameObject layer;

	[Range(0f, 1f)]
	public float weight;

	[Range(1f, 255f)]
	public int round;
}
