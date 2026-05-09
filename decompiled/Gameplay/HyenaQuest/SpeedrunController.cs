using System;
using System.Collections.Generic;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using ZLinq;

namespace HyenaQuest;

[DisallowMultipleComponent]
[DefaultExecutionOrder(-70)]
[RequireComponent(typeof(NetworkObject))]
public class SpeedrunController : NetController<SpeedrunController>
{
	public entity_split_flap_display totalTime;

	public entity_split_flap_display roundTime;

	public TextMeshPro bestTimeText;

	public GameObject pause;

	public GameObject cheating;

	public ParticleSystem confettiFx;

	private readonly NetVar<bool> _isMainTimerPaused = new NetVar<bool>(value: true);

	private readonly NetVar<bool> _isCheating = new NetVar<bool>(value: false);

	private readonly NetVar<uint> _totalTime = new NetVar<uint>(0u);

	private readonly NetVar<uint> _roundTime = new NetVar<uint>(0u);

	private readonly NetVar<uint> _roundBestTime = new NetVar<uint>(0u);

	private uint _totalMsAccumulator;

	public new void Awake()
	{
		base.Awake();
		if (!totalTime)
		{
			throw new UnityException("Missing totalTime entity_split_flap_display");
		}
		if (!roundTime)
		{
			throw new UnityException("Missing roundTime entity_split_flap_display");
		}
		if (!bestTimeText)
		{
			throw new UnityException("Missing bestTimeText TextMeshPro");
		}
		if (!pause)
		{
			throw new UnityException("Missing pause GameObject");
		}
		if (!cheating)
		{
			throw new UnityException("Missing cheating GameObject");
		}
		cheating.SetActive(value: false);
		if (!confettiFx)
		{
			throw new UnityException("Missing confettiFx GameObject");
		}
	}

	public override void OnNetworkSpawn()
	{
		base.OnNetworkSpawn();
		if (base.IsServer)
		{
			CoreController.WaitFor(delegate(IngameController ingameCtrl)
			{
				ingameCtrl.OnStatusUpdated += new Action<INGAME_STATUS, bool>(OnStatusUpdated);
			});
			CoreController.WaitFor(delegate(SettingsController settingsCtrl)
			{
				settingsCtrl.OnSettingsUpdated += new Action(OnSettingsUpdated);
				OnSettingsUpdated();
			});
			if (!NETController.Instance)
			{
				throw new UnityException("Missing NETController");
			}
			NETController.Instance.OnCheatsUpdate += new Action<bool>(OnCheatsUpdate);
			OnCheatsUpdate(NETController.SV_CHEATS);
		}
	}

	private void OnCheatsUpdate(bool set)
	{
		if (!base.IsServer)
		{
			throw new UnityException("Server only");
		}
		_isCheating.SetSpawnValue(set);
	}

	public override void OnNetworkDespawn()
	{
		base.OnNetworkDespawn();
		MonoController<LocalizationController>.Instance?.Cleanup("best-round-timer");
		if (base.IsServer)
		{
			if ((bool)NetController<IngameController>.Instance)
			{
				NetController<IngameController>.Instance.OnStatusUpdated -= new Action<INGAME_STATUS, bool>(OnStatusUpdated);
			}
			if ((bool)MonoController<SettingsController>.Instance)
			{
				MonoController<SettingsController>.Instance.OnSettingsUpdated -= new Action(OnSettingsUpdated);
			}
			if ((bool)NETController.Instance)
			{
				NETController.Instance.OnCheatsUpdate -= new Action<bool>(OnCheatsUpdate);
			}
		}
	}

	[Server]
	public void LoadSave(uint totalMs)
	{
		if (!base.IsServer)
		{
			throw new UnityException("Server only");
		}
		_totalMsAccumulator = totalMs;
		_totalTime.SetSpawnValue(totalMs / 1000);
	}

	[Server]
	public uint GetTotalTimeMs()
	{
		return _totalMsAccumulator;
	}

	public void Update()
	{
		if (!base.IsServer)
		{
			return;
		}
		uint num = (uint)(Time.deltaTime * 1000f);
		if (!_isMainTimerPaused.Value)
		{
			_totalMsAccumulator += num;
			uint num2 = _totalMsAccumulator / 1000;
			if (num2 != _totalTime.Value)
			{
				_totalTime.SetSpawnValue(num2);
			}
		}
		if (NetController<IngameController>.Instance.Status() == INGAME_STATUS.PLAYING)
		{
			_roundTime.SetSpawnValue(_roundTime.Value + num);
		}
	}

