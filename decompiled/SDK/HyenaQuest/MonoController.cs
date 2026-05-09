using UnityEngine;

namespace HyenaQuest;

public abstract class MonoController<T> : MonoBehaviour, IController where T : MonoController<T>
{
	public static T Instance { get; private set; }

	public void Awake()
	{
		Instance = (T)this;
		CoreController.Register(this);
	}

	public void OnDestroy()
	{
		if (!(Instance != this))
		{
			CoreController.Unregister<T>();
			Instance = null;
		}
	}
}
