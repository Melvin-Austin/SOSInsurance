using System.Collections.Generic;
using UnityEngine;

namespace HyenaQuest;

public class entity_client_button : entity_client_usable
{
	public float cooldown = 1f;

	public List<AudioClip> pressSounds = new List<AudioClip>();

	public AudioClip oddSound;

	private bool _isOdd;

	private float _cooldownTimer;

	public override void OnPlayerUse(entity_player player)
	{
		if (!player || IsLocked() || !NetController<SoundController>.Instance || _cooldownTimer > Time.time)
		{
			return;
		}
		_cooldownTimer = Time.time + cooldown;
		List<AudioClip> list = pressSounds;
		if (list != null && list.Count > 0 && (bool)oddSound)
		{
			if ((bool)oddSound)
			{
				_isOdd = !_isOdd;
			}
			NetController<SoundController>.Instance.Play3DSound(((bool)oddSound && _isOdd) ? oddSound : pressSounds[Random.Range(0, pressSounds.Count)], base.transform.position, new AudioData
			{
				volume = 0.6f,
				distance = 5f
			});
		}
		base.OnPlayerUse(player);
	}
}
