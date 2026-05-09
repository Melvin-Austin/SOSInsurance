using UnityEngine;

namespace HyenaQuest;

public class entity_sdk_nav_link : MonoBehaviour
{
	public Transform target;

	[Range(0f, 1f)]
	public float jumpTime = 0.5f;

	public float jumpOffset = 0.5f;

	public float jumpDelay = 0.25f;

	public bool effect = true;

	public void Awake()
	{
		SDK.PatchSDKEntity?.Invoke(base.gameObject);
	}
}
