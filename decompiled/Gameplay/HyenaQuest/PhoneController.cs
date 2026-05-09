using System;
using System.Collections.Generic;
using FailCake;
using TMPro;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

namespace HyenaQuest;

[DisallowMultipleComponent]
[DefaultExecutionOrder(-80)]
[RequireComponent(typeof(NetworkObject))]
public class PhoneController : NetController<PhoneController>
{
	public List<entity_button> phoneButtons;

	public TextMeshPro phoneDisplay;

	public List<AudioClip> phoneMusic;

	public AudioSource phoneAudioSource;

	public ui_conversation conversation;

	[Range(1f, 12f)]
	public int maxPhoneNumberLength = 12;

	public GameEvent<PHONE_STATUS, bool> OnStatusUpdated = new GameEvent<PHONE_STATUS, bool>();

	private util_timer _callTimer;

	private util_timer _resetTimer;

	private util_timer _autoTypeTimer;

	private entity_player _caller;

	private static readonly float CALL_TIME = 2f;

	private static readonly float FAST_CALL_TIME = 1f;

	private static readonly float PHONE_BUTTON_VOLUME = 0.25f;

	private static readonly float PHONE_ANNOUNCER_VOLUME = 0.25f;

	private static readonly float PHONE_RINGING_VOLUME = 0.1f;

	private static readonly float PHONE_MUSIC_VOLUME = 0.25f;

	private static readonly int MIN_CALL_RINGS = 1;

	private static readonly int MAX_CALL_RINGS = 3;

	private static readonly int MIN_RINGS_FOR_QUEUE = 10;

	private static readonly Dictionary<int, string> PHONE_INDEX = new Dictionary<int, string>
	{
		{ 0, "1" },
		{ 1, "2" },
		{ 2, "3" },
		{ 3, "4" },
		{ 4, "5" },
		{ 5, "6" },
		{ 6, "7" },
		{ 7, "8" },
		{ 8, "9" },
		{ 9, "CLEAR" },
		{ 10, "0" },
		{ 11, "CALL" }
	};

	private readonly Dictionary<string, Func<entity_player, List<string>>> _phoneRegistry = new Dictionary<string, Func<entity_player, List<string>>>
	{
		{
			"74627753 ",
			delegate
			{
				NetController<StatsController>.Instance.UnlockAchievementSV(STEAM_ACHIEVEMENTS.ACHIEVEMENT_FORBIDDEN_PIZZA, ulong.MaxValue);
				return new List<string> { "ingame.phone.pizza.intro", "ingame.phone.pizza.outro" };
			}
		},
		{
			"808",
			delegate(entity_player caller)
			{
				if ((bool)caller)
				{
					caller.ToggleBobs();
				}
				return new List<string> { "ingame.phone.code-accepted" };
			}
		},
		{
			"101",
			delegate(entity_player caller)
			{
				if ((bool)caller)
				{
					caller.ToggleSkirt();
				}
				return new List<string> { "ingame.phone.code-accepted" };
			}
		},
		{
			"404",
			delegate(entity_player caller)
			{
				if ((bool)caller)
				{
					caller.ToggleMustache();
				}
				return new List<string> { "ingame.phone.code-accepted" };
			}
		},
		{
			"112",
			(entity_player _) => new List<string> { "ingame.phone.health" }
		},
		{
			"911",
			(entity_player _) => new List<string> { "ingame.phone.health" }
		}
	};

	private bool _fastCallUpgrade;

	private readonly NetVar<FixedString64Bytes> _phoneNumber = new NetVar<FixedString64Bytes>();

	private readonly NetVar<PHONE_STATUS> _phoneStatus = new NetVar<PHONE_STATUS>(PHONE_STATUS.IDLE);

	public new void Awake()
	{
		base.Awake();
		List<entity_button> list = phoneButtons;
		if (list == null || list.Count != 12)
		{
			throw new UnityException("entity_phone requires 12 phone buttons");
		}
		if (!phoneDisplay)
		{
			throw new UnityException("entity_phone requires a phone display");
		}
		if (!phoneAudioSource)
		{
			throw new UnityException("entity_phone requires an AudioSource component");
		}
		if (!conversation)
		{
			throw new UnityException("entity_phone requires ui_conversation");
		}
	}

	public override void OnNetworkSpawn()
	{
		base.OnNetworkSpawn();
		if ((bool)conversation)
		{
			conversation.OnComplete += new Action(OnConversationComplete);
		}
		if (!base.IsServer)
		{
			return;
		}
		for (int i = 0; i < phoneButtons.Count; i++)
		{
			int i2 = i;
			phoneButtons[i].OnUSE += (Action<entity_player>)delegate(entity_player ply)
			{
				OnButtonPress(ply, PHONE_INDEX[i2]);
			};
		}
	}

