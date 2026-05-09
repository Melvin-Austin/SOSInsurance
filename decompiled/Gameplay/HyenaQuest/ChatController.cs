using System.Collections.Generic;
using System.Text.RegularExpressions;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;
using ZLinq;

namespace HyenaQuest;

[DefaultExecutionOrder(-70)]
[DisallowMultipleComponent]
[RequireComponent(typeof(NetworkObject))]
public class ChatController : NetController<ChatController>
{
	private static readonly int MAX_CHAT_HISTORY = 4;

	public InputActionReference openChat;

	public GameObject chatWindow;

	public GameObject chatHistory;

	public GameObject chatPrefab;

	private readonly List<ui_player_chat> _chatMessages = new List<ui_player_chat>();

	private int _currentChatIndex;

	private TMP_InputField _inputField;

	private bool _isChatOpen;

	public new void Awake()
	{
		base.Awake();
		if (!chatWindow)
		{
			throw new UnityException("Chat Window is missing!");
		}
		if (!chatPrefab)
		{
			throw new UnityException("Chat Prefab is missing!");
		}
		if (!chatHistory)
		{
			throw new UnityException("Chat History is missing!");
		}
		chatWindow.SetActive(value: false);
		_inputField = chatWindow.GetComponentInChildren<TMP_InputField>(includeInactive: true);
		if (!_inputField)
		{
			throw new UnityException("Input Field is missing!");
		}
	}

	public override void OnNetworkSpawn()
	{
		base.OnNetworkSpawn();
		if (base.IsClient)
		{
			openChat.action.performed += OnOpenChat;
			_inputField.onSubmit.AddListener(OnSubmit);
		}
	}

	public override void OnNetworkDespawn()
	{
		base.OnNetworkDespawn();
		if (base.IsClient)
		{
			CloseChat();
			if ((bool)openChat)
			{
				openChat.action.performed -= OnOpenChat;
			}
			if ((bool)_inputField)
			{
				_inputField.onSubmit.RemoveAllListeners();
			}
		}
	}

	public void CloseChat()
	{
		if (!chatWindow || !IsChatOpen())
		{
			return;
		}
		MonoController<StartupController>.Instance?.ReleaseCursor("CHAT");
		_inputField?.SetTextWithoutNotify("");
		_inputField?.DeactivateInputField();
		chatWindow.SetActive(value: false);
		_isChatOpen = false;
		foreach (ui_player_chat chatMessage in _chatMessages)
		{
			if ((bool)chatMessage)
			{
				if (chatMessage.IsFresh())
				{
					chatMessage.FadeOut();
				}
				else
				{
					chatMessage.Hide();
				}
			}
		}
	}

	public void OpenChat()
	{
		if (!chatWindow || IsChatOpen())
		{
			return;
		}
		MonoController<StartupController>.Instance?.RequestCursor("CHAT");
		chatWindow.SetActive(value: true);
		_inputField.ActivateInputField();
		_isChatOpen = true;
		foreach (ui_player_chat chatMessage in _chatMessages)
		{
			chatMessage?.Show();
		}
	}

	public bool IsChatOpen()
	{
		return _isChatOpen;
	}

	private void OnOpenChat(InputAction.CallbackContext obj)
	{
		OpenChat();
	}

	private string FogText(string text)
	{
		string fogPalette = "░▒";
		if (!string.IsNullOrEmpty(text))
		{
			return new string(text.AsValueEnumerable().Select(delegate(char c)
			{
				if (c == ' ')
				{
					return ' ';
				}
				return (!(Random.value < 0.7f)) ? c : fogPalette[Random.Range(0, fogPalette.Length)];
			}).ToArray());
		}
		return text;
	}

	private void OnSubmit(string str)
	{
		if (!string.IsNullOrEmpty(_inputField.text))
		{
			ChatServerRPC(_inputField.text, base.RpcTarget.Server);
		}
		CloseChat();
	}

	[Rpc(SendTo.SpecifiedInParams, InvokePermission = RpcInvokePermission.Everyone)]
	private void ChatServerRPC(string text, RpcParams target)
	{
		NetworkManager networkManager = base.NetworkManager;
		if ((object)networkManager == null || !networkManager.IsListening)
		{
			Debug.LogError("Rpc methods can only be invoked after starting the NetworkManager!");
			return;
		}
		if (__rpc_exec_stage != __RpcExecStage.Execute)
		{
			RpcParams rpcParams = target;
			RpcAttribute.RpcAttributeParams attributeParams = new RpcAttribute.RpcAttributeParams
			{
				InvokePermission = RpcInvokePermission.Everyone
			};
			FastBufferWriter bufferWriter = __beginSendRpc(2515253322u, rpcParams, attributeParams, SendTo.SpecifiedInParams, RpcDelivery.Reliable);
			bool value = text != null;
			bufferWriter.WriteValueSafe(in value, default(FastBufferWriter.ForPrimitives));
			if (value)
			{
				bufferWriter.WriteValueSafe(text);
			}
			__endSendRpc(ref bufferWriter, 2515253322u, target, attributeParams, SendTo.SpecifiedInParams, RpcDelivery.Reliable);
		}
		if (__rpc_exec_stage != __RpcExecStage.Execute)
		{
			return;
		}
		__rpc_exec_stage = __RpcExecStage.Send;
		Player playerByConnection = MonoController<PlayerController>.Instance.GetPlayerByConnection(target.Receive.SenderClientId);
		if (playerByConnection == null || !playerByConnection.player)
		{
			return;
		}
		string playerName = playerByConnection.player.GetPlayerName();
		if (string.IsNullOrEmpty(playerName))
		{
			return;
		}
		string text2 = ((playerByConnection.player.HasMask() && !playerByConnection.player.IsDead()) ? FogText(text) : Regex.Replace(text, "<\\/?[a-zA-Z][^>]*>", ""));
		List<entity_player> allPlayers = MonoController<PlayerController>.Instance.GetAllPlayers();
		if (!playerByConnection.player.IsDead())
		{
			foreach (entity_player item in allPlayers)
			{
				if ((bool)item)
				{
					ChatRPC("<size=80%><color=#d19900><b>" + playerName + "</b></color></size>: " + text2, base.RpcTarget.Single(item.GetConnectionID(), RpcTargetUse.Temp));
				}
			}
		}
		else
		{
			ChatRPC("<size=80%><color=#d19900><b>" + playerName + "</b></color></size>: " + text2, base.RpcTarget.ClientsAndHost);
		}
		if (!playerByConnection.player.IsDead() && !playerByConnection.player.HasMask())
		{
			if (playerByConnection.player.Species == 1)
			{
				NetController<SoundController>.Instance.Play3DSound($"Ingame/Player/Taunts/RAT/question_{Random.Range(1, 4)}.ogg", playerByConnection.player.GetHeadPosition(), new AudioData
				{
					pitch = Random.Range(0.8f, 1.2f),
					volume = 0.4f,
					distance = 3f
				}, broadcast: true);
			}
			else
			{
				NetController<SoundController>.Instance.Play3DSound($"Ingame/Player/Chat/chat_{Random.Range(0, 5)}.ogg", playerByConnection.player.GetHeadPosition(), new AudioData
				{
					pitch = Random.Range(0.8f, 1.2f),
					volume = 0.7f,
					distance = 3f
				}, broadcast: true);
			}
		}
	}

