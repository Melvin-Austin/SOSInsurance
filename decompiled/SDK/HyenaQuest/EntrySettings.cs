using System;
using UnityEngine;
using UnityEngine.Scripting;

namespace HyenaQuest;

[Serializable]
[Preserve]
public struct EntrySettings
{
	public GameObject template;

	public EntryRenderSettings settings;
}
