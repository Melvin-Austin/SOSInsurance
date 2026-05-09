using System;
using System.IO;
using UnityEngine;

namespace HyenaQuest;

[Serializable]
public sealed class SaveFileSettings
{
	public string FileName { get; }

	public string FilePath { get; }

	public SaveFileSettings(string fileName)
	{
		FileName = fileName;
		FilePath = Path.Combine(Application.persistentDataPath, fileName);
	}
}
