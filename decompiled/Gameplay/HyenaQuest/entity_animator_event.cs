using UnityEngine;

namespace HyenaQuest;

public class entity_animator_event : MonoBehaviour
{
	public GameEvent<string> OnAnimationEvent = new GameEvent<string>();

	public void SendEvent(string eventName)
	{
		OnAnimationEvent?.Invoke(eventName);
	}
}
