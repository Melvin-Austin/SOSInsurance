using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;

namespace HyenaQuest;

[RequireComponent(typeof(NetworkObject))]
[RequireComponent(typeof(NetworkTransform))]
public class entity_vehicle_seat : NetworkBehaviour
{
	[Range(0f, 360f)]
	public float pitchLimit = 30f;

	[Range(0f, 360f)]
	public float yawLimit = 90f;

	public bool limit = true;

	public Vector3 seatOffset = Vector3.zero;

	private NetworkTransform _networkTransform;

	private bool _shouldBeDisabled => !limit;

	public void Awake()
	{
		_networkTransform = GetComponent<NetworkTransform>();
		if (!_networkTransform)
		{
			throw new UnityException("NetworkTransform missing on entity_vehicle_seat");
		}
	}

	public Vector3 GetSeatPos()
	{
		return base.transform.position;
	}

	public Vector3 GetSeatOffsetPos()
	{
		return seatOffset;
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
		return "entity_vehicle_seat";
	}
}
