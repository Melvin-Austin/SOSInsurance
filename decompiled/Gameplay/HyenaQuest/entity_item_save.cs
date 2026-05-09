using System;
using System.Collections.Generic;
using TMPro;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

namespace HyenaQuest;

public class entity_item_save : entity_item
{
	public static List<Color> ID_COLORS = new List<Color>
	{
		new Color(0f, 0f, 0f, 1f),
		new Color(14.75f, 14.4f, 0f, 1f),
		new Color(14.75f, 0f, 3.67f, 1f),
		new Color(0f, 14.75f, 1.55f, 1f),
		new Color(0f, 10f, 14.75f, 1f),
		new Color(2.26f, 0f, 14.75f, 1f)
	};

	private static readonly int EmissionColor = Shader.PropertyToID("_EmissionColor");

	private SaveData? _data;

	private TextMeshPro _text;

	private MeshRenderer _renderer;

	private float _hitCooldown;

	private readonly NetVar<FixedString128Bytes> _name = new NetVar<FixedString128Bytes>();

	private readonly NetVar<byte> _id = new NetVar<byte>(byte.MaxValue);

	protected override void Init()
	{
		base.Init();
		_renderer = GetComponent<MeshRenderer>();
		if (!_renderer)
		{
			throw new UnityException("Missing MeshRenderer");
		}
		_text = GetComponentInChildren<TextMeshPro>(includeInactive: true);
		if (!_text)
		{
			throw new UnityException("Missing TextMeshPro");
		}
	}

	protected override void OnNetworkPostSpawn()
	{
		base.OnNetworkPostSpawn();
		if (!base.IsClient)
		{
			return;
		}
		_name.RegisterOnValueChanged(delegate(FixedString128Bytes _, FixedString128Bytes newValue)
		{
			if ((bool)_text)
			{
				_text.text = newValue.ToString();
			}
		});
		_id.RegisterOnValueChanged(delegate(byte _, byte newValue)
		{
			if ((bool)_renderer)
			{
				_renderer.material.SetColor(EmissionColor, (newValue == byte.MaxValue) ? ID_COLORS[0] : ID_COLORS[newValue % ID_COLORS.Count]);
			}
		});
	}

	public override void OnNetworkPreDespawn()
	{
		base.OnNetworkPreDespawn();
		if (base.IsClient)
		{
			_name.OnValueChanged = null;
			_id.OnValueChanged = null;
		}
	}

	protected override void OnCollision(Collision col)
	{
		if (base.IsOwner && !(Time.time < _hitCooldown) && !(col.relativeVelocity.sqrMagnitude <= 10f))
		{
			_hitCooldown = Time.time + 1f;
			WorldDamageRPC();
		}
	}

	[Rpc(SendTo.Server, Delivery = RpcDelivery.Unreliable)]
	private void WorldDamageRPC()
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
				Delivery = RpcDelivery.Unreliable
			};
			RpcParams rpcParams = default(RpcParams);
			FastBufferWriter bufferWriter = __beginSendRpc(1216058682u, rpcParams, attributeParams, SendTo.Server, RpcDelivery.Unreliable);
			__endSendRpc(ref bufferWriter, 1216058682u, rpcParams, attributeParams, SendTo.Server, RpcDelivery.Unreliable);
		}
		if (__rpc_exec_stage != __RpcExecStage.Execute)
		{
			return;
		}
		__rpc_exec_stage = __RpcExecStage.Send;
		if ((bool)NetController<ShakeController>.Instance && (bool)NetController<LightController>.Instance && _data.HasValue)
		{
			NetController<ShakeController>.Instance.ShakeRPC(ShakeMode.SHAKE_ALL, UnityEngine.Random.Range(0.15f, 0.35f), UnityEngine.Random.Range(0.05f, 0.1f));
			if (UnityEngine.Random.value < 0.2f)
			{
				NetController<LightController>.Instance.ExecuteAllLightCommand(LightCommand.FLICKER);
			}
		}
	}

	[Client]
	public override InteractionData InteractionSelector(Collider obj)
	{
		if (IsLocked())
		{
			return new InteractionData(Interaction.INTERACT_LOCKED, _renderers);
		}
		return new InteractionData(Interaction.INTERACT, _renderers, "ingame.ui.hints.save");
	}

	[Server]
	public SaveData? GetData()
	{
		if (base.IsSpawned && !base.IsServer)
		{
			throw new UnityException("Server only");
		}
		return _data;
	}

	[Server]
	public void ClearData()
	{
		if (base.IsSpawned && !base.IsServer)
		{
			throw new UnityException("Server only");
		}
		_data = null;
		_id.SetSpawnValue(byte.MaxValue);
		_name.SetSpawnValue("");
	}

	[Server]
	public void SetData(SaveData data)
	{
		if (base.IsSpawned && !base.IsServer)
		{
			throw new UnityException("Server only");
		}
		_data = data;
		_id.SetSpawnValue((byte)UnityEngine.Random.Range(1, ID_COLORS.Count));
		_name.SetSpawnValue($"<size=50%>{data.date}</size>\n----------\n{data.round}");
	}

	public override string GetID()
	{
		return "item_save";
	}

	protected override void __initializeVariables()
	{
		if (_name == null)
		{
			throw new Exception("entity_item_save._name cannot be null. All NetworkVariableBase instances must be initialized.");
		}
		_name.Initialize(this);
		__nameNetworkVariable(_name, "_name");
		NetworkVariableFields.Add(_name);
		if (_id == null)
		{
			throw new Exception("entity_item_save._id cannot be null. All NetworkVariableBase instances must be initialized.");
		}
		_id.Initialize(this);
		__nameNetworkVariable(_id, "_id");
		NetworkVariableFields.Add(_id);
		base.__initializeVariables();
	}

	protected override void __initializeRpcs()
	{
		__registerRpc(1216058682u, __rpc_handler_1216058682, "WorldDamageRPC", RpcInvokePermission.Everyone);
		base.__initializeRpcs();
	}

	private static void __rpc_handler_1216058682(NetworkBehaviour target, FastBufferReader reader, __RpcParams rpcParams)
	{
		NetworkManager networkManager = target.NetworkManager;
		if ((object)networkManager != null && networkManager.IsListening)
		{
			target.__rpc_exec_stage = __RpcExecStage.Execute;
			((entity_item_save)target).WorldDamageRPC();
			target.__rpc_exec_stage = __RpcExecStage.Send;
		}
	}

	protected internal override string __getTypeName()
	{
		return "entity_item_save";
	}
}
