using FailCake;
using UnityEngine;

namespace HyenaQuest;

[RequireComponent(typeof(MeshRenderer))]
public class entity_fade_death : MonoBehaviour
{
	[Header("Settings")]
	public float fadeSpeed = 0.1f;

	private util_timer _delayTimer;

	private util_fade_timer _fadeTimer;

	private bool _destroying;

	public void OnEnable()
	{
		Destroy();
	}

	public void OnDestroy()
	{
		_delayTimer?.Stop();
		_fadeTimer?.Stop();
	}

	private void Destroy()
	{
		if (_destroying)
		{
			return;
		}
		_destroying = true;
		if (_delayTimer != null)
		{
			_delayTimer.Stop();
		}
		_delayTimer = util_timer.Simple(Random.Range(0.2f, 0.8f), delegate
		{
			if (_fadeTimer != null)
			{
				_fadeTimer.Stop();
			}
			_fadeTimer = util_fade_timer.Fade(fadeSpeed, 1f, 0f, delegate(float value)
			{
				base.transform.localScale = Vector3.one * value;
			}, delegate
			{
				Object.Destroy(base.gameObject);
			});
		});
	}
}
