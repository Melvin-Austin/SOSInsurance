using Unity.Netcode;
using UnityEngine;

namespace __GEN;

internal class NetworkVariableSerializationHelper
{
	[RuntimeInitializeOnLoadMethod]
	internal static void InitializeSerialization()
	{
		NetworkVariableSerializationTypedInitializers.InitializeSerializer_UnmanagedByMemcpy<bool>();
		NetworkVariableSerializationTypedInitializers.InitializeEqualityChecker_UnmanagedIEquatable<bool>();
	}
}