	[Rpc(SendTo.SpecifiedInParams, InvokePermission = RpcInvokePermission.Server)]
	private void ChatRPC(string text, RpcParams target)
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
				InvokePermission = RpcInvokePermission.Server
			};
			FastBufferWriter bufferWriter = __beginSendRpc(4272609595u, target, attributeParams, SendTo.SpecifiedInParams, RpcDelivery.Reliable);
			bool value = text != null;
			bufferWriter.WriteValueSafe(in value, default(FastBufferWriter.ForPrimitives));
			if (value)
			{
				bufferWriter.WriteValueSafe(text);
			}
			__endSendRpc(ref bufferWriter, 4272609595u, target, attributeParams, SendTo.SpecifiedInParams, RpcDelivery.Reliable);
		}
		if (__rpc_exec_stage != __RpcExecStage.Execute)
		{
			return;
		}
		__rpc_exec_stage = __RpcExecStage.Send;
		if (string.IsNullOrEmpty(text))
		{
			return;
		}
		ui_player_chat ui_player_chat2;
		if (_chatMessages.Count < MAX_CHAT_HISTORY)
		{
			GameObject obj = Object.Instantiate(chatPrefab, chatHistory.transform);
			if (!obj)
			{
				throw new UnityException("Failed to create chat instance");
			}
			ui_player_chat2 = obj.GetComponent<ui_player_chat>();
			if (!ui_player_chat2)
			{
				throw new UnityException("Missing ui_player_chat component");
			}
			_chatMessages.Add(ui_player_chat2);
		}
		else
		{
			ui_player_chat2 = _chatMessages[_currentChatIndex];
			_currentChatIndex = (_currentChatIndex + 1) % MAX_CHAT_HISTORY;
			ui_player_chat2.transform.SetAsLastSibling();
		}
		ui_player_chat2.SetText(text);
		if (_isChatOpen)
		{
			ui_player_chat2.FadeOut();
			ui_player_chat2.Show();
		}
		else
		{
			ui_player_chat2.FadeOut();
		}
	}

	protected override void __initializeVariables()
	{
		base.__initializeVariables();
	}

	protected override void __initializeRpcs()
	{
		__registerRpc(2515253322u, __rpc_handler_2515253322, "ChatServerRPC", RpcInvokePermission.Everyone);
		__registerRpc(4272609595u, __rpc_handler_4272609595, "ChatRPC", RpcInvokePermission.Server);
		base.__initializeRpcs();
	}

	private static void __rpc_handler_2515253322(NetworkBehaviour target, FastBufferReader reader, __RpcParams rpcParams)
	{
		NetworkManager networkManager = target.NetworkManager;
		if ((object)networkManager != null && networkManager.IsListening)
		{
			reader.ReadValueSafe(out bool value, default(FastBufferWriter.ForPrimitives));
			string s = null;
			if (value)
			{
				reader.ReadValueSafe(out s, oneByteChars: false);
			}
			RpcParams ext = rpcParams.Ext;
			target.__rpc_exec_stage = __RpcExecStage.Execute;
			((ChatController)target).ChatServerRPC(s, ext);
			target.__rpc_exec_stage = __RpcExecStage.Send;
		}
	}

	private static void __rpc_handler_4272609595(NetworkBehaviour target, FastBufferReader reader, __RpcParams rpcParams)
	{
		NetworkManager networkManager = target.NetworkManager;
		if ((object)networkManager != null && networkManager.IsListening)
		{
			reader.ReadValueSafe(out bool value, default(FastBufferWriter.ForPrimitives));
			string s = null;
			if (value)
			{
				reader.ReadValueSafe(out s, oneByteChars: false);
			}
			RpcParams ext = rpcParams.Ext;
			target.__rpc_exec_stage = __RpcExecStage.Execute;
			((ChatController)target).ChatRPC(s, ext);
			target.__rpc_exec_stage = __RpcExecStage.Send;
		}
	}

	protected internal override string __getTypeName()
	{
		return "ChatController";
	}
}
