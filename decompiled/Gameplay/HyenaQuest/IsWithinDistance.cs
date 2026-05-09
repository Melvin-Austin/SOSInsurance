using Opsive.BehaviorDesigner.Runtime.Tasks;
using Opsive.BehaviorDesigner.Runtime.Tasks.Conditionals;
using Opsive.GraphDesigner.Runtime.Variables;
using UnityEngine;
using UnityEngine.Scripting;

namespace HyenaQuest;

[Preserve]
public class IsWithinDistance : Conditional
{
	[RequireShared]
	[SerializeField]
	protected SharedVariable<GameObject> target;

	[SerializeField]
	protected SharedVariable<float> distanceCheck = 10f;

	public override TaskStatus OnUpdate()
	{
		if (!target.Value)
		{
			return TaskStatus.Failure;
		}
		if (!(Vector3.Distance(target.Value.transform.position, transform.position) <= distanceCheck.Value))
		{
			return TaskStatus.Failure;
		}
		return TaskStatus.Success;
	}
}
