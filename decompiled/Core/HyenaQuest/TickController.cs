using FailCake;
using UnityEngine;

namespace HyenaQuest;

[DisallowMultipleComponent]
[DefaultExecutionOrder(-95)]
public class TickController : MonoBehaviour
{
	public void OnDestroy()
	{
		util_timer.Clear();
		util_fade_timer.Clear();
		util_collider.Clear();
	}

	public void FixedUpdate()
	{
		util_timer.Tick();
		util_fade_timer.Tick(Time.fixedDeltaTime);
		util_collider.Tick();
	}
}
