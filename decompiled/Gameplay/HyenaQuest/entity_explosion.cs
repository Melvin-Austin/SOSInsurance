using System.Collections;
using System.Collections.Generic;
using FailCake;
using UnityEngine;
using UnityEngine.VFX;

namespace HyenaQuest;

[RequireComponent(typeof(entity_attractor))]
public class entity_explosion : MonoBehaviour
{
	public List<VisualEffect> bigExplosionFX;

	public List<VisualEffect> mediumExplosionFX;

	public List<VisualEffect> smallExplosionFX;

	public List<AudioClip> smallExplosionSound;

	public List<AudioClip> mediumExplosionSound;

	public List<AudioClip> bigExplosionSound;

	private float _distance;

	private entity_attractor _attractor;

	private VisualEffect _pickedFX;

	public void Awake()
	{
		_attractor = GetComponent<entity_attractor>();
		if (!_attractor)
		{
			throw new UnityException("entity_explosion requires entity_attractor component");
		}
	}

	[Client]
	public void SetDistance(float distance)
	{
		if (distance <= 0f)
		{
			throw new UnityException("Invalid distance value");
		}
		List<VisualEffect> list = ((distance >= 7f) ? bigExplosionFX : ((!(distance >= 3f)) ? smallExplosionFX : mediumExplosionFX));
		List<VisualEffect> list2 = list;
		if (list2 == null || list2.Count == 0)
		{
			throw new UnityException("Missing explosion FX");
		}
		_pickedFX = list2[Random.Range(0, list2.Count)];
		if (!_pickedFX)
		{
			throw new UnityException("Missing explosion FX");
		}
		_pickedFX.gameObject.SetActive(value: true);
		_pickedFX.transform.localScale = Vector3.one * (0.425f * distance);
		_attractor.distance = distance;
		_attractor.force = 5f * distance;
		_attractor.enabled = true;
		_distance = distance;
	}

	[Client]
	public void Explode()
	{
		if (!_pickedFX)
		{
			throw new UnityException("Missing explosion FX");
		}
		_pickedFX.Play();
		util_timer.Simple(0.05f, delegate
		{
			_attractor.enabled = false;
		});
		StartCoroutine(WaitForVFXComplete());
		float distance = _distance;
		List<AudioClip> list = ((distance >= 7f) ? bigExplosionSound : ((!(distance >= 3f)) ? smallExplosionSound : mediumExplosionSound));
		List<AudioClip> list2 = list;
		if (list2 == null || list2.Count == 0)
		{
			throw new UnityException("Missing explosion SFX");
		}
		NetController<SoundController>.Instance.Play3DSound(list2[Random.Range(0, list2.Count)], base.transform.position, new AudioData
		{
			pitch = Random.Range(0.7f, 1.3f),
			distance = _distance + 10f
		});
		NetController<ShakeController>.Instance.Local3DShake(base.transform.position, ShakeMode.SHAKE_ALL, 0.15f, 0.4f * _distance, ShakeSoundMode.OFF, _distance);
	}

	private IEnumerator WaitForVFXComplete()
	{
		yield return new WaitForSeconds(0.1f);
		int frames = 0;
		while (frames < 10 && (bool)_pickedFX && base.isActiveAndEnabled)
		{
			frames = ((_pickedFX.aliveParticleCount == 0) ? (frames + 1) : 0);
			yield return null;
		}
		Object.Destroy(base.gameObject);
	}
}
