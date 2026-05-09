using System;
using System.Collections.Generic;
using UnityEngine;
using ZLinq;

namespace HyenaQuest;

public class util_filtered_log : ILogHandler
{
	private readonly ILogHandler _originalLogHandler;

	private readonly List<string> _suppressedMessage;

	public util_filtered_log(List<string> suppressedMessages)
	{
		_originalLogHandler = Debug.unityLogger.logHandler;
		_suppressedMessage = suppressedMessages;
	}

	public void LogFormat(LogType logType, UnityEngine.Object context, string format, params object[] args)
	{
		if (!_suppressedMessage.AsValueEnumerable().Any((string msg) => format?.Contains(msg, StringComparison.InvariantCultureIgnoreCase) ?? false))
		{
			_originalLogHandler.LogFormat(logType, context, format, args);
		}
	}

	public void LogException(Exception exception, UnityEngine.Object context)
	{
		if (!_suppressedMessage.AsValueEnumerable().Any((string msg) => exception.Message?.Contains(msg, StringComparison.InvariantCultureIgnoreCase) ?? false))
		{
			_originalLogHandler.LogException(exception, context);
		}
	}
}
