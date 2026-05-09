using Opsive.BehaviorDesigner.Runtime.Tasks;
using Opsive.BehaviorDesigner.Runtime.Tasks.Actions;
using Opsive.GraphDesigner.Runtime.Variables;
using UnityEngine;
using UnityEngine.Scripting;

namespace HyenaQuest;

[Preserve]
public class SetGameObjectNull : Action
{
	[RequireShared]
	[SerializeField]
	protected SharedVariable<GameObject> target;

	public override TaskStatus OnUpdate()
	{
		target.Value = null;
		return TaskStatus.Success;
	}
}
