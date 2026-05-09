using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

namespace HyenaQuest;

public class entity_item_spray_base : entity_item_pickable
{
	protected readonly List<GameObject> _sprays = new List<GameObject>();

	protected int _layerMask;

	protected RenderingLayerMask _renderingLayerMask;

	protected GameObject _sprayRoot;

	protected float _lastSprayTime;

	protected float _lastDecalCheck;

	protected Vector3 _lastSprayPosition;

	protected readonly NetVar<bool> _spraying = new NetVar<bool>(value: false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

	[Client]
	public override void OnUse(entity_player ply, Collider obj, bool pressing)
	{
		_spraying.Value = (bool)ply && base.IsOwner && pressing;
	}

	public override void OnNetworkDespawn()
	{
		base.OnNetworkDespawn();
		foreach (GameObject spray in _sprays)
		{
			if ((bool)spray)
			{
				UnityEngine.Object.Destroy(spray);
			}
		}
	}

	public override void OnNetworkSpawn()
	{
		base.OnNetworkSpawn();
		if (base.IsServer)
		{
			CoreController.WaitFor(delegate(PlayerController ctr)
			{
				ctr.OnPlayerCreated += new Action<entity_player, bool>(OnPlayerCreated);
			});
		}
	}

	public new void Update()
	{
		base.Update();
		if (Time.time >= _lastDecalCheck)
		{
			_lastDecalCheck = Time.time + 1f;
			List<GameObject> sprays = _sprays;
			if (sprays != null && sprays.Count > 0)
			{
				for (int num = _sprays.Count - 1; num >= 0; num--)
				{
					GameObject gameObject = _sprays[num];
					if ((bool)gameObject)
					{
						Vector3 position = gameObject.transform.position;
						Vector3 forward = gameObject.transform.forward;
						if (!Physics.Raycast(position, forward, 0.05f, _layerMask))
						{
							UnityEngine.Object.Destroy(gameObject.gameObject);
							_sprays.RemoveAt(num);
						}
					}
				}
			}
		}
		if (IsItemOwner() && _spraying.Value)
		{
			TrySpray();
		}
	}

	protected override void Init()
	{
		base.Init();
		_layerMask = LayerMask.GetMask("entity_ground");
		_renderingLayerMask = RenderingLayerMask.GetMask("RecieveDecals");
		_sprayRoot = GameObject.Find("SprayController");
		if (!_sprayRoot)
		{
			throw new UnityException("Missing SprayController gameobject");
		}
	}

	private void OnPlayerCreated(entity_player ply, bool server)
	{
		if (!server || !ply || _sprays.Count == 0)
		{
			return;
		}
		List<Vector3> list = new List<Vector3>();
		List<Vector3> list2 = new List<Vector3>();
		foreach (GameObject spray in _sprays)
		{
			if ((bool)spray)
			{
				list.Add(spray.transform.position - spray.transform.forward * 0.01f);
				list2.Add(-spray.transform.forward);
			}
		}
		if (list.Count > 0)
		{
			SendConnectingSprayRPC(list.ToArray(), list2.ToArray(), base.RpcTarget.Single(ply.GetConnectionID(), RpcTargetUse.Temp));
		}
	}

	[Rpc(SendTo.SpecifiedInParams)]
	private void SendConnectingSprayRPC(Vector3[] sprayPositions, Vector3[] sprayNormals, RpcParams target)
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
			FastBufferWriter bufferWriter = __beginSendRpc(3270916971u, target, attributeParams, SendTo.SpecifiedInParams, RpcDelivery.Reliable);
			bool value = sprayPositions != null;
			bufferWriter.WriteValueSafe(in value, default(FastBufferWriter.ForPrimitives));
			if (value)
			{
				bufferWriter.WriteValueSafe(sprayPositions);
			}
			bool value2 = sprayNormals != null;
			bufferWriter.WriteValueSafe(in value2, default(FastBufferWriter.ForPrimitives));
			if (value2)
			{
				bufferWriter.WriteValueSafe(sprayNormals);
			}
			__endSendRpc(ref bufferWriter, 3270916971u, target, attributeParams, SendTo.SpecifiedInParams, RpcDelivery.Reliable);
		}
		if (__rpc_exec_stage != __RpcExecStage.Execute)
		{
			return;
		}
		__rpc_exec_stage = __RpcExecStage.Send;
		if (sprayPositions != null && sprayNormals != null)
		{
			for (int i = 0; i < sprayPositions.Length; i++)
			{
				OnSprayRequest(sprayPositions[i], sprayNormals[i]);
			}
		}
	}

	protected virtual int GetMaxDecals()
	{
		throw new NotImplementedException();
	}

	protected virtual GameObject GetDecalTemplate()
	{
		throw new NotImplementedException();
	}

	protected virtual Vector3 GetSpraySize()
	{
		throw new NotImplementedException();
	}

	protected override void InternalChangeOwner(byte newOwner, bool server)
	{
		base.InternalChangeOwner(newOwner, server);
		ResetSprayRPC();
	}

	[Rpc(SendTo.Owner)]
	private void ResetSprayRPC()
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
			FastBufferWriter bufferWriter = __beginSendRpc(3706326984u, rpcParams, attributeParams, SendTo.Owner, RpcDelivery.Reliable);
			__endSendRpc(ref bufferWriter, 3706326984u, rpcParams, attributeParams, SendTo.Owner, RpcDelivery.Reliable);
		}
		if (__rpc_exec_stage == __RpcExecStage.Execute)
		{
			__rpc_exec_stage = __RpcExecStage.Send;
			OnResetSpray();
		}
	}

	protected virtual void OnResetSpray()
	{
		_spraying.Value = false;
	}

	protected virtual void TrySpray()
	{
		if ((bool)SDK.MainCamera && !(Time.time < _lastSprayTime) && (bool)_sprayRoot && Physics.Raycast(SDK.MainCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f)), out var hitInfo, 2f, _layerMask) && (!hitInfo.collider.TryGetComponent<MeshRenderer>(out var component, 1) || component.renderingLayerMask == (uint)_renderingLayerMask) && !(Vector3.Distance(hitInfo.point, _lastSprayPosition) < 0.005f))
		{
			_lastSprayTime = Time.time + 0.045f;
			_lastSprayPosition = hitInfo.point;
			SprayRPC(hitInfo.point, hitInfo.normal);
		}
	}

	[Rpc(SendTo.ClientsAndHost, InvokePermission = RpcInvokePermission.Everyone, Delivery = RpcDelivery.Unreliable)]
	private void SprayRPC(Vector3 point, Vector3 normal)
	{
		NetworkManager networkManager = base.NetworkManager;
		if ((object)networkManager == null || !networkManager.IsListening)
		{
			Debug.LogError("Rpc methods can only be invoked after starting the NetworkManager!");
			return;
		}
		if (__rpc_exec_stage != __RpcExecStage.Execute)
		{
			RpcAttribute.RpcAttributeParams attributeParams = new RpcAttribute.RpcAttributeParams
			{
				InvokePermission = RpcInvokePermission.Everyone,
				Delivery = RpcDelivery.Unreliable
			};
			RpcParams rpcParams = default(RpcParams);
			FastBufferWriter bufferWriter = __beginSendRpc(698622393u, rpcParams, attributeParams, SendTo.ClientsAndHost, RpcDelivery.Unreliable);
			bufferWriter.WriteValueSafe(in point);
			bufferWriter.WriteValueSafe(in normal);
			__endSendRpc(ref bufferWriter, 698622393u, rpcParams, attributeParams, SendTo.ClientsAndHost, RpcDelivery.Unreliable);
		}
		if (__rpc_exec_stage == __RpcExecStage.Execute)
		{
			__rpc_exec_stage = __RpcExecStage.Send;
			OnSprayRequest(point, normal);
		}
	}

	protected virtual void OnSprayRequest(Vector3 point, Vector3 normal)
	{
		if ((bool)_sprayRoot)
		{
			GameObject gameObject = null;
			if (_sprays.Count >= GetMaxDecals())
			{
				gameObject = _sprays[0];
				_sprays.RemoveAt(0);
			}
			else
			{
				gameObject = GetDecalTemplate();
			}
			if (!gameObject)
			{
				throw new UnityException("Failed to create / get spray decal");
			}
			gameObject.transform.parent = _sprayRoot.transform;
			gameObject.transform.position = point + normal * 0.01f;
			gameObject.transform.rotation = Quaternion.LookRotation(-normal) * Quaternion.Euler(0f, 0f, UnityEngine.Random.Range(0f, 360f));
			gameObject.transform.localScale = GetSpraySize();
			_sprays.Add(gameObject);
		}
	}

	protected override void __initializeVariables()
	{
		if (_spraying == null)
		{
			throw new Exception("entity_item_spray_base._spraying cannot be null. All NetworkVariableBase instances must be initialized.");
		}
		_spraying.Initialize(this);
		__nameNetworkVariable(_spraying, "_spraying");
		NetworkVariableFields.Add(_spraying);
		base.__initializeVariables();
	}

	protected override void __initializeRpcs()
	{
		__registerRpc(3270916971u, __rpc_handler_3270916971, "SendConnectingSprayRPC", RpcInvokePermission.Everyone);
		__registerRpc(3706326984u, __rpc_handler_3706326984, "ResetSprayRPC", RpcInvokePermission.Everyone);
		__registerRpc(698622393u, __rpc_handler_698622393, "SprayRPC", RpcInvokePermission.Everyone);
		base.__initializeRpcs();
	}

	private static void __rpc_handler_3270916971(NetworkBehaviour target, FastBufferReader reader, __RpcParams rpcParams)
	{
		NetworkManager networkManager = target.NetworkManager;
		if ((object)networkManager != null && networkManager.IsListening)
		{
			reader.ReadValueSafe(out bool value, default(FastBufferWriter.ForPrimitives));
			Vector3[] value2 = null;
			if (value)
			{
				reader.ReadValueSafe(out value2);
			}
			reader.ReadValueSafe(out bool value3, default(FastBufferWriter.ForPrimitives));
			Vector3[] value4 = null;
			if (value3)
			{
				reader.ReadValueSafe(out value4);
			}
			RpcParams ext = rpcParams.Ext;
			target.__rpc_exec_stage = __RpcExecStage.Execute;
			((entity_item_spray_base)target).SendConnectingSprayRPC(value2, value4, ext);
			target.__rpc_exec_stage = __RpcExecStage.Send;
		}
	}

	private static void __rpc_handler_3706326984(NetworkBehaviour target, FastBufferReader reader, __RpcParams rpcParams)
	{
		NetworkManager networkManager = target.NetworkManager;
		if ((object)networkManager != null && networkManager.IsListening)
		{
			target.__rpc_exec_stage = __RpcExecStage.Execute;
			((entity_item_spray_base)target).ResetSprayRPC();
			target.__rpc_exec_stage = __RpcExecStage.Send;
		}
	}

	private static void __rpc_handler_698622393(NetworkBehaviour target, FastBufferReader reader, __RpcParams rpcParams)
	{
		NetworkManager networkManager = target.NetworkManager;
		if ((object)networkManager != null && networkManager.IsListening)
		{
			reader.ReadValueSafe(out Vector3 value);
			reader.ReadValueSafe(out Vector3 value2);
			target.__rpc_exec_stage = __RpcExecStage.Execute;
			((entity_item_spray_base)target).SprayRPC(value, value2);
			target.__rpc_exec_stage = __RpcExecStage.Send;
		}
	}

	protected internal override string __getTypeName()
	{
		return "entity_item_spray_base";
	}
}
