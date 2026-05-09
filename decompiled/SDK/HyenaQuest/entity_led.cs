using FailCake;
using UnityEngine;

namespace HyenaQuest;

public class entity_led : MonoBehaviour
{
	[ColorUsage(true, true)]
	public Color activeColor;

	[ColorUsage(true, true)]
	public Color disabledColor = Color.black;

	[SerializeField]
	public bool active;

	public Renderer mesh;

	[Range(0f, 100f)]
	public byte materialIndex;

	public float blink;

	public float blinkDelay;

	public float maxDistance = 2f;

	public AudioClip enableSnd;

	public AudioClip disableSnd;

	private static readonly int ShaderColor = Shader.PropertyToID("_BaseColor");

	private util_timer _timer;

	private util_timer _delayTimer;

	private bool _blinkState = true;

	public void Awake()
	{
		if (!mesh)
		{
			mesh = GetComponent<Renderer>();
		}
		if (!mesh)
		{
			throw new UnityException("entity_led requires a Renderer component to work.");
		}
		if (active && blink > 0f)
		{
			StartBlinking();
		}
		UpdateMaterial();
	}

	public void OnDestroy()
	{
		StopBlinking();
	}

	public void SetActive(bool enable, bool sound = false)
	{
		if (active == enable)
		{
			return;
		}
		active = enable;
		if (active)
		{
			if (blink > 0f)
			{
				StartBlinking();
			}
		}
		else
		{
			StopBlinking();
		}
		UpdateMaterial();
		if (sound)
		{
			SDK.Play3DSoundClip(active ? enableSnd : disableSnd, base.transform.position, new AudioData
			{
				distance = maxDistance
			}, arg4: false);
		}
	}

	public void SetActiveColor(Color color)
	{
		activeColor = color;
		UpdateMaterial();
	}

	public void SetDisabledColor(Color color)
	{
		disabledColor = color;
		UpdateMaterial();
	}

	private void StartBlinking()
	{
		_blinkState = true;
		_delayTimer?.Stop();
		_delayTimer = util_timer.Simple(blinkDelay, delegate
		{
			_timer?.Stop();
			_timer = util_timer.Create(-1, blink, delegate
			{
				_blinkState = !_blinkState;
				UpdateMaterial();
			});
		});
	}

	private void StopBlinking()
	{
		_timer?.Stop();
		_delayTimer?.Stop();
		_blinkState = true;
	}

	private void UpdateMaterial()
	{
		if ((bool)mesh)
		{
			bool flag = active && _blinkState;
			mesh.materials[materialIndex].SetColor(ShaderColor, flag ? activeColor : disabledColor);
		}
	}
}