	protected override void OnNetworkPostSpawn()
	{
		base.OnNetworkPostSpawn();
		if (!base.IsClient)
		{
			return;
		}
		_totalTime.RegisterOnValueChanged(delegate(uint _, uint newValue)
		{
			if ((bool)totalTime)
			{
				totalTime.SetText(SplitFlapMode.INSTANT, TimeUtils.SecondsToTime(newValue));
			}
		});
		_roundTime.RegisterOnValueChanged(delegate(uint _, uint newValue)
		{
			if ((bool)roundTime)
			{
				roundTime.SetText(SplitFlapMode.INSTANT, TimeUtils.SecondsToMsTime(newValue));
			}
		});
		_roundBestTime.RegisterOnValueChanged(delegate(uint _, uint newValue)
		{
			if ((bool)bestTimeText && (bool)MonoController<LocalizationController>.Instance)
			{
				MonoController<LocalizationController>.Instance.Cleanup("best-round-timer");
				MonoController<LocalizationController>.Instance.Get("best-round-timer", "ingame.speedrun.best", delegate(string t)
				{
					if ((bool)bestTimeText)
					{
						bestTimeText.text = t;
					}
				}, new Dictionary<string, string> { 
				{
					"0",
					TimeUtils.SecondsToMsTime(newValue)
				} });
			}
		});
		_isMainTimerPaused.RegisterOnValueChanged(delegate(bool _, bool newValue)
		{
			if ((bool)pause)
			{
				pause.SetActive(newValue);
			}
		});
		_isCheating.RegisterOnValueChanged(delegate(bool _, bool newValue)
		{
			if ((bool)cheating)
			{
				cheating.SetActive(newValue);
			}
		});
	}

	public override void OnNetworkPreDespawn()
	{
		base.OnNetworkPreDespawn();
		if (base.IsClient)
		{
			_totalTime.OnValueChanged = null;
			_roundTime.OnValueChanged = null;
			_roundBestTime.OnValueChanged = null;
			_isCheating.OnValueChanged = null;
			_isMainTimerPaused.OnValueChanged = null;
		}
	}

	private void OnSettingsUpdated()
	{
		if ((bool)MonoController<SettingsController>.Instance)
		{
			LoadBestTime();
		}
	}

	[Server]
	private void LoadBestTime()
	{
		if (!base.IsServer)
		{
			throw new UnityException("Server only");
		}
		if (!MonoController<SettingsController>.Instance)
		{
			throw new UnityException("Missing SettingsController instance");
		}
		if (!NetController<SpeedrunController>.Instance)
		{
			throw new UnityException("Missing IngameController instance");
		}
		byte currentRound = NetController<IngameController>.Instance.GetCurrentRound();
		PlayerSettings currentSettings = MonoController<SettingsController>.Instance.CurrentSettings;
		if (currentSettings.bestTimes != null)
		{
			_roundBestTime.SetSpawnValue(currentSettings.bestTimes.AsValueEnumerable().ElementAtOrDefault(currentRound - 1));
		}
	}

	[Server]
	private void SaveBestTime()
	{
		if (!base.IsServer)
		{
			throw new UnityException("Server only");
		}
		if (NETController.SV_CHEATS)
		{
			return;
		}
		if (!MonoController<SettingsController>.Instance)
		{
			throw new UnityException("Missing SettingsController instance");
		}
		if (!NetController<SpeedrunController>.Instance)
		{
			throw new UnityException("Missing IngameController instance");
		}
		if (!NetController<CurrencyController>.Instance)
		{
			throw new UnityException("Missing CurrencyController instance");
		}
		if (NetController<CurrencyController>.Instance.GetDebt() > 0)
		{
			return;
		}
		uint value = _roundTime.Value;
		if (value != 0)
		{
			byte currentRound = NetController<IngameController>.Instance.GetCurrentRound();
			PlayerSettings currentSettings = MonoController<SettingsController>.Instance.CurrentSettings;
			uint num = currentSettings.bestTimes.AsValueEnumerable().ElementAtOrDefault(currentRound - 1);
			bool flag = num == 0 || value < num;
			BeatBestTimeRPC(flag);
			if (flag)
			{
				currentSettings.bestTimes[currentRound - 1] = value;
				MonoController<SettingsController>.Instance.CurrentSettings = currentSettings;
				_roundBestTime.SetSpawnValue(value);
			}
		}
	}

