using System;
using System.Collections.Generic;
using FailCake;
using UnityEngine;
using VolumetricFogAndMist2;

namespace HyenaQuest;

[DisallowMultipleComponent]
[DefaultExecutionOrder(-69)]
public class FOGController : MonoController<FOGController>
{
	private static readonly FogSettings FALLBACK_FOG = new FogSettings
	{
		color = Color.black,
		density = 0.12f
	};

	private static readonly Dictionary<VolumeType, FogSettings> VOLUME_FOG_SETTINGS = new Dictionary<VolumeType, FogSettings>
	{
		{
			VolumeType.WATER,
			new FogSettings
			{
				color = new Color(0.102f, 0.169f, 0.2f, 1f),
				density = 0.12f
			}
		},
		{
			VolumeType.QUICKSAND,
			new FogSettings
			{
				color = new Color(0.5f, 0.4f, 0.3f, 1f),
				density = 0.98f
			}
		}
	};

	private static readonly Dictionary<bool, float> FOG_DENSITY_POWER_SETTINGS = new Dictionary<bool, float>
	{
		{ false, 6f },
		{ true, 1f }
	};

	private VolumeType _currentVolumeType;

	private FogVoidManager _fogVoidManager;

	private util_fade_timer _fogDensityTimer;

	private float _currentDensityPower = 1f;

	public new void Awake()
	{
		base.Awake();
		CoreController.WaitFor(delegate(MapController mapCtrl)
		{
			mapCtrl.OnMapGenerated += new Action<bool>(OnMapUpdated);
			mapCtrl.OnMapCleared += new Action<bool>(OnMapUpdated);
		});
		CoreController.WaitFor(delegate(PowerController powerCtrl)
		{
			powerCtrl.OnGridUpdate += new Action<PowerGrid, bool, bool>(OnGridUpdate);
		});
		_fogVoidManager = GetComponentInChildren<FogVoidManager>(includeInactive: true);
		if (!_fogVoidManager)
		{
			throw new UnityException("No FogVoidManager found");
		}
		OnMapUpdated(server: false);
	}

	private void OnGridUpdate(PowerGrid grid, bool on, bool server)
	{
		if (server || grid != PowerGrid.MAP)
		{
			return;
		}
		float valueOrDefault = FOG_DENSITY_POWER_SETTINGS.GetValueOrDefault(!on, 1f);
		float valueOrDefault2 = FOG_DENSITY_POWER_SETTINGS.GetValueOrDefault(on, 1f);
		if (!Mathf.Approximately(valueOrDefault, valueOrDefault2))
		{
			_fogDensityTimer?.Stop();
			_fogDensityTimer = util_fade_timer.Fade(0.2f, valueOrDefault, valueOrDefault2, delegate(float f)
			{
				_currentDensityPower = f;
				UpdateFogSettings();
			});
		}
	}

	public new void OnDestroy()
	{
		_fogDensityTimer?.Stop();
		if ((bool)NetController<MapController>.Instance)
		{
			NetController<MapController>.Instance.OnMapGenerated -= new Action<bool>(OnMapUpdated);
			NetController<MapController>.Instance.OnMapCleared -= new Action<bool>(OnMapUpdated);
		}
		if ((bool)NetController<PowerController>.Instance)
		{
			NetController<PowerController>.Instance.OnGridUpdate -= new Action<PowerGrid, bool, bool>(OnGridUpdate);
		}
		base.OnDestroy();
	}

	public void SetInsideVolume(VolumeType type, bool inside)
	{
		_currentVolumeType = (inside ? type : VolumeType.NONE);
		UpdateFogSettings();
	}

	private void OnMapUpdated(bool server)
	{
		UpdateFogSettings();
		if ((bool)_fogVoidManager)
		{
			_fogVoidManager.trackingCenter = SDK.MainCamera?.transform;
			_fogVoidManager.TrackFogVoids(forceImmediateUpdate: true);
		}
	}

	private void UpdateFogSettings()
	{
		FogSettings defaultValue = (NetController<MapController>.Instance?.GetGeneratedWorld())?.fog ?? FALLBACK_FOG;
		if (_currentVolumeType == VolumeType.NONE)
		{
			RenderSettings.fogColor = defaultValue.color;
			RenderSettings.fogDensity = defaultValue.density * _currentDensityPower;
		}
		else
		{
			FogSettings valueOrDefault = VOLUME_FOG_SETTINGS.GetValueOrDefault(_currentVolumeType, defaultValue);
			RenderSettings.fogColor = valueOrDefault.color;
			RenderSettings.fogDensity = valueOrDefault.density;
		}
	}
}
