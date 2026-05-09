using System;
using System.Collections.Generic;
using FailCake;
using Unity.Netcode;
using UnityEngine;

namespace HyenaQuest;

[DisallowMultipleComponent]
[RequireComponent(typeof(NetworkObject))]
public class entity_yeen_of_the_year : NetworkBehaviour
{
	private static readonly string PICTURE_ID = "YEEN-DATA";

	private static readonly int MAX_SNAP_RETRIES = 10;

	public GameObject canvas;

	public RenderTexture renderTexture;

	private Camera _camera;

	private Texture2D _cachedTexture;

	private readonly Collider[] _raycastHits = new Collider[5];

	private int _layerMask;

	private util_timer _lastAttempt;

	private util_timer _randomAttempt;

	private util_net_picture _netPicture;

	private string _pictureChannel;

	private readonly NetVar<bool> _showCanvas = new NetVar<bool>(value: false);

	public void Awake()
	{
		if (!canvas)
		{
			throw new UnityException("Missing Canvas");
		}
		if (!renderTexture)
		{
			throw new UnityException("Missing RenderTexture");
		}
		_camera = GetComponentInChildren<Camera>(includeInactive: true);
		if (!_camera)
		{
			throw new UnityException("Missing Camera");
		}
		_camera.gameObject.SetActive(value: false);
		_layerMask = LayerMask.GetMask("entity_ground");
	}

	public override void OnNetworkSpawn()
	{
		base.OnNetworkSpawn();
		_pictureChannel = PICTURE_ID + "-" + base.NetworkObjectId;
		CoreController.WaitFor(delegate(EndController reportCtrl)
		{
			reportCtrl.OnReportStart += new Action<bool>(OnReportStart);
		});
		if (!base.IsServer)
		{
			_netPicture = new util_net_picture(this, _pictureChannel, null, OnDataReceived);
			RequestPaintDataRPC(base.RpcTarget.Server);
			return;
		}
		_netPicture = new util_net_picture(this, _pictureChannel, GetRenderTextureJPG);
		CoreController.WaitFor(delegate(SettingsController settingsCtrl)
		{
			if (_netPicture != null)
			{
				byte[] array = settingsCtrl.LoadYeenOfMonth();
				if (array != null && array.Length > 0)
				{
					OnDataReceived(array);
					_netPicture?.PreloadData(array);
					_showCanvas.Value = true;
				}
			}
		});
		CoreController.WaitFor(delegate(IngameController ingameCtrl)
		{
			ingameCtrl.OnStatusUpdated += new Action<INGAME_STATUS, bool>(OnIngameStatusUpdated);
		});
	}

	public override void OnNetworkDespawn()
	{
		base.OnNetworkDespawn();
		if ((bool)NetController<EndController>.Instance)
		{
			NetController<EndController>.Instance.OnReportStart -= new Action<bool>(OnReportStart);
		}
		if (base.IsServer)
		{
			if ((bool)_camera && (bool)_camera.targetTexture && (bool)renderTexture)
			{
				Graphics.CopyTexture(_camera.targetTexture, renderTexture);
			}
			if ((bool)renderTexture && _showCanvas.Value)
			{
				byte[] renderTextureJPG = GetRenderTextureJPG();
				if ((bool)MonoController<SettingsController>.Instance && renderTextureJPG != null)
				{
					MonoController<SettingsController>.Instance.SaveYeenMonthPaintData(renderTextureJPG);
				}
			}
			if ((bool)NetController<IngameController>.Instance)
			{
				NetController<IngameController>.Instance.OnStatusUpdated -= new Action<INGAME_STATUS, bool>(OnIngameStatusUpdated);
			}
			_randomAttempt?.Stop();
			_lastAttempt?.Stop();
		}
		if ((bool)_cachedTexture)
		{
			UnityEngine.Object.Destroy(_cachedTexture);
		}
		_cachedTexture = null;
		_netPicture?.Dispose();
		_netPicture = null;
	}

	protected override void OnNetworkPostSpawn()
	{
		base.OnNetworkPostSpawn();
		if (!base.IsClient)
		{
			return;
		}
		_showCanvas.RegisterOnValueChanged(delegate(bool _, bool newValue)
		{
			if ((bool)canvas)
			{
				canvas.SetActive(newValue);
			}
		});
	}

