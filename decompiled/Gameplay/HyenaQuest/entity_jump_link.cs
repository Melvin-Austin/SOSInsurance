using System.Collections;
using Pathfinding;
using Pathfinding.ECS;
using UnityEngine;

namespace HyenaQuest;

[RequireComponent(typeof(NodeLink2))]
[DisallowMultipleComponent]
public class entity_jump_link : MonoBehaviour, IOffMeshLinkHandler, IOffMeshLinkStateMachine
{
	[Range(0f, 1f)]
	public float jumpTime = 0.5f;

	public float jumpOffset = 0.5f;

	public float jumpDelay = 0.25f;

	public bool effect = true;

	private bool _playedEffect;

	private void OnEnable()
	{
		GetComponent<NodeLink2>().onTraverseOffMeshLink = this;
	}

	private void OnDisable()
	{
		GetComponent<NodeLink2>().onTraverseOffMeshLink = null;
	}

	void IOffMeshLinkStateMachine.OnFinishTraversingOffMeshLink(AgentOffMeshLinkTraversalContext context)
	{
		_playedEffect = false;
	}

	void IOffMeshLinkStateMachine.OnAbortTraversingOffMeshLink()
	{
		_playedEffect = false;
	}

	IOffMeshLinkStateMachine IOffMeshLinkHandler.GetOffMeshLinkStateMachine(AgentOffMeshLinkTraversalContext context)
	{
		return this;
	}

	IEnumerable IOffMeshLinkStateMachine.OnTraverseOffMeshLink(AgentOffMeshLinkTraversalContext ctx)
	{
		Vector3 start = ctx.link.relativeStart;
		Vector3 end = ctx.link.relativeEnd;
		Vector3 dir = end - start;
		if (!_playedEffect && effect)
		{
			NetController<SoundController>.Instance?.Play3DSound("Ingame/Monsters/generic_jump_0.ogg", start, new AudioData
			{
				pitch = Random.Range(0.8f, 1.2f),
				volume = 0.5f,
				distance = 4f
			}, broadcast: true);
			_playedEffect = true;
		}
		ctx.DisableLocalAvoidance();
		while (!ctx.MoveTowards(start, Quaternion.LookRotation(dir, (Vector3)ctx.movementPlane.up), gravity: true, slowdown: true).reached)
		{
			yield return null;
		}
		for (float t = 0f; t < jumpDelay; t += ctx.deltaTime)
		{
			yield return null;
		}
		Vector3 bezierP0 = start;
		Vector3 bezierP1 = start + Vector3.up * jumpOffset;
		Vector3 bezierP2 = end + Vector3.up * jumpOffset;
		Vector3 bezierP3 = end;
		for (float t = 0f; t < jumpTime; t += ctx.deltaTime)
		{
			ctx.transform.Position = AstarSplines.CubicBezier(bezierP0, bezierP1, bezierP2, bezierP3, t / jumpTime);
			yield return null;
		}
	}

	string IOffMeshLinkHandler.get_name()
	{
		return base.name;
	}
}
