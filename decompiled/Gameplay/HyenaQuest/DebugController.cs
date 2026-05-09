using UnityEngine;

namespace HyenaQuest;

[DefaultExecutionOrder(-97)]
[DisallowMultipleComponent]
public class DebugController : MonoController<DebugController>
{
	public void Start()
	{
		Object.DestroyImmediate(this);
	}
}
