using System;
using System.Collections.Generic;
using ECM2;
using UnityEngine;

namespace HyenaQuest;

[RequireComponent(typeof(BoxCollider))]
public class entity_movement_volume : PhysicsVolume
{
	public BoxCollider ignoreArea;

	protected readonly List<AffectorData> _affectors = new List<AffectorData>();

	public void Awake()
	{
		priority = UnityEngine.Random.Range(0, 100000);
	}

	public virtual VolumeType GetVolumeType()
	{
		throw new NotImplementedException();
	}

	public void OnTriggerEnter(Collider other)
	{
		if (base.isActiveAndEnabled && (bool)other.attachedRigidbody && other.attachedRigidbody.TryGetComponent<entity_volume_affector>(out var component))
		{
			_affectors.Add(new AffectorData
			{
				collider = other,
				affector = component
			});
			OnVolumeInternalUpdate(other, enter: true);
		}
	}

	public void OnTriggerExit(Collider other)
	{
		if ((bool)other.attachedRigidbody && other.attachedRigidbody.TryGetComponent<entity_volume_affector>(out var affector))
		{
			AffectorData item = _affectors.Find((AffectorData x) => x.collider == other && x.affector == affector);
			if ((bool)item.affector)
			{
				item.affector.SetOnVolume(this, VolumeImmersionType.NONE);
				_affectors.Remove(item);
				OnVolumeInternalUpdate(other, enter: false);
			}
		}
	}

	public void Update()
	{
		foreach (AffectorData affector in _affectors)
		{
			if ((bool)affector.affector && (bool)affector.collider)
			{
				VolumeImmersionType immersionType = GetImmersionType(affector.collider);
				if ((bool)ignoreArea && ignoreArea.bounds.Contains(affector.collider.transform.position))
				{
					immersionType = VolumeImmersionType.NONE;
				}
				affector.affector.SetOnVolume(this, immersionType);
			}
		}
	}

	public bool IsInsideVolume(Vector3 pos)
	{
		if ((bool)ignoreArea && ignoreArea.bounds.Contains(pos))
		{
			return false;
		}
		return base.boxCollider.bounds.Contains(pos);
	}

	public void OnDestroy()
	{
		ResetVolume();
	}

	public void OnDisable()
	{
		ResetVolume();
	}

	public void RemoveAffector(entity_volume_affector affector)
	{
		if ((bool)affector)
		{
			_affectors.RemoveAll((AffectorData a) => a.affector == affector);
		}
	}

	private Vector3 GetFullPoint(Collider other)
	{
		if (other.CompareTag("Player"))
		{
			if (other.gameObject == PlayerController.LOCAL?.gameObject && (bool)SDK.MainCamera)
			{
				entity_player spectatingTarget = MonoController<SpectateController>.Instance.GetSpectatingTarget();
				if ((bool)spectatingTarget)
				{
					return spectatingTarget.transform.position;
				}
				return SDK.MainCamera.transform.position;
			}
			if (other.attachedRigidbody.TryGetComponent<entity_player>(out var component))
			{
				return component.head.transform.position;
			}
		}
		return other.transform.position;
	}

	private bool IsFullyImmersed(Collider other)
	{
		Vector3 fullPoint = GetFullPoint(other);
		return base.boxCollider.bounds.Contains(fullPoint);
	}

	private bool IsPartiallyImmersed(Collider other)
	{
		return base.boxCollider.bounds.Intersects(other.bounds);
	}

	public VolumeImmersionType GetImmersionType(Collider other)
	{
		if (IsFullyImmersed(other))
		{
			return VolumeImmersionType.FULL;
		}
		if (!IsPartiallyImmersed(other))
		{
			return VolumeImmersionType.NONE;
		}
		return VolumeImmersionType.PARTIAL;
	}

	protected virtual void OnVolumeInternalUpdate(Collider other, bool enter)
	{
	}

	private void ResetVolume()
	{
		for (int num = _affectors.Count - 1; num >= 0; num--)
		{
			if ((bool)_affectors[num].affector)
			{
				_affectors[num].affector.SetOnVolume(this, VolumeImmersionType.NONE);
			}
		}
		_affectors.Clear();
	}
}
