using System;
using System.Collections.Generic;
using Pathfinding.Util;
using UnityEngine;

namespace HyenaQuest;

[Serializable]
[Preserve]
[CreateAssetMenu(menuName = "HyenaQuest/Store Item Settings")]
public class StoreItem : ScriptableObject
{
	public string itemName;

	[Range(0f, 500f)]
	public int itemPrice;

	public GameObject itemPrefab;

	[Range(1f, 100f)]
	public int minRounds = 1;

	[Range(1f, 20f)]
	public int minPlayers = 1;

	[Range(0f, 1f)]
	public float priority;

	public StoreItemLimit limit;

	public List<Sprite> itemSprites = new List<Sprite>();
}
