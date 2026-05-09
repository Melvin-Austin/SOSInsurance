using System;
using UnityEngine;

namespace HyenaQuest;

public class entity_health_controller : MonoBehaviour
{
	private entity_led_controller _controller;

	public void Awake()
	{
		_controller = GetComponent<entity_led_controller>();
		if (!_controller)
		{
			throw new UnityException("entity_health_controller requires entity_led_controller component");
		}
		if (!NetController<IngameController>.Instance)
		{
			throw new UnityException("Missing IngameController");
		}
		CoreController.WaitFor(delegate(IngameController ingameCtrl)
		{
			ingameCtrl.OnStoreHealthUpdate += new Action<byte, bool>(OnStoreHealthUpdate);
			OnStoreHealthUpdate(ingameCtrl.GetHealth(), server: false);
		});
	}

	public void OnDestroy()
	{
		if ((bool)NetController<IngameController>.Instance)
		{
			NetController<IngameController>.Instance.OnStoreHealthUpdate -= new Action<byte, bool>(OnStoreHealthUpdate);
		}
	}

	private void OnStoreHealthUpdate(byte health, bool server)
	{
		if (!server)
		{
			_controller.SetActive(health);
		}
	}
}
