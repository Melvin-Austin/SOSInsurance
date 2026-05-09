using UnityEngine;

namespace HyenaQuest;

public class entity_movement_train_module : MonoBehaviour
{
	public float moduleLength = 10f;

	private MeshRenderer _meshRenderer;

	public void Awake()
	{
		_meshRenderer = GetComponent<MeshRenderer>();
		if (!_meshRenderer)
		{
			_meshRenderer = GetComponentInChildren<MeshRenderer>(includeInactive: true);
		}
		if (!_meshRenderer)
		{
			throw new UnityException("Missing mesh renderer");
		}
	}

	public Bounds GetBounds()
	{
		if ((bool)_meshRenderer && _meshRenderer.enabled)
		{
			return _meshRenderer.bounds;
		}
		return new Bounds(base.transform.position, Vector3.zero);
	}
}
