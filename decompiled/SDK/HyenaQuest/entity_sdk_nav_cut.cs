using UnityEngine;

namespace HyenaQuest;

public class entity_sdk_nav_cut : MonoBehaviour
{
	public void Awake()
	{
		SDK.PatchSDKEntity?.Invoke(base.gameObject);
	}
}
