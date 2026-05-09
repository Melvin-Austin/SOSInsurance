using System;
using UnityEngine;

namespace HyenaQuest;

public class entity_phys_prop_scrap_tuna : entity_phys_prop_scrap
{
	private Vector3 _swimDir;

	private float _nextFloop;

	private bool _swimming;

	private readonly NetVar<bool> _isAlive = new NetVar<bool>(value: false);

	public void Awake()
	{
		base.name = "fish_mx_1";
	}

	public override void OnNetworkSpawn()
	{
		base.OnNetworkSpawn();
		if (base.IsServer)
		{
			_isAlive.SetSpawnValue(UnityEngine.Random.Range(0, 10) > 5);
		}
		_nextFloop = Time.time + UnityEngine.Random.Range(0.4f, 1.2f);
	}

	private void FixedUpdate()
	{
		if (!base.IsOwner || !_isAlive.Value || !_rigidbody)
		{
			return;
		}
		if (_swimming)
		{
			if (!_volume || !_volume.InsideAnyVolume(waterOnly: true, fullOnly: true))
			{
				_swimming = false;
				return;
			}
			_rigidbody.AddForce(_swimDir * 6f, ForceMode.Force);
			Vector3 vector = Vector3.Cross(-base.transform.right, _swimDir);
			_rigidbody.AddTorque(vector * 2f, ForceMode.Force);
			Vector3 vector2 = Vector3.Cross(-base.transform.forward, Vector3.up);
			_rigidbody.AddTorque(vector2 * 3.5f, ForceMode.Force);
			_rigidbody.AddTorque(base.transform.forward * (Mathf.Sin(Time.time * 10f) * 0.3f), ForceMode.Force);
			_rigidbody.linearVelocity *= 0.95f;
			_rigidbody.angularVelocity *= 0.9f;
			if (_rigidbody.linearVelocity.sqrMagnitude > 4f)
			{
				_rigidbody.linearVelocity = _rigidbody.linearVelocity.normalized * 2f;
			}
		}
		if (Time.time < _nextFloop || !_volume || IsBeingGrabbed())
		{
			return;
		}
		if (_volume.InsideAnyVolume(waterOnly: true, fullOnly: true))
		{
			if (UnityEngine.Random.value < 0.3f)
			{
				_swimDir = UnityEngine.Random.onUnitSphere;
				_swimDir.y *= 0.2f;
				_swimDir.Normalize();
			}
			else
			{
				_swimDir += UnityEngine.Random.insideUnitSphere * 0.3f;
				_swimDir.y *= 0.4f;
				_swimDir.Normalize();
			}
			_swimming = true;
			_nextFloop = Time.time + UnityEngine.Random.Range(1.5f, 3f);
		}
		else
		{
			_swimming = false;
			_nextFloop = Time.time + UnityEngine.Random.Range(0.4f, 1.2f);
			Vector3 force = new Vector3(UnityEngine.Random.Range(-0.3f, 0.3f), UnityEngine.Random.Range(0.5f, 1f), UnityEngine.Random.Range(-0.3f, 0.3f)) * 1.8f;
			_rigidbody.AddForce(force, ForceMode.Impulse);
			_rigidbody.AddTorque(UnityEngine.Random.insideUnitSphere * 3.6f, ForceMode.Impulse);
		}
	}

	protected override void __initializeVariables()
	{
		if (_isAlive == null)
		{
			throw new Exception("entity_phys_prop_scrap_tuna._isAlive cannot be null. All NetworkVariableBase instances must be initialized.");
		}
		_isAlive.Initialize(this);
		__nameNetworkVariable(_isAlive, "_isAlive");
		NetworkVariableFields.Add(_isAlive);
		base.__initializeVariables();
	}

	protected override void __initializeRpcs()
	{
		base.__initializeRpcs();
	}

	protected internal override string __getTypeName()
	{
		return "entity_phys_prop_scrap_tuna";
	}
}
