using UnityEngine;

namespace HyenaQuest;

[RequireComponent(typeof(BoxCollider))]
public class entity_area_purger_safezone : MonoBehaviour
{
	[Header("Settings")]
	public bool active;

	private BoxCollider _collider;

	public void Awake()
	{
		_collider = GetComponent<BoxCollider>();
		if (!_collider)
		{
			throw new UnityException("Missing BoxCollider");
		}
		CoreController.WaitFor(delegate(PurgeController purgeCtrl)
		{
			purgeCtrl.RegisterSafeArea(this);
		});
	}

	public void OnDestroy()
	{
		if ((bool)MonoController<PurgeController>.Instance)
		{
			MonoController<PurgeController>.Instance.UnRegisterSafeArea(this);
		}
	}

	public bool HasEntity(GameObject obj)
	{
		if (!active || !obj)
		{
			return false;
		}
		return _collider.bounds.Contains(obj.transform.position);
	}

	public bool HasEntity(Vector3 pos)
	{
		if (!active)
		{
			return false;
		}
		return _collider.bounds.Contains(pos);
	}
}
