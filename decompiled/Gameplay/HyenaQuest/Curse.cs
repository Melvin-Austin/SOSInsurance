using System.Reflection;
using UnityEngine;
using UnityEngine.Scripting;

namespace HyenaQuest;

[Preserve]
public abstract class Curse
{
	protected entity_player _owner;

	protected bool _destroying;

	public Curse(entity_player owner, bool server)
	{
		if (!owner)
		{
			throw new UnityException("Missing owner");
		}
		_owner = owner;
	}

	public entity_player GetOwner()
	{
		return _owner;
	}

	public void MarkForDeletion()
	{
		_destroying = true;
	}

	public CURSE_TYPE GetCurseType()
	{
		return (GetType().GetCustomAttribute<CurseTypeAttribute>() ?? throw new UnityException("Curse class " + GetType().Name + " is missing CurseTypeAttribute")).Type;
	}

	public virtual void OnTick(bool server)
	{
	}

	public virtual void OnCurseStart(bool server)
	{
	}

	public virtual void OnCurseEnd(bool server)
	{
	}

	public virtual bool IsOwner()
	{
		return _owner == PlayerController.LOCAL;
	}

	public virtual bool HasEnded()
	{
		if (!_destroying && (bool)_owner)
		{
			return _owner.IsDead();
		}
		return true;
	}
}
