using UnityEngine;

namespace HyenaQuest;

public class entity_animator_shake : MonoBehaviour
{
	public void Shake(string data)
	{
		if (string.IsNullOrEmpty(data))
		{
			return;
		}
		string[] array = data.Split(',');
		if (array.Length >= 4 && int.TryParse(array[0], out var result) && int.TryParse(array[1], out var result2) && float.TryParse(array[2], out var result3) && float.TryParse(array[3], out var result4))
		{
			if (!NetController<ShakeController>.Instance)
			{
				throw new UnityException("Missing ShakeController");
			}
			if (result == 0)
			{
				NetController<ShakeController>.Instance.LocalShake((ShakeMode)result2, result3, result4);
			}
			else
			{
				NetController<ShakeController>.Instance.Local3DShake(base.transform.position, (ShakeMode)result2, result3, result4);
			}
		}
	}
}
