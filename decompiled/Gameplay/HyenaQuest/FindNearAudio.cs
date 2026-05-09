using System.Collections.Generic;
using System.Linq;
using Opsive.BehaviorDesigner.Runtime.Tasks;
using Opsive.BehaviorDesigner.Runtime.Tasks.Conditionals;
using Opsive.GraphDesigner.Runtime.Variables;
using UnityEngine;
using UnityEngine.Scripting;

namespace HyenaQuest;

[Preserve]
public class FindNearAudio : Conditional
{
	[RequireShared]
	[SerializeField]
	protected SharedVariable<GameObject> target;

	[SerializeField]
	protected SharedVariable<float> range;

	[SerializeField]
	protected SharedVariable<float> detectionVolume;

	public override TaskStatus OnUpdate()
	{
		target.Value = null;
		entity_sound[] array = Object.FindObjectsByType<entity_sound>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
		if (array == null || array.Length == 0)
		{
			return TaskStatus.Failure;
		}
		List<(GameObject, float)> list = new List<(GameObject, float)>();
		entity_sound[] array2 = array;
		for (int i = 0; i < array2.Length; i++)
		{
			AudioSource component = array2[i].GetComponent<AudioSource>();
			if ((bool)component && component.isActiveAndEnabled && component.gameObject.activeInHierarchy && !component.transform.IsChildOf(transform) && component.isPlaying && Mathf.Approximately(component.spatialBlend, 1f) && !(component.volume <= detectionVolume.Value))
			{
				float num = Vector3.Distance(transform.position, component.transform.position);
				if (!(num > range.Value))
				{
					list.Add((component.gameObject, num));
				}
			}
		}
		if (list.Count <= 0)
		{
			return TaskStatus.Failure;
		}
		(GameObject, float) tuple = list.OrderBy<(GameObject, float), float>(((GameObject obj, float distance) s) => s.distance).First();
		target.Value = tuple.Item1;
		return TaskStatus.Success;
	}
}
