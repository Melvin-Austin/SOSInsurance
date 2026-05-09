using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

namespace HyenaQuest;

public class entity_phys_bricky : entity_phys
{
	private const float ALONE_RANGE = 4f;

	private const float FOG_START_DELAY = 2f;

	private const float FOG_TRANSITION_TIME = 2f;

	private const float STARE_DURATION = 4f;

	private const float STARE_THRESHOLD = 0.7f;

	private const float STARE_RANGE = 1.35f;

	private const float SHAKE_START_TIME = 5f;

	private const float SHAKE_RAMP_TIME = 8f;

	private const float KILL_START_TIME = 8f;

	private const float KILL_REQUIRED_SPEED = 10f;

	private const byte KILL_DAMAGE = 2;

	private const float CREEPY_EYE_DELAY = 1.5f;

	private const float CREEPY_EYE_RESET_DELAY = 0.1f;

	private entity_ambient_sound_mixer _ambientSound;

	private entity_googly[] _eyes;

	private entity_shake _shake;

	private float _canWhisperAfter;

	private float _stareStartTime;

	private float _originalFogDensity;

	private Color _originalFogColor;

	private float _offScreenTime;

	private float _onScreenTime;

	private bool _eyesLocked;

	private bool _hasWhispered;

	private bool _shakeStarted;

	private bool _wasGrabbed;

	private bool _damagePlayer;

	private bool _creepyEyesActive;

	[Client]
	public override InteractionData InteractionSelector(Collider obj)
	{
		if (!IsBeingGrabbed())
		{
			return new InteractionData(Interaction.INTERACT, _renderers, "BRICKY");
		}
		return null;
	}

	public new void Update()
	{
		base.Update();
		if (!base.IsOwner || !_ambientSound)
		{
			return;
		}
		if (!IsPlayerAlone())
		{
			ResetState();
			return;
		}
		bool flag = IsBeingGrabbed();
		if (_wasGrabbed && !flag)
		{
			_damagePlayer = false;
			ResetState();
		}
		if (flag)
		{
			UpdateStareBehavior();
			_wasGrabbed = true;
		}
		else
		{
			_wasGrabbed = false;
			UpdateCreepyEyeBehavior();
		}
	}

	protected override void Init()
	{
		base.Init();
		_ambientSound = GetComponentInChildren<entity_ambient_sound_mixer>(includeInactive: true);
		if (!_ambientSound)
		{
			throw new UnityException("entity_phys_bricky requires entity_ambient_sound_mixer");
		}
		_ambientSound.gameObject.SetActive(value: false);
		_eyes = GetComponentsInChildren<entity_googly>(includeInactive: true);
		entity_googly[] eyes = _eyes;
		if (eyes == null || eyes.Length <= 0)
		{
			throw new UnityException("entity_phys_bricky requires entity_googly");
		}
		_shake = GetComponentInChildren<entity_shake>(includeInactive: true);
		if (!_shake)
		{
			throw new UnityException("entity_phys_bricky requires entity_shake");
		}
		_shake.active = false;
	}

	protected override void OnNetworkPostSpawn()
	{
		base.OnNetworkPostSpawn();
		if (base.IsClient)
		{
			_grabbingOwnerId.RegisterOnValueChanged(delegate
			{
				_damagePlayer = false;
				ResetState();
			});
		}
	}

	protected override void OnCollision(Collision col)
	{
		if (base.IsOwner && _damagePlayer && col.collider.CompareTag("Player") && !(col.relativeVelocity.sqrMagnitude <= 10f) && col.gameObject.TryGetComponent<entity_player>(out var component) && !(component == PlayerController.LOCAL) && !component.IsDead())
		{
			_damagePlayer = false;
			RequestDamageRPC(component.GetPlayerID());
		}
	}

