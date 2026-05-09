using UnityEngine;

namespace HyenaQuest;

public class ui_notification_base : MonoBehaviour
{
	public GameEvent OnDestroyNotification = new GameEvent();

	protected string _id;

	public void Destroy()
	{
		OnDestroyNotification?.Invoke();
		Object.Destroy(base.gameObject);
	}

	public void OnDestroy()
	{
		OnDestroyNotification?.Invoke();
	}

	public virtual void SetID(string id)
	{
		_id = id;
	}

	public virtual string GetID()
	{
		return _id;
	}
}
