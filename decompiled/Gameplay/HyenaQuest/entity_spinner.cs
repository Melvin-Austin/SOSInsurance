using FailCake;
using UnityEngine;
using UnityEngine.Animations;

namespace HyenaQuest;

public class entity_spinner : MonoBehaviour
{
	[Range(-10000f, 10000f)]
	public float maxSpeed = 10f;

	public Axis axis = Axis.Z;

	public float rampUp = 0.5f;

	public bool active = true;

	private float _currentSpeed;

	private util_fade_timer _timer;

	public void Awake()
	{
		_currentSpeed = (active ? maxSpeed : 0f);
		SetStatus(active);
	}

	public void OnDestroy()
	{
		_timer?.Stop();
	}

	[Client]
	public void SetEnabled(bool enable)
	{
		active = enable;
		SetStatus(enable);
	}

	public void Update()
	{
		if (_currentSpeed != 0f)
		{
			Quaternion localRotation = base.transform.localRotation;
			localRotation *= (Quaternion)(axis switch
			{
				Axis.X => Quaternion.Euler(_currentSpeed * Time.deltaTime, 0f, 0f), 
				Axis.Y => Quaternion.Euler(0f, _currentSpeed * Time.deltaTime, 0f), 
				Axis.Z => Quaternion.Euler(0f, 0f, _currentSpeed * Time.deltaTime), 
				_ => Quaternion.identity, 
			});
			base.transform.localRotation = localRotation;
		}
	}

	private void SetStatus(bool status)
	{
		if (rampUp <= 0f)
		{
			_currentSpeed = (status ? maxSpeed : 0f);
			return;
		}
		if (_timer != null)
		{
			_timer.Stop();
		}
		_timer = util_fade_timer.Fade(rampUp, _currentSpeed, status ? maxSpeed : 0f, delegate(float speed)
		{
			_currentSpeed = speed;
		});
	}
}
