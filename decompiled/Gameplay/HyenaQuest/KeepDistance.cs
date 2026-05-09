using Opsive.BehaviorDesigner.Runtime.Tasks;
using Opsive.GraphDesigner.Runtime;
using Opsive.GraphDesigner.Runtime.Variables;
using Pathfinding;
using UnityEngine;
using UnityEngine.Scripting;

namespace HyenaQuest;

[Preserve]
[NodeIcon("62cf5eb42708803499769e3858eeed2f", "f0b7471e9adc1bb4b91d015446de30b8")]
public class KeepDistance : AIBase
{
	[SerializeField]
	protected SharedVariable<float> Distance;

	[RequireShared]
	[SerializeField]
	protected SharedVariable<GameObject> Target;

	[SerializeField]
	protected float RepositionThreshold = 1.5f;

	[SerializeField]
	protected float StopThreshold = 0.9f;

	[SerializeField]
	protected float UpdateFrequency = 0.5f;

	[SerializeField]
	protected Vector2 DistanceErrorRange = new Vector2(0.85f, 1.15f);

	private float _currentErrorFactor = 1f;

	private float _lastUpdateTime;

	private bool _isRepositioning;

	public override void OnStart()
	{
		base.OnStart();
		_lastUpdateTime = 0f;
		_isRepositioning = false;
	}

	public override TaskStatus OnUpdate()
	{
		if (!Target.Value)
		{
			return TaskStatus.Failure;
		}
		float num = Vector3.Distance(transform.position, Target.Value.transform.position);
		if (_isRepositioning)
		{
			float num2 = Distance.Value / StopThreshold;
			float num3 = Distance.Value * StopThreshold;
			if (num >= num2 && num <= num3)
			{
				_isRepositioning = false;
				_ai.ResetPath();
				return TaskStatus.Success;
			}
		}
		if (Time.time >= _lastUpdateTime + UpdateFrequency || _ai.Arrived())
		{
			_lastUpdateTime = Time.time;
			bool flag = num < Distance.Value / RepositionThreshold;
			bool flag2 = num > Distance.Value * RepositionThreshold;
			if (flag || flag2)
			{
				if (!_isRepositioning)
				{
					_currentErrorFactor = Random.Range(DistanceErrorRange.x, DistanceErrorRange.y);
				}
				_isRepositioning = true;
				float num4 = Distance.Value * _currentErrorFactor;
				Vector3 vector = (flag ? (transform.position - Target.Value.transform.position).normalized : (Target.Value.transform.position - transform.position).normalized);
				Vector3 position = Target.Value.transform.position + vector * num4;
				if (!(Object)(object)AstarPath.active)
				{
					return TaskStatus.Running;
				}
				NNInfo nearest = AstarPath.active.GetNearest(position, NearestNodeConstraint.Walkable);
				GraphNode node = nearest.node;
				if (node != null && node.Walkable)
				{
					_ai.SetPath(nearest.position);
				}
				else
				{
					bool flag3 = false;
					for (int i = 0; i < 5; i++)
					{
						Vector3 vector2 = Quaternion.Euler(0f, Random.Range(-45f, 45f), 0f) * vector;
						Vector3 position2 = Target.Value.transform.position + vector2 * num4;
						NNInfo nearest2 = AstarPath.active.GetNearest(position2, NearestNodeConstraint.Walkable);
						node = nearest2.node;
						if (node != null && node.Walkable)
						{
							_ai.SetPath(nearest2.position);
							flag3 = true;
							break;
						}
					}
					if (!flag3)
					{
						return TaskStatus.Failure;
					}
				}
			}
			else if (_isRepositioning && _ai.Arrived())
			{
				_isRepositioning = false;
				_ai.ResetPath();
			}
		}
		return TaskStatus.Running;
	}
}
