using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Unity.Netcode;
using UnityEngine;
using ZLinq;

namespace HyenaQuest;

[DefaultExecutionOrder(-80)]
[DisallowMultipleComponent]
[RequireComponent(typeof(NetworkObject))]
public class NotificationController : NetController<NotificationController>
{
	public Transform NotificationParent;

	public GameObject NotificationPrefab;

	public GameObject Notification3DPrefab;

	public GameObject NotificationDetailedPrefab;

	private readonly Dictionary<string, ui_notification_base> _notifications = new Dictionary<string, ui_notification_base>();

	public new void Awake()
	{
		base.Awake();
		if (!NotificationParent)
		{
			throw new UnityException("Notification parent is null");
		}
		if (!NotificationPrefab)
		{
			throw new UnityException("Notification prefab is null");
		}
		if (!NotificationDetailedPrefab)
		{
			throw new UnityException("Notification detailed prefab is null");
		}
		if (!Notification3DPrefab)
		{
			throw new UnityException("3D Notification prefab is null");
		}
	}

	public void BroadcastRPC(NotificationData data, ulong connectionId)
	{
		if (string.IsNullOrEmpty(data.id.ToString()) || string.IsNullOrEmpty(data.text.ToString()))
		{
			throw new UnityException("Invalid ID or Text! Cannot be null or empty");
		}
		BroadcastRPC(data, base.RpcTarget.Single(connectionId, RpcTargetUse.Temp));
	}

	public void BroadcastRemoveRPC(string id, ulong connectionId)
	{
		if (string.IsNullOrEmpty(id))
		{
			throw new UnityException("Invalid id, cannot be empty");
		}
		BroadcastRemoveRPC(id, base.RpcTarget.Single(connectionId, RpcTargetUse.Temp));
	}

