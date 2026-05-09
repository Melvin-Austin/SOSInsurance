using System.Collections.Generic;
using UnityEngine;

namespace HyenaQuest;

[DisallowMultipleComponent]
[DefaultExecutionOrder(-70)]
public class DeathController : MonoController<DeathController>
{
	public List<Death> deaths = new List<Death>();

	private Death? _currentDeath;

	public void Death(DamageType type)
	{
		if (_currentDeath.HasValue)
		{
			throw new UnityException("Triggered another death while previous is still active");
		}
		Death? currentDeath = deaths.Find((Death d) => (d.type & type) != 0);
		if (!currentDeath.HasValue)
		{
			throw new UnityException("Death not found");
		}
		if ((bool)MonoController<UIController>.Instance)
		{
			MonoController<UIController>.Instance.SetFade(fadeIn: true, 1000f);
		}
		_currentDeath = currentDeath;
		if (!_currentDeath.Value.death)
		{
			OnAnimationComplete();
		}
		else
		{
			_currentDeath.Value.death?.Show(OnAnimationComplete);
		}
	}

	private void OnAnimationComplete()
	{
		_currentDeath = null;
	}
}
