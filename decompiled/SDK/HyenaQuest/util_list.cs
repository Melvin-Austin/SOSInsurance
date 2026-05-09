using System;
using System.Collections.Generic;

namespace HyenaQuest;

public static class util_list
{
	private static Random rng = new Random();

	public static void SetSeed(int seed)
	{
		rng = ((seed == 0) ? new Random() : new Random(seed));
	}

	public static IList<T> Shuffle<T>(this IList<T> list)
	{
		if (list == null)
		{
			return null;
		}
		int num = list.Count;
		while (num > 1)
		{
			num--;
			int num2 = rng.Next(num + 1);
			int index = num2;
			int index2 = num;
			T val = list[num];
			T val2 = list[num2];
			T val4 = (list[index] = val);
			val4 = (list[index2] = val2);
		}
		return list;
	}

	public static IList<T> ShuffleExcept<T>(this IList<T> list, int index)
	{
		if (list == null)
		{
			return null;
		}
		int count = list.Count;
		if (index < 0 || index >= count)
		{
			return list;
		}
		for (int num = count - 1; num > 0; num--)
		{
			if (num != index)
			{
				int num2 = rng.Next(num + 1);
				if (num2 == index)
				{
					num2 = num;
				}
				int index2 = num2;
				int index3 = num;
				T val = list[num];
				T val2 = list[num2];
				T val4 = (list[index2] = val);
				val4 = (list[index3] = val2);
			}
		}
		return list;
	}

	public static IList<T> ShuffleWithNew<T>(this IList<T> list)
	{
		if (list == null)
		{
			return null;
		}
		List<T> list2 = new List<T>(list);
		int num = list2.Count;
		while (num > 1)
		{
			num--;
			int num2 = rng.Next(num + 1);
			List<T> list3 = list2;
			int index = num2;
			List<T> list4 = list2;
			int index2 = num;
			T val = list2[num];
			T val2 = list2[num2];
			T val4 = (list3[index] = val);
			val4 = (list4[index2] = val2);
		}
		return list2;
	}

	public static void Shuffle<T>(this HashSet<T> set)
	{
		if (set == null || set.Count == 0)
		{
			return;
		}
		List<T> list = new List<T>(set);
		list.Shuffle();
		set.Clear();
		foreach (T item in list)
		{
			set.Add(item);
		}
	}
}
