using System.Collections.Generic;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;

namespace HyenaQuest;

[DisallowMultipleComponent]
[RequireComponent(typeof(NetworkObject), typeof(NetworkTransform))]
[RequireComponent(typeof(NetworkRigidbody), typeof(Rigidbody))]
public class entity_sdk_custom_phys_scrap : NetworkBehaviour
{
	public SoundTypes collisionMaterial;

	public List<AudioClip> collideSounds = new List<AudioClip>();

	[Range(1f, 100f)]
	public int scrap = 10;

	public GameObject viewModel;

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
		return "entity_sdk_custom_phys_scrap";
	}
}
