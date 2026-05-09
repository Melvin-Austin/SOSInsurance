using System;
using System.Reflection;
using Unity.Netcode;

namespace HyenaQuest;

[Serializable]
[GenerateSerializationForGenericParameter(0)]
public class NetVar<T> : NetworkVariable<T>
{
	private readonly T _initialValue;

	private static FieldInfo _previousValueField;

	public virtual T PrevValue
	{
		get
		{
			if (_previousValueField == null)
			{
				_previousValueField = typeof(NetworkVariable<T>).GetField("m_PreviousValue", BindingFlags.Instance | BindingFlags.NonPublic);
			}
			return (T)_previousValueField.GetValue(this);
		}
	}

	public void RegisterOnValueChanged(OnValueChangedDelegate action)
	{
		OnValueChanged = (OnValueChangedDelegate)Delegate.Combine(OnValueChanged, action);
		OnValueChanged?.Invoke(_initialValue, Value);
	}

	public void SetSpawnValue(T value)
	{
		Value = value;
	}

	public override void SetDirty(bool isDirty)
	{
		if ((bool)GetBehaviour())
		{
			base.SetDirty(isDirty);
		}
	}

	public NetVar(T value = default(T), NetworkVariableReadPermission readPerm = NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission writePerm = NetworkVariableWritePermission.Server)
		: base(value, readPerm, writePerm)
	{
		_initialValue = value;
	}
}
