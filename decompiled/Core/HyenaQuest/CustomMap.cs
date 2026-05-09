using System;
using UnityEngine;

namespace HyenaQuest;

[Serializable]
public struct CustomMap
{
	public AssetBundle bundle;

	public AssetBundle shaderBundle;

	public WorldSettings settings;
}
