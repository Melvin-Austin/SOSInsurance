using Opsive.BehaviorDesigner.Runtime.Tasks;
using Opsive.BehaviorDesigner.Runtime.Tasks.Actions;
using Opsive.BehaviorDesigner.Runtime.Utility;
using Opsive.GraphDesigner.Runtime;
using Opsive.GraphDesigner.Runtime.Variables;
using Pathfinding;
using UnityEngine;
using UnityEngine.Scripting;

namespace HyenaQuest;

[Preserve]
[NodeIcon("f900ccca7c66371459b52036efeb8778", "cfd0e78235c50db46bc12d1751492ecf")]
public class Wander : Action
{
	[SerializeField]
	private SharedVariable<RangeFloat> radius = new RangeFloat(5f, 10f);

	[SerializeField]
	private SharedVariable<RangeFloat> wait = new RangeFloat(1f, 3f);

	[SerializeField]
	private SharedVariable<float> stuckDist = 0.5f;

	[SerializeField]
	private SharedVariable<float> stuckTime = 2f;

	private IAstarAI _ai;

	private float _nextAction;

	private float _noMoveTime;

	private Vector3 _lastPos;

	public override void OnAwake()
	{
		_ai = GetComponent<IAstarAI>();
		if (_ai == null)
		{
			throw new UnityException("Missing IAstarAI pathfinding component");
		}
	}

	public override void OnStart()
	{
		if (_ai != null)
		{
			_ai.isStopped = false;
			_nextAction = 0f;
			_noMoveTime = 0f;
			_lastPos = transform.position;
		}
	}

	public override TaskStatus OnUpdate()
	{
		if (_ai == null || !(Object)(object)AstarPath.active)
		{
			return TaskStatus.Failure;
		}
		if (!_ai.pathPending && (_ai.reachedEndOfPath || !_ai.hasPath))
		{
			if (_nextAction == 0f)
			{
				_nextAction = Time.time + 0.45f;
				_ai.SetPath(null);
				return TaskStatus.Running;
			}
			if (Time.time < _nextAction)
			{
				return TaskStatus.Running;
			}
			SetDestination();
			_nextAction = 0f;
			return TaskStatus.Running;
		}
		if (_ai.hasPath && Vector3.Distance(_lastPos, transform.position) < stuckDist.Value * Time.deltaTime)
		{
			_noMoveTime += Time.deltaTime;
			if (!(_noMoveTime > stuckTime.Value))
			{
				return TaskStatus.Running;
			}
			SetDestination();
			_noMoveTime = 0f;
			_nextAction = 0f;
		}
		else
		{
			_noMoveTime = 0f;
			_lastPos = transform.position;
		}
		return TaskStatus.Running;
	}

	public override void OnEnd()
	{
		if (_ai != null)
		{
			_ai.SetPath(null);
			_ai.isStopped = true;
		}
	}

	private void SetDestination()
	{
		Vector3 onUnitSphere = Random.onUnitSphere;
		onUnitSphere.y = 0f;
		Vector3 position = _ai.position + onUnitSphere.normalized * radius.Value.RandomValue;
		NNInfo nearest = AstarPath.active.GetNearest(position, NearestNodeConstraint.Walkable);
		if (nearest.node != null)
		{
			_ai.destination = nearest.position;
			_ai.SearchPath();
		}
		else
		{
			_nextAction = Time.time + 1f;
		}
	}
}
