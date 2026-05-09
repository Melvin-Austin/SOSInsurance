using UnityEngine;

namespace HyenaQuest;

public class entity_sound_pitcher : MonoBehaviour
{
	public Vector2 pitch;

	private AudioSource _source;

	public void Awake()
	{
		_source = GetComponent<AudioSource>();
		if (!_source)
		{
			throw new UnityException("Missing AudioSource");
		}
	}

	public void OnEnable()
	{
		if ((bool)_source)
		{
			_source.pitch = Random.Range(pitch.x, pitch.y);
			_source.Play();
		}
	}
}
