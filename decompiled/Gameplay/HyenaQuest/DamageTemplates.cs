using System;
using System.Collections.Generic;
using UnityEngine;

namespace HyenaQuest;

[Serializable]
public class DamageTemplates
{
	public DamageType type;

	public List<GameObject> templates;

	public List<AudioClip> sounds;

	[Range(0f, 1f)]
	public float shakeIntensity = 0.1f;

	[Range(0f, 1f)]
	public float shakeDuration = 0.05f;

	public bool fullscreen = true;

	public Color overlayColor = new Color(0.4f, 0f, 0f, 1f);
}
