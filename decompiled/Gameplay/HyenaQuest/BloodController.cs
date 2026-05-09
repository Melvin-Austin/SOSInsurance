using System.Collections.Generic;
using BloodEffectsPack;
using UnityEngine;

namespace HyenaQuest;

[DisallowMultipleComponent]
[DefaultExecutionOrder(-70)]
public class BloodController : MonoController<BloodController>
{
	public static bool DISABLE_BLOOD;

	[Header("Settings")]
	public RenderingLayerMask renderingLayerMask = RenderingLayerMask.defaultRenderingLayerMask;

	public List<GameObject> smallBlood = new List<GameObject>();

	public List<GameObject> bigBlood = new List<GameObject>();

	private readonly List<ProjectorSpawner_URP> _spawners = new List<ProjectorSpawner_URP>();

	private int _layerMask;

	public new void Awake()
	{
		base.Awake();
		_layerMask = LayerMask.GetMask("entity_ground");
	}

	public new void OnDestroy()
	{
		ClearBlood();
		base.OnDestroy();
	}

	public void SpawnBlood(Vector3 position, Vector2 size)
	{
		if (!DISABLE_BLOOD && Physics.Raycast(position, Vector3.down, out var hitInfo, 10f, _layerMask))
		{
			float num = Random.Range(size.x, size.y);
			GameObject gameObject = smallBlood[Random.Range(0, smallBlood.Count)];
			if (num > 1.5f)
			{
				gameObject = bigBlood[Random.Range(0, bigBlood.Count)];
			}
			if (!gameObject)
			{
				throw new UnityException("Blood prefab is not set");
			}
			GameObject obj = Object.Instantiate(gameObject, hitInfo.point + Vector3.up * 0.025f, Quaternion.identity, base.transform);
			if (!obj)
			{
				throw new UnityException("Failed to instantiate blood prefab");
			}
			ProjectorSpawner_URP component = obj.GetComponent<ProjectorSpawner_URP>();
			if (!component)
			{
				throw new UnityException("Failed to get ProjectorSpawner_URP component");
			}
			component.size = Mathf.Clamp(num, 0.1f, 1.2f);
			component.destroyAfter = false;
			component.ResetAndInitialize(renderingLayerMask);
			_spawners.Add(component);
		}
	}

	public void ClearBlood()
	{
		List<ProjectorSpawner_URP> spawners = _spawners;
		if (spawners == null || spawners.Count <= 0)
		{
			return;
		}
		foreach (ProjectorSpawner_URP spawner in _spawners)
		{
			if ((bool)spawner)
			{
				Object.Destroy(spawner.gameObject);
			}
		}
	}
}
