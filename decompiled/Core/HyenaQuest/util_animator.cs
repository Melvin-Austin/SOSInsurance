using UnityEngine;

namespace HyenaQuest;

public static class util_animator
{
	public static bool ContainsParam(this Animator _Anim, string _ParamName)
	{
		if (!_Anim || string.IsNullOrEmpty(_ParamName))
		{
			return false;
		}
		AnimatorControllerParameter[] parameters = _Anim.parameters;
		for (int i = 0; i < parameters.Length; i++)
		{
			if (parameters[i].name == _ParamName)
			{
				return true;
			}
		}
		return false;
	}
}
