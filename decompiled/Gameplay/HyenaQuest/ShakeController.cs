using System.Collections.Generic;
using Steamworks;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

namespace HyenaQuest;

[DisallowMultipleComponent]
[DefaultExecutionOrder(-70)]
[RequireComponent(typeof(NetworkObject))]
public class ShakeController : NetController<ShakeController>
{
	[Header("Settings")]
	public List<entity_shake> globalShakes;

	private readonly List<entity_shake> _shakes = new List<entity_shake>();

	private readonly Dictionary<string, (float lowFreq, float highFreq)> _vibrationSources = new Dictionary<string, (float, float)>();

	public override void OnNetworkDespawn()
	{
		base.OnNetworkDespawn();
		StopAllControllerVibration();
	}

	public void Register(entity_shake shake)
	{
		if ((bool)shake && !_shakes.Contains(shake))
		{
			_shakes.Add(shake);
		}
	}

	public void Unregister(entity_shake shake)
	{
		if ((bool)shake && _shakes.Contains(shake))
		{
			_shakes.Remove(shake);
		}
	}

	[Rpc(SendTo.ClientsAndHost)]
	public void ShakeRPC(ShakeMode mode, float time = 1.5f, float intensity = 0.005f, ShakeSoundMode soundMode = ShakeSoundMode.OFF)
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
			FastBufferWriter bufferWriter = __beginSendRpc(2135468764u, rpcParams, attributeParams, SendTo.ClientsAndHost, RpcDelivery.Reliable);
			bufferWriter.WriteValueSafe(in mode, default(FastBufferWriter.ForEnums));
			bufferWriter.WriteValueSafe(in time, default(FastBufferWriter.ForPrimitives));
			bufferWriter.WriteValueSafe(in intensity, default(FastBufferWriter.ForPrimitives));
			bufferWriter.WriteValueSafe(in soundMode, default(FastBufferWriter.ForEnums));
			__endSendRpc(ref bufferWriter, 2135468764u, rpcParams, attributeParams, SendTo.ClientsAndHost, RpcDelivery.Reliable);
		}
		if (__rpc_exec_stage == __RpcExecStage.Execute)
		{
			__rpc_exec_stage = __RpcExecStage.Send;
			LocalShake(mode, time, intensity, soundMode);
		}
	}

	[Rpc(SendTo.ClientsAndHost)]
	public void Shake3DRPC(Vector3 pos, ShakeMode mode, float time = 1.5f, float intensity = 0.005f, ShakeSoundMode soundMode = ShakeSoundMode.OFF, float radius = 3f)
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
			FastBufferWriter bufferWriter = __beginSendRpc(625564028u, rpcParams, attributeParams, SendTo.ClientsAndHost, RpcDelivery.Reliable);
			bufferWriter.WriteValueSafe(in pos);
			bufferWriter.WriteValueSafe(in mode, default(FastBufferWriter.ForEnums));
			bufferWriter.WriteValueSafe(in time, default(FastBufferWriter.ForPrimitives));
			bufferWriter.WriteValueSafe(in intensity, default(FastBufferWriter.ForPrimitives));
			bufferWriter.WriteValueSafe(in soundMode, default(FastBufferWriter.ForEnums));
			bufferWriter.WriteValueSafe(in radius, default(FastBufferWriter.ForPrimitives));
			__endSendRpc(ref bufferWriter, 625564028u, rpcParams, attributeParams, SendTo.ClientsAndHost, RpcDelivery.Reliable);
		}
		if (__rpc_exec_stage == __RpcExecStage.Execute)
		{
			__rpc_exec_stage = __RpcExecStage.Send;
			Local3DShake(pos, mode, time, intensity, soundMode, radius);
		}
	}

	[Client]
	public void LocalShake(ShakeMode mode, float time = 1.5f, float intensity = 0.005f, ShakeSoundMode soundMode = ShakeSoundMode.OFF)
	{
		entity_shake availableGlobalShake = GetAvailableGlobalShake();
		if ((bool)availableGlobalShake)
		{
			if (time <= 0f)
			{
				throw new UnityException("Shake time must be greater than 0 for global shake");
			}
			availableGlobalShake.SetSoundMode(soundMode);
			availableGlobalShake.SetShakeMode(mode);
			availableGlobalShake.SetIntensity(intensity);
			availableGlobalShake.SetRadius(0f);
			availableGlobalShake.SetActive(act: true, time);
		}
	}

	[Client]
	public void Local3DShake(Vector3 pos, ShakeMode mode, float time = 1.5f, float intensity = 0.005f, ShakeSoundMode soundMode = ShakeSoundMode.OFF, float radius = 2f)
	{
		entity_shake availableGlobalShake = GetAvailableGlobalShake();
		if ((bool)availableGlobalShake)
		{
			if (time <= 0f)
			{
				throw new UnityException("Shake time must be greater than 0 for global shake");
			}
			availableGlobalShake.transform.position = pos;
			availableGlobalShake.SetSoundMode(soundMode);
			availableGlobalShake.SetShakeMode(mode);
			availableGlobalShake.SetIntensity(intensity);
			availableGlobalShake.SetRadius(radius);
			availableGlobalShake.SetActive(act: true, time);
		}
	}

	[Client]
	public Vector3 ApplyShakes(Vector3 pos)
	{
		entity_player lOCAL = PlayerController.LOCAL;
		if (!lOCAL)
		{
			return pos;
		}
		Vector3 result = pos;
		float num = 0f;
		for (int i = 0; i < _shakes.Count; i++)
		{
			entity_shake entity_shake2 = _shakes[i];
			if (!entity_shake2 || !entity_shake2.IsActive())
			{
				continue;
			}
			float num2 = entity_shake2.GetIntensity();
			if (!entity_shake2.IsGlobal())
			{
				float num3 = Vector3.Distance(lOCAL.spectate.position, entity_shake2.transform.position);
				if (num3 > entity_shake2.GetRadius())
				{
					continue;
				}
				if (entity_shake2.ScaleWithIntensity())
				{
					float num4 = 1f - num3 / entity_shake2.GetRadius();
					num2 = entity_shake2.GetIntensity() * num4;
				}
			}
			if (num2 > num)
			{
				num = num2;
			}
			switch (entity_shake2.GetShakeMode())
			{
			case ShakeMode.SHAKE_ALL:
				result.z += Random.value * num2 * 2f - num2;
				result.x += Random.value * num2 * 2f - num2;
				break;
			case ShakeMode.SHAKE_UP:
				result.z += Random.value * num2 * 2f - num2;
				break;
			case ShakeMode.SHAKE_DOWN:
				result.z -= Random.value * num2 * 2f - num2;
				break;
			case ShakeMode.SHAKE_LEFT:
				result.x += Random.value * num2 * 2f - num2;
				break;
			case ShakeMode.SHAKE_RIGHT:
				result.x -= Random.value * num2 * 2f - num2;
				break;
			}
		}
		float num5 = Mathf.Clamp01(num * 80f);
		SetControllerVibration("camera_shake", num5, num5);
		return result;
	}

	public entity_shake GetAvailableGlobalShake()
	{
		foreach (entity_shake globalShake in globalShakes)
		{
			if (!globalShake.IsActive())
			{
				return globalShake;
			}
		}
		return globalShakes[0];
	}

	[Client]
	public void SetControllerVibration(string sourceId, float lowFreq, float highFreq)
	{
		if (lowFreq <= 0f && highFreq <= 0f)
		{
			_vibrationSources.Remove(sourceId);
		}
		else
		{
			_vibrationSources[sourceId] = (lowFreq, highFreq);
		}
		ApplyControllerVibration();
	}

	[Client]
	public void StopAllControllerVibration()
	{
		_vibrationSources.Clear();
		Gamepad.current?.SetMotorSpeeds(0f, 0f);
	}

	[Client]
	public void StopControllerVibration(string sourceId)
	{
		if (_vibrationSources.Remove(sourceId))
		{
			ApplyControllerVibration();
		}
	}

	[Client]
	private void ApplyControllerVibration()
	{
		if (!MonoController<SettingsController>.Instance)
		{
			return;
		}
		float num = 0f;
		float num2 = 0f;
		if (!MonoController<SettingsController>.Instance.CurrentSettings.disableVibration)
		{
			foreach (var value in _vibrationSources.Values)
			{
				num = Mathf.Max(num, value.lowFreq);
				num2 = Mathf.Max(num2, value.highFreq);
			}
			if (SteamworksController.IsSteamRunning)
			{
				InputHandle_t[] array = new InputHandle_t[16];
				SteamInput.GetConnectedControllers(array);
				if (array.Length != 0 && SteamInput.BNewDataAvailable())
				{
					SteamInput.TriggerVibration(array[0], (ushort)(Mathf.Clamp01(num) * 65535f), (ushort)(Mathf.Clamp01(num2) * 65535f));
					return;
				}
			}
			Gamepad.current?.SetMotorSpeeds(Mathf.Clamp01(num), Mathf.Clamp01(num2));
		}
		else
		{
			StopAllControllerVibration();
		}
	}

	protected override void __initializeVariables()
	{
		base.__initializeVariables();
	}

	protected override void __initializeRpcs()
	{
		__registerRpc(2135468764u, __rpc_handler_2135468764, "ShakeRPC", RpcInvokePermission.Everyone);
		__registerRpc(625564028u, __rpc_handler_625564028, "Shake3DRPC", RpcInvokePermission.Everyone);
		base.__initializeRpcs();
	}

	private static void __rpc_handler_2135468764(NetworkBehaviour target, FastBufferReader reader, __RpcParams rpcParams)
	{
		NetworkManager networkManager = target.NetworkManager;
		if ((object)networkManager != null && networkManager.IsListening)
		{
			reader.ReadValueSafe(out ShakeMode value, default(FastBufferWriter.ForEnums));
			reader.ReadValueSafe(out float value2, default(FastBufferWriter.ForPrimitives));
			reader.ReadValueSafe(out float value3, default(FastBufferWriter.ForPrimitives));
			reader.ReadValueSafe(out ShakeSoundMode value4, default(FastBufferWriter.ForEnums));
			target.__rpc_exec_stage = __RpcExecStage.Execute;
			((ShakeController)target).ShakeRPC(value, value2, value3, value4);
			target.__rpc_exec_stage = __RpcExecStage.Send;
		}
	}

	private static void __rpc_handler_625564028(NetworkBehaviour target, FastBufferReader reader, __RpcParams rpcParams)
	{
		NetworkManager networkManager = target.NetworkManager;
		if ((object)networkManager != null && networkManager.IsListening)
		{
			reader.ReadValueSafe(out Vector3 value);
			reader.ReadValueSafe(out ShakeMode value2, default(FastBufferWriter.ForEnums));
			reader.ReadValueSafe(out float value3, default(FastBufferWriter.ForPrimitives));
			reader.ReadValueSafe(out float value4, default(FastBufferWriter.ForPrimitives));
			reader.ReadValueSafe(out ShakeSoundMode value5, default(FastBufferWriter.ForEnums));
			reader.ReadValueSafe(out float value6, default(FastBufferWriter.ForPrimitives));
			target.__rpc_exec_stage = __RpcExecStage.Execute;
			((ShakeController)target).Shake3DRPC(value, value2, value3, value4, value5, value6);
			target.__rpc_exec_stage = __RpcExecStage.Send;
		}
	}

	protected internal override string __getTypeName()
	{
		return "ShakeController";
	}
}
