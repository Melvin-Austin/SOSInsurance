using FailCake;
using UnityEngine;

namespace HyenaQuest;

public class entity_ambient_sound : MonoBehaviour
{
	[Header("Settings")]
	public GameObject speaker;

	private AudioSource _speaker;

	private Collider[] _areas;

	private util_fade_timer _transition;

	private float _volume;

	public void Awake()
	{
		if (!speaker)
		{
			throw new UnityException("entity_ambient_sound requires GameObject speaker component");
		}
		speaker.isStatic = false;
		_speaker = speaker.GetComponent<AudioSource>();
		if (!_speaker)
		{
			throw new UnityException("entity_ambient_sound requires AudioSource component");
		}
		_speaker.loop = true;
		_volume = _speaker.volume;
		_areas = GetComponentsInChildren<Collider>(includeInactive: true);
		if (_areas == null || _areas.Length == 0)
		{
			throw new UnityException("entity_ambient_sound requires Collider component");
		}
	}

	public void OnDestroy()
	{
		Stop();
	}

	public Collider[] GetArea()
	{
		return _areas;
	}

	public void Update()
	{
		if (!SDK.MainCamera || !_speaker || !_speaker.isPlaying)
		{
			return;
		}
		Vector3 position = SDK.MainCamera.transform.position;
		Vector3 position2 = Vector3.zero;
		float num = float.PositiveInfinity;
		Collider[] areas = _areas;
		foreach (Collider collider in areas)
		{
			Vector3 vector = ((collider is MeshCollider) ? Physics.ClosestPoint(position, collider, collider.transform.position, collider.transform.rotation) : collider.ClosestPointOnBounds(position));
			float sqrMagnitude = (vector - position).sqrMagnitude;
			if (sqrMagnitude < num)
			{
				num = sqrMagnitude;
				position2 = vector;
			}
		}
		speaker.transform.position = position2;
	}

	public void Play()
	{
		if ((bool)_speaker && _speaker.enabled)
		{
			_speaker.Play();
		}
	}

	public void SetClip(AudioClip clip)
	{
		if ((bool)_speaker)
		{
			_speaker.clip = clip;
		}
	}

	public void TransitionSound(AudioClip clip)
	{
		if (!_speaker || _speaker.clip == clip)
		{
			return;
		}
		if (!_speaker.isPlaying)
		{
			_speaker.volume = _volume;
			SetClip(clip);
			Play();
			return;
		}
		if (_transition != null)
		{
			_transition.Stop();
		}
		_transition = util_fade_timer.Fade(0.5f, _speaker.volume, 0f, delegate(float f)
		{
			_speaker.volume = f;
		}, delegate
		{
			SetClip(clip);
			Play();
			_transition = util_fade_timer.Fade(0.5f, 0f, _volume, delegate(float f)
			{
				_speaker.volume = f;
			});
		});
	}

	public void Stop()
	{
		if ((bool)_speaker)
		{
			if (_transition != null)
			{
				_transition.Stop();
			}
			_speaker.Stop();
		}
	}
}