	public override void OnNetworkPreDespawn()
	{
		base.OnNetworkPreDespawn();
		if (base.IsClient)
		{
			_showCanvas.OnValueChanged = null;
		}
	}

	private void OnIngameStatusUpdated(INGAME_STATUS status, bool server)
	{
		if (!server)
		{
			return;
		}
		_randomAttempt?.Stop();
		if (status == INGAME_STATUS.PLAYING)
		{
			float delay = UnityEngine.Random.Range(120, 280);
			_randomAttempt = util_timer.Simple(delay, delegate
			{
				AttemptSnap(all: true);
			});
		}
	}

	[Server]
	private void Snap(entity_player ply, Action onPictureTaken = null, Action onPictureFailed = null, int retries = 0)
	{
		if (!base.IsServer)
		{
			throw new UnityException("Server only");
		}
		if (!ply)
		{
			throw new UnityException("Invalid entity_player ply");
		}
		_lastAttempt?.Stop();
		bool isDead = ply.IsDead();
		float delay = (isDead ? 0f : 0.25f);
		_camera.gameObject.SetActive(value: true);
		_lastAttempt?.Stop();
		Vector3 snapPosition;
		Vector3 snapForward;
		_lastAttempt = util_timer.Simple(delay, delegate
		{
			if (isDead)
			{
				entity_ragdoll ragdoll = ply.GetRagdoll();
				if ((bool)ragdoll)
				{
					Transform transform = ragdoll.vfxFlies.transform;
					snapPosition = transform.position;
					snapForward = transform.forward;
				}
				else
				{
					snapPosition = ply.GetDeathLocation();
					snapForward = Vector3.forward;
				}
			}
			else
			{
				Transform headTransform = ply.GetHeadTransform();
				snapPosition = headTransform?.position ?? Vector3.zero;
				snapForward = headTransform?.forward ?? Vector3.zero;
			}
			Vector3 worldPosition = snapPosition;
			Vector3 position = snapPosition + new Vector3(isDead ? UnityEngine.Random.Range(-0.6f, 0.6f) : 0f, isDead ? 0.8f : UnityEngine.Random.Range(0.2f, 0.8f), isDead ? UnityEngine.Random.Range(-0.6f, 0.6f) : 0f) + snapForward * UnityEngine.Random.Range(0.73f, 1f);
			_camera.transform.position = position;
			_camera.transform.LookAt(worldPosition);
			if (!isDead && Physics.OverlapSphereNonAlloc(position, 0.12f, _raycastHits, _layerMask, QueryTriggerInteraction.Ignore) > 0)
			{
				if (retries >= MAX_SNAP_RETRIES)
				{
					_camera.gameObject.SetActive(value: false);
					onPictureFailed?.Invoke();
				}
				else
				{
					_lastAttempt = util_timer.Simple(0.5f, delegate
					{
						Snap(ply, onPictureTaken, onPictureFailed, retries + 1);
					});
				}
			}
			else
			{
				Physics.SyncTransforms();
				ply.RenderPlayerHead(render: true);
				_camera.Render();
				_camera.gameObject.SetActive(value: false);
				ply.RenderPlayerHead(render: false);
				_netPicture?.Transmit(NETController.Instance.LocalClient.ClientId);
				onPictureTaken?.Invoke();
			}
		});
	}

	[Server]
	private void AttemptSnap(bool all)
	{
		if (!base.IsServer)
		{
			throw new UnityException("Server only");
		}
		List<entity_player> list = ((!all) ? (MonoController<PlayerController>.Instance?.GetAlivePlayers() ?? null) : (MonoController<PlayerController>.Instance?.GetAllPlayers() ?? null));
		if (list == null || list.Count <= 0)
		{
			return;
		}
		entity_player entity_player2 = list[UnityEngine.Random.Range(0, list.Count)];
		if (entity_player2.IsDead())
		{
			Snap(entity_player2, null, delegate
			{
				AttemptSnap(all: false);
			});
		}
		else
		{
			Snap(entity_player2);
		}
	}

	private void OnReportStart(bool server)
	{
		if ((bool)renderTexture && (bool)_camera.targetTexture)
		{
			Graphics.CopyTexture(_camera.targetTexture, renderTexture);
			if (server)
			{
				_showCanvas.Value = true;
			}
		}
	}

