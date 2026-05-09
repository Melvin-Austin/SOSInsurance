using UnityEngine;

namespace HyenaQuest;

public class entity_ambient_sound_mixer : MonoBehaviour
{
	public string id;

	[Range(1f, 100f)]
	public float maxVolume = 100f;

	public bool fullDistance;

	private entity_ambient_sound_mixer_controller _controller;

	private BoxCollider _collider;

	public void Awake()
	{
		if (string.IsNullOrEmpty(id))
		{
			throw new UnityException("ID missing!");
		}
		_collider = GetComponent<BoxCollider>();
		GameObject gameObject = GameObject.Find(id);
		if (!gameObject)
		{
			throw new UnityException("Register not found!");
		}
		_controller = gameObject.GetComponent<entity_ambient_sound_mixer_controller>();
		if (!_controller)
		{
			throw new UnityException("Register is not a controller!");
		}
		_controller.Register(this);
	}

	public Bounds? GetBounds()
	{
		if (!_collider)
		{
			return null;
		}
		return _collider.bounds;
	}

	public void OnDestroy()
	{
		if ((bool)_controller)
		{
			_controller.Unregister(this);
		}
	}
}
