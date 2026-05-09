using UnityEngine;

namespace HyenaQuest;

public class util_teleportal
{
	[Client]
	public static bool ShouldTeleport(Bounds area)
	{
		if (!SDK.MainCamera || !PlayerController.LOCAL)
		{
			return false;
		}
		Vector3 position = SDK.MainCamera.transform.position;
		if (!area.Contains(position))
		{
			return false;
		}
		entity_player_movement movement = PlayerController.LOCAL.GetMovement();
		if ((bool)movement)
		{
			return movement.IsMoving();
		}
		return false;
	}

	public static void TeleportLocalClient(Transform start, Transform end)
	{
		if ((bool)PlayerController.LOCAL)
		{
			Vector3 vector = start.InverseTransformPoint(PlayerController.LOCAL.transform.position);
			vector = Quaternion.Euler(0f, 180f, 0f) * vector;
			Quaternion quaternion = Quaternion.Inverse(start.rotation) * PlayerController.LOCAL.transform.rotation;
			quaternion = Quaternion.Euler(0f, 180f, 0f) * quaternion;
			PlayerController.LOCAL.SetPosition(end.transform.TransformPoint(vector) + start.forward * 0.85f, end.transform.rotation * quaternion);
			Physics.SyncTransforms();
		}
	}
}
