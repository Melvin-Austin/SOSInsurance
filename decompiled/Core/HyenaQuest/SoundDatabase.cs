using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Scripting;

namespace HyenaQuest;

[Serializable]
[Preserve]
public class SoundDatabase : ScriptableObject
{
	public SerializedDictionary<string, AudioClip> database = new SerializedDictionary<string, AudioClip>();
}
