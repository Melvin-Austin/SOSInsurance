using System.Collections.Generic;
using UnityEngine;

namespace HyenaQuest;

public class entity_window_light : MonoBehaviour
{
	public List<Light> lights = new List<Light>();

	public bool isOutside;

	public void Awake()
	{
		if (lights.Count == 0)
		{
			throw new UnityException("Missing 'lights' references");
		}
	}

	public void SetColor(Color cl, float outsideIntensity)
	{
		foreach (Light light in lights)
		{
			if ((bool)light)
			{
				light.color = cl;
				if (isOutside)
				{
					light.intensity = outsideIntensity;
				}
			}
		}
	}
}
