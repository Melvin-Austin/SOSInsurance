using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.Pool;

namespace HyenaQuest;

[DisallowMultipleComponent]
[DefaultExecutionOrder(-90)]
[RequireComponent(typeof(NetworkObject))]
public class SoundController : NetController<SoundController>
{
	public SoundDatabase database;

	public AudioMixer mixer;

	public GameObject soundTemplate;

	private IObjectPool<entity_sound> _soundPool;

	private AudioLowPassFilter _lowPassFilter;

	public new void Awake()
	{
		base.Awake();
		if ((bool)SDK.MainCamera)
		{
			_lowPassFilter = SDK.MainCamera.GetComponent<AudioLowPassFilter>();
			if (!_lowPassFilter)
			{
				throw new UnityException("AudioLowPassFilter not found on main camera");
			}
			_lowPassFilter.enabled = false;
		}
		_soundPool = new ObjectPool<entity_sound>(CreatePooledItem, OnTakeFromPool, OnReturnedToPool, OnDestroyPoolObject, collectionCheck: true, 5, 30);
		SDK.Play2DSound = delegate(string path, AudioData data, bool broadcast)
		{
			PlaySound(path, data, broadcast);
		};
		SDK.Play2DSoundClip = delegate(AudioClip clip, AudioData data, bool broadcast)
		{
			PlaySound(clip, data, broadcast);
		};
		SDK.Play3DSound = delegate(string path, Vector3 pos, AudioData data, bool broadcast)
		{
			Play3DSound(path, pos, data, broadcast);
		};
		SDK.Play3DSoundClip = delegate(AudioClip clip, Vector3 pos, AudioData data, bool broadcast)
		{
			Play3DSound(clip, pos, data, broadcast);
		};
	}

	public override void OnNetworkSpawn()
	{
		base.OnNetworkSpawn();
		CoreController.WaitFor(delegate(SettingsController settCtrl)
		{
			settCtrl.OnSettingsUpdated += new Action(OnSettingsUpdated);
		});
	}

	public override void OnDestroy()
	{
		if ((bool)MonoController<SettingsController>.Instance)
		{
			MonoController<SettingsController>.Instance.OnSettingsUpdated -= new Action(OnSettingsUpdated);
		}
		SDK.Play2DSound = null;
		SDK.Play2DSoundClip = null;
		SDK.Play3DSound = null;
		SDK.Play3DSoundClip = null;
		base.OnDestroy();
	}

	public void SetInsideVolume(VolumeType type, bool inside)
	{
		if ((bool)_lowPassFilter)
		{
			_lowPassFilter.enabled = type == VolumeType.WATER && inside;
		}
	}

	public entity_sound Play3DSound(AudioClip clip, Transform position, AudioData data, bool broadcast = false)
	{
		if (!clip || !SDK.MainCamera)
		{
			return null;
		}
		string pathByAudio = GetPathByAudio(clip);
		if (string.IsNullOrEmpty(pathByAudio))
		{
			return null;
		}
		if (broadcast)
		{
			PlaySoundRPC(pathByAudio, position.position, data, base.RpcTarget.NotMe);
		}
		if (Vector3.Distance(position.position, SDK.MainCamera.transform.position) > data.distance + 3f)
		{
			return null;
		}
		entity_sound entity_sound2 = CreateSoundObject();
		NetworkBehaviour networkBehaviour = NETController.Get<NetworkBehaviour>(data.parent);
		if ((bool)networkBehaviour)
		{
			entity_sound2.transform.SetParent(networkBehaviour.transform, worldPositionStays: false);
		}
		entity_sound2.SetMixer(mixer.FindMatchingGroups(data.mixer.ToString())[0]);
		entity_sound2.SetClip(clip);
		entity_sound2.Set3DTarget(position, data.distance);
		entity_sound2.SetPitch(data.pitch);
		entity_sound2.SetVolume(data.volume);
		entity_sound2.PlayOnce();
		return entity_sound2;
	}

