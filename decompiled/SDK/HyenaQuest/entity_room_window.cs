using System;
using System.Collections.Generic;
using UnityEngine;

namespace HyenaQuest;

public class entity_room_window : MonoBehaviour
{
	public List<GameObject> openLayers = new List<GameObject>();

	public List<GameObject> closedLayers = new List<GameObject>();

	public void SetStatus(string seed, bool forceClosed = false)
	{
		List<GameObject> list = closedLayers;
		if (!forceClosed)
		{
			list.AddRange(openLayers);
		}
		byte b = (byte)new System.Random(int.Parse(seed)).Next(0, list.Count);
		for (int i = 0; i < list.Count; i++)
		{
			if ((bool)list[i])
			{
				if (i == b)
				{
					list[i].SetActive(value: true);
				}
				else
				{
					UnityEngine.Object.Destroy(list[i]);
				}
			}
		}
	}
}
