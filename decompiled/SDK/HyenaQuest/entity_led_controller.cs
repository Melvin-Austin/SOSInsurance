using System.Collections.Generic;
using UnityEngine;

namespace HyenaQuest;

public class entity_led_controller : MonoBehaviour
{
	[Header("LEDS")]
	public List<entity_led> LEDS = new List<entity_led>();

	public void SetActive(int active)
	{
		for (int i = 0; i < LEDS.Count; i++)
		{
			LEDS[i].SetActive(i < active);
		}
	}
}