	[Rpc(SendTo.ClientsAndHost)]
	private void BeatBestTimeRPC(bool beat)
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
			FastBufferWriter bufferWriter = __beginSendRpc(327600051u, rpcParams, attributeParams, SendTo.ClientsAndHost, RpcDelivery.Reliable);
			bufferWriter.WriteValueSafe(in beat, default(FastBufferWriter.ForPrimitives));
			__endSendRpc(ref bufferWriter, 327600051u, rpcParams, attributeParams, SendTo.ClientsAndHost, RpcDelivery.Reliable);
		}
		if (__rpc_exec_stage != __RpcExecStage.Execute)
		{
			return;
		}
		__rpc_exec_stage = __RpcExecStage.Send;
		if ((bool)NetController<SoundController>.Instance && (bool)confettiFx)
		{
			if (beat)
			{
				confettiFx.Play();
			}
			NetController<SoundController>.Instance.Play3DSound(beat ? $"Ingame/Entities/Speedrun/new_best_{UnityEngine.Random.Range(0, 4)}.ogg" : $"Ingame/Notifications/UI_Error_Double_Tone_0{UnityEngine.Random.Range(1, 3)}_mono.ogg", pause.transform.position, new AudioData
			{
				distance = 8f,
				volume = 0.6f,
				pitch = (beat ? 1f : UnityEngine.Random.Range(0.8f, 1.1f))
			});
		}
	}

	private void OnStatusUpdated(INGAME_STATUS status, bool server)
	{
		if (server)
		{
			INGAME_STATUS iNGAME_STATUS = NetController<IngameController>.Instance.OldStatus();
			_isMainTimerPaused.SetSpawnValue((iNGAME_STATUS == INGAME_STATUS.IDLE && status == INGAME_STATUS.IDLE) || status == INGAME_STATUS.GENERATE || status == INGAME_STATUS.ROUND_END || status == INGAME_STATUS.GAMEOVER);
			switch (status)
			{
			case INGAME_STATUS.WAITING_PLAY_CONFIRMATION:
				_roundTime.SetSpawnValue(0u);
				LoadBestTime();
				break;
			case INGAME_STATUS.ROUND_END:
				SaveBestTime();
				break;
			}
		}
	}

	[Shared]
	public uint GetTotalTime()
	{
		return _totalTime.Value;
	}

	protected override void __initializeVariables()
	{
		if (_isMainTimerPaused == null)
		{
			throw new Exception("SpeedrunController._isMainTimerPaused cannot be null. All NetworkVariableBase instances must be initialized.");
		}
		_isMainTimerPaused.Initialize(this);
		__nameNetworkVariable(_isMainTimerPaused, "_isMainTimerPaused");
		NetworkVariableFields.Add(_isMainTimerPaused);
		if (_isCheating == null)
		{
			throw new Exception("SpeedrunController._isCheating cannot be null. All NetworkVariableBase instances must be initialized.");
		}
		_isCheating.Initialize(this);
		__nameNetworkVariable(_isCheating, "_isCheating");
		NetworkVariableFields.Add(_isCheating);
		if (_totalTime == null)
		{
			throw new Exception("SpeedrunController._totalTime cannot be null. All NetworkVariableBase instances must be initialized.");
		}
		_totalTime.Initialize(this);
		__nameNetworkVariable(_totalTime, "_totalTime");
		NetworkVariableFields.Add(_totalTime);
		if (_roundTime == null)
		{
			throw new Exception("SpeedrunController._roundTime cannot be null. All NetworkVariableBase instances must be initialized.");
		}
		_roundTime.Initialize(this);
		__nameNetworkVariable(_roundTime, "_roundTime");
		NetworkVariableFields.Add(_roundTime);
		if (_roundBestTime == null)
		{
			throw new Exception("SpeedrunController._roundBestTime cannot be null. All NetworkVariableBase instances must be initialized.");
		}
		_roundBestTime.Initialize(this);
		__nameNetworkVariable(_roundBestTime, "_roundBestTime");
		NetworkVariableFields.Add(_roundBestTime);
		base.__initializeVariables();
	}

	protected override void __initializeRpcs()
	{
		__registerRpc(327600051u, __rpc_handler_327600051, "BeatBestTimeRPC", RpcInvokePermission.Everyone);
		base.__initializeRpcs();
	}

	private static void __rpc_handler_327600051(NetworkBehaviour target, FastBufferReader reader, __RpcParams rpcParams)
	{
		NetworkManager networkManager = target.NetworkManager;
		if ((object)networkManager != null && networkManager.IsListening)
		{
			reader.ReadValueSafe(out bool value, default(FastBufferWriter.ForPrimitives));
			target.__rpc_exec_stage = __RpcExecStage.Execute;
			((SpeedrunController)target).BeatBestTimeRPC(value);
			target.__rpc_exec_stage = __RpcExecStage.Send;
		}
	}

	protected internal override string __getTypeName()
	{
		return "SpeedrunController";
	}
}
