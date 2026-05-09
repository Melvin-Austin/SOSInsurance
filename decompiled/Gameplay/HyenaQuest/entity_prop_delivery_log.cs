using UnityEngine;

namespace HyenaQuest;

public class entity_prop_delivery_log : entity_prop_delivery_easter_chance
{
	private AudioSource _audio;

	private entity_led _led;

	protected override void Init()
	{
		base.Init();
		_led = GetComponentInChildren<entity_led>(includeInactive: true);
		if (!_led)
		{
			throw new UnityException("entity_prop_delivery_log requires entity_led object");
		}
		_audio = GetComponentInChildren<AudioSource>(includeInactive: true);
		if (!_audio)
		{
			throw new UnityException("entity_prop_delivery_log requires AudioSource object");
		}
	}

	protected override void OnEaster(byte indx)
	{
		if ((bool)_audio && (bool)_led && indx == 1)
		{
			_led.SetActive(enable: true);
			_audio.Play();
		}
	}

	protected override void __initializeVariables()
	{
		base.__initializeVariables();
	}

	protected override void __initializeRpcs()
	{
		base.__initializeRpcs();
	}

	protected internal override string __getTypeName()
	{
		return "entity_prop_delivery_log";
	}
}
