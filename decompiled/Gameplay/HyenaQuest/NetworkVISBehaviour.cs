using System;
using Unity.Netcode;
using UnityEngine;

namespace HyenaQuest;

public class NetworkVISBehaviour : NetworkBehaviour
{
	[Range(1f, 20f)]
	public float visDistance = 7f;

	public override void OnNetworkSpawn()
	{
		base.OnNetworkSpawn();
		if (base.IsServer)
		{
			NetworkObject networkObject = base.NetworkObject;
			networkObject.CheckObjectVisibility = (NetworkObject.VisibilityDelegate)Delegate.Combine(networkObject.CheckObjectVisibility, new NetworkObject.VisibilityDelegate(CheckObjectVisibility));
			base.NetworkManager.NetworkTickSystem.Tick += OnNetworkTick;
		}
	}

	public override void OnNetworkDespawn()
	{
		base.OnNetworkDespawn();
		if (base.IsServer)
		{
			if ((bool)base.NetworkObject)
			{
				NetworkObject networkObject = base.NetworkObject;
				networkObject.CheckObjectVisibility = (NetworkObject.VisibilityDelegate)Delegate.Remove(networkObject.CheckObjectVisibility, new NetworkObject.VisibilityDelegate(CheckObjectVisibility));
			}
			if ((bool)base.NetworkManager)
			{
				base.NetworkManager.NetworkTickSystem.Tick -= OnNetworkTick;
			}
		}
	}

	private bool CheckObjectVisibility(ulong clientId)
	{
		if (!this || !base.IsServer || !base.IsSpawned)
		{
			return false;
		}
		NetworkClient value = default(NetworkClient);
		if (base.NetworkManager?.ConnectedClients?.TryGetValue(clientId, out value) != true || !(value?.PlayerObject))
		{
			return false;
		}
		return Vector3.Distance(value.PlayerObject.transform.position, base.transform.position) <= visDistance;
	}

	private void OnNetworkTick()
	{
		if (!this || !base.IsServer || !base.IsSpawned || !base.NetworkManager || base.NetworkManager.ConnectedClients == null)
		{
			return;
		}
		foreach (ulong connectedClientsId in base.NetworkManager.ConnectedClientsIds)
		{
			bool num = CheckObjectVisibility(connectedClientsId);
			bool flag = base.NetworkObject.IsNetworkVisibleTo(connectedClientsId);
			if (num)
			{
				if (!flag)
				{
					base.NetworkObject.NetworkShow(connectedClientsId);
				}
			}
			else if (flag)
			{
				base.NetworkObject.NetworkHide(connectedClientsId);
			}
		}
	}

	protected override void __initializeVariables()
	{
		base.__initializeVariables();
	}

	protected override void __initializeRpcs()
	{
		base.__initializeRpcs();
	}

	protected internal override string __getTypeName()
	{
		return "NetworkVISBehaviour";
	}
}