	public override void OnNetworkDespawn()
	{
		base.OnNetworkDespawn();
		_resetTimer?.Stop();
		_callTimer?.Stop();
		_autoTypeTimer?.Stop();
		if ((bool)conversation)
		{
			conversation.OnComplete -= new Action(OnConversationComplete);
		}
		MonoController<LocalizationController>.Instance?.Cleanup("phone.status");
		if (base.IsClient)
		{
			conversation?.Clear();
			if ((bool)phoneAudioSource)
			{
				phoneAudioSource.Stop();
				phoneAudioSource.clip = null;
			}
		}
		if (!base.IsServer)
		{
			return;
		}
		foreach (entity_button phoneButton in phoneButtons)
		{
			if ((bool)phoneButton)
			{
				phoneButton.OnUSE = null;
			}
		}
	}

	public PHONE_STATUS Status()
	{
		return _phoneStatus.Value;
	}

	[Server]
	public bool AutoType(string number, float speed, float delay, Action onComplete)
	{
		if (!base.IsServer)
		{
			throw new UnityException("AutoTYPE called on client");
		}
		if (string.IsNullOrEmpty(number))
		{
			throw new UnityException("Phone number is empty");
		}
		if (number.Length > maxPhoneNumberLength)
		{
			throw new UnityException($"Phone number length goes beyond the max phone lenght '{maxPhoneNumberLength}'");
		}
		if (_phoneStatus.Value != 0)
		{
			return false;
		}
		SetButtonsLocked(locked: true);
		_phoneNumber.Value = "";
		_autoTypeTimer?.Stop();
		_autoTypeTimer = util_timer.Create(number.Length, speed, delegate(int tick)
		{
			char c = number[number.Length - 1 - tick];
			switch (c)
			{
			default:
				throw new UnityException("Phone number contains invalid characters");
			case ' ':
			case '0':
			case '1':
			case '2':
			case '3':
			case '4':
			case '5':
			case '6':
			case '7':
			case '8':
			case '9':
				OnButtonPress(null, c.ToString());
				break;
			}
		}, delegate
		{
			_autoTypeTimer = null;
			_autoTypeTimer = util_timer.Simple(delay, delegate
			{
				OnButtonPress(null, "CALL");
				_autoTypeTimer = util_timer.Simple(1f, delegate
				{
					onComplete?.Invoke();
				});
			});
		});
		return true;
	}

	[Server]
	public bool Register(string number, Func<entity_player, List<string>> onPhoneNumberCalled)
	{
		if (string.IsNullOrEmpty(number))
		{
			throw new UnityException("Phone number is empty");
		}
		return _phoneRegistry.TryAdd(number, onPhoneNumberCalled);
	}

	[Server]
	public bool Unregister(string number)
	{
		if (string.IsNullOrEmpty(number))
		{
			throw new UnityException("Phone number is empty");
		}
		return _phoneRegistry.Remove(number);
	}

	protected override void OnNetworkPostSpawn()
	{
		base.OnNetworkPostSpawn();
		if (!base.IsClient)
		{
			return;
		}
		_phoneNumber.RegisterOnValueChanged(delegate(FixedString64Bytes _, FixedString64Bytes newValue)
		{
			string text = newValue.ToString();
			if (text.StartsWith("ingame."))
			{
				MonoController<LocalizationController>.Instance.Get("phone.status", text, delegate(string v)
				{
					if ((bool)phoneDisplay)
					{
						phoneDisplay.text = "<align=center>" + v + "</align>";
					}
				});
			}
			else
			{
				phoneDisplay.text = newValue.ToString();
			}
		});
		_phoneStatus.RegisterOnValueChanged(delegate(PHONE_STATUS _, PHONE_STATUS newValue)
		{
			OnStatusUpdate(newValue, server: false);
		});
	}

	public override void OnNetworkPreDespawn()
	{
		base.OnNetworkPreDespawn();
		if (base.IsClient)
		{
			_phoneNumber.OnValueChanged = null;
			_phoneStatus.OnValueChanged = null;
		}
	}

	[Server]
	public void SetFastCallUpgrade(bool set)
	{
		if (!base.IsServer)
		{
			throw new UnityException("Server only");
		}
		_fastCallUpgrade = set;
	}

	private void OnConversationComplete()
	{
		if (base.IsServer)
		{
			if (_phoneStatus.Value != PHONE_STATUS.TALKING)
			{
				throw new UnityException("Invalid phone status");
			}
			SetStatus(PHONE_STATUS.IDLE);
		}
		NetController<SoundController>.Instance.Play3DSound("Ingame/Entities/Phone/hangup.ogg", phoneAudioSource.transform, new AudioData
		{
			distance = 4f,
			volume = 0.25f
		});
	}

