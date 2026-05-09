using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

namespace HyenaQuest;

public class util_net_picture
{
	private static readonly int CHUNK_SIZE = 8192;

	private static readonly int CHUNKS_PER_COOLDOWN = 30;

	private static readonly float CHUNK_COOLDOWN = 0.01f;

	private static readonly float STALE_TRANSFER_TIMEOUT = 30f;

	private static readonly float DIRTY_THROTTLE_INTERVAL = 0.2f;

	private static readonly float SEND_START_DELAY = 1.5f;

	private readonly MonoBehaviour _coroutineRunner;

	private readonly string _messageChannel;

	private readonly Func<byte[]> _getDataCallback;

	private readonly Action<byte[]> _onDataReceivedCallback;

	private readonly bool _alwaysReloadOnRequest;

	private byte[] _compressedData;

	private readonly HashSet<ulong> _pendingRequests = new HashSet<ulong>();

	private readonly Dictionary<ulong, Coroutine> _activeTransfers = new Dictionary<ulong, Coroutine>();

	private readonly object _compressionLock = new object();

	private bool _hasNewData;

	private bool _isCompressing;

	private bool _disposed;

	private int _sendTransferId;

	private int _receiveTransferId = -1;

	private byte[][] _receivedChunks;

	private int _receivedChunkCount;

	private float _lastChunkReceivedTime;

	private float _lastDirtyTime;

	private bool _dirtyThrottlePending;

	public util_net_picture(MonoBehaviour coroutineRunner, string messageChannel, Func<byte[]> getDataCallback = null, Action<byte[]> onDataReceivedCallback = null, bool alwaysReloadOnRequest = false)
	{
		_coroutineRunner = coroutineRunner ?? throw new ArgumentNullException("coroutineRunner");
		_messageChannel = messageChannel ?? throw new ArgumentNullException("messageChannel");
		_getDataCallback = getDataCallback;
		_onDataReceivedCallback = onDataReceivedCallback;
		_alwaysReloadOnRequest = alwaysReloadOnRequest;
		if (_onDataReceivedCallback != null)
		{
			NetworkManager.Singleton.CustomMessagingManager.RegisterNamedMessageHandler(_messageChannel, OnMessageReceived);
		}
	}

	public void Dispose()
	{
		_disposed = true;
		try
		{
			foreach (Coroutine value in _activeTransfers.Values)
			{
				if ((bool)_coroutineRunner)
				{
					_coroutineRunner.StopCoroutine(value);
				}
			}
			_activeTransfers.Clear();
			_pendingRequests.Clear();
			_receivedChunks = null;
			_compressedData = null;
			if (_onDataReceivedCallback != null)
			{
				NetworkManager.Singleton?.CustomMessagingManager?.UnregisterNamedMessageHandler(_messageChannel);
			}
		}
		catch (Exception ex)
		{
			Debug.LogError("Error disposing util_net_picture: " + ex.Message);
		}
	}

	public void AddRequest(ulong clientID)
	{
		if (!_disposed)
		{
			if (_alwaysReloadOnRequest)
			{
				_compressedData = null;
			}
			if (_hasNewData || _compressedData == null)
			{
				_pendingRequests.Add(clientID);
				LoadAndCompressDataAsync().Forget();
			}
			else
			{
				SendToClient(clientID, withDelay: true);
			}
		}
	}

	public void Transmit(ulong skip = 0uL, bool force = true)
	{
		if (_disposed)
		{
			return;
		}
		if (force)
		{
			MarkDirty();
		}
		foreach (ulong connectedClientsId in NETController.Instance.ConnectedClientsIds)
		{
			if (connectedClientsId != skip)
			{
				AddRequest(connectedClientsId);
			}
		}
	}

	public void PreloadData(byte[] rawData)
	{
		if (rawData != null && rawData.Length > 0)
		{
			PreloadDataAsync(rawData).Forget();
		}
	}

	public void MarkDirty()
	{
		if (_disposed || _hasNewData)
		{
			return;
		}
		float realtimeSinceStartup = Time.realtimeSinceStartup;
		if (realtimeSinceStartup - _lastDirtyTime < DIRTY_THROTTLE_INTERVAL)
		{
			if (!_dirtyThrottlePending)
			{
				_dirtyThrottlePending = true;
				ThrottledMarkDirtyAsync().Forget();
			}
		}
		else
		{
			_lastDirtyTime = realtimeSinceStartup;
			_hasNewData = true;
			_compressedData = null;
			LoadAndCompressDataAsync().Forget();
		}
	}

