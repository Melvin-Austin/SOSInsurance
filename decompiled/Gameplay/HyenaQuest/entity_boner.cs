using System.Collections.Generic;
using UnityEngine;

namespace HyenaQuest;

public class entity_boner : MonoBehaviour
{
	private readonly Dictionary<HumanBodyBones, Quaternion> _boneRotationOverride = new Dictionary<HumanBodyBones, Quaternion>();

	private readonly Dictionary<AvatarIKGoal, Vector3> _bonePositionOverride = new Dictionary<AvatarIKGoal, Vector3>();

	private Animator _animator;

	public void Awake()
	{
		_animator = GetComponent<Animator>();
		if (!_animator)
		{
			throw new UnityException("entity_boner requires Animator component");
		}
	}

	public void SetBone(HumanBodyBones bone, Quaternion rotationOverride)
	{
		_boneRotationOverride[bone] = rotationOverride;
	}

	public void SetBone(AvatarIKGoal bone, Vector3 position)
	{
		_bonePositionOverride[bone] = position;
	}

	public void OnAnimatorIK(int layer)
	{
		if (!_animator)
		{
			return;
		}
		foreach (KeyValuePair<HumanBodyBones, Quaternion> item in _boneRotationOverride)
		{
			_animator.SetBoneLocalRotation(item.Key, item.Value);
		}
		foreach (KeyValuePair<AvatarIKGoal, Vector3> item2 in _bonePositionOverride)
		{
			_animator.SetIKPosition(item2.Key, item2.Value);
			_animator.SetIKPositionWeight(item2.Key, 1f);
		}
	}
}
