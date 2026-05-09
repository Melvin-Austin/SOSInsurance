using System;
using System.Collections.Generic;
using UnityEngine;

namespace HyenaQuest;

[DisallowMultipleComponent]
[DefaultExecutionOrder(-70)]
public class RagdollController : MonoController<RagdollController>
{
	[Header("Templates")]
	public GameObject playerRagdoll;

	private readonly Dictionary<byte, entity_ragdoll> _playerRagdolls = new Dictionary<byte, entity_ragdoll>();

	public new void Awake()
	{
		base.Awake();
		if (!playerRagdoll)
		{
			throw new UnityException("Player ragdoll prefab not set!");
		}
		CoreController.WaitFor(delegate(PlayerController plyCtrl)
		{
			plyCtrl.OnPlayerCreated += new Action<entity_player, bool>(OnPlayerCreated);
			plyCtrl.OnPlayerRemoved += new Action<entity_player, bool>(OnPlayerRemoved);
		});
	}

	public new void OnDestroy()
	{
		if ((bool)MonoController<PlayerController>.Instance)
		{
			MonoController<PlayerController>.Instance.OnPlayerCreated -= new Action<entity_player, bool>(OnPlayerCreated);
			MonoController<PlayerController>.Instance.OnPlayerRemoved -= new Action<entity_player, bool>(OnPlayerRemoved);
		}
		base.OnDestroy();
	}

	public entity_ragdoll SpawnRagdoll(entity_player ply, Vector3 position)
	{
		if (!_playerRagdolls.TryGetValue(ply.GetPlayerID(), out var value))
		{
			throw new UnityException("Ragdoll not found");
		}
		value.transform.position = position;
		value.transform.rotation = ply.transform.rotation;
		value.SetEnabled(enable: true);
		value.UpdateBadge();
		return value;
	}

	public void UpdateRagdollSkin(byte plyId)
	{
		if (_playerRagdolls.TryGetValue(plyId, out var value))
		{
			value.UpdateSkin();
		}
	}

	public void UpdateRagdollBadge(byte plyId)
	{
		if (_playerRagdolls.TryGetValue(plyId, out var value))
		{
			value.UpdateBadge();
		}
	}

	public void RemoveRagdoll(byte plyId)
	{
		if (_playerRagdolls.TryGetValue(plyId, out var value))
		{
			value.SetEnabled(enable: false);
			ResetPos(value);
		}
	}

	private void ResetPos(entity_ragdoll ragdoll)
	{
		ragdoll.transform.position = new Vector3(0f, 1000f, 0f);
		ragdoll.transform.rotation = Quaternion.identity;
		Physics.SyncTransforms();
	}

	private void OnPlayerRemoved(entity_player ply, bool server)
	{
		if (server || !ply)
		{
			return;
		}
		byte playerID = ply.GetPlayerID();
		if (!_playerRagdolls.TryGetValue(playerID, out var value))
		{
			Debug.LogWarning($"Failed to remove ragdoll for disconnected player {playerID}");
			return;
		}
		if ((bool)value)
		{
			UnityEngine.Object.Destroy(value.gameObject);
		}
		_playerRagdolls.Remove(playerID);
	}

	private void OnPlayerCreated(entity_player ply, bool server)
	{
		if (server || !ply)
		{
			return;
		}
		byte playerID = ply.GetPlayerID();
		if (_playerRagdolls.TryGetValue(playerID, out var value))
		{
			if ((bool)value)
			{
				UnityEngine.Object.Destroy(value.gameObject);
			}
			_playerRagdolls.Remove(playerID);
		}
		GameObject obj = UnityEngine.Object.Instantiate(playerRagdoll, new Vector3(0f, 1000f, 0f), Quaternion.identity, base.transform);
		if (!obj)
		{
			throw new UnityException("Failed to instantiate ragdoll");
		}
		entity_ragdoll component = obj.GetComponent<entity_ragdoll>();
		if (!component)
		{
			throw new UnityException("entity_ragdoll required");
		}
		component.SetOwner(ply);
		_playerRagdolls.Add(playerID, component);
		Debug.Log($"Created ragdoll for player {ply.GetPlayerID()}");
	}
}
