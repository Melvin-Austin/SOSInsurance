using UnityEngine;

namespace HyenaQuest;

public class entity_prop_delivery_avi : entity_prop_delivery
{
	private float _lastOwnerCD;

	protected override void OnNetworkPostSpawn()
	{
		base.OnNetworkPostSpawn();
		if (!base.IsClient)
		{
			return;
		}
		_grabbingOwnerId.RegisterOnValueChanged(delegate(byte _, byte newValue)
		{
			if (!(Time.time < _lastOwnerCD))
			{
				_lastOwnerCD = Time.time + 2f;
				if (newValue != byte.MaxValue)
				{
					NetController<SoundController>.Instance.Play3DSound($"Ingame/Props/Special/Avi/avi_sound_{Random.Range(0, 5)}.ogg", base.transform.position, new AudioData
					{
						distance = 5f,
						volume = 0.15f,
						parent = this
					});
				}
			}
		});
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
		return "entity_prop_delivery_avi";
	}
}
