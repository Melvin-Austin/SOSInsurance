using Unity.Netcode;
using UnityEngine;

namespace HyenaQuest;

[DisallowMultipleComponent]
public class entity_network_template_base : MonoBehaviour
{
	public GameObject template;

	public bool flipTest;

	protected NetworkObject _spawnedGameObject;

	private bool IsServer
	{
		get
		{
			if ((bool)NetworkManager.Singleton)
			{
				return NetworkManager.Singleton.IsServer;
			}
			return false;
		}
	}

	public void Awake()
	{
		if (!template)
		{
			throw new UnityException("No templates assigned to entity_network_template!");
		}
	}

	public void OnDestroy()
	{
		if (IsServer && (bool)_spawnedGameObject && _spawnedGameObject.IsSpawned)
		{
			_spawnedGameObject.Despawn();
		}
	}

	[Server]
	public (GameObject, NetworkObject) NetworkSpawn()
	{
		if (!IsServer)
		{
			return (null, null);
		}
		if (!template)
		{
			throw new UnityException("No templates assigned to entity_network_template!");
		}
		if (SDK.PreNetworkTemplateSpawn != null)
		{
			template = SDK.PreNetworkTemplateSpawn?.Invoke(template);
		}
		GameObject gameObject = Object.Instantiate(template, base.transform.position, base.transform.rotation);
		if (!gameObject)
		{
			throw new UnityException("Failed to instantiate template " + template.name);
		}
		SDK.PostNetworkTemplateSpawn?.Invoke(gameObject);
		if (flipTest)
		{
			entity_room_interior componentInParent = GetComponentInParent<entity_room_interior>(includeInactive: false);
			if ((bool)componentInParent && componentInParent.IsRoomFlipped())
			{
				gameObject.transform.localEulerAngles = new Vector3(gameObject.transform.localEulerAngles.x, 180f, gameObject.transform.localEulerAngles.z);
			}
		}
		gameObject.transform.localScale = Vector3.Scale(base.transform.lossyScale, gameObject.transform.lossyScale);
		NetworkObject component = gameObject.GetComponent<NetworkObject>();
		if (!component)
		{
			throw new UnityException("NetworkObject component missing on template " + gameObject.name);
		}
		_spawnedGameObject = component;
		return (gameObject, _spawnedGameObject);
	}

	public virtual bool CanSpawn()
	{
		return false;
	}
}
