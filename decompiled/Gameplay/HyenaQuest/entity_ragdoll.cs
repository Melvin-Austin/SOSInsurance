using System.Collections.Generic;
using FailCake;
using UnityEngine;

namespace HyenaQuest;

public class entity_ragdoll : MonoBehaviour
{
	public GameObject vfxFlies;

	public GameObject model;

	private entity_player_badge _badge;

	private List<Rigidbody> _rigidbodies;

	private entity_player _owner;

	private util_timer _fliesTimer;

	private Dictionary<string, Transform> _ragdollBoneMap = new Dictionary<string, Transform>();

	private readonly List<(SkinnedMeshRenderer renderer, bool wasEnabled)> _borrowedAccessories = new List<(SkinnedMeshRenderer, bool)>();

	public void Awake()
	{
		if (!model)
		{
			throw new UnityException("Missing entity_ragdoll model");
		}
		_badge = GetComponentInChildren<entity_player_badge>(includeInactive: true);
		if (!_badge)
		{
			throw new UnityException("entity_ragdoll requires an entity_player_badge component.");
		}
		_rigidbodies = new List<Rigidbody>(GetComponentsInChildren<Rigidbody>(includeInactive: true));
		List<Rigidbody> rigidbodies = _rigidbodies;
		if (rigidbodies == null || rigidbodies.Count <= 0)
		{
			throw new UnityException("entity_ragdoll requires at least one Rigidbody component to work.");
		}
		if (!vfxFlies)
		{
			throw new UnityException("Ragdoll flies VFX not set.");
		}
		vfxFlies.SetActive(value: false);
		SetEnabled(enable: false);
	}

	public void SetOwner(entity_player owner)
	{
		if (!owner)
		{
			throw new UnityException("Invalid owner");
		}
		_owner = owner;
		SkinnedMeshRenderer componentInChildren = model.GetComponentInChildren<SkinnedMeshRenderer>(includeInactive: true);
		if (!componentInChildren)
		{
			return;
		}
		Transform[] bones = componentInChildren.bones;
		foreach (Transform transform in bones)
		{
			if ((bool)transform)
			{
				_ragdollBoneMap[transform.name] = transform;
			}
		}
	}

	public void SetEnabled(bool enable)
	{
		if ((bool)_owner)
		{
			SkinnedMeshRenderer[] componentsInChildren = model.GetComponentsInChildren<SkinnedMeshRenderer>(includeInactive: true);
			SkinnedMeshRenderer[] componentsInChildren2 = _owner.model.GetComponentsInChildren<SkinnedMeshRenderer>(includeInactive: true);
			int num = Mathf.Min(componentsInChildren.Length, componentsInChildren2.Length);
			for (int i = 0; i < num; i++)
			{
				if (!componentsInChildren[i] || !componentsInChildren2[i])
				{
					continue;
				}
				Transform[] bones = componentsInChildren[i].bones;
				Transform[] bones2 = componentsInChildren2[i].bones;
				int num2 = Mathf.Min(bones.Length, bones2.Length);
				for (int j = 0; j < num2; j++)
				{
					if ((bool)bones[j] && (bool)bones2[j])
					{
						bones[j].localPosition = bones2[j].localPosition;
						bones[j].localRotation = bones2[j].localRotation;
					}
				}
			}
		}
		foreach (Rigidbody rigidbody in _rigidbodies)
		{
			rigidbody.isKinematic = !enable;
		}
		if (enable)
		{
			BorrowAccessories();
		}
		else
		{
			ReturnAccessories();
		}
		_fliesTimer?.Stop();
		if (enable)
		{
			_fliesTimer = util_timer.Simple(Random.Range(20, 40), delegate
			{
				if ((bool)vfxFlies)
				{
					vfxFlies.SetActive(value: true);
				}
			});
		}
		else
		{
			vfxFlies.SetActive(value: false);
		}
	}

