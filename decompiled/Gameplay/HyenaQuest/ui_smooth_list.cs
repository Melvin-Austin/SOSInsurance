using System.Collections.Generic;
using SaintsField;
using UnityEngine;

namespace HyenaQuest;

public class ui_smooth_list : MonoBehaviour
{
	public SaintsDictionary<string, float> padding = new SaintsDictionary<string, float>();

	private readonly List<Vector3> _velocities = new List<Vector3>();

	public void Update()
	{
		int childCount = base.transform.childCount;
		if (childCount == 0)
		{
			return;
		}
		while (_velocities.Count < childCount)
		{
			_velocities.Add(Vector3.zero);
		}
		float num = 0f;
		for (int i = 0; i < childCount; i++)
		{
			Transform child = base.transform.GetChild(i);
			if ((bool)child)
			{
				RectTransform rectTransform = child as RectTransform;
				if ((bool)rectTransform)
				{
					Vector3 localPosition = child.localPosition;
					string key = child.name.Replace("(Clone)", string.Empty).Trim();
					localPosition.y = 0f - num + padding.GetValueOrDefault(key, -2f);
					Vector3 currentVelocity = _velocities[i];
					child.localPosition = Vector3.SmoothDamp(child.localPosition, localPosition, ref currentVelocity, 0.05f);
					_velocities[i] = currentVelocity;
					num += rectTransform.rect.height * child.localScale.y;
				}
			}
		}
	}
}
