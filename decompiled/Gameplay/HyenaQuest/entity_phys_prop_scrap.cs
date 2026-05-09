using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using ZLinq;

namespace HyenaQuest;

public class entity_phys_prop_scrap : entity_phys
{
	private static readonly float VACUUM_TIME = 1f;

	private static readonly float VACUUM_VOLUME_MULT = 2.25f;

	private static readonly float VACUUM_DECAY_RATE = 0.8f;

	[Range(1f, 100f)]
	public int scrap = 10;

	public GameObject viewModel;

	private readonly HashSet<entity_player> _scrappingPlayers = new HashSet<entity_player>();

	private Vector3 _originalSize;

	private float _currentScale = 1f;

	private float _serverProgress;

	private bool _destroying;

	private readonly NetVar<byte> _progress = new NetVar<byte>(0);

	public override void OnNetworkSpawn()
	{
		base.OnNetworkSpawn();
		if (!viewModel)
		{
			throw new UnityException("Missing mdl GameObject");
		}
		_originalSize = viewModel.transform.localScale;
		if (base.IsServer)
		{
			scrap = Mathf.Max(1, scrap + UnityEngine.Random.Range(-5, 5));
			if (!IsInsideInterior())
			{
				scrap = Mathf.Max(1, Mathf.RoundToInt((float)scrap * 0.4f));
			}
			SetLocked(_rigidbody.isKinematic ? LOCK_TYPE.LOCKED : LOCK_TYPE.NONE);
		}
	}

	public new void Update()
	{
		base.Update();
		if (base.IsServer && !_destroying)
		{
			if (_scrappingPlayers.Count > 0)
			{
				float vacuumTime = GetVacuumTime();
				_serverProgress = Mathf.MoveTowards(_serverProgress, 1f, Time.deltaTime / vacuumTime);
				if (_serverProgress >= 1f)
				{
					ScrapObject();
					return;
				}
			}
			else if (_serverProgress > 0f)
			{
				_serverProgress = Mathf.MoveTowards(_serverProgress, 0f, Time.deltaTime * VACUUM_DECAY_RATE);
			}
			_progress.Value = (byte)(Mathf.Clamp01(_serverProgress) * 255f);
		}
		if (base.IsClient && (bool)viewModel)
		{
			float num = (float)(int)_progress.Value / 255f;
			if (num > 0.001f)
			{
				Bounds bounds = GetBounds();
				float a = (bounds.size.x + bounds.size.y + bounds.size.z) / 3f;
				float num2 = 0.06f / Mathf.Max(a, 0.1f);
				float num3 = Mathf.Lerp(0.3f, 1f, num * num) * num2;
				Vector3 vector = new Vector3(UnityEngine.Random.Range(0f - num3, num3), UnityEngine.Random.Range(0f - num3, num3), UnityEngine.Random.Range(0f - num3, num3));
				_currentScale = Mathf.Lerp(1f, 0.95f, num * num);
				viewModel.transform.localScale = Vector3.Scale(_originalSize * _currentScale, Vector3.one + vector);
			}
			else
			{
				_currentScale = Mathf.Lerp(_currentScale, 1f, Time.deltaTime * 8f);
				viewModel.transform.localScale = _originalSize * _currentScale;
			}
		}
	}

	[Rpc(SendTo.Server)]
	public void SetVacuumingRPC(NetworkBehaviourReference plyRef, bool scrapping)
	{
		NetworkManager networkManager = base.NetworkManager;
		if ((object)networkManager == null || !networkManager.IsListening)
		{
			Debug.LogError("Rpc methods can only be invoked after starting the NetworkManager!");
			return;
		}
		if (__rpc_exec_stage != __RpcExecStage.Execute)
		{
			RpcAttribute.RpcAttributeParams attributeParams = default(RpcAttribute.RpcAttributeParams);
			RpcParams rpcParams = default(RpcParams);
			FastBufferWriter bufferWriter = __beginSendRpc(356401686u, rpcParams, attributeParams, SendTo.Server, RpcDelivery.Reliable);
			bufferWriter.WriteValueSafe(in plyRef, default(FastBufferWriter.ForNetworkSerializable));
			bufferWriter.WriteValueSafe(in scrapping, default(FastBufferWriter.ForPrimitives));
			__endSendRpc(ref bufferWriter, 356401686u, rpcParams, attributeParams, SendTo.Server, RpcDelivery.Reliable);
		}
		if (__rpc_exec_stage != __RpcExecStage.Execute)
		{
			return;
		}
		__rpc_exec_stage = __RpcExecStage.Send;
		if (!base.IsServer)
		{
			throw new UnityException("SetScrapping can only be called on the server.");
		}
		entity_player entity_player2 = NETController.Get<entity_player>(plyRef);
		if (!entity_player2)
		{
			throw new UnityException("Invalid player reference in SetScrappingRPC.");
		}
		if (scrapping)
		{
			if (CanScrap(entity_player2))
			{
				_scrappingPlayers.Add(entity_player2);
			}
		}
		else
		{
			_scrappingPlayers.Remove(entity_player2);
		}
	}

