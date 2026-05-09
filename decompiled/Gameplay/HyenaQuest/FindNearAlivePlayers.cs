using System.Collections.Generic;
using Opsive.BehaviorDesigner.Runtime.Tasks;
using Opsive.BehaviorDesigner.Runtime.Tasks.Conditionals;
using Opsive.GraphDesigner.Runtime.Variables;
using UnityEngine;
using UnityEngine.Scripting;

namespace HyenaQuest;

[Preserve]
public class FindNearAlivePlayers : Conditional
{
	[RequireShared]
	[SerializeField]
	protected SharedVariable<entity_player> target;

	[SerializeField]
	protected SharedVariable<bool> includeShip = false;

	public override TaskStatus OnUpdate()
	{
		target.Value = null;
		List<entity_player> list = MonoController<PlayerController>.Instance?.GetAlivePlayers();
		if (list != null && list.Count > 0)
		{
			target.Value = SortPlayersByDistance(list);
		}
		if (!target.Value)
		{
			return TaskStatus.Failure;
		}
		return TaskStatus.Success;
	}

	private entity_player SortPlayersByDistance(List<entity_player> alivePlayers)
	{
		entity_player result = null;
		float num = float.MaxValue;
		foreach (entity_player alivePlayer in alivePlayers)
		{
			if ((bool)alivePlayer && (includeShip.Value || NetController<IngameController>.Instance.IsShipArea(alivePlayer)))
			{
				float num2 = Vector3.Distance(transform.position, alivePlayer.transform.position);
				if (num2 < num)
				{
					num = num2;
					result = alivePlayer;
				}
			}
		}
		return result;
	}
}
