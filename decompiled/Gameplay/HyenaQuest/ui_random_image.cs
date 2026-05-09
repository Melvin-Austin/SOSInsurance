using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace HyenaQuest;

[RequireComponent(typeof(Image))]
[DisallowMultipleComponent]
public class ui_random_image : MonoBehaviour
{
	[Header("Settings")]
	public List<Sprite> images = new List<Sprite>();

	private Image _image;

	public void Awake()
	{
		_image = GetComponent<Image>();
		if (!_image)
		{
			throw new UnityException("ui_random_image requires Image component");
		}
		_image.sprite = images[Random.Range(0, images.Count)];
	}
}
