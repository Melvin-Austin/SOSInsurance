using TMPro;
using UnityEngine;

namespace HyenaQuest;

public class entity_flavor_text : MonoBehaviour
{
	public int min = 1000;

	public int max = 10000;

	private TextMeshPro _text;

	public void Awake()
	{
		_text = GetComponent<TextMeshPro>();
		if (!_text)
		{
			throw new UnityException("Missing text component");
		}
		_text.text = _text.text.Replace("[RAND]", Random.Range(min, max).ToString());
	}
}
