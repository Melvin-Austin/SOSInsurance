using System;
using FailCake;
using Unity.Netcode.Components;
using UnityEngine;

namespace HyenaQuest;

public class entity_door : MonoBehaviour
{
	[Range(0f, 10f)]
	public float speed = 1f;

	public bool open;

	public bool shake;

	public Vector3 openPosition;

	public Vector3 openRotation;

	public Vector3 closePosition;

	public Vector3 closeRotation;

	public AnimationCurve curve = AnimationCurve.Linear(0f, 0f, 1f, 1f);

	public AudioClip openSND;

	public AudioClip closeSND;

	public AudioClip closeStopSND;

	public AudioClip openStopSND;

	public GameObject target;

	public GameEvent<bool> OnDoorUpdate = new GameEvent<bool>();

	private util_fade_timer _timer;

	private NetworkTransform _targetTransform;

	public void Awake()
	{
		if (!target)
		{
			throw new UnityException("Target is not assigned!");
		}
	}

	public void OnDestroy()
	{
		_timer?.Stop();
	}

	public void SetOpen(bool newValue, Action<bool> onComplete = null)
	{
		if (open != newValue)
		{
			open = newValue;
			Animate(newValue, onComplete);
		}
	}

	private void Animate(bool newValue, Action<bool> onComplete = null)
	{
		AudioData data = new AudioData
		{
			pitch = UnityEngine.Random.Range(0.8f, 1.1f),
			distance = 8f,
			volume = 0.5f
		};
		NetController<SoundController>.Instance?.Play3DSound(newValue ? openSND : closeSND, target.transform.position, data);
		if (newValue)
		{
			onComplete?.Invoke(obj: true);
		}
		Vector3 startPos = (newValue ? closePosition : openPosition);
		Vector3 endPos = (newValue ? openPosition : closePosition);
		Vector3 startRot = (newValue ? closeRotation : openRotation);
		Vector3 endRot = (newValue ? openRotation : closeRotation);
		_timer?.Stop();
		_timer = util_fade_timer.Fade(speed, 0f, 1f, delegate(float t)
		{
			float t2 = curve.Evaluate(t);
			target.transform.localPosition = Vector3.Lerp(startPos, endPos, t2);
			target.transform.localRotation = Quaternion.Euler(Vector3.Lerp(startRot, endRot, t2));
		}, delegate
		{
			NetController<SoundController>.Instance?.Play3DSound(newValue ? openStopSND : closeStopSND, target.transform.position, data);
			if (shake)
			{
				NetController<ShakeController>.Instance?.Shake3DRPC(base.transform.position, ShakeMode.SHAKE_ALL, 0.05f);
			}
			onComplete?.Invoke(newValue);
			OnDoorUpdate.Invoke(newValue);
		});
	}
}
