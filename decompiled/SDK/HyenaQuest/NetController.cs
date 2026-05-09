using Unity.Netcode;

namespace HyenaQuest;

public abstract class NetController<T> : NetworkBehaviour, IController where T : NetController<T>
{
	public static T Instance { get; private set; }

	public void Awake()
	{
		Instance = (T)this;
		CoreController.Register(this);
	}

	public override void OnDestroy()
	{
		if (Instance == this)
		{
			CoreController.Unregister<T>();
			Instance = null;
		}
		base.OnDestroy();
	}

	protected override void __initializeVariables()
	{
		base.__initializeVariables();
	}

	protected override void __initializeRpcs()
	{
		base.__initializeRpcs();
	}

	protected internal override string __getTypeName()
	{
		return "NetController`1";
	}
}
