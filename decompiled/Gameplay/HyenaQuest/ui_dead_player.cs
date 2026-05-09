using TMPro;
using UnityEngine;

namespace HyenaQuest;

public class ui_dead_player : MonoBehaviour
{
	public static readonly float MIC_RANGE = 7f;

	public CanvasGroup body;

	public GameObject mouth;

	public TextMeshProUGUI playerName;

	private entity_player _owner;

	private Transform _grid;

	private bool _visible = true;

	public void Awake()
	{
		if (!body)
		{
			throw new UnityException("ui_dead_player requires a body component");
		}
		if (!mouth)
		{
			throw new UnityException("ui_dead_player requires a mouth component");
		}
		if (!playerName)
		{
			throw new UnityException("ui_dead_player requires a playerName component");
		}
		_grid = base.transform.parent;
		SetVisible(visible: false);
	}

	public void Setup(entity_player owner)
	{
		if (!owner)
		{
			throw new UnityException("Setup requires a valid player");
		}
		_owner = owner;
		playerName.text = owner.GetPlayerName();
	}

	public void Update()
	{
		if (!_owner || !_owner.IsDead() || !PlayerController.LOCAL)
		{
			SetVisible(visible: false);
			return;
		}
		float num = Vector3.Distance(PlayerController.LOCAL.IsDead() ? PlayerController.LOCAL.GetCameraPosition() : PlayerController.LOCAL.transform.position, _owner.GetCameraPosition());
		bool flag = num <= MIC_RANGE + 0.5f;
		SetVisible(flag);
		if (flag)
		{
			body.alpha = ((num <= 4f) ? 1f : Mathf.InverseLerp(4f, MIC_RANGE + 0.5f, num));
			mouth.transform.localEulerAngles = new Vector3(0f, 0f, Mathf.Clamp((0f - _owner.GetCommsVoiceIntensity()) * 100f, -80f, -10f));
		}
	}

	private void SetVisible(bool visible)
	{
		if (_visible != visible)
		{
			_visible = visible;
			base.transform.SetParent(visible ? _grid : null, worldPositionStays: false);
			base.transform.localPosition = (visible ? Vector3.zero : (Vector3.up * 100f));
		}
	}
}