	private void OnMessageReceived(ulong sender, FastBufferReader payload)
	{
		payload.ReadValueSafe(out int value, default(FastBufferWriter.ForPrimitives));
		payload.ReadValueSafe(out int value2, default(FastBufferWriter.ForPrimitives));
		payload.ReadValueSafe(out int value3, default(FastBufferWriter.ForPrimitives));
		if (value2 < 0 || value3 <= 0 || value2 >= value3)
		{
			return;
		}
		int num = payload.Length - payload.Position;
		if (num > 0)
		{
			byte[] value4 = new byte[num];
			payload.ReadBytesSafe(ref value4, num);
			if (_receivedChunks != null && Time.realtimeSinceStartup - _lastChunkReceivedTime > STALE_TRANSFER_TIMEOUT)
			{
				Debug.LogWarning("[" + _messageChannel + "] Cleaned up stale transfer");
				_receivedChunks = null;
				_receivedChunkCount = 0;
				_receiveTransferId = -1;
			}
			if (value != _receiveTransferId || _receivedChunks == null || _receivedChunks.Length != value3)
			{
				_receivedChunks = new byte[value3][];
				_receivedChunkCount = 0;
				_receiveTransferId = value;
			}
			if (_receivedChunks[value2] == null)
			{
				_receivedChunkCount++;
			}
			_receivedChunks[value2] = value4;
			_lastChunkReceivedTime = Time.realtimeSinceStartup;
			if (_receivedChunkCount == value3)
			{
				ProcessReceivedDataAsync().Forget();
			}
		}
	}

	private async UniTaskVoid ThrottledMarkDirtyAsync()
	{
		float num = DIRTY_THROTTLE_INTERVAL - (Time.realtimeSinceStartup - _lastDirtyTime);
		if (num > 0f)
		{
			await UniTask.Delay(TimeSpan.FromSeconds(num), ignoreTimeScale: true);
		}
		_dirtyThrottlePending = false;
		if (!_disposed)
		{
			_lastDirtyTime = Time.realtimeSinceStartup;
			_hasNewData = true;
			_compressedData = null;
			LoadAndCompressDataAsync().Forget();
		}
	}

	private async UniTaskVoid PreloadDataAsync(byte[] rawData)
	{
		lock (_compressionLock)
		{
			if (_isCompressing)
			{
				return;
			}
			_isCompressing = true;
		}
		_hasNewData = false;
		await UniTask.Yield();
		_compressedData = await util_compression.CompressAsync(rawData);
		lock (_compressionLock)
		{
			_isCompressing = false;
		}
		if (!_disposed)
		{
			if (_hasNewData && _pendingRequests.Count > 0)
			{
				LoadAndCompressDataAsync().Forget();
			}
			else
			{
				SendToAllPending();
			}
		}
	}

	private async UniTaskVoid LoadAndCompressDataAsync()
	{
		lock (_compressionLock)
		{
			if (_isCompressing)
			{
				return;
			}
			_isCompressing = true;
		}
		_hasNewData = false;
		await UniTask.Yield(PlayerLoopTiming.PostLateUpdate);
		if (_disposed)
		{
			lock (_compressionLock)
			{
				_isCompressing = false;
				return;
			}
		}
		byte[] rawData = _getDataCallback?.Invoke();
		if (rawData == null || rawData.Length == 0)
		{
			lock (_compressionLock)
			{
				_isCompressing = false;
				return;
			}
		}
		await UniTask.Yield();
		_compressedData = await util_compression.CompressAsync(rawData);
		lock (_compressionLock)
		{
			_isCompressing = false;
		}
		if (!_disposed)
		{
			if (_hasNewData && _pendingRequests.Count > 0)
			{
				LoadAndCompressDataAsync().Forget();
			}
			else
			{
				SendToAllPending();
			}
		}
	}

	private void SendToAllPending()
	{
		if (_compressedData == null)
		{
			return;
		}
		List<ulong> list = _pendingRequests.ToList();
		_pendingRequests.Clear();
		foreach (ulong item in list)
		{
			if (NETController.Instance.IsConnected(item))
			{
				SendToClient(item, withDelay: true);
			}
		}
	}

