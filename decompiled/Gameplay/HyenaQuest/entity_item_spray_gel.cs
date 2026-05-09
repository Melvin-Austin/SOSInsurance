using UnityEngine;

namespace HyenaQuest;

public class entity_item_spray_gel : entity_item_spray_base
{
	public GameObject gelPrefab;

	private AudioSource _audio;

	protected override void OnNetworkPostSpawn()
	{
		base.OnNetworkPostSpawn();
		if (!base.IsClient)
		{
			return;
		}
		_spraying.RegisterOnValueChanged(delegate(bool _, bool newVal)
		{
			if ((bool)_audio)
			{
				if (newVal)
				{
					if ((bool)NetController<SoundController>.Instance)
					{
						_audio.clip = NetController<SoundController>.Instance.GetClip($"Ingame/Entities/Spraycan/spray_loop_{Random.Range(0, 2)}.ogg");
						_audio.Play();
					}
				}
				else
				{
					_audio.Stop();
				}
			}
		});
	}

	public override void OnNetworkPreDespawn()
	{
		base.OnNetworkPreDespawn();
		if (base.IsClient)
		{
			_spraying.OnValueChanged = null;
		}
	}

	protected override Vector3 GetSpraySize()
	{
		return new Vector3(Random.Range(1.45f, 1.65f), Random.Range(1.45f, 1.65f), 0.2f);
	}

	protected override GameObject GetDecalTemplate()
	{
		return Object.Instantiate(gelPrefab);
	}

	protected override void Init()
	{
		base.Init();
		_audio = GetComponentInChildren<AudioSource>(includeInactive: true);
		if (!_audio)
		{
			throw new UnityException("Missing AudioSource component");
		}
		_audio.Stop();
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
		return "entity_item_spray_gel";
	}
}
