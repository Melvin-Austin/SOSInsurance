using UnityEngine;

namespace HyenaQuest;

public class ui_notification_detailed : ui_notification
{
	private entity_mesh_preview _preview;

	public new void Awake()
	{
		base.Awake();
		_preview = GetComponentInChildren<entity_mesh_preview>(includeInactive: true);
		if (!_preview)
		{
			throw new UnityException("Missing entity_mesh_preview");
		}
		_preview.isTransparent = true;
	}

	public void SetText(MeshRenderer render, MeshFilter filter, string text, float duration)
	{
		if (!_preview)
		{
			throw new UnityException("Missing entity_mesh_preview");
		}
		base.SetText(text, duration);
		_preview.SetMesh(render, filter);
	}

	public void Update()
	{
		if ((bool)_preview)
		{
			_preview.transform.localEulerAngles = new Vector3(-90f, 0f, Time.time * 100f);
		}
	}

	protected override bool AutoScale()
	{
		return false;
	}
}
