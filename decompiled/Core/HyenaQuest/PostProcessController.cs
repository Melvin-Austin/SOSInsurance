using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace HyenaQuest;

[DisallowMultipleComponent]
[DefaultExecutionOrder(-90)]
[RequireComponent(typeof(Volume))]
public class PostProcessController : MonoController<PostProcessController>
{
	public static readonly float BASE_EXPOSURE = 1f;

	public static readonly float MAX_EXPOSURE = 3f;

	private Volume _volume;

	private ColorAdjustments _colorVFX;

	private DepthOfField _depthOfField;

	private float _saturation;

	public new void Awake()
	{
		base.Awake();
		UnityEngine.Object.DontDestroyOnLoad(base.gameObject);
	}

	public new void OnDestroy()
	{
		ResetSaturation();
		ResetDepthOfField();
		base.OnDestroy();
	}

	public void ResetSaturation()
	{
		if ((bool)_colorVFX)
		{
			_colorVFX.saturation.value = _saturation;
		}
	}

	public void SetSaturation(float value)
	{
		if ((bool)_colorVFX && !Mathf.Approximately(_colorVFX.saturation.value, value))
		{
			_colorVFX.saturation.value = value;
		}
	}

	public void SetDepthOfField(float start, float end)
	{
		if ((bool)_depthOfField)
		{
			_depthOfField.mode.value = ((!(start <= 0f) || !(end <= 0f)) ? DepthOfFieldMode.Gaussian : DepthOfFieldMode.Off);
			_depthOfField.gaussianStart.value = start;
			_depthOfField.gaussianEnd.value = end;
		}
	}

	public void ResetDepthOfField()
	{
		if ((bool)_depthOfField)
		{
			_depthOfField.mode.value = DepthOfFieldMode.Off;
		}
	}

	public IEnumerator Init()
	{
		yield return new WaitForSecondsRealtime(0.2f);
		_volume = GetComponent<Volume>();
		if (!_volume)
		{
			throw new UnityException("Volume missing!");
		}
		_volume.profile.TryGet<ColorAdjustments>(out _colorVFX);
		if (!_colorVFX)
		{
			throw new UnityException("ColorAdjustments missing!");
		}
		_volume.profile.TryGet<DepthOfField>(out _depthOfField);
		if (!_depthOfField)
		{
			throw new UnityException("MotionBlur missing!");
		}
		_saturation = _colorVFX.saturation.value;
		_depthOfField.mode.value = DepthOfFieldMode.Off;
		CoreController.WaitFor(delegate(SettingsController settingsCtrl)
		{
			settingsCtrl.OnSettingsUpdated += new Action(OnSettingsUpdated);
		});
	}

	private void OnSettingsUpdated()
	{
		if ((bool)MonoController<SettingsController>.Instance && (bool)_colorVFX)
		{
			PlayerSettings currentSettings = MonoController<SettingsController>.Instance.CurrentSettings;
			_colorVFX.postExposure.overrideState = true;
			_colorVFX.postExposure.value = Mathf.Lerp(BASE_EXPOSURE, MAX_EXPOSURE, Mathf.Clamp01(currentSettings.brightness));
		}
	}
}
