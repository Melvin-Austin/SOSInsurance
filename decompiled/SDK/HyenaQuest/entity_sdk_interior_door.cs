using System.Collections.Generic;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;

namespace HyenaQuest;

[DisallowMultipleComponent]
[RequireComponent(typeof(NetworkObject), typeof(NetworkTransform))]
[RequireComponent(typeof(NetworkRigidbody), typeof(Rigidbody))]
public class entity_sdk_interior_door : NetworkBehaviour
{
	public List<GameObject> layers = new List<GameObject>();

	public GameObject trap;

	public SoundTypes collisionMaterial;

	public List<AudioClip> collideSounds = new List<AudioClip>();

	public List<AudioClip> damageSounds = new List<AudioClip>();

	private bool UseCustomSounds => collisionMaterial == SoundTypes.CUSTOM;

	public void Awake()
	{
		SDK.PatchSDKEntity?.Invoke(base.gameObject);
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
		return "entity_sdk_interior_door";
	}
}
