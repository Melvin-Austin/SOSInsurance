using System;
using System.Collections.Generic;
using FailCake;
using Unity.Netcode;
using UnityEngine;

namespace HyenaQuest;

[RequireComponent(typeof(NetworkObject))]
public class entity_led_switcher : NetworkBehaviour
{
	public List<entity_switch> switches = new List<entity_switch>();

	public float timePerLED = 0.5f;

	public List<entity_led> LEDS = new List<entity_led>();

	public AudioClip tickSound;

	[Range(1f, 100f)]
	public float maxDistance = 2f;

	[Range(0f, 1f)]
	public float pitchChange = 0.2f;

	public GameEvent OnComplete = new GameEvent();

	public GameEvent<byte, bool> OnTick = new GameEvent<byte, bool>();

	private readonly NetVar<byte> _progress = new NetVar<byte>(0);

	private util_timer _timer;

	private util_timer _completeTimer;

	protected override void OnNetworkPostSpawn()
	{
		base.OnNetworkPostSpawn();
		if (!base.IsClient)
		{
			return;
		}
		_progress.RegisterOnValueChanged(delegate(byte oldValue, byte newValue)
		{
			if (oldValue != newValue)
			{
				if (newValue != 0)
				{
					NetController<SoundController>.Instance.Play3DSound(tickSound, base.transform, new AudioData
					{
						pitch = ((pitchChange > 0f) ? ((float)(int)newValue * pitchChange + 0.5f) : 1f),
						distance = maxDistance
					});
					OnTick?.Invoke(newValue, param2: false);
				}
				for (byte b = 0; b < LEDS.Count; b++)
				{
					LEDS[b].SetActive(b < newValue);
				}
			}
		});
	}

	public override void OnNetworkPreDespawn()
	{
		base.OnNetworkPreDespawn();
		if (base.IsClient)
		{
			_progress.OnValueChanged = null;
		}
	}

	public override void OnNetworkSpawn()
	{
		base.OnNetworkSpawn();
		if (!base.IsServer)
		{
			return;
		}
		foreach (entity_switch @switch in switches)
		{
			@switch.OnUSE += new Action<bool>(OnSwitchPress);
		}
	}

	public override void OnNetworkDespawn()
	{
		base.OnNetworkDespawn();
		if (!base.IsServer)
		{
			return;
		}
		_timer?.Stop();
		_completeTimer?.Stop();
		foreach (entity_switch @switch in switches)
		{
			@switch.OnUSE -= new Action<bool>(OnSwitchPress);
		}
	}

	[Server]
	public void SetHint(string hint)
	{
		if (!base.IsServer)
		{
			throw new UnityException("OnLeverPick can only be called on the server");
		}
		foreach (entity_switch @switch in switches)
		{
			@switch.SetHint(hint);
		}
	}

	public void SetClickDistance(float distance)
	{
		foreach (entity_switch @switch in switches)
		{
			@switch.clickDistance = distance;
		}
	}

	[Server]
	public void SetSwitches(List<entity_switch> newSwitches)
	{
		if (!base.IsServer)
		{
			return;
		}
		List<entity_switch> list = switches;
		if (list != null && list.Count > 0)
		{
			throw new UnityException("Switches already set");
		}
		switches = newSwitches;
		foreach (entity_switch @switch in switches)
		{
			@switch.SetLocked(IsLocked());
			@switch.OnUSE += new Action<bool>(OnSwitchPress);
		}
	}

	[Server]
	public void SetLocked(bool locked)
	{
		if (locked == IsLocked())
		{
			return;
		}
		foreach (entity_switch @switch in switches)
		{
			@switch.SetLocked(locked);
		}
		ResetSwitch();
	}

	public bool IsLocked()
	{
		bool flag = false;
		foreach (entity_switch @switch in switches)
		{
			flag |= @switch.IsLocked();
		}
		return flag;
	}

	[Server]
	private void ResetSwitch()
	{
		_timer?.Stop();
		SetProgress(0);
	}

	[Server]
	private void OnSwitchPress(bool down)
	{
		if (IsLocked())
		{
			return;
		}
		if (!down)
		{
			ResetSwitch();
			return;
		}
		_timer?.Stop();
		_timer = util_timer.Create(LEDS.Count, timePerLED, delegate
		{
			Tick();
		});
	}

	[Server]
	private void Tick()
	{
		SetProgress((byte)(_progress.Value + 1));
		if (_progress.Value >= LEDS.Count)
		{
			Complete();
		}
	}

	[Server]
	private void Complete()
	{
		SetProgress((byte)LEDS.Count);
		_completeTimer?.Stop();
		_completeTimer = util_timer.Simple(0.5f, delegate
		{
			OnComplete?.Invoke();
		});
	}

	[Server]
	private void SetProgress(byte tick)
	{
		_progress.Value = tick;
		if (tick != 0)
		{
			OnTick?.Invoke(tick, param2: true);
		}
	}

	protected override void __initializeVariables()
	{
		if (_progress == null)
		{
			throw new Exception("entity_led_switcher._progress cannot be null. All NetworkVariableBase instances must be initialized.");
		}
		_progress.Initialize(this);
		__nameNetworkVariable(_progress, "_progress");
		NetworkVariableFields.Add(_progress);
		base.__initializeVariables();
	}

	protected override void __initializeRpcs()
	{
		base.__initializeRpcs();
	}

	protected internal override string __getTypeName()
	{
		return "entity_led_switcher";
	}
}
