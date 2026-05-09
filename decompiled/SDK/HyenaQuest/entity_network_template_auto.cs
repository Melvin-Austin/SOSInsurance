using System.Collections;
using Unity.Netcode;
using UnityEngine;

namespace HyenaQuest;

public class entity_network_template_auto : entity_network_template_base
{
	private Coroutine _coroutine;

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

	public new void Awake()
	{
		base.Awake();
		_coroutine = StartCoroutine(Spawn());
	}

	public new void OnDestroy()
	{
		base.OnDestroy();
		if (_coroutine != null)
		{
			StopCoroutine(_coroutine);
		}
	}

	public override bool CanSpawn()
	{
		return true;
	}

	private IEnumerator Spawn()
	{
		yield return new WaitForSecondsRealtime(1f);
		if (IsServer)
		{
			(GameObject, NetworkObject) tuple = NetworkSpawn();
			if ((bool)tuple.Item1 && (bool)tuple.Item2)
			{
				tuple.Item2.Spawn();
			}
		}
	}
}
