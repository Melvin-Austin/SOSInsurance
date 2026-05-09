using System.Collections;
using System.Collections.Generic;
using System.Linq;
using FailCake;
using UnityEngine;
using UnityEngine.Pool;
using UnityEngine.UI;

namespace HyenaQuest;

public class DamageController : MonoController<DamageController>
{
	public List<DamageTemplates> damages = new List<DamageTemplates>();

	public List<Image> damageImage = new List<Image>();

	public Image mainBloodFade;

	private float _lastKillCD;

	private float _lastCleanCheck;

	private readonly Dictionary<GameObject, float> _lastHurt = new Dictionary<GameObject, float>();

	private readonly Dictionary<GameObject, Vector3> _lastHurtPos = new Dictionary<GameObject, Vector3>();

	private util_fade_timer[] _fades;

	private util_fade_timer _mainRedFade;

	private int _damageCount;

	private readonly Dictionary<GameObject, ObjectPool<GameObject>> _templatePools = new Dictionary<GameObject, ObjectPool<GameObject>>();

	private bool IsServer
	{
		get
		{
			if ((bool)NETController.Instance)
			{
				return NETController.Instance.IsServer;
			}
			return false;
		}
	}

	public new void Awake()
	{
		base.Awake();
		if (!mainBloodFade)
		{
			throw new UnityException("Main blood fade is not set");
		}
		List<Image> list = damageImage;
		if (list == null || list.Count <= 0)
		{
			throw new UnityException("Damage image is not set");
		}
		_fades = new util_fade_timer[damageImage.Count];
		SDK.OnDamageRequest = OnDamageRequest;
		SDK.OnKillRequest = OnKillRequest;
		InitializePools();
	}

	private void InitializePools()
	{
		foreach (DamageTemplates damage in damages)
		{
			if (damage.templates == null || damage.templates.Count == 0)
			{
				continue;
			}
			foreach (GameObject template in damage.templates)
			{
				if ((bool)template && !_templatePools.ContainsKey(template))
				{
					_templatePools.Add(template, new ObjectPool<GameObject>(() => Object.Instantiate(template, base.transform), delegate(GameObject instance)
					{
						instance.SetActive(value: true);
					}, delegate(GameObject instance)
					{
						instance.SetActive(value: false);
					}, Object.Destroy, collectionCheck: true, 2, 10));
				}
			}
		}
	}

	public new void OnDestroy()
	{
		SDK.OnDamageRequest = null;
		SDK.OnKillRequest = null;
		if (_fades != null)
		{
			util_fade_timer[] fades = _fades;
			for (int i = 0; i < fades.Length; i++)
			{
				fades[i]?.Stop();
			}
		}
		_mainRedFade?.Stop();
		_templatePools.Clear();
		base.OnDestroy();
	}

	public void Damage(DamageType type)
	{
		if (!PlayerController.LOCAL)
		{
			throw new UnityException("Local player is not set");
		}
		DamageTemplates damage = damages.Find((DamageTemplates d) => (d.type & type) != 0);
		if (damage == null)
		{
			throw new UnityException($"Damage {type} not found");
		}
		List<GameObject> templates = damage.templates;
		if (templates != null && templates.Count > 0)
		{
			GameObject gameObject = damage.templates[Random.Range(0, damage.templates.Count)];
			if (!gameObject)
			{
				throw new UnityException($"Damage {type} template not found");
			}
			if (!_templatePools.TryGetValue(gameObject, out var value))
			{
				throw new UnityException("Pool for damage template");
			}
			GameObject gameObject2 = value.Get();
			if (!gameObject2)
			{
				throw new UnityException($"Damage {type} instance not created");
			}
			if (!damage.fullscreen)
			{
				RectTransform component = gameObject2.GetComponent<RectTransform>();
				if ((bool)component)
				{
					float num = component.rect.width / 2f;
					float num2 = component.rect.height / 2f;
					float x = Random.Range(0f - num, num);
					float y = Random.Range(0f - num2, num2);
					component.anchoredPosition = new Vector2(x, y);
					float z = Random.Range(0f, 360f);
					component.rotation = Quaternion.Euler(0f, 0f, z);
				}
			}
			StartCoroutine(ReturnToPool(gameObject2, value, 2f));
		}
		List<AudioClip> sounds = damage.sounds;
		if (sounds != null && sounds.Count > 0)
		{
			AudioClip audioClip = damage.sounds[Random.Range(0, damage.sounds.Count)];
			if (!audioClip)
			{
				throw new UnityException("Damage sound not found");
			}
			AudioData data = new AudioData
			{
				pitch = Random.Range(0.9f, 1.1f),
				volume = Random.Range(0.7f, 1f),
				distance = 2f
			};
			NetController<SoundController>.Instance.Play3DSound(audioClip, SDK.MainCamera.transform, data, broadcast: true);
		}
		if (damage.shakeDuration > 0f)
		{
			NetController<ShakeController>.Instance.LocalShake(ShakeMode.SHAKE_ALL, damage.shakeDuration, damage.shakeIntensity);
		}
		_mainRedFade?.Stop();
		_mainRedFade = util_fade_timer.Fade(4f, 1f, 0f, delegate(float fade)
		{
			mainBloodFade.color = new Color(damage.overlayColor.r, damage.overlayColor.g, damage.overlayColor.b, fade);
		});
		_damageCount = Mathf.Clamp(_damageCount + 1, 0, damageImage.Count);
		for (int i = 0; i < _damageCount; i++)
		{
			int i2 = i;
			Image damageImg = damageImage[i];
			if ((bool)damageImg)
			{
				damageImg.enabled = true;
				if (_fades[i2] != null)
				{
					_fades[i2].Stop();
				}
				_fades[i2] = util_fade_timer.Fade(1f, 1f, 0f, delegate(float alpha)
				{
					damageImg.color = new Color(damage.overlayColor.r, damage.overlayColor.g, damage.overlayColor.b, alpha);
				}, delegate
				{
					damageImg.enabled = false;
					_damageCount = Mathf.Clamp(_damageCount - 1, 0, damageImage.Count);
					_fades[i2] = null;
				});
			}
		}
	}

