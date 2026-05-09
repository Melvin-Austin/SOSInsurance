using UnityEngine;

namespace HyenaQuest;

public class entity_animator_sound : MonoBehaviour
{
	public void Play3DSound(string data)
	{
		if (string.IsNullOrEmpty(data))
		{
			return;
		}
		string[] array = data.Split(',');
		if (array.Length < 8)
		{
			return;
		}
		string[] array2 = array[2].Trim('[', ']').Split('|');
		if (array2.Length != 0 && int.TryParse(array[0], out var result) && float.TryParse(array[3], out var result2) && float.TryParse(array[4], out var result3) && float.TryParse(array[5], out var result4) && float.TryParse(array[6], out var result5) && float.TryParse(array[7], out var result6))
		{
			string text = array2[Random.Range(0, array2.Length)];
			if (result == 0)
			{
				NetController<SoundController>.Instance?.PlaySound(array[1] + "/" + text + ".ogg", new AudioData
				{
					pitch = Random.Range(result2, result3),
					volume = result6
				});
			}
			else
			{
				NetController<SoundController>.Instance?.Play3DSound(array[1] + "/" + text + ".ogg", base.transform.position + new Vector3(0f, result5, 0f), new AudioData
				{
					pitch = Random.Range(result2, result3),
					distance = result4,
					volume = result6
				});
			}
		}
	}
}