	[Rpc(SendTo.SpecifiedInParams)]
	public void BroadcastRemoveRPC(string id, RpcParams rpcParams)
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
			FastBufferWriter bufferWriter = __beginSendRpc(829637062u, rpcParams, attributeParams, SendTo.SpecifiedInParams, RpcDelivery.Reliable);
			bool value = id != null;
			bufferWriter.WriteValueSafe(in value, default(FastBufferWriter.ForPrimitives));
			if (value)
			{
				bufferWriter.WriteValueSafe(id);
			}
			__endSendRpc(ref bufferWriter, 829637062u, rpcParams, attributeParams, SendTo.SpecifiedInParams, RpcDelivery.Reliable);
		}
		if (__rpc_exec_stage == __RpcExecStage.Execute)
		{
			__rpc_exec_stage = __RpcExecStage.Send;
			if (string.IsNullOrEmpty(id))
			{
				throw new UnityException("Invalid id, cannot be empty");
			}
			RemoveNotification(id);
		}
	}

	[Rpc(SendTo.SpecifiedInParams)]
	public void BroadcastRPC(NotificationData data, RpcParams rpcParams)
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
			FastBufferWriter bufferWriter = __beginSendRpc(1935839193u, rpcParams, attributeParams, SendTo.SpecifiedInParams, RpcDelivery.Reliable);
			bool value = (object)data != null;
			bufferWriter.WriteValueSafe(in value, default(FastBufferWriter.ForPrimitives));
			if (value)
			{
				bufferWriter.WriteValueSafe(in data, default(FastBufferWriter.ForNetworkSerializable));
			}
			__endSendRpc(ref bufferWriter, 1935839193u, rpcParams, attributeParams, SendTo.SpecifiedInParams, RpcDelivery.Reliable);
		}
		if (__rpc_exec_stage == __RpcExecStage.Execute)
		{
			__rpc_exec_stage = __RpcExecStage.Send;
			if (string.IsNullOrEmpty(data.id.ToString()) || string.IsNullOrEmpty(data.text.ToString()))
			{
				throw new UnityException("Invalid ID or Text! Cannot be null or empty");
			}
			CreateNotification(data);
		}
	}

	[Rpc(SendTo.ClientsAndHost)]
	public void BroadcastAllRPC(NotificationData data)
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
			FastBufferWriter bufferWriter = __beginSendRpc(3589787394u, rpcParams, attributeParams, SendTo.ClientsAndHost, RpcDelivery.Reliable);
			bool value = (object)data != null;
			bufferWriter.WriteValueSafe(in value, default(FastBufferWriter.ForPrimitives));
			if (value)
			{
				bufferWriter.WriteValueSafe(in data, default(FastBufferWriter.ForNetworkSerializable));
			}
			__endSendRpc(ref bufferWriter, 3589787394u, rpcParams, attributeParams, SendTo.ClientsAndHost, RpcDelivery.Reliable);
		}
		if (__rpc_exec_stage == __RpcExecStage.Execute)
		{
			__rpc_exec_stage = __RpcExecStage.Send;
			if (string.IsNullOrEmpty(data.id.ToString()) || string.IsNullOrEmpty(data.text.ToString()))
			{
				throw new UnityException("Invalid ID or Text! Cannot be null or empty");
			}
			CreateNotification(data);
		}
	}

	[Rpc(SendTo.ClientsAndHost)]
	public void BroadcastRemoveAllRPC(string id)
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
			FastBufferWriter bufferWriter = __beginSendRpc(2220586703u, rpcParams, attributeParams, SendTo.ClientsAndHost, RpcDelivery.Reliable);
			bool value = id != null;
			bufferWriter.WriteValueSafe(in value, default(FastBufferWriter.ForPrimitives));
			if (value)
			{
				bufferWriter.WriteValueSafe(id);
			}
			__endSendRpc(ref bufferWriter, 2220586703u, rpcParams, attributeParams, SendTo.ClientsAndHost, RpcDelivery.Reliable);
		}
		if (__rpc_exec_stage == __RpcExecStage.Execute)
		{
			__rpc_exec_stage = __RpcExecStage.Send;
			if (string.IsNullOrEmpty(id))
			{
				throw new UnityException("Invalid id, cannot be empty");
			}
			RemoveNotification(id);
		}
	}

	[Client]
	public void CreateNotification(NotificationData data)
	{
		if (!MonoController<LocalizationController>.Instance)
		{
			throw new UnityException("Missing LocalizationController");
		}
		if (string.IsNullOrEmpty(data.id.ToString()))
		{
			throw new UnityException("Notification ID is null or empty");
		}
		if (string.IsNullOrEmpty(data.text.ToString()))
		{
			throw new UnityException("Notification text is null or empty");
		}
		string text = data.text.ToString();
		string[] array = text.Split(new string[1] { "%##%" }, StringSplitOptions.None);
		text = array[0];
		if (text.StartsWith("ingame.") || text.StartsWith("general."))
		{
			text = MonoController<LocalizationController>.Instance.Get(text);
			if (text.Contains("<##>"))
			{
				string[] array2 = text.Split(new string[1] { "<##>" }, StringSplitOptions.None);
				text = array2[UnityEngine.Random.Range(0, array2.Length)];
			}
		}
		if (array.Length > 1)
		{
			for (int i = 1; i < array.Length; i++)
			{
				text += array[i];
			}
		}
		text = new Regex("\\$\\[([^\\]]+)\\]\\$").Replace(text, (Match match) => "<b>[" + MonoController<LocalizationController>.Instance.GetKeybindingText(match.Groups[1].Value) + "]</b>");
		if (_notifications.TryGetValue(data.id.ToString(), out var value))
		{
			if (value is ui_notification ui_notification2)
			{
				ui_notification2.SetText(text, data.duration);
			}
			return;
		}
		bool num = data.detailedIndex != byte.MaxValue;
		GameObject gameObject = UnityEngine.Object.Instantiate(num ? NotificationDetailedPrefab : NotificationPrefab, NotificationParent);
		if (!gameObject)
		{
			throw new UnityException("Failed to instantiate notification prefab");
		}
		ui_notification ui_notification3 = null;
		if (num)
		{
			ui_notification_detailed component = gameObject.GetComponent<ui_notification_detailed>();
			if (!component)
			{
				throw new UnityException("Missing ui_notification_slot component");
			}
			GameObject obj = NetController<DeliveryController>.Instance.propPrefabs[data.detailedIndex];
			if (!obj)
			{
				throw new UnityException($"Failed to get prop {data.detailedIndex}");
			}
			entity_prop_delivery component2 = obj.GetComponent<entity_prop_delivery>();
			if (!component2)
			{
				throw new UnityException("Failed to get delivery component");
			}
			MeshRenderer meshRenderer = component2.notificationPreview.GetComponent<MeshRenderer>();
			if (!meshRenderer)
			{
				meshRenderer = component2.notificationPreview.GetComponentInChildren<MeshRenderer>(includeInactive: true);
			}
			if (!meshRenderer)
			{
				throw new UnityException("Failed to get notification preview component");
			}
			MeshFilter meshFilter = component2.notificationPreview.GetComponent<MeshFilter>();
			if (!meshFilter)
			{
				meshFilter = component2.notificationPreview.GetComponentInChildren<MeshFilter>(includeInactive: true);
			}
			if (!meshFilter)
			{
				throw new UnityException("Failed to get notification preview component");
			}
			component.SetText(meshRenderer, meshFilter, text, data.duration);
			ui_notification3 = component;
		}
		else
		{
			ui_notification3 = gameObject.GetComponent<ui_notification>();
			if (!ui_notification3)
			{
				throw new UnityException("Missing ui_notification_slot component");
			}
			ui_notification3.SetText(text, data.duration);
		}
		if (!ui_notification3)
		{
			throw new UnityException("Failed to initialize notification");
		}
		if (!string.IsNullOrEmpty(data.soundEffect.ToString()))
		{
			NetController<SoundController>.Instance.PlaySound(data.soundEffect.ToString(), new AudioData
			{
				volume = data.soundVolume,
				pitch = data.soundPitch
			});
		}
		ui_notification3.SetID(data.id.ToString());
		ui_notification3.OnDestroyNotification += (Action)delegate
		{
			_notifications.Remove(data.id.ToString());
		};
		_notifications[data.id.ToString()] = ui_notification3;
	}

	[Client]
	public void RemoveNotification(string id)
	{
		if (string.IsNullOrEmpty(id))
		{
			throw new UnityException("Notification ID is null or empty");
		}
		if (_notifications.TryGetValue(id, out var value))
		{
			value.Destroy();
		}
	}

	[Client]
	public void ClearNotifications()
	{
		foreach (ui_notification_base item in _notifications.Values.AsValueEnumerable().ToList())
		{
			if ((bool)item)
			{
				item.Destroy();
			}
		}
		_notifications.Clear();
	}

	[Rpc(SendTo.SpecifiedInParams)]
	public void Broadcast3DRPC(NotificationData3D data, RpcParams rpcParams)
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
			FastBufferWriter bufferWriter = __beginSendRpc(3175140379u, rpcParams, attributeParams, SendTo.SpecifiedInParams, RpcDelivery.Reliable);
			bool value = (object)data != null;
			bufferWriter.WriteValueSafe(in value, default(FastBufferWriter.ForPrimitives));
			if (value)
			{
				bufferWriter.WriteValueSafe(in data, default(FastBufferWriter.ForNetworkSerializable));
			}
			__endSendRpc(ref bufferWriter, 3175140379u, rpcParams, attributeParams, SendTo.SpecifiedInParams, RpcDelivery.Reliable);
		}
		if (__rpc_exec_stage == __RpcExecStage.Execute)
		{
			__rpc_exec_stage = __RpcExecStage.Send;
			if (string.IsNullOrEmpty(data.message.ToString()))
			{
				throw new UnityException("Notification message is null or empty");
			}
			Create3DNotification(data);
		}
	}

	[Rpc(SendTo.ClientsAndHost)]
	public void BroadcastAll3DRPC(NotificationData3D data)
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
			FastBufferWriter bufferWriter = __beginSendRpc(3648903951u, rpcParams, attributeParams, SendTo.ClientsAndHost, RpcDelivery.Reliable);
			bool value = (object)data != null;
			bufferWriter.WriteValueSafe(in value, default(FastBufferWriter.ForPrimitives));
			if (value)
			{
				bufferWriter.WriteValueSafe(in data, default(FastBufferWriter.ForNetworkSerializable));
			}
			__endSendRpc(ref bufferWriter, 3648903951u, rpcParams, attributeParams, SendTo.ClientsAndHost, RpcDelivery.Reliable);
		}
		if (__rpc_exec_stage == __RpcExecStage.Execute)
		{
			__rpc_exec_stage = __RpcExecStage.Send;
			if (string.IsNullOrEmpty(data.message.ToString()))
			{
				throw new UnityException("Notification message is null or empty");
			}
			Create3DNotification(data);
		}
	}

	[Client]
	public void Create3DNotification(NotificationData3D data)
	{
		if (!MonoController<LocalizationController>.Instance)
		{
			throw new UnityException("Missing LocalizationController");
		}
		if (string.IsNullOrEmpty(data.message.ToString()))
		{
			throw new UnityException("Notification message is null or empty");
		}
		GameObject obj = UnityEngine.Object.Instantiate(Notification3DPrefab);
		if (!obj)
		{
			throw new UnityException("Failed to instantiate 3D notification prefab");
		}
		obj.transform.position = data.position;
		ui_notification_3d component = obj.GetComponent<ui_notification_3d>();
		if (!component)
		{
			throw new UnityException("Missing ui_notification_3d component");
		}
		string text = data.message.ToString();
		if (text.StartsWith("ingame.") || text.StartsWith("general."))
		{
			text = MonoController<LocalizationController>.Instance.Get(text);
			if (text.Contains("<##>"))
			{
				string[] array = text.Split(new string[1] { "<##>" }, StringSplitOptions.None);
				text = array[UnityEngine.Random.Range(0, array.Length)];
			}
		}
		component.SetText(text, data.fadeSpeed, data.scale, data.startColor, data.endColor);
	}

	protected override void __initializeVariables()
	{
		base.__initializeVariables();
	}

	protected override void __initializeRpcs()
	{
		__registerRpc(829637062u, __rpc_handler_829637062, "BroadcastRemoveRPC", RpcInvokePermission.Everyone);
		__registerRpc(1935839193u, __rpc_handler_1935839193, "BroadcastRPC", RpcInvokePermission.Everyone);
		__registerRpc(3589787394u, __rpc_handler_3589787394, "BroadcastAllRPC", RpcInvokePermission.Everyone);
		__registerRpc(2220586703u, __rpc_handler_2220586703, "BroadcastRemoveAllRPC", RpcInvokePermission.Everyone);
		__registerRpc(3175140379u, __rpc_handler_3175140379, "Broadcast3DRPC", RpcInvokePermission.Everyone);
		__registerRpc(3648903951u, __rpc_handler_3648903951, "BroadcastAll3DRPC", RpcInvokePermission.Everyone);
		base.__initializeRpcs();
	}

	private static void __rpc_handler_829637062(NetworkBehaviour target, FastBufferReader reader, __RpcParams rpcParams)
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
			((NotificationController)target).BroadcastRemoveRPC(s, ext);
			target.__rpc_exec_stage = __RpcExecStage.Send;
		}
	}

	private static void __rpc_handler_1935839193(NetworkBehaviour target, FastBufferReader reader, __RpcParams rpcParams)
	{
		NetworkManager networkManager = target.NetworkManager;
		if ((object)networkManager != null && networkManager.IsListening)
		{
			reader.ReadValueSafe(out bool value, default(FastBufferWriter.ForPrimitives));
			NotificationData value2 = null;
			if (value)
			{
				reader.ReadValueSafe(out value2, default(FastBufferWriter.ForNetworkSerializable));
			}
			RpcParams ext = rpcParams.Ext;
			target.__rpc_exec_stage = __RpcExecStage.Execute;
			((NotificationController)target).BroadcastRPC(value2, ext);
			target.__rpc_exec_stage = __RpcExecStage.Send;
		}
	}

	private static void __rpc_handler_3589787394(NetworkBehaviour target, FastBufferReader reader, __RpcParams rpcParams)
	{
		NetworkManager networkManager = target.NetworkManager;
		if ((object)networkManager != null && networkManager.IsListening)
		{
			reader.ReadValueSafe(out bool value, default(FastBufferWriter.ForPrimitives));
			NotificationData value2 = null;
			if (value)
			{
				reader.ReadValueSafe(out value2, default(FastBufferWriter.ForNetworkSerializable));
			}
			target.__rpc_exec_stage = __RpcExecStage.Execute;
			((NotificationController)target).BroadcastAllRPC(value2);
			target.__rpc_exec_stage = __RpcExecStage.Send;
		}
	}

	private static void __rpc_handler_2220586703(NetworkBehaviour target, FastBufferReader reader, __RpcParams rpcParams)
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
			target.__rpc_exec_stage = __RpcExecStage.Execute;
			((NotificationController)target).BroadcastRemoveAllRPC(s);
			target.__rpc_exec_stage = __RpcExecStage.Send;
		}
	}

	private static void __rpc_handler_3175140379(NetworkBehaviour target, FastBufferReader reader, __RpcParams rpcParams)
	{
		NetworkManager networkManager = target.NetworkManager;
		if ((object)networkManager != null && networkManager.IsListening)
		{
			reader.ReadValueSafe(out bool value, default(FastBufferWriter.ForPrimitives));
			NotificationData3D value2 = null;
			if (value)
			{
				reader.ReadValueSafe(out value2, default(FastBufferWriter.ForNetworkSerializable));
			}
			RpcParams ext = rpcParams.Ext;
			target.__rpc_exec_stage = __RpcExecStage.Execute;
			((NotificationController)target).Broadcast3DRPC(value2, ext);
			target.__rpc_exec_stage = __RpcExecStage.Send;
		}
	}

	private static void __rpc_handler_3648903951(NetworkBehaviour target, FastBufferReader reader, __RpcParams rpcParams)
	{
		NetworkManager networkManager = target.NetworkManager;
		if ((object)networkManager != null && networkManager.IsListening)
		{
			reader.ReadValueSafe(out bool value, default(FastBufferWriter.ForPrimitives));
			NotificationData3D value2 = null;
			if (value)
			{
				reader.ReadValueSafe(out value2, default(FastBufferWriter.ForNetworkSerializable));
			}
			target.__rpc_exec_stage = __RpcExecStage.Execute;
			((NotificationController)target).BroadcastAll3DRPC(value2);
			target.__rpc_exec_stage = __RpcExecStage.Send;
		}
	}

	protected internal override string __getTypeName()
	{
		return "NotificationController";
	}
}
