using Opsive.BehaviorDesigner.Runtime.Tasks;
using Opsive.BehaviorDesigner.Runtime.Tasks.Conditionals;
using Opsive.GraphDesigner.Runtime.Variables;
using UnityEngine;
using UnityEngine.Scripting;

namespace HyenaQuest;

[Preserve]
public class FindNearProps : Conditional
{
	[RequireShared]
	[SerializeField]
	protected SharedVariable<GameObject> target;

	[SerializeField]
	protected SharedVariable<float> maxDistance;

	public override TaskStatus OnUpdate()
	{
		target.Value = GetClosestProp<entity_prop_delivery>();
		if (!target.Value)
		{
			return TaskStatus.Failure;
		}
		return TaskStatus.Success;
	}

	private GameObject GetClosestProp<T>() where T : entity_phys_breakable
	{
		T[] array = Object.FindObjectsByType<T>(FindObjectsSortMode.None);
		if (array != null && array.Length == 0)
		{
			return null;
		}
		GameObject result = null;
		float num = float.MaxValue;
		Vector3 position = transform.position;
		T[] array2 = array;
		foreach (T val in array2)
		{
			if (!val || val.IsBeingGrabbed() || val.IsLocked())
			{
				continue;
			}
			float num2 = Vector3.Distance(position, val.transform.position);
			if (!(num2 >= num))
			{
				SharedVariable<float> sharedVariable = maxDistance;
				if (sharedVariable == null || !(sharedVariable.Value > 0f) || !(num2 > maxDistance?.Value))
				{
					num = num2;
					result = val.gameObject;
				}
			}
		}
		return result;
	}
}