	private void Update()
	{
		if (!(Time.time < _lastCleanCheck))
		{
			_lastCleanCheck = Time.time + 1f;
			Cleanup();
		}
	}

	private IEnumerator ReturnToPool(GameObject instance, ObjectPool<GameObject> pool, float delay)
	{
		yield return new WaitForSeconds(delay);
		if ((bool)instance)
		{
			pool?.Release(instance);
		}
	}

	private void Cleanup()
	{
		foreach (GameObject item in _lastHurt.Keys.ToList())
		{
			if (!item)
			{
				_lastHurt.Remove(item);
				_lastHurtPos.Remove(item);
			}
		}
	}

	private void OnDamageRequest(DamageType damageType, byte damage, float cooldown, bool damageOnMove, Collider col)
	{
		if (IsServer && col.TryGetComponent<entity_monster_ai>(out var component))
		{
			if (CheckAndUpdateCooldown(col, damageOnMove, cooldown))
			{
				component.TakeHealth(damage);
			}
		}
		else if (!(col.gameObject != PlayerController.LOCAL.gameObject) && !PlayerController.LOCAL.IsDead() && CheckAndUpdateCooldown(col, damageOnMove, cooldown))
		{
			PlayerController.LOCAL.TakeHealth(damage, damageType);
		}
	}

	private bool CheckAndUpdateCooldown(Collider col, bool damageOnMove, float cooldown)
	{
		if (_lastHurt.ContainsKey(col.gameObject) && _lastHurt[col.gameObject] > Time.time)
		{
			return false;
		}
		_lastHurt[col.gameObject] = Time.time + cooldown;
		if (!damageOnMove)
		{
			return true;
		}
		if (_lastHurtPos.ContainsKey(col.gameObject) && col.transform.position == _lastHurtPos[col.gameObject])
		{
			return false;
		}
		_lastHurtPos[col.gameObject] = col.transform.position;
		return true;
	}

	private void OnKillRequest(DamageType damageType, Collider col)
	{
		if (IsServer)
		{
			entity_phys component2;
			if (!col.attachedRigidbody)
			{
				if (col.TryGetComponent<entity_monster_ai>(out var component) && component.IsSpawned)
				{
					component.Kill();
					return;
				}
			}
			else if (!col.CompareTag("ENTITY/PHYS-SHARD") && col.attachedRigidbody.TryGetComponent<entity_phys>(out component2) && component2.IsSpawned)
			{
				component2.Destroy();
				return;
			}
		}
		if (!(Time.time < _lastKillCD) && (bool)PlayerController.LOCAL && PlayerController.LOCAL.IsSpawned && !PlayerController.LOCAL.IsDead() && col.gameObject == PlayerController.LOCAL.gameObject)
		{
			_lastKillCD = Time.time + 2f;
			PlayerController.LOCAL.TakeHealth(byte.MaxValue, damageType);
		}
	}
}
