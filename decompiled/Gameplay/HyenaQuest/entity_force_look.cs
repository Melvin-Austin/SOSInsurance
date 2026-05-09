using UnityEngine;

namespace HyenaQuest;

public class entity_force_look : MonoBehaviour
{
	[Range(0.1f, 15f)]
	public float forceLookSpeed = 1f;

	[Range(1f, 50f)]
	public float forceLookDistance = 10f;

	[Range(0.1f, 15f)]
	public float lookThreshold = 1f;

	public GameEvent<bool> IsLookingAtTarget = new GameEvent<bool>();

	private int _layer;

	public void Awake()
	{
		_layer = LayerMask.GetMask("entity_ground");
	}

	public void Update()
	{
		if (!SDK.MainCamera || !base.isActiveAndEnabled || !IsOccluded())
		{
			return;
		}
		entity_player lOCAL = PlayerController.LOCAL;
		if (!lOCAL)
		{
			return;
		}
		entity_player_camera camera = lOCAL.GetCamera();
		if ((bool)camera)
		{
			Vector3 forward = base.transform.position - SDK.MainCamera.transform.position;
			if (forward.magnitude > forceLookDistance)
			{
				IsLookingAtTarget.Invoke(param1: false);
				return;
			}
			camera.LookAt(base.transform, forceLookSpeed);
			Quaternion b = Quaternion.LookRotation(forward);
			float num = Quaternion.Angle(camera.transform.rotation, b);
			IsLookingAtTarget.Invoke(num < lookThreshold);
		}
	}

	private bool IsOccluded()
	{
		if (!SDK.MainCamera)
		{
			return true;
		}
		RaycastHit hitInfo;
		return !Physics.Linecast(base.transform.position, SDK.MainCamera.transform.position, out hitInfo, _layer, QueryTriggerInteraction.Ignore);
	}
}
