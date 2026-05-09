using UnityEngine;

namespace HyenaQuest;

public class entity_server_only : MonoBehaviour
{
	public void Awake()
	{
		if (!NETController.Instance || !NETController.Instance.IsServer)
		{
			Object.Destroy(base.gameObject);
		}
	}
}
