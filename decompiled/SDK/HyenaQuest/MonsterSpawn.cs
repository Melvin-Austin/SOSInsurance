using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Scripting;

namespace HyenaQuest;

[Serializable]
[Preserve]
public struct MonsterSpawn
{
	[Range(0f, 1f)]
	public float chance;

	public List<GameObject> variants;
}
