using System;
using FailCake;
using UnityEngine;

namespace HyenaQuest;

[RequireComponent(typeof(Light))]
[DisallowMultipleComponent]
public class entity_light : MonoBehaviour
{
	public PowerGrid area = PowerGrid.UNCONTROLLED;

	public bool on;

	public bool breakable;

	private Light _light;

	private entity_led_material _led;

	private float _intensity;

	private bool _broken;

	private Color _color;

	private util_timer _flickerTimer;

	public void Awake()
	{
		_light = GetComponent<Light>();
		if (!_light)
		{
			throw new UnityException("Light component not found on entity_light");
		}
		_led = GetComponent<entity_led_material>();
		_intensity = _light.intensity;
		_color = _light.color;
		CoreController.WaitFor(delegate(LightController lightCtrl)
		{
			lightCtrl.OnLightAreaCommand += new Action<PowerGrid, LightCommand, bool>(OnLightAreaCommand);
		});
	}

	public void OnDestroy()
	{
		_flickerTimer?.Stop();
		if ((bool)NetController<LightController>.Instance)
		{
			NetController<LightController>.Instance.OnLightAreaCommand -= new Action<PowerGrid, LightCommand, bool>(OnLightAreaCommand);
		}
	}

	public bool IsOn()
	{
		return on;
	}

	public bool IsSpotlight()
	{
		Light light = _light;
		if ((object)light == null)
		{
			return false;
		}
		return light.type == LightType.Spot;
	}

	public float GetSpotlightAngle()
	{
		return _light?.spotAngle ?? 0f;
	}

	public float GetRange()
	{
		return _light?.range ?? 0f;
	}

	[Client]
	public void SetIntensity(float intensity)
	{
		if ((bool)_light)
		{
			_light.intensity = intensity;
			_intensity = intensity;
		}
	}

	[Client]
	public float GetIntensity()
	{
		if (!_light)
		{
			return 0f;
		}
		return _light.intensity;
	}

	[Client]
	public void SetColor(Color? color)
	{
		if ((bool)_light)
		{
			_light.color = color ?? _color;
		}
	}

	[Client]
	public void Break()
	{
		if (breakable && !_broken)
		{
			SDK.Play3DSound?.Invoke("General/Entities/Light/light_break.ogg", base.transform.position, new AudioData
			{
				distance = 3f,
				volume = 0.8f
			}, arg4: false);
			_light.intensity = 0f;
			_broken = true;
		}
	}

	[Client]
	public void Restore()
	{
		if (breakable && _broken)
		{
			_light.intensity = _intensity;
			_broken = false;
		}
	}

	[Client]
	public void Flicker(bool stayOn = true)
	{
		_flickerTimer?.Stop();
		_flickerTimer = util_timer.Create(UnityEngine.Random.Range(2, 8), 0.06f, delegate
		{
			_light.intensity = UnityEngine.Random.Range(0.25f, _intensity);
		}, delegate
		{
			_light.intensity = _intensity;
			if (!stayOn)
			{
				SetLightStatus(enable: false);
			}
		});
	}

	[Client]
	public void SetLightStatus(bool enable, bool skipAudio = false)
	{
		if (enable != on)
		{
			on = enable;
			if ((bool)_led)
			{
				_led.SetActive(on);
			}
			if ((bool)_light)
			{
				_light.enabled = on;
			}
			if (!skipAudio)
			{
				SDK.Play3DSound?.Invoke(enable ? "General/Entities/Light/light_on.ogg" : "General/Entities/Light/light_off.ogg", base.transform.position, new AudioData
				{
					pitch = UnityEngine.Random.Range(0.8f, 1.2f),
					distance = 2f,
					volume = 0.15f
				}, arg4: false);
			}
		}
	}

	[Client]
	private void OnLightAreaCommand(PowerGrid lightArea, LightCommand command, bool server)
	{
		if (lightArea != PowerGrid.UNCONTROLLED && lightArea == area)
		{
			switch (command)
			{
			case LightCommand.OFF:
				SetLightStatus(enable: false);
				break;
			case LightCommand.ON:
				SetLightStatus(enable: true);
				break;
			case LightCommand.FLICKER:
				Flicker();
				break;
			case LightCommand.FLICKER_OFF:
				Flicker(stayOn: false);
				break;
			}
		}
	}
}
