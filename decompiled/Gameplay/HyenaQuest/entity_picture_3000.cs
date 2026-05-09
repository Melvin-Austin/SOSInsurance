using System;
using System.Collections.Generic;
using FailCake;
using Steamworks;
using Unity.Netcode;
using UnityEngine;
using ZLinq;

namespace HyenaQuest;

[RequireComponent(typeof(NetworkObject))]
public class entity_picture_3000 : NetworkBehaviour
{
	public Camera snapCamera;

	public entity_lever lever;

	public GameObject flash;

	public BoxCollider screenshotArea;

	private util_timer _timer;

	private util_timer _flash;

	private float _killChance;

	public void Awake()
	{
		if (!lever)
		{
			throw new UnityException("Missing entity_lever");
		}
		if (!flash)
		{
			throw new UnityException("Missing flash gameobject");
		}
		if (!screenshotArea)
		{
			throw new UnityException("Missing screenshotArea gameobject");
		}
		if (!snapCamera)
		{
			throw new UnityException("Missing flash snapCamera");
		}
		snapCamera.gameObject.SetActive(value: false);
		util_render_target.ClearRenderTarget(snapCamera.targetTexture);
	}

	public override void OnNetworkSpawn()
	{
		base.OnNetworkSpawn();
		if (base.IsServer)
		{
			lever.OnUSE += new Action<entity_player, bool>(OnUSE);
		}
	}

	public override void OnNetworkDespawn()
	{
		base.OnNetworkDespawn();
		_flash?.Stop();
		_timer?.Stop();
		if (base.IsServer)
		{
			lever.OnUSE -= new Action<entity_player, bool>(OnUSE);
		}
	}

	[Server]
	private void OnUSE(entity_player caller, bool use)
	{
		if (!base.IsServer)
		{
			throw new UnityException("Server only");
		}
		if (!use || !caller || caller.IsDead())
		{
			return;
		}
		lever.SetLocked(newVal: true);
		NetController<SoundController>.Instance?.Play3DSound("Ingame/Entities/Flash/camera_picture.ogg", base.transform.position, new AudioData
		{
			distance = 5f,
			volume = 0.5f
		}, broadcast: true);
		_timer?.Stop();
		_timer = util_timer.Create(4, 1f, delegate(int t)
		{
			switch (t)
			{
			case 1:
				NetController<ShakeController>.Instance?.Shake3DRPC(base.transform.position, ShakeMode.SHAKE_ALL, 2f, 0.004f);
				break;
			case 3:
				NetController<EffectController>.Instance?.PlayEffectRPC(EffectType.SPARKS, snapCamera.gameObject.transform.position, new EffectSettings
				{
					playSound = true
				});
				break;
			}
		}, delegate
		{
			_killChance += 0.05f;
			if (UnityEngine.Random.value <= _killChance)
			{
				_killChance = 0f;
				ErasePlayers();
			}
			NetController<ShakeController>.Instance?.Shake3DRPC(flash.transform.position, ShakeMode.SHAKE_ALL, 0.25f, 0.2f);
			NetController<SoundController>.Instance?.Play3DSound("Ingame/Entities/Flash/camera_flash.ogg", flash.transform.position, new AudioData
			{
				distance = 5f,
				volume = 0.8f
			}, broadcast: true);
			_timer?.Stop();
			_timer = util_timer.Simple(0.01f, delegate
			{
				if ((bool)lever)
				{
					SnapRPC();
					_timer?.Stop();
					_timer = util_timer.Simple(0.8f, delegate
					{
						lever.SetActive(active: false);
						lever.SetLocked(newVal: false);
					});
				}
			});
		});
	}

	private List<entity_player> GetPlayersInArea()
	{
		if (!MonoController<PlayerController>.Instance || !screenshotArea)
		{
			return null;
		}
		return (from ply in MonoController<PlayerController>.Instance.GetAllPlayers().AsValueEnumerable()
			where (bool)ply && screenshotArea.bounds.Contains(ply.GetChestTransform().position)
			select ply).ToList();
	}

