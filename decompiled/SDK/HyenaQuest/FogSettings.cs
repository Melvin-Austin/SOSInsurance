using System;
using UnityEngine;
using UnityEngine.Scripting;

namespace HyenaQuest;

[Serializable]
[Preserve]
public struct FogSettings
{
	public Color color;

	[Range(0f, 1f)]
	public float density;
}
