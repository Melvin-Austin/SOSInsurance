using System;
using System.Collections.Generic;

namespace HyenaQuest;

[Serializable]
public struct SaveData
{
	public byte slot;

	public string date;

	public bool cheated;

	public int autoRevives;

	public int currency;

	public byte round;

	public byte health;

	public int totalScrap;

	public int totalDeliveries;

	public uint totalTimeMs;

	public List<SaveDataItems> items;

	public Dictionary<string, int> upgrades;
}
