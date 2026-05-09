using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace HyenaQuest;

public class entity_prop_delivery_animal : entity_prop_delivery_easter_chance
{
	public List<AudioClip> animals = new List<AudioClip>();

	private AudioSource _easterSnd;

	private byte _lastPlayed;

	protected override void Init()
	{
		base.Init();
		_easterSnd = GetComponentInChildren<AudioSource>(includeInactive: true);
		if (!_easterSnd)
		{
			throw new UnityException("Missing AudioSource");
		}
		_easterSnd.playOnAwake = false;
		_easterSnd.loop = false;
	}

	protected override float GetEasterHitChance()
	{
		return 0.2f;
	}

	protected override void OnEaster(byte indx)
	{
		if ((bool)_easterSnd && indx != 0)
		{
			System.Random rnd = new System.Random((int)base.NetworkObjectId);
			_lastPlayed = (byte)new List<int>(from i in Enumerable.Range(0, animals.Count)
				where i != _lastPlayed
				select i).OrderBy((int _) => rnd.NextDouble()).FirstOrDefault();
			_easterSnd.Stop();
			_easterSnd.clip = animals[_lastPlayed];
			_easterSnd.Play();
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
		return "entity_prop_delivery_animal";
	}
}
