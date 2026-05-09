using UnityEngine;
using UnityEngine.Animations;

namespace HyenaQuest;

public class entity_camera_follower : MonoBehaviour
{
	public Axis followAxis;

	public bool useOriginalPosition = true;

	public Vector3 offset;

	private Vector3 _originalPosition;

	public void Awake()
	{
		_originalPosition = base.transform.position;
	}

	public void LateUpdate()
	{
		if ((bool)SDK.MainCamera)
		{
			Vector3 zero = Vector3.zero;
			Vector3 position = SDK.MainCamera.transform.position;
			if (followAxis.HasFlag(Axis.X))
			{
				zero.x = position.x;
			}
			if (followAxis.HasFlag(Axis.Y))
			{
				zero.y = position.y;
			}
			if (followAxis.HasFlag(Axis.Z))
			{
				zero.z = position.z;
			}
			base.transform.position = (useOriginalPosition ? (_originalPosition + zero) : zero) + offset;
		}
	}
}
