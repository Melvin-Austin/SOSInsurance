using Opsive.BehaviorDesigner.Runtime.Tasks;
using Opsive.BehaviorDesigner.Runtime.Tasks.Conditionals;
using Opsive.GraphDesigner.Runtime.Variables;
using UnityEngine;
using UnityEngine.Scripting;

namespace HyenaQuest;

[Preserve]
public class IsPlayerAlive : Conditional
{
	[SerializeField]
	protected SharedVariable<entity_player> targetPly;

	[SerializeField]
	protected SharedVariable<GameObject> target;

	public override TaskStatus OnUpdate()
	{
		if ((bool)targetPly.Value)
		{
			if (!targetPly.Value.IsDead())
			{
				return TaskStatus.Success;
			}
			return TaskStatus.Failure;
		}
		if ((bool)target.Value)
		{
			entity_player component = target.Value.GetComponent<entity_player>();
			if (!component)
			{
				return TaskStatus.Failure;
			}
			if (!component.IsDead())
			{
				return TaskStatus.Success;
			}
			return TaskStatus.Failure;
		}
		return TaskStatus.Failure;
	}
}
