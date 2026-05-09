using UnityEngine;
using UnityEngine.Animations;

namespace HyenaQuest;

public class entity_spinner_snap : MonoBehaviour
{
	[Header("Settings")]
	public float speed = 1f;

	public int rotation = 45;

	public Axis snapAxis = Axis.Y;

	private float _time;

	private int _rotation;

	public void Update()
	{
		if (!(Time.time < _time))
		{
			_time = Time.time + speed;
			_rotation += rotation;
			switch (snapAxis)
			{
			case Axis.X:
				base.transform.localRotation = Quaternion.Euler(_rotation, 0f, 0f);
				break;
			case Axis.Y:
				base.transform.localRotation = Quaternion.Euler(0f, _rotation, 0f);
				break;
			case Axis.Z:
				base.transform.localRotation = Quaternion.Euler(0f, 0f, _rotation);
				break;
			case Axis.X | Axis.Y:
				break;
			}
		}
	}
}
