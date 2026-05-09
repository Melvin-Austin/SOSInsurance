using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.VFX;

namespace HyenaQuest;

public class entity_particle_effect : MonoBehaviour
{
	public List<AudioClip> soundFx = new List<AudioClip>();

	public bool playOnAwake;

	public float hearingDistance = 3f;

	public int count = 30;

	public float delay;

	private VisualEffect _vfx;

	private ParticleSystem _ps;

	private bool _playing;

	public void Awake()
	{
		_vfx = GetComponent<VisualEffect>();
		if (!_vfx)
		{
			_vfx = GetComponentInChildren<VisualEffect>(includeInactive: true);
		}
		_ps = GetComponent<ParticleSystem>();
		if (!_ps)
		{
			_ps = GetComponentInChildren<ParticleSystem>(includeInactive: true);
		}
		if (!_vfx && !_ps)
		{
			throw new UnityException("entity_particle_effect requires either a VisualEffect or ParticleSystem component");
		}
		if ((bool)_vfx && (bool)_ps)
		{
			throw new UnityException("entity_particle_effect can either have VisualEffect or ParticleSystem components, not both!");
		}
	}

	public void OnEnable()
	{
		if (playOnAwake)
		{
			Play();
		}
		else
		{
			Stop();
		}
	}

	public void OnDestroy()
	{
		if ((bool)_vfx)
		{
			_vfx.ResetOverride("SkinnedMeshRendererPropertyID");
		}
	}

	public virtual EffectType GetEffectType()
	{
		return EffectType.UNDEFINED;
	}

	public void Update()
	{
		if (_playing && (bool)_ps && !_ps.isPlaying)
		{
			Finish();
		}
	}

	public virtual void Play(bool playSound = true, float volume = 1f)
	{
		StopAllCoroutines();
		_playing = false;
		if (playSound && soundFx.Count != 0)
		{
			NetController<SoundController>.Instance.Play3DSound(soundFx[Random.Range(0, soundFx.Count)], base.transform, new AudioData
			{
				volume = volume,
				pitch = Random.Range(0.85f, 1.15f),
				distance = hearingDistance
			});
		}
		if ((bool)_vfx)
		{
			_vfx.Reinit();
			_vfx.Play();
			if (count > 0 && _vfx.HasInt("Count"))
			{
				_vfx.SetInt("Count", count);
			}
			if (delay > 0f && _vfx.HasFloat("Delay"))
			{
				_vfx.SetFloat("Delay", delay);
			}
			if (base.isActiveAndEnabled)
			{
				StartCoroutine(WaitForVFXComplete());
			}
		}
		if ((bool)_ps)
		{
			ParticleSystem.MainModule main = _ps.main;
			if (delay > 0f)
			{
				main.startDelay = delay;
			}
			if (count > 0)
			{
				main.maxParticles = count;
			}
			main.loop = false;
			_ps.Clear(withChildren: true);
			_ps.Stop(withChildren: true);
			_ps.Play(withChildren: true);
		}
		_playing = true;
	}

	public virtual void Stop()
	{
		if (_playing)
		{
			_playing = false;
			StopAllCoroutines();
			if ((bool)_vfx)
			{
				_vfx.ResetOverride("SkinnedMeshRendererPropertyID");
				_vfx.Reinit();
				_vfx.Stop();
			}
			if ((bool)_ps)
			{
				_ps.Stop(withChildren: true);
			}
		}
	}

	private void Finish()
	{
		Stop();
		if (GetEffectType() != 0)
		{
			NetController<EffectController>.Instance?.OnParticleFinish(this);
		}
	}

	private IEnumerator WaitForVFXComplete()
	{
		yield return new WaitForSeconds(0.1f);
		int frames = 0;
		while (frames < 10 && (bool)_vfx && base.isActiveAndEnabled)
		{
			frames = ((_vfx.aliveParticleCount == 0) ? (frames + 1) : 0);
			yield return null;
		}
		Finish();
	}
}
