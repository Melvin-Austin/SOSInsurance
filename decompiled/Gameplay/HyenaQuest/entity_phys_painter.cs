using PaintCore;
using PaintIn3D;
using Unity.Netcode;
using UnityEngine;

namespace HyenaQuest;

public class entity_phys_painter : entity_phys, IHitPoint, IHit, IHitLine
{
	public Color color = Color.white;

	public bool updateModelColor = true;

	private CwPaintSphere _paintSphere;

	public void HandleHitPoint(bool preview, int priority, float pressure, int seed, Vector3 position, Quaternion rotation)
	{
		if (base.IsOwner)
		{
			_paintSphere.HandleHitPoint(preview, priority, pressure, seed, position, rotation);
			HandleHitPointRpc(preview, priority, pressure, seed, position, rotation);
		}
	}

	public void HandleHitLine(bool preview, int priority, float pressure, int seed, Vector3 position, Vector3 endPosition, Quaternion rotation, bool clip)
	{
		if (base.IsOwner)
		{
			_paintSphere.HandleHitLine(preview, priority, pressure, seed, position, endPosition, rotation, clip);
			HandleHitLineRpc(preview, priority, pressure, seed, position, endPosition, rotation, clip);
		}
	}

	protected override void Init()
	{
		base.Init();
		_paintSphere = GetComponent<CwPaintSphere>();
		if (!_paintSphere)
		{
			_paintSphere = base.gameObject.GetComponentInChildren<CwPaintSphere>(includeInactive: true);
		}
		if (!_paintSphere)
		{
			throw new UnityException("entity_phys_painter requires CwPaintSphere component");
		}
		UpdateColor();
	}

	[Rpc(SendTo.NotOwner, InvokePermission = RpcInvokePermission.Everyone)]
	private void HandleHitPointRpc(bool preview, int priority, float pressure, int seed, Vector3 position, Quaternion rotation)
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
				InvokePermission = RpcInvokePermission.Everyone
			};
			RpcParams rpcParams = default(RpcParams);
			FastBufferWriter bufferWriter = __beginSendRpc(1849533939u, rpcParams, attributeParams, SendTo.NotOwner, RpcDelivery.Reliable);
			bufferWriter.WriteValueSafe(in preview, default(FastBufferWriter.ForPrimitives));
			BytePacker.WriteValueBitPacked(bufferWriter, priority);
			bufferWriter.WriteValueSafe(in pressure, default(FastBufferWriter.ForPrimitives));
			BytePacker.WriteValueBitPacked(bufferWriter, seed);
			bufferWriter.WriteValueSafe(in position);
			bufferWriter.WriteValueSafe(in rotation);
			__endSendRpc(ref bufferWriter, 1849533939u, rpcParams, attributeParams, SendTo.NotOwner, RpcDelivery.Reliable);
		}
		if (__rpc_exec_stage == __RpcExecStage.Execute)
		{
			__rpc_exec_stage = __RpcExecStage.Send;
			if (!base.IsOwner)
			{
				_paintSphere.HandleHitPoint(preview, priority, pressure, seed, position, rotation);
			}
		}
	}

	[Rpc(SendTo.NotOwner, InvokePermission = RpcInvokePermission.Everyone)]
	private void HandleHitLineRpc(bool preview, int priority, float pressure, int seed, Vector3 position, Vector3 endPosition, Quaternion rotation, bool clip)
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
				InvokePermission = RpcInvokePermission.Everyone
			};
			RpcParams rpcParams = default(RpcParams);
			FastBufferWriter bufferWriter = __beginSendRpc(1369965528u, rpcParams, attributeParams, SendTo.NotOwner, RpcDelivery.Reliable);
			bufferWriter.WriteValueSafe(in preview, default(FastBufferWriter.ForPrimitives));
			BytePacker.WriteValueBitPacked(bufferWriter, priority);
			bufferWriter.WriteValueSafe(in pressure, default(FastBufferWriter.ForPrimitives));
			BytePacker.WriteValueBitPacked(bufferWriter, seed);
			bufferWriter.WriteValueSafe(in position);
			bufferWriter.WriteValueSafe(in endPosition);
			bufferWriter.WriteValueSafe(in rotation);
			bufferWriter.WriteValueSafe(in clip, default(FastBufferWriter.ForPrimitives));
			__endSendRpc(ref bufferWriter, 1369965528u, rpcParams, attributeParams, SendTo.NotOwner, RpcDelivery.Reliable);
		}
		if (__rpc_exec_stage == __RpcExecStage.Execute)
		{
			__rpc_exec_stage = __RpcExecStage.Send;
			if (!base.IsOwner)
			{
				_paintSphere.HandleHitLine(preview, priority, pressure, seed, position, endPosition, rotation, clip);
			}
		}
	}

	private void UpdateColor()
	{
		_paintSphere.Color = color;
		if (updateModelColor)
		{
			Renderer[] renderers = _renderers;
			if (renderers != null && renderers.Length == 1)
			{
				_renderers[0].material.color = color;
			}
		}
	}

	protected override void __initializeVariables()
	{
		base.__initializeVariables();
	}

	protected override void __initializeRpcs()
	{
		__registerRpc(1849533939u, __rpc_handler_1849533939, "HandleHitPointRpc", RpcInvokePermission.Everyone);
		__registerRpc(1369965528u, __rpc_handler_1369965528, "HandleHitLineRpc", RpcInvokePermission.Everyone);
		base.__initializeRpcs();
	}

	private static void __rpc_handler_1849533939(NetworkBehaviour target, FastBufferReader reader, __RpcParams rpcParams)
	{
		NetworkManager networkManager = target.NetworkManager;
		if ((object)networkManager != null && networkManager.IsListening)
		{
			reader.ReadValueSafe(out bool value, default(FastBufferWriter.ForPrimitives));
			ByteUnpacker.ReadValueBitPacked(reader, out int value2);
			reader.ReadValueSafe(out float value3, default(FastBufferWriter.ForPrimitives));
			ByteUnpacker.ReadValueBitPacked(reader, out int value4);
			reader.ReadValueSafe(out Vector3 value5);
			reader.ReadValueSafe(out Quaternion value6);
			target.__rpc_exec_stage = __RpcExecStage.Execute;
			((entity_phys_painter)target).HandleHitPointRpc(value, value2, value3, value4, value5, value6);
			target.__rpc_exec_stage = __RpcExecStage.Send;
		}
	}

	private static void __rpc_handler_1369965528(NetworkBehaviour target, FastBufferReader reader, __RpcParams rpcParams)
	{
		NetworkManager networkManager = target.NetworkManager;
		if ((object)networkManager != null && networkManager.IsListening)
		{
			reader.ReadValueSafe(out bool value, default(FastBufferWriter.ForPrimitives));
			ByteUnpacker.ReadValueBitPacked(reader, out int value2);
			reader.ReadValueSafe(out float value3, default(FastBufferWriter.ForPrimitives));
			ByteUnpacker.ReadValueBitPacked(reader, out int value4);
			reader.ReadValueSafe(out Vector3 value5);
			reader.ReadValueSafe(out Vector3 value6);
			reader.ReadValueSafe(out Quaternion value7);
			reader.ReadValueSafe(out bool value8, default(FastBufferWriter.ForPrimitives));
			target.__rpc_exec_stage = __RpcExecStage.Execute;
			((entity_phys_painter)target).HandleHitLineRpc(value, value2, value3, value4, value5, value6, value7, value8);
			target.__rpc_exec_stage = __RpcExecStage.Send;
		}
	}

	protected internal override string __getTypeName()
	{
		return "entity_phys_painter";
	}
}