	[Server]
	private void OnButtonPress(entity_player caller, string number)
	{
		if (_phoneStatus.Value != 0)
		{
			return;
		}
		AudioData data = new AudioData
		{
			distance = 4f,
			volume = PHONE_BUTTON_VOLUME
		};
		if (!(number == "CLEAR"))
		{
			if (number == "CALL")
			{
				if (_phoneNumber.Value.Length != 0)
				{
					_caller = caller;
					NetController<SoundController>.Instance.Play3DSound("Ingame/Entities/Phone/pound.ogg", phoneAudioSource.transform, data, broadcast: true);
					CallNumber(_phoneNumber.Value.ToString());
				}
			}
			else if (_phoneNumber.Value.Length < maxPhoneNumberLength)
			{
				NetVar<FixedString64Bytes> phoneNumber = _phoneNumber;
				phoneNumber.Value = phoneNumber.Value.ToString() + number;
				NetController<SoundController>.Instance.Play3DSound("Ingame/Entities/Phone/" + ((number == " ") ? "star" : number) + ".ogg", phoneAudioSource.transform, data, broadcast: true);
			}
		}
		else
		{
			_phoneNumber.Value = "";
			_caller = null;
			NetController<SoundController>.Instance.Play3DSound("Ingame/Entities/Phone/star.ogg", phoneAudioSource.transform, data, broadcast: true);
		}
	}

	[Server]
	private void CallNumber(string number)
	{
		if (_phoneStatus.Value != 0)
		{
			return;
		}
		SetStatus(PHONE_STATUS.CALLING);
		_resetTimer?.Stop();
		_callTimer?.Stop();
		NetController<SoundController>.Instance.Play3DSound("Ingame/Entities/Phone/ringing-tone.ogg", phoneAudioSource.transform, new AudioData
		{
			pitch = 0.8f,
			distance = 4f,
			volume = PHONE_RINGING_VOLUME
		}, broadcast: true);
		int times = (_fastCallUpgrade ? 1 : UnityEngine.Random.Range(MIN_CALL_RINGS, MAX_CALL_RINGS));
		_callTimer = util_timer.Create(times, _fastCallUpgrade ? FAST_CALL_TIME : CALL_TIME, delegate(int tick)
		{
			if (tick == times - 1 && times >= MIN_RINGS_FOR_QUEUE)
			{
				NetController<SoundController>.Instance.Play3DSound($"Ingame/Entities/Phone/ring-queue-{UnityEngine.Random.Range(0, 4)}.ogg", phoneAudioSource.transform, new AudioData
				{
					distance = 4f,
					volume = PHONE_ANNOUNCER_VOLUME
				}, broadcast: true);
			}
			if (tick != 0)
			{
				NetController<SoundController>.Instance.Play3DSound("Ingame/Entities/Phone/ringing-tone.ogg", phoneAudioSource.transform, new AudioData
				{
					distance = 4f,
					pitch = 0.8f,
					volume = PHONE_RINGING_VOLUME
				}, broadcast: true);
			}
		}, delegate
		{
			if (_phoneRegistry.TryGetValue(number, out var value))
			{
				List<string> list = value(_caller);
				if (list != null && list.Count > 0)
				{
					ChatRPC(new NetworkStrings(list));
					NetController<SoundController>.Instance.Play3DSound($"Ingame/Entities/Phone/pickup-{UnityEngine.Random.Range(0, 2)}.ogg", phoneAudioSource.transform, new AudioData
					{
						distance = 4f,
						volume = 0.25f
					}, broadcast: true);
					SetStatus(PHONE_STATUS.TALKING);
				}
				else
				{
					SetStatus(PHONE_STATUS.SPECIAL_MODE);
				}
			}
			else
			{
				SetStatus(PHONE_STATUS.INVALID_NUMBER);
			}
		});
	}

