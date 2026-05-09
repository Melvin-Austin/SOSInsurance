using System.Collections.Generic;
using UnityEngine;
using ZLinq;

namespace HyenaQuest;

public class entity_volume_affector : MonoBehaviour
{
	public GameEvent<VolumeType, VolumeImmersionType> OnVolumeUpdate = new GameEvent<VolumeType, VolumeImmersionType>();

	private readonly HashSet<entity_movement_volume> _partial = new HashSet<entity_movement_volume>();

	private readonly HashSet<entity_movement_volume> _full = new HashSet<entity_movement_volume>();

	private VolumeImmersionType _currentImmersion;

	private VolumeType _currentVolume;

	public virtual void SetOnVolume(entity_movement_volume volume, VolumeImmersionType immersionType)
	{
		if (!volume)
		{
			return;
		}
		_partial.RemoveWhere((entity_movement_volume v) => !v);
		_full.RemoveWhere((entity_movement_volume v) => !v);
		switch (immersionType)
		{
		case VolumeImmersionType.NONE:
			_partial.Remove(volume);
			_full.Remove(volume);
			break;
		case VolumeImmersionType.PARTIAL:
			_partial.Add(volume);
			_full.Remove(volume);
			break;
		case VolumeImmersionType.FULL:
			_partial.Remove(volume);
			_full.Add(volume);
			break;
		}
		entity_movement_volume entity_movement_volume2 = null;
		VolumeImmersionType volumeImmersionType = VolumeImmersionType.NONE;
		if (_full.Count > 0)
		{
			entity_movement_volume2 = (from v in _full.AsValueEnumerable()
				orderby v.priority descending
				select v).First();
			volumeImmersionType = VolumeImmersionType.FULL;
		}
		else if (_partial.Count > 0)
		{
			entity_movement_volume2 = (from v in _partial.AsValueEnumerable()
				orderby v.priority descending
				select v).First();
			volumeImmersionType = VolumeImmersionType.PARTIAL;
		}
		VolumeType volumeType = entity_movement_volume2?.GetVolumeType() ?? VolumeType.NONE;
		if (_currentImmersion != volumeImmersionType || _currentVolume != volumeType)
		{
			_currentImmersion = volumeImmersionType;
			_currentVolume = volumeType;
			OnVolumeUpdate.Invoke(volumeType, volumeImmersionType);
		}
	}

	public void OnDestroy()
	{
		ResetVolume();
	}

	public void ResetVolume()
	{
		foreach (entity_movement_volume item in _full)
		{
			if ((bool)item)
			{
				item.RemoveAffector(this);
			}
		}
		foreach (entity_movement_volume item2 in _partial)
		{
			if ((bool)item2)
			{
				item2.RemoveAffector(this);
			}
		}
		_full.Clear();
		_partial.Clear();
		_currentImmersion = VolumeImmersionType.NONE;
		_currentVolume = VolumeType.NONE;
		OnVolumeUpdate.Invoke(VolumeType.NONE, VolumeImmersionType.NONE);
	}

	public VolumeImmersionType GetCurrentImmersion()
	{
		return _currentImmersion;
	}

	public VolumeType GetCurrentVolumeType()
	{
		return _currentVolume;
	}

	public bool InsideAnyVolume(bool waterOnly = false, bool fullOnly = false)
	{
		_partial.RemoveWhere((entity_movement_volume v) => !v);
		_full.RemoveWhere((entity_movement_volume v) => !v);
		if (fullOnly)
		{
			if (!waterOnly)
			{
				return _full.Count > 0;
			}
			return _full.AsValueEnumerable().Any((entity_movement_volume x) => x.waterVolume);
		}
		if (waterOnly)
		{
			if (!_full.AsValueEnumerable().Any((entity_movement_volume x) => x.waterVolume))
			{
				return _partial.AsValueEnumerable().Any((entity_movement_volume x) => x.waterVolume);
			}
			return true;
		}
		if (_full.Count <= 0)
		{
			return _partial.Count > 0;
		}
		return true;
	}
}
