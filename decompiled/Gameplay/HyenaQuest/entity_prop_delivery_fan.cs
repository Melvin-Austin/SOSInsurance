using UnityEngine;
using UnityEngine.VFX;

namespace HyenaQuest;

public class entity_prop_delivery_fan : entity_prop_delivery
{
	public GameObject fan;

	private entity_attractor[] _attractors;

	private AudioSource _fanSnd;

	private VisualEffect _fanVFX;

	protected override void OnNetworkPostSpawn()
	{
		base.OnNetworkPostSpawn();
		if (!base.IsClient)
		{
			return;
		}
		_grabbingOwnerId.RegisterOnValueChanged(delegate(byte _, byte newValue)
		{
			bool flag = newValue != byte.MaxValue;
			if ((bool)_fanVFX)
			{
				_fanVFX.enabled = flag;
			}
			if (flag)
			{
				_fanSnd?.Play();
			}
			else
			{
				_fanSnd?.Stop();
			}
			if (_attractors != null)
			{
				entity_attractor[] attractors = _attractors;
				foreach (entity_attractor entity_attractor2 in attractors)
				{
					if ((bool)entity_attractor2)
					{
						entity_attractor2.enabled = flag;
					}
				}
			}
		});
	}

	public new void Update()
	{
		base.Update();
		if (base.IsClient && (bool)fan)
		{
			fan.transform.localEulerAngles = new Vector3(0f, IsBeingGrabbed() ? (Time.time * 1000f) : 0f, 0f);
		}
	}

	protected override void Init()
	{
		base.Init();
		if (!fan)
		{
			throw new UnityException("Missing fan GameObject");
		}
		_attractors = GetComponentsInChildren<entity_attractor>(includeInactive: true);
		entity_attractor[] attractors = _attractors;
		if (attractors == null || attractors.Length <= 0)
		{
			throw new UnityException("Missing attractors");
		}
		_fanSnd = GetComponentInChildren<AudioSource>(includeInactive: true);
		if (!_fanSnd)
		{
			throw new UnityException("Missing fan AudioSource");
		}
		_fanSnd.Stop();
		_fanVFX = GetComponentInChildren<VisualEffect>(includeInactive: true);
		if (!_fanVFX)
		{
			throw new UnityException("Missing fan VisualEffect");
		}
	}

	protected override void __initializeVariables()
	{
		base.__initializeVariables();
	}

	protected override void __initializeRpcs()
	{
		base.__initializeRpcs();
	}

	protected internal override string __getTypeName()
	{
		return "entity_prop_delivery_fan";
	}
}