	public entity_sound Play3DSound(AudioClip clip, Vector3 position, AudioData data, bool broadcast = false)
	{
		if (data == null)
		{
			throw new UnityException("AudioData cannot be null");
		}
		if (!clip || !SDK.MainCamera)
		{
			return null;
		}
		string pathByAudio = GetPathByAudio(clip);
		if (string.IsNullOrEmpty(pathByAudio))
		{
			return null;
		}
		if (broadcast)
		{
			PlaySoundRPC(pathByAudio, position, data, base.RpcTarget.NotMe);
		}
		if (Vector3.Distance(position, SDK.MainCamera.transform.position) > data.distance + 3f)
		{
			return null;
		}
		entity_sound entity_sound2 = CreateSoundObject();
		NetworkBehaviour networkBehaviour = NETController.Get<NetworkBehaviour>(data.parent);
		if ((bool)networkBehaviour)
		{
			entity_sound2.transform.SetParent(networkBehaviour.transform, worldPositionStays: false);
		}
		entity_sound2.SetMixer(mixer.FindMatchingGroups(data.mixer.ToString())[0]);
		entity_sound2.SetClip(clip);
		entity_sound2.Set3DTarget(position, data.distance);
		entity_sound2.SetPitch(data.pitch);
		entity_sound2.SetVolume(data.volume);
		entity_sound2.PlayOnce();
		return entity_sound2;
	}

	public entity_sound Play3DSound(string path, Vector3 position, AudioData data, bool broadcast = false)
	{
		if (data == null)
		{
			throw new UnityException("AudioData cannot be null");
		}
		if (string.IsNullOrEmpty(path))
		{
			return null;
		}
		AudioClip audioByPath = GetAudioByPath(path);
		if (!audioByPath)
		{
			return null;
		}
		return Play3DSound(audioByPath, position, data, broadcast);
	}

	public entity_sound Play3DSound(string path, Transform position, AudioData data, bool broadcast = false)
	{
		if (data == null)
		{
			throw new UnityException("AudioData cannot be null");
		}
		if (string.IsNullOrEmpty(path))
		{
			return null;
		}
		AudioClip audioByPath = GetAudioByPath(path);
		if (!audioByPath)
		{
			return null;
		}
		return Play3DSound(audioByPath, position, data, broadcast);
	}

	public entity_sound PlaySound(AudioClip clip, AudioData data, bool broadcast = false)
	{
		if (data == null)
		{
			throw new UnityException("AudioData cannot be null");
		}
		if (!clip)
		{
			return null;
		}
		string pathByAudio = GetPathByAudio(clip);
		if (string.IsNullOrEmpty(pathByAudio))
		{
			return null;
		}
		if (broadcast)
		{
			data.distance = 0f;
			PlaySoundRPC(pathByAudio, Vector3.zero, data, base.RpcTarget.NotMe);
		}
		entity_sound obj = CreateSoundObject();
		obj.SetMixer(mixer.FindMatchingGroups(data.mixer.ToString())[0]);
		obj.SetClip(clip);
		obj.Set2D();
		obj.SetPitch(data.pitch);
		obj.SetVolume(data.volume);
		obj.PlayOnce();
		return obj;
	}

	public entity_sound PlaySound(string path, AudioData data, bool broadcast = false)
	{
		if (data == null)
		{
			throw new UnityException("AudioData cannot be null");
		}
		if (string.IsNullOrEmpty(path))
		{
			return null;
		}
		AudioClip audioByPath = GetAudioByPath(path);
		if (!audioByPath)
		{
			return null;
		}
		return PlaySound(audioByPath, data, broadcast);
	}

	public entity_sound PlaySound(string path, AudioData data, ulong connectionID)
	{
		if (data == null)
		{
			throw new UnityException("AudioData cannot be null");
		}
		if (!base.IsServer || string.IsNullOrEmpty(path))
		{
			return null;
		}
		data.distance = 0f;
		PlaySoundRPC(path, Vector3.zero, data, base.RpcTarget.Single(connectionID, RpcTargetUse.Temp));
		return null;
	}

	public AudioClip GetClip(string path)
	{
		if (!string.IsNullOrEmpty(path))
		{
			return GetAudioByPath(path);
		}
		return null;
	}

	public void QueueSound(entity_sound snd)
	{
		_soundPool?.Release(snd);
	}