	[Rpc(SendTo.ClientsAndHost)]
	private void ChatRPC(NetworkStrings messages)
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
			FastBufferWriter bufferWriter = __beginSendRpc(1744769105u, rpcParams, attributeParams, SendTo.ClientsAndHost, RpcDelivery.Reliable);
			bufferWriter.WriteValueSafe(in messages, default(FastBufferWriter.ForNetworkSerializable));
			__endSendRpc(ref bufferWriter, 1744769105u, rpcParams, attributeParams, SendTo.ClientsAndHost, RpcDelivery.Reliable);
		}
		if (__rpc_exec_stage != __RpcExecStage.Execute)
		{
			return;
		}
		__rpc_exec_stage = __RpcExecStage.Send;
		if (!conversation)
		{
			throw new UnityException("Missing ui_conversation");
		}
		conversation.Clear();
		FixedString512Bytes[] data = messages.data;
		for (int i = 0; i < data.Length; i++)
		{
			FixedString512Bytes fixedString512Bytes = data[i];
			string text = fixedString512Bytes.ToString();
			if (text.StartsWith("ingame."))
			{
				if (!MonoController<LocalizationController>.Instance)
				{
					throw new UnityException("Missing LocalizationController");
				}
				text = MonoController<LocalizationController>.Instance.Get(text);
				if (text.Contains("<##>"))
				{
					string[] array = text.Split(new string[1] { "<##>" }, StringSplitOptions.None);
					text = array[UnityEngine.Random.Range(0, array.Length)];
				}
			}
			conversation.QueueConversation(new Conversation(text, 0.6f, 0.7f, phoneAudioSource.transform.position));
		}
	}

	[Server]
	private void SetStatus(PHONE_STATUS status)
	{
		if (_phoneStatus.Value != status)
		{
			_phoneStatus.Value = status;
			OnStatusUpdate(status, server: true);
		}
	}

	[Server]
	private void SetButtonsLocked(bool locked)
	{
		if (!base.IsServer)
		{
			throw new UnityException("SetButtonsLocked called on client");
		}
		foreach (entity_button phoneButton in phoneButtons)
		{
			phoneButton.SetLocked(locked);
		}
	}

	private void OnStatusUpdate(PHONE_STATUS status, bool server)
	{
		switch (status)
		{
		case PHONE_STATUS.IDLE:
			if (server)
			{
				_phoneNumber.Value = "";
				_caller = null;
			}
			break;
		case PHONE_STATUS.CALLING:
			if (server)
			{
				_phoneNumber.Value = "ingame.phone.status.connecting";
				break;
			}
			phoneAudioSource.clip = phoneMusic[UnityEngine.Random.Range(0, phoneMusic.Count)];
			phoneAudioSource.volume = PHONE_MUSIC_VOLUME;
			break;
		case PHONE_STATUS.SPECIAL_MODE:
			if (server)
			{
				_phoneNumber.Value = "ingame.phone.status.command";
			}
			break;
		case PHONE_STATUS.TALKING:
			if (server)
			{
				_phoneNumber.Value = "ingame.phone.status.support";
			}
			break;
		case PHONE_STATUS.INVALID_NUMBER:
			if (server)
			{
				_phoneNumber.Value = "ingame.phone.status.invalid";
				_resetTimer?.Stop();
				_resetTimer = util_timer.Simple(2f, delegate
				{
					SetStatus(PHONE_STATUS.IDLE);
				});
			}
			else
			{
				NetController<SoundController>.Instance.Play3DSound($"Ingame/Entities/Phone/invalid-{UnityEngine.Random.Range(0, 2)}.ogg", phoneAudioSource.transform, new AudioData
				{
					distance = 4f,
					volume = PHONE_ANNOUNCER_VOLUME
				});
			}
			break;
		}
		if (server)
		{
			SetButtonsLocked(status != PHONE_STATUS.IDLE);
		}
		else if (status != PHONE_STATUS.CALLING)
		{
			phoneAudioSource.Stop();
		}
		else
		{
			phoneAudioSource.Play();
		}
		OnStatusUpdated?.Invoke(status, server);
	}

	protected override void __initializeVariables()
	{
		if (_phoneNumber == null)
		{
			throw new Exception("PhoneController._phoneNumber cannot be null. All NetworkVariableBase instances must be initialized.");
		}
		_phoneNumber.Initialize(this);
		__nameNetworkVariable(_phoneNumber, "_phoneNumber");
		NetworkVariableFields.Add(_phoneNumber);
		if (_phoneStatus == null)
		{
			throw new Exception("PhoneController._phoneStatus cannot be null. All NetworkVariableBase instances must be initialized.");
		}
		_phoneStatus.Initialize(this);
		__nameNetworkVariable(_phoneStatus, "_phoneStatus");
		NetworkVariableFields.Add(_phoneStatus);
		base.__initializeVariables();
	}

	protected override void __initializeRpcs()
	{
		__registerRpc(1744769105u, __rpc_handler_1744769105, "ChatRPC", RpcInvokePermission.Everyone);
		base.__initializeRpcs();
	}

	private static void __rpc_handler_1744769105(NetworkBehaviour target, FastBufferReader reader, __RpcParams rpcParams)
	{
		NetworkManager networkManager = target.NetworkManager;
		if ((object)networkManager != null && networkManager.IsListening)
		{
			reader.ReadValueSafe(out NetworkStrings value, default(FastBufferWriter.ForNetworkSerializable));
			target.__rpc_exec_stage = __RpcExecStage.Execute;
			((PhoneController)target).ChatRPC(value);
			target.__rpc_exec_stage = __RpcExecStage.Send;
		}
	}

	protected internal override string __getTypeName()
	{
		return "PhoneController";
	}
}