	private byte[] GetRenderTextureJPG()
	{
		if (!_camera)
		{
			return null;
		}
		RenderTexture active = RenderTexture.active;
		RenderTexture.active = _camera.targetTexture;
		bool linear = !RenderTexture.active.sRGB;
		int width = RenderTexture.active.width;
		int height = RenderTexture.active.height;
		if (!_cachedTexture || _cachedTexture.width != width || _cachedTexture.height != height)
		{
			if ((bool)_cachedTexture)
			{
				UnityEngine.Object.Destroy(_cachedTexture);
			}
			_cachedTexture = new Texture2D(width, height, TextureFormat.RGB24, mipChain: false, linear);
		}
		_cachedTexture.ReadPixels(new Rect(0f, 0f, width, height), 0, 0);
		_cachedTexture.Apply();
		RenderTexture.active = active;
		return _cachedTexture.EncodeToJPG(90);
	}

	private void ApplyTextureData(byte[] imageData, RenderTexture texture)
	{
		if (imageData != null && imageData.Length != 0)
		{
			bool linear = !texture.sRGB;
			if (!_cachedTexture)
			{
				_cachedTexture = new Texture2D(2, 2, TextureFormat.RGB24, mipChain: false, linear);
			}
			if (_cachedTexture.LoadImage(imageData))
			{
				Graphics.Blit(_cachedTexture, texture);
			}
		}
	}

	[Rpc(SendTo.SpecifiedInParams)]
	private void RequestPaintDataRPC(RpcParams param)
	{
		NetworkManager networkManager = base.NetworkManager;
		if ((object)networkManager == null || !networkManager.IsListening)
		{
			Debug.LogError("Rpc methods can only be invoked after starting the NetworkManager!");
			return;
		}
		if (__rpc_exec_stage != __RpcExecStage.Execute)
		{
			RpcParams rpcParams = param;
			RpcAttribute.RpcAttributeParams attributeParams = default(RpcAttribute.RpcAttributeParams);
			FastBufferWriter bufferWriter = __beginSendRpc(252911679u, rpcParams, attributeParams, SendTo.SpecifiedInParams, RpcDelivery.Reliable);
			__endSendRpc(ref bufferWriter, 252911679u, param, attributeParams, SendTo.SpecifiedInParams, RpcDelivery.Reliable);
		}
		if (__rpc_exec_stage == __RpcExecStage.Execute)
		{
			__rpc_exec_stage = __RpcExecStage.Send;
			if (!base.IsServer)
			{
				throw new UnityException("RequestPaintDataRPC can only be called on the server");
			}
			_netPicture?.AddRequest(param.Receive.SenderClientId);
		}
	}

	private void OnDataReceived(byte[] imageData)
	{
		if ((bool)_camera && imageData != null && imageData.Length > 0)
		{
			ApplyTextureData(imageData, _camera.targetTexture);
			IngameController instance = NetController<IngameController>.Instance;
			if ((object)instance == null || instance.Status() == INGAME_STATUS.IDLE)
			{
				OnReportStart(server: false);
			}
		}
	}

	protected override void __initializeVariables()
	{
		if (_showCanvas == null)
		{
			throw new Exception("entity_yeen_of_the_year._showCanvas cannot be null. All NetworkVariableBase instances must be initialized.");
		}
		_showCanvas.Initialize(this);
		__nameNetworkVariable(_showCanvas, "_showCanvas");
		NetworkVariableFields.Add(_showCanvas);
		base.__initializeVariables();
	}

	protected override void __initializeRpcs()
	{
		__registerRpc(252911679u, __rpc_handler_252911679, "RequestPaintDataRPC", RpcInvokePermission.Everyone);
		base.__initializeRpcs();
	}

	private static void __rpc_handler_252911679(NetworkBehaviour target, FastBufferReader reader, __RpcParams rpcParams)
	{
		NetworkManager networkManager = target.NetworkManager;
		if ((object)networkManager != null && networkManager.IsListening)
		{
			RpcParams ext = rpcParams.Ext;
			target.__rpc_exec_stage = __RpcExecStage.Execute;
			((entity_yeen_of_the_year)target).RequestPaintDataRPC(ext);
			target.__rpc_exec_stage = __RpcExecStage.Send;
		}
	}

	protected internal override string __getTypeName()
	{
		return "entity_yeen_of_the_year";
	}
}