	private void OnSettingsUpdated()
	{
		if ((bool)MonoController<SettingsController>.Instance && (bool)mixer)
		{
			PlayerSettings currentSettings = MonoController<SettingsController>.Instance.CurrentSettings;
			mixer.SetFloat("MasterVolume", Mathf.Lerp(-60f, 0f, currentSettings.masterVolume));
			mixer.SetFloat("MusicVolume", Mathf.Lerp(-60f, 0f, currentSettings.musicVolume));
			mixer.SetFloat("SFXVolume", Mathf.Lerp(-60f, -5f, currentSettings.sfxVolume));
			mixer.SetFloat("MicrophoneVolume", Mathf.Lerp(-60f, 0f, currentSettings.microphoneVolume));
		}
	}

	private entity_sound CreatePooledItem()
	{
		return UnityEngine.Object.Instantiate(soundTemplate, Vector3.zero, Quaternion.identity, base.gameObject.transform).GetComponent<entity_sound>();
	}

	private void OnReturnedToPool(entity_sound sound)
	{
		sound.transform.parent = base.gameObject.transform;
	}

	private void OnTakeFromPool(entity_sound sound)
	{
	}

	private void OnDestroyPoolObject(entity_sound sound)
	{
		UnityEngine.Object.Destroy(sound.gameObject);
	}

	private entity_sound CreateSoundObject()
	{
		return _soundPool?.Get();
	}

	private AudioClip GetAudioByPath(string path)
	{
		if (!database.database.TryGetValue(path, out var value))
		{
			Debug.LogError("Audio clip " + path + " not found in database. Did you run CollectSounds?");
			return null;
		}
		return value;
	}

	private string GetPathByAudio(AudioClip clip)
	{
		foreach (KeyValuePair<string, AudioClip> item in database.database)
		{
			if (item.Value == clip)
			{
				return item.Key;
			}
		}
		Debug.LogError("Audio clip not found in database. Did you run CollectSounds?");
		return null;
	}

	[Rpc(SendTo.SpecifiedInParams)]
	private void PlaySoundRPC(FixedString512Bytes clip, Vector3 pos, AudioData data, RpcParams param = default(RpcParams))
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
			FastBufferWriter bufferWriter = __beginSendRpc(2855580144u, param, attributeParams, SendTo.SpecifiedInParams, RpcDelivery.Reliable);
			bufferWriter.WriteValueSafe(in clip, default(FastBufferWriter.ForFixedStrings));
			bufferWriter.WriteValueSafe(in pos);
			bool value = (object)data != null;
			bufferWriter.WriteValueSafe(in value, default(FastBufferWriter.ForPrimitives));
			if (value)
			{
				bufferWriter.WriteValueSafe(in data, default(FastBufferWriter.ForNetworkSerializable));
			}
			__endSendRpc(ref bufferWriter, 2855580144u, param, attributeParams, SendTo.SpecifiedInParams, RpcDelivery.Reliable);
		}
		if (__rpc_exec_stage == __RpcExecStage.Execute)
		{
			__rpc_exec_stage = __RpcExecStage.Send;
			if (data.distance <= 0f)
			{
				NetController<SoundController>.Instance.PlaySound(clip.ToString(), data);
			}
			else
			{
				NetController<SoundController>.Instance.Play3DSound(clip.ToString(), pos, data);
			}
		}
	}

	protected override void __initializeVariables()
	{
		base.__initializeVariables();
	}

	protected override void __initializeRpcs()
	{
		__registerRpc(2855580144u, __rpc_handler_2855580144, "PlaySoundRPC", RpcInvokePermission.Everyone);
		base.__initializeRpcs();
	}

	private static void __rpc_handler_2855580144(NetworkBehaviour target, FastBufferReader reader, __RpcParams rpcParams)
	{
		NetworkManager networkManager = target.NetworkManager;
		if ((object)networkManager != null && networkManager.IsListening)
		{
			reader.ReadValueSafe(out FixedString512Bytes value, default(FastBufferWriter.ForFixedStrings));
			reader.ReadValueSafe(out Vector3 value2);
			reader.ReadValueSafe(out bool value3, default(FastBufferWriter.ForPrimitives));
			AudioData value4 = null;
			if (value3)
			{
				reader.ReadValueSafe(out value4, default(FastBufferWriter.ForNetworkSerializable));
			}
			RpcParams ext = rpcParams.Ext;
			target.__rpc_exec_stage = __RpcExecStage.Execute;
			((SoundController)target).PlaySoundRPC(value, value2, value4, ext);
			target.__rpc_exec_stage = __RpcExecStage.Send;
		}
	}

	protected internal override string __getTypeName()
	{
		return "SoundController";
	}
}
