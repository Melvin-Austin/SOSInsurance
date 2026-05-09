using System;
using System.Collections.Generic;

namespace HyenaQuest;

[Serializable]
public struct I18N
{
	public string key;

	public Dictionary<string, string> args;

	public Action<string> callback;
}