	private void SendToClient(ulong clientID, bool withDelay = false)
	{
		if (_disposed || _compressedData == null)
		{
			return;
		}
		if (_activeTransfers.TryGetValue(clientID, out var value))
		{
			if ((bool)_coroutineRunner)
			{
				_coroutineRunner.StopCoroutine(value);
			}
			_activeTransfers.Remove(clientID);
		}
		int transferId = ++_sendTransferId;
		int totalChunks = Mathf.CeilToInt((float)_compressedData.Length / (float)CHUNK_SIZE);
		byte[] compressedData = _compressedData;
		_activeTransfers[clientID] = _coroutineRunner.StartCoroutine(SendChunks(compressedData, totalChunks, transferId, clientID, withDelay));
	}

	private IEnumerator SendChunks(byte[] compressedData, int totalChunks, int transferId, ulong clientID, bool withDelay = false)
	{
		if (compressedData == null || compressedData.Length == 0)
		{
			yield break;
		}
		if (withDelay)
		{
			yield return new WaitForSecondsRealtime(SEND_START_DELAY);
		}
		if (_disposed || !NETController.Instance.IsConnected(clientID))
		{
			_activeTransfers.Remove(clientID);
			yield break;
		}
		for (int i = 0; i < totalChunks; i++)
		{
			if (_disposed || !NetworkManager.Singleton || NetworkManager.Singleton.CustomMessagingManager == null || !NETController.Instance.IsConnected(clientID))
			{
				_activeTransfers.Remove(clientID);
				yield break;
			}
			int num = i * CHUNK_SIZE;
			int num2 = Mathf.Min(CHUNK_SIZE, compressedData.Length - num);
			try
			{
				using FastBufferWriter messageStream = new FastBufferWriter(12 + num2, Allocator.Temp);
				messageStream.WriteValueSafe(in transferId, default(FastBufferWriter.ForPrimitives));
				messageStream.WriteValueSafe(in i, default(FastBufferWriter.ForPrimitives));
				messageStream.WriteValueSafe(in totalChunks, default(FastBufferWriter.ForPrimitives));
				messageStream.WriteBytesSafe(compressedData, num2, num);
				NetworkManager.Singleton.CustomMessagingManager.SendNamedMessage(_messageChannel, clientID, messageStream, NetworkDelivery.ReliableFragmentedSequenced);
			}
			catch (Exception ex)
			{
				Debug.LogError($"Error sending chunk {i}: {ex.Message}");
				_activeTransfers.Remove(clientID);
				yield break;
			}
			if ((i + 1) % CHUNKS_PER_COOLDOWN == 0)
			{
				yield return new WaitForSecondsRealtime(CHUNK_COOLDOWN);
			}
			else
			{
				yield return null;
			}
		}
		_activeTransfers.Remove(clientID);
		Debug.Log($"[{_messageChannel}] Sent {totalChunks} chunks (transfer #{transferId}) to client {clientID}");
	}

	private async UniTaskVoid ProcessReceivedDataAsync()
	{
		byte[][] receivedChunks = _receivedChunks;
		_receivedChunks = null;
		_receivedChunkCount = 0;
		_receiveTransferId = -1;
		if (receivedChunks == null)
		{
			return;
		}
		int num = 0;
		for (int i = 0; i < receivedChunks.Length; i++)
		{
			num += receivedChunks[i].Length;
		}
		byte[] array = new byte[num];
		int num2 = 0;
		for (int j = 0; j < receivedChunks.Length; j++)
		{
			if (receivedChunks[j] == null)
			{
				Debug.LogError($"[{_messageChannel}] Missing chunk {j}, aborting reassembly");
				return;
			}
			Buffer.BlockCopy(receivedChunks[j], 0, array, num2, receivedChunks[j].Length);
			num2 += receivedChunks[j].Length;
		}
		try
		{
			byte[] obj = await util_compression.DecompressAsync(array);
			if (!_disposed)
			{
				_onDataReceivedCallback?.Invoke(obj);
			}
		}
		catch (Exception ex)
		{
			Debug.LogError("[" + _messageChannel + "] Failed to decompress received data: " + ex.Message);
		}
	}
}
