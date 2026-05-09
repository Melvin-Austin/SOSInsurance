using UnityEngine;
using UnityEngine.UI;

namespace HyenaQuest;

public class ui_outfit : MonoBehaviour
{
	public Image preview;

	public Button button;

	public void OnDestroy()
	{
		if ((bool)button)
		{
			button.onClick.RemoveAllListeners();
		}
	}

	public void SetSelected(bool set)
	{
		if ((bool)button)
		{
			ColorBlock colors = button.colors;
			colors.normalColor = (set ? Color.white : new Color(0.3f, 0.3f, 0.3f));
			button.colors = colors;
		}
	}

	public void SetAccessory(AccessoryData? data, Sprite locked, Sprite none)
	{
		if (!data.HasValue)
		{
			if ((bool)preview)
			{
				preview.sprite = none;
			}
			if ((bool)button)
			{
				button.interactable = false;
			}
		}
		else
		{
			if ((bool)button)
			{
				button.interactable = !data.Value.locked;
			}
			if ((bool)preview)
			{
				preview.sprite = (data.Value.locked ? locked : (data.Value.preview ?? none));
			}
		}
	}
}
