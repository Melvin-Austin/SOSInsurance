using System;
using UnityEngine;

namespace HyenaQuest;

public static class SDK
{
	private static Camera _mainCamera;

	public static Func<byte> GetCurrentRound;

	public static Func<GameObject, GameObject> PreNetworkTemplateSpawn;

	public static Action<GameObject> PostNetworkTemplateSpawn;

	public static Action<GameObject> PatchSDKEntity;

	public static Action<DamageType, Collider> OnKillRequest;

	public static Action<DamageType, byte, float, bool, Collider> OnDamageRequest;

	public static Func<int> GetSeed;

	public static Action<entity_room_base> OnRoomSpawn;

	public static Action<string, Vector3, AudioData, bool> Play3DSound;

	public static Action<AudioClip, Vector3, AudioData, bool> Play3DSoundClip;

	public static Action<AudioClip, AudioData, bool> Play2DSoundClip;

	public static Action<string, AudioData, bool> Play2DSound;

	public static Action<entity_delivery_spot> OnDeliverySpotRegister;

	public static Action<entity_delivery_spot> OnDeliverySpotUnregister;

	public static Camera MainCamera
	{
		get
		{
			if (!_mainCamera)
			{
				_mainCamera = Camera.main;
			}
			return _mainCamera;
		}
	}
}