	public virtual int GetReward()
	{
		return scrap;
	}

	public virtual bool CanScrap(entity_player scrapper)
	{
		return true;
	}

	public override void SetLocked(LOCK_TYPE locked)
	{
		base.SetLocked(locked);
		_scrappingPlayers.Clear();
		_serverProgress = 0f;
		_progress.Value = 0;
	}

	[Client]
	public override InteractionData InteractionSelector(Collider obj)
	{
		bool flag = IsBeingGrabbed();
		return new InteractionData(IsLocked() ? Interaction.INTERACT_LOCKED : Interaction.INTERACT, _renderers, flag ? "ingame.ui.hints.steal" : "ingame.ui.hints.scrap");
	}

	private bool IsInsideInterior()
	{
		if (!NetController<MapController>.Instance)
		{
			return false;
		}
		return NetController<MapController>.Instance.GetSpawnedInteriors()?.AsValueEnumerable().Any((entity_room_interior room) => (bool)room && room.GetBounds().Contains(base.transform.position)) ?? false;
	}

	private float GetVacuumTime()
	{
		Bounds bounds = GetBounds();
		return Mathf.Clamp(bounds.size.x * bounds.size.y * bounds.size.z * VACUUM_VOLUME_MULT, 0.1f, VACUUM_TIME);
	}

	[Server]
	private void ScrapObject()
	{
		if (!base.IsServer)
		{
			throw new UnityException("ScrapObject can only be called on the server.");
		}
		if (!CanScrap(null) || _destroying)
		{
			return;
		}
		_destroying = true;
		if (_scrappingPlayers.Count > 0)
		{
			entity_player entity_player2 = (from player in _scrappingPlayers.AsValueEnumerable()
				where (bool)player && (bool)player.GetVacuum()?.GetVacuumHolder()
				select player).FirstOrDefault();
			if ((bool)entity_player2)
			{
				int reward = GetReward();
				if (reward > 0)
				{
					Bounds bounds = GetBounds();
					entity_player_vacuum vacuum = entity_player2.GetVacuum();
					if ((bool)vacuum)
					{
						entity_item_vacuum vacuumHolder = vacuum.GetVacuumHolder();
						if ((bool)vacuumHolder)
						{
							vacuumHolder.AddScrap(reward);
						}
					}
					NetController<ScrapController>.Instance?.RemoveWorldScrap(reward);
					NetController<StatsController>.Instance?.RegisterScrap(entity_player2.GetPlayerID(), reward);
					NetController<NotificationController>.Instance?.BroadcastAll3DRPC(new NotificationData3D
					{
						message = $"ingame.ui.notification.scrap-add||{reward}",
						position = bounds.center,
						startColor = Color.white,
						endColor = Color.white,
						fadeSpeed = 0.5f,
						scale = 0.8f
					});
				}
				NetController<SoundController>.Instance?.Play3DSound($"Ingame/Player/Scrap/snatch_get0{UnityEngine.Random.Range(1, 4)}.ogg", entity_player2.transform.position, new AudioData
				{
					distance = 3f,
					pitch = UnityEngine.Random.Range(0.8f, 1.2f),
					volume = UnityEngine.Random.Range(0.8f, 1f)
				}, broadcast: true);
			}
		}
		base.NetworkObject.Despawn();
	}

	protected override void __initializeVariables()
	{
		if (_progress == null)
		{
			throw new Exception("entity_phys_prop_scrap._progress cannot be null. All NetworkVariableBase instances must be initialized.");
		}
		_progress.Initialize(this);
		__nameNetworkVariable(_progress, "_progress");
		NetworkVariableFields.Add(_progress);
		base.__initializeVariables();
	}

	protected override void __initializeRpcs()
	{
		__registerRpc(356401686u, __rpc_handler_356401686, "SetVacuumingRPC", RpcInvokePermission.Everyone);
		base.__initializeRpcs();
	}

	private static void __rpc_handler_356401686(NetworkBehaviour target, FastBufferReader reader, __RpcParams rpcParams)
	{
		NetworkManager networkManager = target.NetworkManager;
		if ((object)networkManager != null && networkManager.IsListening)
		{
			reader.ReadValueSafe(out NetworkBehaviourReference value, default(FastBufferWriter.ForNetworkSerializable));
			reader.ReadValueSafe(out bool value2, default(FastBufferWriter.ForPrimitives));
			target.__rpc_exec_stage = __RpcExecStage.Execute;
			((entity_phys_prop_scrap)target).SetVacuumingRPC(value, value2);
			target.__rpc_exec_stage = __RpcExecStage.Send;
		}
	}

	protected internal override string __getTypeName()
	{
		return "entity_phys_prop_scrap";
	}
}
