using System.Collections.Generic;
using UnityEngine;

namespace HyenaQuest;

public class entity_movement_crusher : entity_movement_networked
{
	private entity_kill _kill;

	private bool IsClient => NETController.Instance?.IsClient ?? false;

	private bool IsServer => NETController.Instance?.IsServer ?? false;

	public new void Awake()
	{
		base.Awake();
		_kill = obj.GetComponentInChildren<entity_kill>(includeInactive: true);
		if (!_kill)
		{
			throw new UnityException("Missing entity_kill");
		}
	}

	public void FixedUpdate()
	{
		if ((bool)obj && (bool)_kill && IsClient && points.Count == 2)
		{
			List<Point> list = points;
			Point point = list[list.Count - 1];
			Vector3 spaceRelativePosition = _networkTransform.GetSpaceRelativePosition(getCurrentState: true);
			float num = Mathf.Max(0f, Vector3.Distance(spaceRelativePosition, base.transform.TransformPoint(point.pos)));
			bool active = num < 0.6f && num > 0.1f;
			_kill.gameObject.SetActive(active);
		}
	}

	protected override void OnPointReached(Point dest)
	{
		if (IsServer)
		{
			List<Point> list = points;
			if (dest == list[list.Count - 1])
			{
				NetController<ShakeController>.Instance?.Shake3DRPC(_kill.transform.position, ShakeMode.SHAKE_ALL, 0.25f, Random.Range(0.05f, 0.15f), ShakeSoundMode.OFF, 5f);
			}
			base.OnPointReached(dest);
		}
	}
}
