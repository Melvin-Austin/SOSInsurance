using System;
using System.Collections.Generic;
using UnityEngine;

namespace HyenaQuest;

public class entity_movement : MonoBehaviour
{
	public GameObject obj;

	public List<Point> points = new List<Point>();

	public float speed = 1f;

	public bool autoFacePoints;

	public bool startActive;

	public bool reverse;

	public bool loop;

	public bool catmullSmooth;

	public AudioClip stopSound;

	public AudioClip startSound;

	public AudioClip pathSound;

	private bool _isActive;

	private int _pointIndex;

	private int _targetPointIndex;

	private float _movementStartTime;

	private float _delayEndTime;

	private bool _isDelaying;

	private Action _onCompleteCallback;

	private Vector3 CatmullRom(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float t)
	{
		float num = t * t;
		float num2 = num * t;
		return 0.5f * (2f * p1 + (-p0 + p2) * t + (2f * p0 - 5f * p1 + 4f * p2 - p3) * num + (-p0 + 3f * p1 - 3f * p2 + p3) * num2);
	}

	private Vector3 CatmullRomDerivative(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float t)
	{
		float num = t * t;
		return 0.5f * (-p0 + p2 + (2f * p0 - 5f * p1 + 4f * p2 - p3) * (2f * t) + (-p0 + 3f * p1 - 3f * p2 + p3) * (3f * num));
	}

	private Vector3 ApplyAutoFace(Vector3 direction, Vector3 angleOffset)
	{
		if (direction.sqrMagnitude < 0.0001f)
		{
			return angleOffset;
		}
		Quaternion quaternion = Quaternion.LookRotation(direction);
		Quaternion quaternion2 = Quaternion.Euler(angleOffset);
		return (quaternion * quaternion2).eulerAngles;
	}

	private void GetCatmullRomIndices(out int p0Index, out int p1Index, out int p2Index, out int p3Index)
	{
		p1Index = _pointIndex;
		p2Index = _targetPointIndex;
		if (reverse)
		{
			p0Index = p1Index + 1;
			p3Index = p2Index - 1;
		}
		else
		{
			p0Index = p1Index - 1;
			p3Index = p2Index + 1;
		}
		if (loop)
		{
			if (p0Index < 0)
			{
				p0Index = points.Count + p0Index;
			}
			if (p0Index >= points.Count)
			{
				p0Index -= points.Count;
			}
			if (p3Index < 0)
			{
				p3Index = points.Count + p3Index;
			}
			if (p3Index >= points.Count)
			{
				p3Index -= points.Count;
			}
		}
		else
		{
			p0Index = Mathf.Clamp(p0Index, 0, points.Count - 1);
			p3Index = Mathf.Clamp(p3Index, 0, points.Count - 1);
		}
	}

	private float CalculateCurveLength(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, int segments = 20)
	{
		float num = 0f;
		Vector3 a = p1;
		for (int i = 1; i <= segments; i++)
		{
			float t = (float)i / (float)segments;
			Vector3 vector = CatmullRom(p0, p1, p2, p3, t);
			num += Vector3.Distance(a, vector);
			a = vector;
		}
		return num;
	}

	private float CalculateT(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float targetFraction, int segments = 20)
	{
		if (targetFraction <= 0f)
		{
			return 0f;
		}
		if (targetFraction >= 1f)
		{
			return 1f;
		}
		float num = 0f;
		float[] array = new float[segments];
		Vector3 a = p1;
		for (int i = 1; i <= segments; i++)
		{
			float t = (float)i / (float)segments;
			Vector3 vector = CatmullRom(p0, p1, p2, p3, t);
			num += (array[i - 1] = Vector3.Distance(a, vector));
			a = vector;
		}
		float num2 = num * targetFraction;
		float num3 = 0f;
		for (int j = 0; j < segments; j++)
		{
			float num4 = num3 + array[j];
			if (num4 >= num2)
			{
				float t2 = ((array[j] > 0f) ? ((num2 - num3) / array[j]) : 0f);
				float a2 = (float)j / (float)segments;
				float b = (float)(j + 1) / (float)segments;
				return Mathf.Lerp(a2, b, t2);
			}
			num3 = num4;
		}
		return 1f;
	}

	public void Awake()
	{
		if (!obj)
		{
			throw new UnityException("Missing game object");
		}
		if (points.Count < 2)
		{
			throw new UnityException("At least 2 points are needed");
		}
		if (startActive)
		{
			StartMovement();
		}
	}

