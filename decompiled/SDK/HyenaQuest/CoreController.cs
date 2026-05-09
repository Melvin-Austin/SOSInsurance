using System;
using System.Collections.Generic;
using UnityEngine;

namespace HyenaQuest;

[DisallowMultipleComponent]
[DefaultExecutionOrder(-220)]
public class CoreController : MonoBehaviour
{
	private static readonly Dictionary<Type, IController> _controllers = new Dictionary<Type, IController>();

	private static readonly Dictionary<Type, List<Delegate>> _pendingCallbacks = new Dictionary<Type, List<Delegate>>();

	public void Awake()
	{
		UnityEngine.Object.DontDestroyOnLoad(base.gameObject);
	}

	public static void Register<T>(T controller) where T : IController
	{
		Type type = controller.GetType();
		if (!_controllers.TryAdd(type, controller))
		{
			throw new UnityException("Controller of type " + type.Name + " is already registered");
		}
		if (!_pendingCallbacks.TryGetValue(type, out var value))
		{
			return;
		}
		foreach (Delegate item in value)
		{
			item.DynamicInvoke(controller);
		}
		_pendingCallbacks.Remove(type);
	}

	public static void Unregister<T>() where T : IController
	{
		Type typeFromHandle = typeof(T);
		if (!_controllers.Remove(typeFromHandle, out var _))
		{
			throw new UnityException("Tried to unregister " + typeFromHandle.Name + " but it wasn't registered");
		}
	}

	public static void WaitFor<T>(Action<T> onComplete) where T : IController
	{
		Type typeFromHandle = typeof(T);
		if (_controllers.TryGetValue(typeFromHandle, out var value))
		{
			onComplete((T)value);
			return;
		}
		if (!_pendingCallbacks.ContainsKey(typeFromHandle))
		{
			_pendingCallbacks[typeFromHandle] = new List<Delegate>();
		}
		_pendingCallbacks[typeFromHandle].Add(onComplete);
	}

	private void OnDestroy()
	{
		_controllers.Clear();
		_pendingCallbacks.Clear();
	}
}
