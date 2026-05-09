using System;
using UnityEngine;
using UnityEngine.Scripting;

namespace HyenaQuest;

[Preserve]
[CurseType(CURSE_TYPE.ABYSS)]
public class VoidCurse : Curse
{
	private readonly PlayerAction _cursedDirection;

	private float _lastCut;

	private PlayerAction _lastMovementAction;

	public VoidCurse(entity_player owner, bool server, params object[] args)
		: base(owner, server)
	{
		if (server)
		{
			PlayerAction playerAction = ((args.Length != 0 && args[0] is PlayerAction) ? ((PlayerAction)args[0]) : PlayerAction.NONE);
			_cursedDirection = ((playerAction != 0) ? playerAction : ((PlayerAction)UnityEngine.Random.Range(1, 5)));
			owner.OnPlayerAction += new Action<PlayerAction, bool>(OnPlayerAction);
		}
	}

	public override void OnTick(bool server)
	{
		if (server && (bool)_owner && !_owner.IsDead() && !(Time.time < _lastCut) && _lastMovementAction != 0 && _lastMovementAction == _cursedDirection)
		{
			_owner.TakeHealthRPC((byte)UnityEngine.Random.Range(10, 15), DamageType.CURSE);
			_lastCut = Time.time + 0.2f;
		}
	}

	public override void OnCurseEnd(bool server)
	{
		if (server && (bool)_owner)
		{
			_owner.OnPlayerAction -= new Action<PlayerAction, bool>(OnPlayerAction);
		}
	}

	private void OnPlayerAction(PlayerAction action, bool server)
	{
		if (server && (int)action <= 4)
		{
			_lastMovementAction = action;
		}
	}
}
