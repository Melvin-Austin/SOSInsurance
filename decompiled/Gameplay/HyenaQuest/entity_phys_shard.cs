using FailCake;
using UnityEngine;

namespace HyenaQuest;

[RequireComponent(typeof(MeshRenderer))]
public class entity_phys_shard : MonoBehaviour
{
	private GameObject _fractureParent;

	private entity_fade_death _fadeDeath;

	private MeshRenderer _renderer;

	private Rigidbody _rigidbody;

	private util_timer _excludeTimer;

	public void Awake()
	{
		_fadeDeath = GetComponent<entity_fade_death>();
		if ((bool)_fadeDeath)
		{
			_fadeDeath.enabled = false;
			_fractureParent = GameObject.Find("[FRACTURES]");
			if (!_fractureParent)
			{
				throw new UnityException("Fracture parent not found");
			}
		}
		_rigidbody = GetComponent<Rigidbody>();
		if ((bool)_rigidbody)
		{
			_rigidbody.isKinematic = true;
		}
		_renderer = GetComponent<MeshRenderer>();
		if (!_renderer)
		{
			throw new UnityException("entity_phys_shard requires a MeshRenderer component to work.");
		}
		base.gameObject.tag = "ENTITY/PHYS-SHARD";
	}

	public void OnDestroy()
	{
		_excludeTimer?.Stop();
	}

	public MeshRenderer GetRenderer()
	{
		return _renderer;
	}

	public Rigidbody GetBody()
	{
		return _rigidbody;
	}

	public void Shred(params string[] excludeLayers)
	{
		if ((bool)_fadeDeath)
		{
			base.transform.SetParent(_fractureParent.transform, worldPositionStays: true);
			_fadeDeath.enabled = true;
		}
		if (!_rigidbody)
		{
			_rigidbody = base.gameObject.AddComponent<Rigidbody>();
		}
		if (!_rigidbody)
		{
			throw new UnityException("Failed to add Rigidbody to entity_phys_shard");
		}
		_rigidbody.interpolation = RigidbodyInterpolation.Interpolate;
		_rigidbody.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
		_rigidbody.isKinematic = false;
		if (excludeLayers == null || excludeLayers.Length <= 0)
		{
			return;
		}
		_excludeTimer?.Stop();
		_excludeTimer = util_timer.Simple(25f, delegate
		{
			if ((bool)_rigidbody)
			{
				_rigidbody.excludeLayers = LayerMask.GetMask(excludeLayers);
			}
		});
	}
}
