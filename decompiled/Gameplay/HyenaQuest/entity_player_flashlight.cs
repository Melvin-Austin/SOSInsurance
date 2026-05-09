using UnityEngine;

namespace HyenaQuest;

public class entity_player_flashlight : MonoBehaviour
{
	public GameObject lightSprite;

	public float intensity = 4f;

	private entity_light _light;

	private float _cooldownTimer;

	public void Awake()
	{
		_light = GetComponentInChildren<entity_light>(includeInactive: true);
		if (!_light)
		{
			throw new UnityException("Light component not found on flashlight");
		}
		_light.SetIntensity(intensity);
	}

	public void SetEnabled(bool enable)
	{
		if ((bool)lightSprite)
		{
			lightSprite.SetActive(enable);
		}
		if ((bool)_light)
		{
			_light.SetLightStatus(enable);
		}
	}
}
