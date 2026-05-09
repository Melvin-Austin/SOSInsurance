using System;
using HyenaQuest;
using UnityEngine;

namespace Gameplay.Tutorial;

public class entity_tutorial_section : MonoBehaviour
{
	public entity_trigger trigger;

	public TutorialSection section;

	public entity_door door;

	private bool _hasActivated;

	private bool _isCompleted;

	public void Awake()
	{
		if (!trigger)
		{
			throw new UnityException("Trigger not set");
		}
		trigger.triggerOnce = true;
		trigger.OnEnter += new Action<Collider>(OnPlayerEnter);
	}

	public void OnDestroy()
	{
		if ((bool)trigger)
		{
			trigger.OnEnter -= new Action<Collider>(OnPlayerEnter);
		}
	}

	public bool IsCompleted()
	{
		return _isCompleted;
	}

	public void MarkCompleted()
	{
		_isCompleted = true;
		if ((bool)door)
		{
			door.SetOpen(newValue: true);
		}
	}

	public TutorialSection GetSection()
	{
		return section;
	}

	private void OnPlayerEnter(Collider _)
	{
		if (!_hasActivated)
		{
			if (!NetController<TutorialController>.Instance)
			{
				throw new UnityException("TutorialController not found");
			}
			NetController<TutorialController>.Instance.ActivateSection(this);
			_hasActivated = true;
		}
	}
}