	public void Update()
	{
		if (!_isActive)
		{
			return;
		}
		if (_isDelaying)
		{
			if (Time.time >= _delayEndTime)
			{
				_isDelaying = false;
				_movementStartTime = Time.time;
				if (_pointIndex == (reverse ? (points.Count - 1) : 0))
				{
					SDK.Play3DSoundClip?.Invoke(startSound, obj.transform.position, new AudioData
					{
						distance = 4f,
						pitch = UnityEngine.Random.Range(0.85f, 1.15f),
						volume = 0.5f
					}, ShouldBroadcastSound());
				}
			}
			return;
		}
		Point point = points[_pointIndex];
		Point dest = points[_targetPointIndex];
		float num;
		if (catmullSmooth && dest.smoothPos)
		{
			GetCatmullRomIndices(out var p0Index, out var p1Index, out var p2Index, out var p3Index);
			num = CalculateCurveLength(points[p0Index].pos, points[p1Index].pos, points[p2Index].pos, points[p3Index].pos);
		}
		else
		{
			num = Vector3.Distance(point.pos, dest.pos);
		}
		float num2 = Quaternion.Angle(Quaternion.Euler(point.angle), Quaternion.Euler(dest.angle));
		float num3 = speed * ((dest.speedModifier > 0f) ? dest.speedModifier : 1f);
		float num4 = ((!(num < 0.001f)) ? (num / num3) : (num2 / (num3 * 90f)));
		if (num4 <= 0f)
		{
			_pointIndex = _targetPointIndex;
			MoveToNextPoint();
			return;
		}
		float num5 = (Time.time - _movementStartTime) / num4;
		if (num5 >= 1f || (!dest.smoothPos && !dest.smoothAngle))
		{
			_pointIndex = _targetPointIndex;
			OnPointReached(dest);
			SDK.Play3DSoundClip?.Invoke(pathSound, obj.transform.position, new AudioData
			{
				distance = 4f,
				pitch = UnityEngine.Random.Range(0.85f, 1.15f),
				volume = 0.5f
			}, ShouldBroadcastSound());
			MoveToNextPoint();
			return;
		}
		Vector3 direction = Vector3.zero;
		Vector3 position;
		if (catmullSmooth && dest.smoothPos)
		{
			GetCatmullRomIndices(out var p0Index2, out var p1Index2, out var p2Index2, out var p3Index2);
			Vector3 pos = points[p0Index2].pos;
			Vector3 pos2 = points[p1Index2].pos;
			Vector3 pos3 = points[p2Index2].pos;
			Vector3 pos4 = points[p3Index2].pos;
			float t = CalculateT(pos, pos2, pos3, pos4, num5);
			position = CatmullRom(pos, pos2, pos3, pos4, t);
			if (autoFacePoints)
			{
				direction = CatmullRomDerivative(pos, pos2, pos3, pos4, t);
			}
		}
		else
		{
			position = ((!dest.smoothPos) ? dest.pos : Vector3.Lerp(point.pos, dest.pos, num5));
			if (autoFacePoints)
			{
				direction = dest.pos - point.pos;
			}
		}
		SetPosition(position);
		Vector3 vector = ((!dest.smoothAngle) ? dest.angle : Vector3.Lerp(point.angle, dest.angle, num5));
		if (autoFacePoints)
		{
			SetAngle(ApplyAutoFace(direction, vector));
		}
		else
		{
			SetAngle(vector);
		}
	}

	public virtual void StartMovement(bool reset = true, Action onComplete = null)
	{
		if (points.Count < 2)
		{
			throw new UnityException("At least 2 points are needed");
		}
		if (reset)
		{
			ResetMovement();
		}
		_onCompleteCallback = onComplete;
		_targetPointIndex = ((!reverse) ? ((_pointIndex < points.Count - 1) ? (_pointIndex + 1) : ((!loop) ? (points.Count - 1) : 0)) : ((_pointIndex > 0) ? (_pointIndex - 1) : (loop ? (points.Count - 1) : 0)));
		_movementStartTime = Time.time;
		_isActive = true;
		SDK.Play3DSoundClip?.Invoke(startSound, obj.transform.position, new AudioData
		{
			distance = 4f,
			pitch = UnityEngine.Random.Range(0.85f, 1.15f),
			volume = 0.5f
		}, ShouldBroadcastSound());
	}

	public Point GetPoint(int index)
	{
		if (points == null || points.Count < 2 || index > points.Count)
		{
			return default(Point);
		}
		return points[index];
	}

	public virtual void StopMovement()
	{
		_isActive = false;
		_isDelaying = false;
	}

	protected virtual bool ShouldBroadcastSound()
	{
		return false;
	}

	protected virtual void ResetMovement()
	{
		if (points != null && points.Count >= 2)
		{
			_pointIndex = (reverse ? (points.Count - 1) : 0);
			_isDelaying = false;
			_isActive = false;
			Point point = points[_pointIndex];
			ForcePosition(point);
		}
	}

	protected virtual void OnPointReached(Point dest)
	{
	}

	protected virtual void ForcePosition(Point point)
	{
		SetPosition(point.pos);
		if (autoFacePoints)
		{
			int num = _pointIndex + ((!reverse) ? 1 : (-1));
			if (loop)
			{
				if (num < 0)
				{
					num = points.Count - 1;
				}
				else if (num >= points.Count)
				{
					num = 0;
				}
			}
			else
			{
				num = Mathf.Clamp(num, 0, points.Count - 1);
			}
			Vector3 direction = points[num].pos - point.pos;
			SetAngle(ApplyAutoFace(direction, point.angle));
		}
		else
		{
			SetAngle(point.angle);
		}
	}

	private void SetPosition(Vector3 pos)
	{
		if (!obj)
		{
			throw new UnityException("Missing game object");
		}
		obj.transform.localPosition = pos;
	}

	private void SetAngle(Vector3 angle)
	{
		if (!obj)
		{
			throw new UnityException("Missing game object");
		}
		obj.transform.localEulerAngles = angle;
	}

	private void MoveToNextPoint()
	{
		int num = _pointIndex + ((!reverse) ? 1 : (-1));
		if (num < 0 || num >= points.Count)
		{
			SDK.Play3DSoundClip?.Invoke(stopSound, obj.transform.position, new AudioData
			{
				distance = 4f,
				pitch = UnityEngine.Random.Range(0.85f, 1.15f),
				volume = 0.5f
			}, ShouldBroadcastSound());
			if (!loop)
			{
				_isActive = false;
				_onCompleteCallback?.Invoke();
				return;
			}
			num = (reverse ? (points.Count - 1) : 0);
		}
		_targetPointIndex = num;
		Point point = points[num];
		if (point.delay != Vector2.zero)
		{
			_isDelaying = true;
			_delayEndTime = Time.time + UnityEngine.Random.Range(point.delay.x, point.delay.y);
		}
		else
		{
			_movementStartTime = Time.time;
		}
	}
}