	[Server]
	private void ErasePlayers()
	{
		if (!base.IsServer)
		{
			throw new UnityException("Server only");
		}
		List<entity_player> playersInArea = GetPlayersInArea();
		if (playersInArea == null || playersInArea.Count <= 0)
		{
			return;
		}
		foreach (entity_player item in playersInArea)
		{
			if ((bool)item && !item.IsDead())
			{
				item.Kill(DamageType.ELECTRIC_ASHES);
			}
		}
	}

	[Rpc(SendTo.ClientsAndHost)]
	private void SnapRPC()
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
			FastBufferWriter bufferWriter = __beginSendRpc(1203973843u, rpcParams, attributeParams, SendTo.ClientsAndHost, RpcDelivery.Reliable);
			__endSendRpc(ref bufferWriter, 1203973843u, rpcParams, attributeParams, SendTo.ClientsAndHost, RpcDelivery.Reliable);
		}
		if (__rpc_exec_stage != __RpcExecStage.Execute)
		{
			return;
		}
		__rpc_exec_stage = __RpcExecStage.Send;
		if (!PlayerController.LOCAL || !snapCamera || !screenshotArea)
		{
			return;
		}
		bool flag = screenshotArea.bounds.Contains(PlayerController.LOCAL.transform.position);
		snapCamera.gameObject.SetActive(value: true);
		if (flag)
		{
			PlayerController.LOCAL.RenderPlayerHead(render: true);
		}
		snapCamera.Render();
		if (flag)
		{
			PlayerController.LOCAL.RenderPlayerHead(render: false);
		}
		flash.SetActive(value: true);
		snapCamera.gameObject.SetActive(value: false);
		_flash?.Stop();
		_flash = util_timer.Simple(0.1f, delegate
		{
			if ((bool)flash)
			{
				flash.SetActive(value: false);
			}
		});
		if (!(SteamworksController.IsSteamRunning && flag))
		{
			return;
		}
		RenderTexture targetTexture = snapCamera.targetTexture;
		if ((bool)targetTexture)
		{
			RenderTexture active = RenderTexture.active;
			RenderTexture.active = targetTexture;
			Texture2D texture2D = new Texture2D(targetTexture.width, targetTexture.height, TextureFormat.RGB24, mipChain: false);
			texture2D.ReadPixels(new Rect(0f, 0f, targetTexture.width, targetTexture.height), 0, 0);
			texture2D.Apply();
			RenderTexture.active = active;
			Color[] pixels = texture2D.GetPixels();
			Color[] array = new Color[pixels.Length];
			for (int i = 0; i < targetTexture.height; i++)
			{
				Array.Copy(pixels, i * targetTexture.width, array, (targetTexture.height - 1 - i) * targetTexture.width, targetTexture.width);
			}
			for (int j = 0; j < array.Length; j++)
			{
				array[j] = array[j].gamma;
			}
			texture2D.SetPixels(array);
			texture2D.Apply();
			byte[] rawTextureData = texture2D.GetRawTextureData();
			SteamScreenshots.WriteScreenshot(rawTextureData, (uint)rawTextureData.Length, targetTexture.width, targetTexture.height);
			UnityEngine.Object.Destroy(texture2D);
		}
	}

	protected override void __initializeVariables()
	{
		base.__initializeVariables();
	}

	protected override void __initializeRpcs()
	{
		__registerRpc(1203973843u, __rpc_handler_1203973843, "SnapRPC", RpcInvokePermission.Everyone);
		base.__initializeRpcs();
	}

	private static void __rpc_handler_1203973843(NetworkBehaviour target, FastBufferReader reader, __RpcParams rpcParams)
	{
		NetworkManager networkManager = target.NetworkManager;
		if ((object)networkManager != null && networkManager.IsListening)
		{
			target.__rpc_exec_stage = __RpcExecStage.Execute;
			((entity_picture_3000)target).SnapRPC();
			target.__rpc_exec_stage = __RpcExecStage.Send;
		}
	}

	protected internal override string __getTypeName()
	{
		return "entity_picture_3000";
	}
}