	[Rpc(SendTo.Server)]
	private void RequestDamageRPC(byte playerID)
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
			FastBufferWriter bufferWriter = __beginSendRpc(259425506u, rpcParams, attributeParams, SendTo.Server, RpcDelivery.Reliable);
			bufferWriter.WriteValueSafe(in playerID, default(FastBufferWriter.ForPrimitives));
			__endSendRpc(ref bufferWriter, 259425506u, rpcParams, attributeParams, SendTo.Server, RpcDelivery.Reliable);
		}
		if (__rpc_exec_stage != __RpcExecStage.Execute)
		{
			return;
		}
		__rpc_exec_stage = __RpcExecStage.Send;
		Player player = MonoController<PlayerController>.Instance.GetPlayer(playerID);
		if ((bool)player?.player && !(Vector3.Distance(base.transform.position, player.player.transform.position) > 1.8f))
		{
			byte b = (byte)((player.player.GetHealth() - 2 > 0) ? 2 : 0);
			if (b > 0)
			{
				player.player.TakeHealthRPC(b);
			}
			NetController<StatsController>.Instance?.UnlockAchievementSV(STEAM_ACHIEVEMENTS.ACHIEVEMENT_BRICKY, base.OwnerClientId);
		}
	}

	[Client]
	private void UpdateStareBehavior()
	{
		if (!SDK.MainCamera)
		{
			return;
		}
		Transform transform = SDK.MainCamera.transform;
		if (Vector3.Distance(base.transform.position, transform.position) > 1.35f)
		{
			if (_stareStartTime > 0f)
			{
				ResetState();
			}
			return;
		}
		Vector3 normalized = (base.transform.position - transform.position).normalized;
		Vector3 normalized2 = (transform.position - base.transform.position).normalized;
		float num = Vector3.Dot(transform.forward, normalized);
		float num2 = Vector3.Dot(base.transform.up, normalized2);
		if (num < 0.7f || num2 < 0.7f)
		{
			if (_stareStartTime > 0f)
			{
				ResetState();
			}
			return;
		}
		if (_stareStartTime == 0f)
		{
			_stareStartTime = Time.time;
			_canWhisperAfter = Time.time + (float)Random.Range(2, 5);
		}
		float num3 = Time.time - _stareStartTime;
		UpdateFogEffect(num3);
		if (!_eyesLocked && num3 >= 4f)
		{
			ActivateEyeLock(transform);
		}
		if (_eyesLocked)
		{
			UpdateLockedEyesBehavior(num3);
		}
	}

	private void UpdateFogEffect(float duration)
	{
		if (!(duration < 2f))
		{
			if (_originalFogDensity == 0f)
			{
				_originalFogDensity = RenderSettings.fogDensity;
				_originalFogColor = RenderSettings.fogColor;
			}
			float num = duration - 2f;
			if (num < 2f)
			{
				float num2 = num / 2f;
				RenderSettings.fogDensity = Mathf.Lerp(_originalFogDensity, 1f, num2);
				RenderSettings.fogColor = Color.Lerp(_originalFogColor, Color.black, Mathf.Clamp01(num2 * 4f));
			}
			else
			{
				RenderSettings.fogDensity = 1f;
				RenderSettings.fogColor = Color.black;
			}
		}
	}

	private void ActivateEyeLock(Transform target)
	{
		_eyesLocked = true;
		_ambientSound.gameObject.SetActive(value: true);
		entity_googly[] eyes = _eyes;
		foreach (entity_googly entity_googly2 in eyes)
		{
			if ((bool)entity_googly2)
			{
				entity_googly2.LockOnTarget(target, 0.5f);
			}
		}
	}

	private void UpdateLockedEyesBehavior(float duration)
	{
		if (!_hasWhispered && Time.time >= _canWhisperAfter)
		{
			_hasWhispered = true;
			NetController<SoundController>.Instance.PlaySound($"Ingame/Props/Special/Bricky/whispers_{Random.Range(0, 11)}.ogg", new AudioData
			{
				mixer = SoundMixer.CURSES
			});
		}
		if ((bool)_shake && duration >= 5f)
		{
			if (!_shakeStarted)
			{
				_shakeStarted = true;
				_shake.active = true;
			}
			float num = duration - 5f;
			_shake.intensity = Mathf.Lerp(0f, 0.01f, Mathf.Clamp01(num / 8f));
		}
		if (base.IsOwner && !_damagePlayer && duration > 8f)
		{
			_damagePlayer = true;
			NetController<SoundController>.Instance.PlaySound($"Ingame/Props/Special/Bricky/do_it_{Random.Range(0, 3)}.ogg", new AudioData
			{
				mixer = SoundMixer.CURSES
			});
		}
	}

	private bool IsPlayerAlone()
	{
		if (!PlayerController.LOCAL || PlayerController.LOCAL.IsDead())
		{
			return false;
		}
		IngameController instance = NetController<IngameController>.Instance;
		if ((object)instance != null && instance.GetConnectedPlayers() <= 1)
		{
			return false;
		}
		HashSet<entity_player> playerEntitiesByDistance = MonoController<PlayerController>.Instance.GetPlayerEntitiesByDistance(base.transform.position, 4f);
		if (playerEntitiesByDistance.Count == 1)
		{
			return playerEntitiesByDistance.Contains(PlayerController.LOCAL);
		}
		return false;
	}

	[Client]
	private void UpdateCreepyEyeBehavior()
	{
		if (!SDK.MainCamera || !PlayerController.LOCAL)
		{
			return;
		}
		if (!IsVisibleOnScreen())
		{
			if (_creepyEyesActive)
			{
				return;
			}
			if (_offScreenTime == 0f)
			{
				_offScreenTime = Time.time;
			}
			_onScreenTime = 0f;
			if (!(Time.time - _offScreenTime >= 1.5f))
			{
				return;
			}
			_creepyEyesActive = true;
			entity_googly[] eyes = _eyes;
			foreach (entity_googly entity_googly2 in eyes)
			{
				if ((bool)entity_googly2)
				{
					entity_googly2.LockOnTarget(SDK.MainCamera.transform, 2f);
				}
			}
		}
		else if (_creepyEyesActive)
		{
			if (_onScreenTime == 0f)
			{
				_onScreenTime = Time.time;
			}
			if (!(Time.time - _onScreenTime >= 0.1f))
			{
				return;
			}
			_creepyEyesActive = false;
			_offScreenTime = 0f;
			_onScreenTime = 0f;
			entity_googly[] eyes = _eyes;
			foreach (entity_googly entity_googly3 in eyes)
			{
				if ((bool)entity_googly3)
				{
					entity_googly3.Unlock();
				}
			}
		}
		else
		{
			_offScreenTime = 0f;
			_onScreenTime = 0f;
		}
	}

	[Client]
	private bool IsVisibleOnScreen()
	{
		if (!SDK.MainCamera)
		{
			return true;
		}
		Plane[] planes = GeometryUtility.CalculateFrustumPlanes(SDK.MainCamera);
		if (_renderers == null)
		{
			return false;
		}
		Renderer[] renderers = _renderers;
		foreach (Renderer renderer in renderers)
		{
			if ((bool)renderer && GeometryUtility.TestPlanesAABB(planes, renderer.bounds))
			{
				return true;
			}
		}
		return false;
	}

	private void ResetState()
	{
		if (_originalFogDensity > 0f)
		{
			RenderSettings.fogDensity = _originalFogDensity;
			RenderSettings.fogColor = _originalFogColor;
		}
		if ((_eyesLocked || _creepyEyesActive) && _eyes != null)
		{
			entity_googly[] eyes = _eyes;
			foreach (entity_googly entity_googly2 in eyes)
			{
				if ((bool)entity_googly2)
				{
					entity_googly2.Unlock();
				}
			}
		}
		if ((bool)_shake)
		{
			_shake.active = false;
		}
		_stareStartTime = 0f;
		_eyesLocked = false;
		_hasWhispered = false;
		_shakeStarted = false;
		_creepyEyesActive = false;
		_offScreenTime = 0f;
		_onScreenTime = 0f;
		_originalFogDensity = 0f;
		_ambientSound.gameObject.SetActive(value: false);
	}

	protected override void __initializeVariables()
	{
		base.__initializeVariables();
	}

	protected override void __initializeRpcs()
	{
		__registerRpc(259425506u, __rpc_handler_259425506, "RequestDamageRPC", RpcInvokePermission.Everyone);
		base.__initializeRpcs();
	}

	private static void __rpc_handler_259425506(NetworkBehaviour target, FastBufferReader reader, __RpcParams rpcParams)
	{
		NetworkManager networkManager = target.NetworkManager;
		if ((object)networkManager != null && networkManager.IsListening)
		{
			reader.ReadValueSafe(out byte value, default(FastBufferWriter.ForPrimitives));
			target.__rpc_exec_stage = __RpcExecStage.Execute;
			((entity_phys_bricky)target).RequestDamageRPC(value);
			target.__rpc_exec_stage = __RpcExecStage.Send;
		}
	}

	protected internal override string __getTypeName()
	{
		return "entity_phys_bricky";
	}
}
