using UnityEngine;

namespace HyenaQuest;

public class entity_googly : MonoBehaviour
{
	public Transform pupil;

	[Range(0.01f, 1f)]
	public float radius = 0.25f;

	public bool playSound;

	private Vector3 _vel;

	private Vector3 _lastPos;

	private Vector3 _localPos;

	private float _lockSpeed;

	private Transform _lockTarget;

	public void Awake()
	{
		if (!pupil)
		{
			throw new UnityException("Missing pupil");
		}
		_lastPos = base.transform.position;
		_localPos = pupil.localPosition;
	}

	public void Update()
	{
		if (!pupil)
		{
			return;
		}
		if ((bool)_lockTarget)
		{
			Vector3 normalized = (_lockTarget.position - base.transform.position).normalized;
			Vector3 b = base.transform.InverseTransformDirection(normalized).normalized * radius;
			_localPos = Vector3.Lerp(_localPos, b, Time.deltaTime * _lockSpeed);
			pupil.localPosition = _localPos;
			return;
		}
		Vector3 position = base.transform.position;
		Vector3 down = Vector3.down;
		Vector3 direction = (_lastPos - position) / Time.deltaTime;
		Vector3 vector = base.transform.InverseTransformDirection(down);
		Vector3 vector2 = base.transform.InverseTransformDirection(direction);
		Vector3 vector3 = vector * 30f + vector2 * 20f;
		_vel += vector3 * Time.deltaTime;
		_vel *= 0.997f;
		_localPos += _vel * Time.deltaTime;
		if (_localPos.magnitude > radius)
		{
			Vector3 inNormal = -_localPos.normalized;
			if (playSound && _vel.sqrMagnitude >= 15f)
			{
				float magnitude = _vel.magnitude;
				float pitch = Mathf.Lerp(0.8f, 1.4f, Mathf.Clamp01(magnitude / 50f));
				NetController<SoundController>.Instance?.Play3DSound($"Ingame/Entities/Googly/googly_{Random.Range(0, 4)}.ogg", base.transform.position, new AudioData
				{
					distance = 0.5f,
					pitch = pitch,
					volume = 0.15f
				});
			}
			_vel = Vector3.Reflect(_vel, inNormal) * 0.85f;
			_localPos = _localPos.normalized * radius;
		}
		pupil.localPosition = _localPos;
		_lastPos = position;
	}

	public void LockOnTarget(Transform target, float speed = 1f)
	{
		_lockSpeed = speed;
		_lockTarget = target;
		_vel = Vector3.zero;
	}

	public void Unlock()
	{
		_lockSpeed = 0f;
		_lockTarget = null;
	}
}
