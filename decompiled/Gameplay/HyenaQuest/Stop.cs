using Opsive.BehaviorDesigner.Runtime.Tasks.Actions;
using UnityEngine.Scripting;

namespace HyenaQuest;

[Preserve]
public class Stop : Action
{
	private entity_monster_ai _ai;

	public override void OnStart()
	{
		_ai = GetComponent<entity_monster_ai>();
		if ((bool)_ai)
		{
			_ai.ResetPath();
		}
	}
}
