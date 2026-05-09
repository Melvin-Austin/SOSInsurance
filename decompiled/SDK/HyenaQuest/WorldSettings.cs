using System;
using System.Collections.Generic;
using System.Text;
using SaintsField;
using UnityEngine;
using UnityEngine.Scripting;

namespace HyenaQuest;

[Serializable]
[Preserve]
[CreateAssetMenu(menuName = "HyenaQuest/World Settings")]
public class WorldSettings : ScriptableObject
{
	[Range(1f, 255f)]
	public int minRounds = 1;

	[Range(0f, 1f)]
	public float weight = 1f;

	public bool collisionChecks;

	public bool duplicateChecks;

	public bool interiorMirroring;

	public bool exitShuffle;

	public bool visCleanup;

	public ContractModifiers modifiers;

	public AnimationCurve difficultyCurve = new AnimationCurve(new Keyframe(1f, 1f), new Keyframe(2f, 1.8f), new Keyframe(3f, 2.2f), new Keyframe(4f, 2.5f), new Keyframe(5f, 2.8f), new Keyframe(6f, 3f), new Keyframe(7f, 4f));

	[Range(0.1f, 1f)]
	[Tooltip("Monster to room ratio (0.4 = 4 monsters per 10 rooms)")]
	public float monsterDensity = 0.4f;

	[Range(0f, 20f)]
	public float baseRooms = 9f;

	[Range(0f, 20f)]
	public int minInteriorRooms = 3;

	public SaintsDictionary<string, int> biomeLimit;

	public SaintsDictionary<string, int> templateLimit;

	public List<MonsterSpawn> monsters;

	public FogSettings fog;

	public float musicVolume = 0.1f;

	public List<AudioClip> heistMusic;

	public Material skyMaterial;

	public List<EntrySettings> entry;

	public List<GameObject> closers;

	public List<GameObject> interiorClosers;

	public List<GameObject> interiors;

	public List<GameObject> rooms;

	public List<GameObject> traversal;

	public List<GameObject> deadEnds;

	public List<GameObject> extraNetworkObjects;

	private string __CALCULATION__
	{
		get
		{
			StringBuilder stringBuilder = new StringBuilder();
			stringBuilder.AppendLine("---------------- WORLD SIZE ----------------\n");
			int num = 0;
			for (byte b = 1; b < 11; b++)
			{
				stringBuilder.Append($"Round {b} -> {CalculateMapSize(b)} || ");
				num++;
				if (num % 5 == 0)
				{
					stringBuilder.AppendLine("");
				}
			}
			stringBuilder.AppendLine("\n---------------- MAX MONSTERS ----------------\n");
			for (byte b2 = 1; b2 < 11; b2++)
			{
				stringBuilder.Append($"Round {b2} -> {CalculateMaxMonsters(b2)} || ");
				num++;
				if (num % 5 == 0)
				{
					stringBuilder.AppendLine("");
				}
			}
			stringBuilder.AppendLine("\n");
			return stringBuilder.ToString();
		}
	}

	public int CalculateMapSize(byte currentRound)
	{
		int length = difficultyCurve.length;
		float num2;
		if (length >= 2)
		{
			Keyframe keyframe = difficultyCurve[length - 1];
			Keyframe keyframe2 = difficultyCurve[length - 2];
			if ((float)(int)currentRound > keyframe.time)
			{
				float num = (keyframe.value - keyframe2.value) / (keyframe.time - keyframe2.time);
				num2 = keyframe.value + num * ((float)(int)currentRound - keyframe.time);
			}
			else
			{
				num2 = difficultyCurve.Evaluate((int)currentRound);
			}
		}
		else
		{
			num2 = difficultyCurve.Evaluate((int)currentRound);
		}
		float num3 = 1f;
		return Mathf.Clamp(Mathf.RoundToInt(baseRooms * num2 * num3), 3, 100);
	}

	public int CalculateMaxMonsters(byte currentRound)
	{
		float num = (float)CalculateMapSize(currentRound) * monsterDensity;
		float num2 = 1f;
		return Mathf.Clamp(Mathf.RoundToInt(num * num2), 3, 20);
	}
}
