using FailCake;
using UnityEngine;

namespace HyenaQuest;

[RequireComponent(typeof(SpriteRenderer))]
public class entity_blink : MonoBehaviour
{
	public float blinkSpeed = 1f;

	public bool isActive;

	private SpriteRenderer _renderer;

	private util_timer _timer;

	public void Awake()
	{
		_renderer = GetComponent<SpriteRenderer>();
		if (!_renderer)
		{
			throw new UnityException("entity_blink requires SpriteRenderer component");
		}
		UpdateStatus();
	}

	public void SetEnabled(bool active)
	{
		if (isActive != active)
		{
			isActive = active;
			UpdateStatus();
		}
	}

	public void OnDestroy()
	{
		_timer?.Stop();
	}

	private void UpdateStatus()
	{
		if (isActive)
		{
			EnableBlink();
		}
		else
		{
			DisableBlink();
		}
	}

	private void EnableBlink()
	{
		if (blinkSpeed <= 0f)
		{
			_renderer.enabled = true;
			return;
		}
		if (_timer != null)
		{
			_timer.Stop();
		}
		_timer = util_timer.Create(-1, blinkSpeed, delegate
		{
			_renderer.enabled = !_renderer.enabled;
		});
	}

	private void DisableBlink()
	{
		if (_timer != null)
		{
			_timer.Stop();
		}
		_timer = null;
		_renderer.enabled = false;
	}
}
