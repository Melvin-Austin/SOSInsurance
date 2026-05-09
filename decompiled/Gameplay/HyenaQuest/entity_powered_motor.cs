using System;
using UnityEngine;

namespace HyenaQuest;

[RequireComponent(typeof(entity_spinner))]
public class entity_powered_motor : MonoBehaviour
{
	[Header("Settings")]
	public PowerGrid grid;

	private entity_spinner _spinner;

	private AudioSource _audio;

	public void Awake()
	{
		_spinner = GetComponent<entity_spinner>();
		if (!_spinner)
		{
			throw new UnityException("entity_powered_motor requires entity_spinner to work.");
		}
		_audio = GetComponent<AudioSource>();
		if (!_audio)
		{
			_audio = GetComponentInChildren<AudioSource>(includeInactive: true);
		}
		if ((bool)_audio)
		{
			_audio.loop = true;
		}
		CoreController.WaitFor(delegate(PowerController powerCtrl)
		{
			powerCtrl.OnGridUpdate += new Action<PowerGrid, bool, bool>(OnGridUpdate);
		});
	}

	public void OnDestroy()
	{
		if ((bool)NetController<PowerController>.Instance)
		{
			NetController<PowerController>.Instance.OnGridUpdate -= new Action<PowerGrid, bool, bool>(OnGridUpdate);
		}
	}

	private void OnGridUpdate(PowerGrid area, bool enable, bool server)
	{
		if (server || area != grid)
		{
			return;
		}
		if (_audio.enabled)
		{
			if (enable)
			{
				_audio?.Play();
			}
			else
			{
				_audio?.Stop();
			}
		}
		_spinner.SetEnabled(enable);
	}
}
