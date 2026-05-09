using Opsive.BehaviorDesigner.Runtime.Tasks.Actions;
using Opsive.GraphDesigner.Runtime.Variables;
using UnityEngine;
using UnityEngine.Scripting;

namespace HyenaQuest;

[Preserve]
public class AIBase : Action
{
	public SharedVariable<bool> StopOnEventEnd = true;

	protected entity_monster_ai _ai;

	public override void OnStart()
	{
		_ai = GetComponent<entity_monster_ai>();
		if (!_ai)
		{
			throw new UnityException("AIBase requires entity_monster_ai component");
		}
	}

	public override void OnEnd()
	{
		if ((bool)_ai && StopOnEventEnd.Value)
		{
			_ai.ResetPath();
		}
	}
}
