using System;
using UnityEngine;
using UnityEngine.Animations;

namespace HyenaQuest;

public class entity_spinner_clamped : MonoBehaviour
{
	[Range(-10000f, 10000f)]
	public float maxSpeed = 10f;

	[Range(0f, 360f)]
	public float clamp = 30f;

	public Axis axis = Axis.Z;

	private Quaternion _originalOrientation;

	private float _time;

	public void Awake()
	{
		_originalOrientation = base.transform.localRotation;
	}

	public void Update()
	{
		if (maxSpeed != 0f)
		{
			Quaternion identity = Quaternion.identity;
			_time += maxSpeed * Time.deltaTime;
			float num = Mathf.Sin(_time * (MathF.PI / 180f)) * clamp * 0.5f + clamp * 0.5f;
			identity = axis switch
			{
				Axis.X => Quaternion.Euler(num, 0f, 0f), 
				Axis.Y => Quaternion.Euler(0f, num, 0f), 
				Axis.Z => Quaternion.Euler(0f, 0f, num), 
				_ => identity, 
			};
			base.transform.localRotation = _originalOrientation * identity;
		}
	}
}
