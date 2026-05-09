using System;
using System.Collections.Generic;
using FailCake;
using UnityEngine;
using UnityEngine.InputSystem;

namespace HyenaQuest;

[DefaultExecutionOrder(-81)]
[DisallowMultipleComponent]
public class SpectateController : MonoController<SpectateController>
{
	private static readonly float SPECTATE_BODY_DURATION = 3f;

	public GameEvent<entity_player> OnSpectateUpdate = new GameEvent<entity_player>();

	public Transform spectateFallback;

	public InputActionReference nextSpectateAction;

	public InputActionReference prevSpectateAction;

	private entity_player _targetPlayer;

	private util_timer _bodyTimer;

	private bool _isSpectatingOwnBody;

	public new void Awake()
	{
		base.Awake();
		if (!spectateFallback)
		{
			throw new UnityException("Missing spectateFallback");
		}
		if (!nextSpectateAction)
		{
			throw new UnityException("Missing nextSpectateAction");
		}
		if (!prevSpectateAction)
		{
			throw new UnityException("Missing prevSpectateAction");
		}
		PlayerController.OnLocalPlayerSet += new Action(SetupControls);
		CoreController.WaitFor(delegate(PlayerController plyCtrl)
		{
			plyCtrl.OnPlayerRemoved += new Action<entity_player, bool>(OnPlayerRemoved);
			plyCtrl.OnPlayerRevive += new Action<entity_player, bool>(OnPlayerRevived);
			plyCtrl.OnPlayerDeath += new Action<entity_player, bool>(OnPlayerDied);
		});
	}

	public new void OnDestroy()
	{
		_bodyTimer?.Stop();
		PlayerController.OnLocalPlayerSet -= new Action(SetupControls);
		if ((bool)MonoController<PlayerController>.Instance)
		{
			MonoController<PlayerController>.Instance.OnPlayerRemoved -= new Action<entity_player, bool>(OnPlayerRemoved);
			MonoController<PlayerController>.Instance.OnPlayerRevive -= new Action<entity_player, bool>(OnPlayerRevived);
			MonoController<PlayerController>.Instance.OnPlayerDeath -= new Action<entity_player, bool>(OnPlayerDied);
		}
		if ((bool)nextSpectateAction)
		{
			nextSpectateAction.action.performed -= OnNextSpectate;
		}
		if ((bool)prevSpectateAction)
		{
			prevSpectateAction.action.performed -= OnPrevSpectate;
		}
		base.OnDestroy();
	}

	public void StopSpectating()
	{
		entity_player lOCAL = PlayerController.LOCAL;
		if ((bool)lOCAL)
		{
			entity_player_camera camera = lOCAL.GetCamera();
			if ((bool)camera)
			{
				ResetSpectating();
				camera.Spectate(null);
				OnSpectateUpdate?.Invoke(null);
			}
		}
	}

	public void StartSpectating(bool instant = false)
	{
		entity_player lOCAL = PlayerController.LOCAL;
		if (!lOCAL)
		{
			return;
		}
		entity_player_camera camera = lOCAL.GetCamera();
		if ((bool)camera)
		{
			ResetSpectating();
			if (instant)
			{
				SpectateFirstAvailable();
				return;
			}
			_isSpectatingOwnBody = true;
			_targetPlayer = null;
			camera.Spectate(null);
			OnSpectateUpdate?.Invoke(lOCAL);
			_bodyTimer?.Stop();
			_bodyTimer = util_timer.Simple(SPECTATE_BODY_DURATION, OnBodyTimerComplete);
		}
	}

	public entity_player GetSpectatingTarget()
	{
		return _targetPlayer;
	}

	private void SetupControls()
	{
		nextSpectateAction.action.performed -= OnNextSpectate;
		prevSpectateAction.action.performed -= OnPrevSpectate;
		nextSpectateAction.action.performed += OnNextSpectate;
		prevSpectateAction.action.performed += OnPrevSpectate;
	}

	private void OnBodyTimerComplete()
	{
		_bodyTimer = null;
		_isSpectatingOwnBody = false;
		SpectateFirstAvailable();
	}

	private void OnPlayerDied(entity_player ply, bool server)
	{
		if (!server && !(ply != _targetPlayer))
		{
			SpectateFirstAvailable();
		}
	}

	private void OnPlayerRevived(entity_player ply, bool server)
	{
		if (!server && !(ply == PlayerController.LOCAL) && (bool)PlayerController.LOCAL && PlayerController.LOCAL.IsDead() && !_isSpectatingOwnBody && !_targetPlayer)
		{
			SpectateFirstAvailable();
		}
	}

	private void OnPlayerRemoved(entity_player ply, bool server)
	{
		if (!server && (bool)ply && !(ply != _targetPlayer))
		{
			SpectateFirstAvailable(ply);
		}
	}

	private void OnPrevSpectate(InputAction.CallbackContext ctx)
	{
		if (CanSwitchSpectate())
		{
			CycleSpectate(-1);
		}
	}

	private void OnNextSpectate(InputAction.CallbackContext ctx)
	{
		if (CanSwitchSpectate())
		{
			CycleSpectate(1);
		}
	}

	private bool CanSwitchSpectate()
	{
		if (!PlayerController.LOCAL)
		{
			return false;
		}
		if (!PlayerController.LOCAL.IsDead())
		{
			return false;
		}
		return !_isSpectatingOwnBody;
	}

	private void CycleSpectate(int direction)
	{
		entity_player lOCAL = PlayerController.LOCAL;
		if (!lOCAL)
		{
			return;
		}
		List<entity_player> alivePlayers = MonoController<PlayerController>.Instance.GetAlivePlayers(new entity_player[1] { lOCAL });
		if (alivePlayers != null && alivePlayers.Count > 0)
		{
			int num = (_targetPlayer ? alivePlayers.IndexOf(_targetPlayer) : (-1));
			if (num == -1)
			{
				SetSpectateTarget(alivePlayers[0]);
				return;
			}
			int index = (int)Mathf.Repeat(num + direction, alivePlayers.Count);
			SetSpectateTarget(alivePlayers[index]);
		}
	}

	private void SpectateFirstAvailable(entity_player exclude = null)
	{
		entity_player lOCAL = PlayerController.LOCAL;
		if (!lOCAL)
		{
			return;
		}
		List<entity_player> alivePlayers = MonoController<PlayerController>.Instance.GetAlivePlayers(new entity_player[1] { lOCAL });
		if (alivePlayers == null || alivePlayers.Count == 0)
		{
			SetSpectateTarget(null);
			return;
		}
		foreach (entity_player item in alivePlayers)
		{
			if (item != exclude)
			{
				SetSpectateTarget(item);
				return;
			}
		}
		SetSpectateTarget(null);
	}

	private void SetSpectateTarget(entity_player target)
	{
		entity_player lOCAL = PlayerController.LOCAL;
		if ((bool)lOCAL)
		{
			entity_player_camera camera = lOCAL.GetCamera();
			if ((bool)camera)
			{
				_targetPlayer = target;
				camera.Spectate(target?.spectate ?? spectateFallback);
				OnSpectateUpdate?.Invoke(target ?? lOCAL);
			}
		}
	}

	private void ResetSpectating()
	{
		_targetPlayer = null;
		_isSpectatingOwnBody = false;
		_bodyTimer?.Stop();
		_bodyTimer = null;
	}
}
