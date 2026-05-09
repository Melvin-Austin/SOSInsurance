using Opsive.BehaviorDesigner.Runtime.Tasks;
using Opsive.BehaviorDesigner.Runtime.Tasks.Actions;
using Opsive.GraphDesigner.Runtime.Variables;
using UnityEngine;
using UnityEngine.Scripting;

namespace HyenaQuest;

[Preserve]
public class IncreaseSharedInt : Action
{
	[SerializeField]
	protected SharedVariable<int> target;

	[SerializeField]
	protected SharedVariable<int> value;

	public override TaskStatus OnUpdate()
	{
		target.Value += value.Value;
		return TaskStatus.Success;
	}
}
