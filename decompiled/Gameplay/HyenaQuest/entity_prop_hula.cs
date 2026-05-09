using Unity.Netcode;
using UnityEngine;
using UnityEngine.Scripting;
using UnityEngine.VFX;

namespace HyenaQuest;

[Preserve]
public class entity_prop_hula : entity_phys
{
	public Transform kissPoint;

	private VisualEffect _kissVFX;

	private float _lastKiss;

	private bool _kissing;

	public new void Update()
	{
		base.Update();
		if (!base.IsServer)
		{
			return;
		}
		if (!IsBeingGrabbed())
		{
			SetKissing(kissing: false);
			return;
		}
		entity_prop_zebra allMightyZebra = entity_prop_zebra.AllMightyZebra;
		if (!allMightyZebra || !allMightyZebra.IsBeingGrabbed() || Vector3.Distance(kissPoint.position, allMightyZebra.kissPoint.position) > 0.05f)
		{
			SetKissing(kissing: false);
		}
		else
		{
			SetKissing(kissing: true);
		}
	}

	protected override void Init()
	{
		base.Init();
		if (!kissPoint)
		{
			throw new UnityException("Missing kiss point");
		}
		_kissVFX = GetComponentInChildren<VisualEffect>(includeInactive: true);
		if (!_kissVFX)
		{
			throw new UnityException("Missing kiss vfx");
		}
	}

	[Server]
	private void SetKissing(bool kissing)
	{
		if (!base.IsServer)
		{
			throw new UnityException("Missing kiss point");
		}
		if (_kissing != kissing)
		{
			if (_kissing && !kissing && Time.time > _lastKiss)
			{
				_lastKiss = Time.time + 0.2f;
				OnKissRPC();
				UnlockAchievement();
			}
			_kissing = kissing;
		}
	}

	[Rpc(SendTo.ClientsAndHost)]
	private void OnKissRPC()
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
			FastBufferWriter bufferWriter = __beginSendRpc(3535043159u, rpcParams, attributeParams, SendTo.ClientsAndHost, RpcDelivery.Reliable);
			__endSendRpc(ref bufferWriter, 3535043159u, rpcParams, attributeParams, SendTo.ClientsAndHost, RpcDelivery.Reliable);
		}
		if (__rpc_exec_stage == __RpcExecStage.Execute)
		{
			__rpc_exec_stage = __RpcExecStage.Send;
			if ((bool)_kissVFX)
			{
				_kissVFX.Play();
				NetController<SoundController>.Instance?.Play3DSound($"Ingame/Props/Special/ForbiddenLove/kiss_{Random.Range(0, 3)}.ogg", kissPoint.position, new AudioData
				{
					distance = 4f,
					pitch = Random.Range(0.8f, 1.1f),
					volume = Random.Range(0.7f, 1f)
				});
			}
		}
	}

	[Server]
	private void UnlockAchievement()
	{
		if (!base.IsServer)
		{
			throw new UnityException("Server only");
		}
		entity_prop_zebra allMightyZebra = entity_prop_zebra.AllMightyZebra;
		if ((bool)allMightyZebra)
		{
			entity_player grabbingOwner = GetGrabbingOwner();
			entity_player grabbingOwner2 = allMightyZebra.GetGrabbingOwner();
			if ((bool)grabbingOwner2 && (bool)grabbingOwner && !(grabbingOwner2 == grabbingOwner))
			{
				NetController<StatsController>.Instance.UnlockAchievementSV(STEAM_ACHIEVEMENTS.ACHIEVEMENT_FORBIDDEN_LOVE, grabbingOwner2.GetConnectionID());
				NetController<StatsController>.Instance.UnlockAchievementSV(STEAM_ACHIEVEMENTS.ACHIEVEMENT_FORBIDDEN_LOVE, grabbingOwner.GetConnectionID());
			}
		}
	}

	protected override void __initializeVariables()
	{
		base.__initializeVariables();
	}

	protected override void __initializeRpcs()
	{
		__registerRpc(3535043159u, __rpc_handler_3535043159, "OnKissRPC", RpcInvokePermission.Everyone);
		base.__initializeRpcs();
	}

	private static void __rpc_handler_3535043159(NetworkBehaviour target, FastBufferReader reader, __RpcParams rpcParams)
	{
		NetworkManager networkManager = target.NetworkManager;
		if ((object)networkManager != null && networkManager.IsListening)
		{
			target.__rpc_exec_stage = __RpcExecStage.Execute;
			((entity_prop_hula)target).OnKissRPC();
			target.__rpc_exec_stage = __RpcExecStage.Send;
		}
	}

	protected internal override string __getTypeName()
	{
		return "entity_prop_hula";
	}
}