	private void BorrowAccessories()
	{
		ReturnAccessories();
		if (!_owner)
		{
			return;
		}
		Dictionary<string, Transform> ragdollBoneMap = _ragdollBoneMap;
		if (ragdollBoneMap == null || ragdollBoneMap.Count <= 0)
		{
			return;
		}
		foreach (SkinnedMeshRenderer activeAccessory in _owner.GetActiveAccessories())
		{
			if ((bool)activeAccessory)
			{
				_borrowedAccessories.Add((activeAccessory, activeAccessory.enabled));
				activeAccessory.transform.parent.SetParent(model.transform, worldPositionStays: false);
				RemapBones(activeAccessory, _ragdollBoneMap);
				activeAccessory.enabled = true;
			}
		}
	}

	private void ReturnAccessories()
	{
		if (_borrowedAccessories.Count <= 0 || !_owner)
		{
			return;
		}
		Dictionary<string, Transform> boneMap = _owner.GetBoneMap();
		foreach (var (skinnedMeshRenderer, flag) in _borrowedAccessories)
		{
			if ((bool)skinnedMeshRenderer)
			{
				skinnedMeshRenderer.transform.parent.SetParent(_owner.model, worldPositionStays: false);
				if (boneMap != null && boneMap.Count > 0)
				{
					RemapBones(skinnedMeshRenderer, boneMap);
				}
				skinnedMeshRenderer.enabled = flag;
			}
		}
		_borrowedAccessories.Clear();
	}

	private void RemapBones(SkinnedMeshRenderer render, Dictionary<string, Transform> boneMap)
	{
		Transform[] bones = render.bones;
		for (int i = 0; i < bones.Length; i++)
		{
			if ((bool)bones[i] && boneMap.TryGetValue(bones[i].name, out var value))
			{
				bones[i] = value;
			}
		}
		render.bones = bones;
	}

	public void OnDestroy()
	{
		ReturnAccessories();
		_fliesTimer?.Stop();
	}

	public void UpdateBadge()
	{
		if (!_owner)
		{
			throw new UnityException("Owner not set.");
		}
		if (!_badge)
		{
			throw new UnityException("Badge not set.");
		}
		_badge.SetOwner(_owner);
		_badge.SetPlayerName(_owner.GetPlayerName(), _owner.IsDeveloperOrFriend());
		_badge.SetPlayerIcon(_owner.GetPlayerAvatar());
		_badge.SetDeathStats(_owner.GetPlayerDeaths());
		_badge.SetDeliveryStats(_owner.GetPlayerDeliveries());
		_badge.SetScrapStats(_owner.GetPlayerScrap());
		_badge.SetPlayerID(_owner.GetPlayerID());
		_badge.SetBadges(_owner.GetBadgeData());
		_badge.SetHealth(0);
	}

	public void UpdateSkin()
	{
		if (!_owner)
		{
			throw new UnityException("Owner not set.");
		}
		SkinnedMeshRenderer[] componentsInChildren = model.GetComponentsInChildren<SkinnedMeshRenderer>(includeInactive: true);
		SkinnedMeshRenderer[] componentsInChildren2 = _owner.model.GetComponentsInChildren<SkinnedMeshRenderer>(includeInactive: true);
		SkinnedMeshRenderer headRenderer = _owner.GetHeadRenderer();
		int num = Mathf.Min(componentsInChildren.Length, componentsInChildren2.Length);
		for (int i = 0; i < num; i++)
		{
			if (!componentsInChildren[i] || !componentsInChildren2[i])
			{
				continue;
			}
			componentsInChildren[i].sharedMaterials = componentsInChildren2[i].sharedMaterials;
			componentsInChildren[i].transform.localScale = componentsInChildren2[i].transform.localScale;
			componentsInChildren[i].gameObject.SetActive(componentsInChildren2[i].gameObject.activeSelf || componentsInChildren2[i] == headRenderer);
			for (int j = 0; j < componentsInChildren2[i].sharedMesh.blendShapeCount; j++)
			{
				float value = componentsInChildren2[i].GetBlendShapeWeight(j);
				if (i == 0)
				{
					value = 0f;
				}
				componentsInChildren[i].SetBlendShapeWeight(j, value);
			}
		}
	}
}
