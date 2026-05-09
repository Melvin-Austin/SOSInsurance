using Opsive.BehaviorDesigner.Runtime.Tasks;
using Opsive.BehaviorDesigner.Runtime.Tasks.Conditionals;
using Opsive.GraphDesigner.Runtime.Variables;
using UnityEngine;
using UnityEngine.Scripting;

namespace HyenaQuest;

[Preserve]
public class FindInSphere : Conditional
{
	[RequireShared]
	[SerializeField]
	protected SharedVariable<GameObject> Target;

	[SerializeField]
	protected SharedVariable<float> Distance;

	[SerializeField]
	protected SharedVariable<LayerMask> Layer;

	private readonly RaycastHit[] _hits = new RaycastHit[1];

	public override TaskStatus OnUpdate()
	{
		if (Physics.SphereCastNonAlloc(transform.position, Distance.Value, Vector3.up, _hits, Distance.Value, Layer.Value) <= 0)
		{
			Target.Value = null;
			return TaskStatus.Failure;
		}
		Target.Value = _hits[0].collider.gameObject;
		return TaskStatus.Success;
	}
}
