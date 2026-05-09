using UnityEngine;

namespace HyenaQuest;

public class entity_prop_delivery_computer : entity_prop_delivery_easter_chance
{
	public GameObject canvas;

	protected override void Init()
	{
		base.Init();
		if (!canvas)
		{
			throw new UnityException("entity_prop_delivery_computer requires canvas object");
		}
		canvas.SetActive(value: false);
	}

	protected override void OnEaster(byte indx)
	{
		if (indx == 1)
		{
			canvas.SetActive(value: true);
			NetController<SoundController>.Instance?.Play3DSound($"Ingame/Props/Special/Computer/analog_computer_beep_{Random.Range(0, 2)}.ogg", base.transform.position, new AudioData
			{
				pitch = Random.Range(0.8f, 1.2f),
				volume = 0.5f
			});
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
		return "entity_prop_delivery_computer";
	}
}
