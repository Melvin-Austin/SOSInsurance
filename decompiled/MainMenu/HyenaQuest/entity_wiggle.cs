using UnityEngine;

namespace HyenaQuest;

public class entity_wiggle : MonoBehaviour
{
	[Header("Settings")]
	[Range(-100f, 100f)]
	public float speed;

	[Range(-360f, 360f)]
	public float amount;

	public SnapAxis axis;

	private Vector3 _startAngle;

	public void Awake()
	{
		_startAngle = base.transform.localEulerAngles;
	}

	public void Update()
	{
		float num = Mathf.Sin(Time.time * speed) * amount;
		Vector3 zero = Vector3.zero;
		if ((axis & SnapAxis.X) == SnapAxis.X)
		{
			zero.x = num;
		}
		if ((axis & SnapAxis.Y) == SnapAxis.Y)
		{
			zero.y = num;
		}
		if ((axis & SnapAxis.Z) == SnapAxis.Z)
		{
			zero.z = num;
		}
		base.transform.localEulerAngles = _startAngle + zero;
	}
}
