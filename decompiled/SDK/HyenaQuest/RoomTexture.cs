using System;
using System.Collections.Generic;
using FailCake.VMF;
using UnityEngine;

namespace HyenaQuest;

[Serializable]
public struct RoomTexture
{
	public VMFMaterial material;

	public List<Texture2D> texture;
}
