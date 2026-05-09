using Opsive.BehaviorDesigner.Runtime.Tasks.Actions;
using Opsive.GraphDesigner.Runtime.Variables;
using UnityEngine.Scripting;

namespace HyenaQuest;

[Preserve]
public class SetAISpeed : Action
{
	public SharedVariable<float> Speed = 1f;

	protected entity_monster_ai _ai;

	public override void OnStart()
	{
		_ai = GetComponent<entity_monster_ai>();
		if ((bool)_ai)
		{
			_ai.SetSpeed(Speed.Value);
		}
	}
}
