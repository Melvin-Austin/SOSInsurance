using System.Collections.Generic;
using Opsive.BehaviorDesigner.Runtime.Tasks;
using Opsive.BehaviorDesigner.Runtime.Tasks.Conditionals;
using Opsive.GraphDesigner.Runtime.Variables;
using UnityEngine;
using UnityEngine.Scripting;

namespace HyenaQuest;

[Preserve]
public class IsBeingLookedAt : Conditional
{
	[SerializeField]
	protected SharedVariable<GameObject> Target;

	[SerializeField]
	protected SharedVariable<float> maxDistance = 15f;

	[SerializeField]
	protected SharedVariable<float> FOV = 75f;

	private Collider _monsterCollider;

	public override void OnAwake()
	{
		_monsterCollider = GetComponent<Collider>();
		if (!_monsterCollider)
		{
			_monsterCollider = gameObject.GetComponentInChildren<Collider>(includeInactive: true);
		}
		if (!_monsterCollider)
		{
			throw new UnityException("Missing Collider");
		}
	}

	public override TaskStatus OnUpdate()
	{
		if ((bool)Target.Value && Target.Value.TryGetComponent<entity_player>(out var component))
		{
			if (!BehaviorUtils.IsPlayerLookingAtMonster(component, transform.position, _monsterCollider, maxDistance.Value, FOV.Value))
			{
				return TaskStatus.Failure;
			}
			return TaskStatus.Success;
		}
		List<entity_player> list = MonoController<PlayerController>.Instance?.GetAlivePlayers();
		if (list == null || list.Count <= 0)
		{
			return TaskStatus.Failure;
		}
		foreach (entity_player item in list)
		{
			if (BehaviorUtils.IsPlayerLookingAtMonster(item, transform.position, _monsterCollider, maxDistance.Value, FOV.Value))
			{
				return TaskStatus.Success;
			}
		}
		return TaskStatus.Failure;
	}
}
