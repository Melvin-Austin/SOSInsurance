using System;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;

namespace HyenaQuest;

[RequireComponent(typeof(NetworkObject))]
public class entity_movement_networked : entity_movement
{
	protected NetworkTransform _networkTransform;

	protected NetworkObject _networkObject;

	private bool IsServer
	{
		get
		{
			if ((bool)NetworkManager.Singleton)
			{
				return NetworkManager.Singleton.IsServer;
			}
			return false;
		}
	}

	public new void Awake()
	{
		if (!obj)
		{
			throw new UnityException("Missing game object");
		}
		if (points.Count < 2)
		{
			throw new UnityException("At least 2 points are needed");
		}
		_networkTransform = obj.GetComponent<NetworkTransform>();
		if (!_networkTransform)
		{
			throw new UnityException("Missing NetworkTransform on target object");
		}
		_networkObject = GetComponent<NetworkObject>();
		if (!_networkObject)
		{
			throw new UnityException("Missing NetworkObject on target object");
		}
		if (IsServer && startActive)
		{
			StartMovement();
		}
	}

	public new void Update()
	{
		if (IsServer)
		{
			base.Update();
		}
	}

	[Server]
	public override void StartMovement(bool reset = true, Action onComplete = null)
	{
		if (!IsServer)
		{
			throw new UnityException("Server only");
		}
		base.StartMovement(reset, onComplete);
	}

	[Server]
	public override void StopMovement()
	{
		if (!IsServer)
		{
			throw new UnityException("Server only");
		}
		base.StopMovement();
	}

	[Server]
	protected override void ResetMovement()
	{
		NetworkObject networkObject = _networkObject;
		if ((object)networkObject != null && networkObject.IsSpawned && !IsServer)
		{
			throw new UnityException("Server only");
		}
		base.ResetMovement();
	}

	protected override void ForcePosition(Point point)
	{
		if (!_networkTransform || !_networkTransform.IsSpawned)
		{
			base.ForcePosition(point);
		}
		else
		{
			_networkTransform.SetState(base.transform.TransformPoint(point.pos), base.transform.rotation * Quaternion.Euler(point.angle), obj.transform.localScale, teleportDisabled: false);
		}
	}

	protected override void OnPointReached(Point dest)
	{
		if (IsServer && (bool)_networkTransform && _networkTransform.IsSpawned)
		{
			_networkTransform.SetState(base.transform.TransformPoint(dest.pos), base.transform.rotation * Quaternion.Euler(dest.angle), obj.transform.localScale, teleportDisabled: false);
		}
	}

	protected override bool ShouldBroadcastSound()
	{
		return true;
	}
}
