using System.Collections.Generic;
using FailCake;
using UnityEngine;

namespace HyenaQuest;

[RequireComponent(typeof(BoxCollider))]
public class entity_shake_area : MonoBehaviour
{
	public ShakeMode shakeMode = ShakeMode.SHAKE_ALL;

	[Range(0.1f, 3f)]
	public float shakeDuration = 0.5f;

	[Range(0.001f, 0.02f)]
	public float shakeIntensity = 0.005f;

	public Vector2 timeBetweenShakes = new Vector2(2f, 5f);

	public Vector2 shakesPerBurst = new Vector2(1f, 3f);

	public float timeBetweenBurstShakes = 0.2f;

	public List<AudioClip> shakeSound = new List<AudioClip>();

	private BoxCollider _collider;

	private util_timer _shakeTimer;

	private util_timer _shakeBurstTimer;

	private bool _playerInside;

	private int _remainingShakes;

	public void Awake()
	{
		_collider = GetComponent<BoxCollider>();
		if (!_collider)
		{
			throw new UnityException("Missing BoxCollider");
		}
		_collider.isTrigger = true;
		_collider.gameObject.layer = LayerMask.NameToLayer("entity_trigger");
		if (!NetController<ShakeController>.Instance)
		{
			throw new UnityException("Missing ShakeController");
		}
		StartShakeTimer();
	}

	public void OnDestroy()
	{
		_shakeTimer?.Stop();
		_shakeBurstTimer?.Stop();
	}

	private void StartShakeTimer()
	{
		_shakeBurstTimer?.Stop();
		_shakeTimer?.Stop();
		_shakeTimer = util_timer.Simple(Random.Range(timeBetweenShakes.x, timeBetweenShakes.y), TriggerShakeBurst);
	}

	private void TriggerShakeBurst()
	{
		if (!ShouldApplyShake())
		{
			StartShakeTimer();
			return;
		}
		_remainingShakes = Mathf.RoundToInt(Random.Range(shakesPerBurst.x, shakesPerBurst.y));
		ApplyNextShake();
	}

	private void ApplyNextShake()
	{
		if (_remainingShakes <= 0)
		{
			StartShakeTimer();
			return;
		}
		if (ShouldApplyShake())
		{
			NetController<ShakeController>.Instance?.LocalShake(shakeMode, shakeDuration, shakeIntensity);
			List<AudioClip> list = shakeSound;
			if (list != null && list.Count > 0)
			{
				NetController<SoundController>.Instance?.PlaySound(shakeSound[Random.Range(0, shakeSound.Count)], new AudioData
				{
					pitch = Random.Range(0.8f, 1.2f),
					volume = Random.Range(0.1f, 0.2f)
				});
			}
		}
		_remainingShakes--;
		if (_remainingShakes <= 0)
		{
			StartShakeTimer();
			return;
		}
		_shakeBurstTimer?.Stop();
		_shakeBurstTimer = util_timer.Simple(timeBetweenBurstShakes, ApplyNextShake);
	}

	private bool ShouldApplyShake()
	{
		if (!PlayerController.LOCAL)
		{
			return false;
		}
		if (!_collider.bounds.Contains(PlayerController.LOCAL.transform.position))
		{
			return false;
		}
		entity_player_movement movement = PlayerController.LOCAL.GetMovement();
		if ((bool)movement)
		{
			return movement.IsGrounded();
		}
		return false;
	}
}
