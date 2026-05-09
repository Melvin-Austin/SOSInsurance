using System;
using UnityEngine;

namespace HyenaQuest;

public class entity_clock : MonoBehaviour
{
	public GameObject hours;

	public GameObject minutes;

	public void Awake()
	{
		if (!hours)
		{
			throw new UnityException("Missing hours gameobject");
		}
		if (!minutes)
		{
			throw new UnityException("Missing minutes gameobject");
		}
	}

	public void Update()
	{
		if ((bool)hours && (bool)minutes)
		{
			DateTime now = DateTime.Now;
			int num = now.Hour % 12;
			int minute = now.Minute;
			float z = (float)minute * 6f - 180f;
			float z2 = ((float)num + (float)minute / 60f) * 30f - 180f;
			minutes.transform.localEulerAngles = new Vector3(0f, 0f, z);
			hours.transform.localEulerAngles = new Vector3(0f, 0f, z2);
		}
	}
}
