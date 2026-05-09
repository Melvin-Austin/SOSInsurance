using System;
using FailCake;
using Opsive.GraphDesigner.Runtime.Variables;
using Opsive.Shared.Events;
using Unity.Netcode;
using UnityEngine;

namespace HyenaQuest;

public class entity_monster_roomba : entity_monster_ai
{
	public Transform eyeBone;

	public SkinnedMeshRenderer model;

	private SharedVariable<GameObject> _target;

	private AudioSource _audioSource;

	private util_fade_timer _eyeFade;

	private util_timer _pictureTimer;

	private float _focus;

	private readonly NetVar<NetworkBehaviourReference> _targetNET = new NetVar<NetworkBehaviourReference>();

	public new void Awake()
	{
		base.Awake();
		_audioSource = GetComponent<AudioSource>();
		if (!_audioSource)
		{
			throw new UnityException("entity_monster_roomba requires AudioSource component");
		}
		if (!eyeBone)
		{
			throw new UnityException("entity_monster_roomba requires eyeBone");
		}
		if (!MonoController<PlayerController>.Instance)
		{
			throw new UnityException("entity_monster_roomba requires PlayerController");
		}
	}

	public new void LateUpdate()
	{
		base.LateUpdate();
		if (base.IsClient)
		{
			_audioSource.pitch = 1f + GetVelocity() / 10f;
			entity_player entity_player2 = NETController.Get<entity_player>(_targetNET.Value);
			LookAtPlayer(entity_player2 ? (entity_player2.transform.position + Vector3.down * 0.5f) : Vector3.up);
			if ((bool)model && model.sharedMesh.blendShapeCount > 0)
			{
				model.SetBlendShapeWeight(0, _focus);
			}
		}
	}

	public override void OnNetworkSpawn()
	{
		base.OnNetworkSpawn();
		if (base.IsServer)
		{
			_target = _behavior.GetVariable<GameObject>("PLAYER");
			Opsive.Shared.Events.EventHandler.RegisterEvent(_behavior, "FOCUS_TARGET", OnFocusTarget);
		}
	}

	public override void OnNetworkDespawn()
	{
		base.OnNetworkDespawn();
		_eyeFade?.Stop();
		_pictureTimer?.Stop();
		if (base.IsServer)
		{
			_target = null;
			Opsive.Shared.Events.EventHandler.UnregisterEvent(_behavior, "FOCUS_TARGET", OnFocusTarget);
		}
	}

	protected override void OnNetworkPostSpawn()
	{
		base.OnNetworkPostSpawn();
		if (base.IsClient)
		{
			_targetNET.RegisterOnValueChanged(delegate(NetworkBehaviourReference _, NetworkBehaviourReference newValue)
			{
				FocusTarget(NETController.Get<entity_player>(newValue));
			});
		}
	}

	public override void OnNetworkPreDespawn()
	{
		base.OnNetworkPreDespawn();
		if (base.IsClient)
		{
			_targetNET.OnValueChanged = null;
		}
	}

	[Client]
	private void FocusTarget(entity_player ply)
	{
		_pictureTimer?.Stop();
		_eyeFade?.Stop();
		if (!ply)
		{
			_eyeFade = util_fade_timer.Fade(0.35f, _focus, 0f, delegate(float value)
			{
				_focus = value;
			});
		}
		else
		{
			TakePicture();
		}
	}

	[Client]
	private void TakePicture()
	{
		_pictureTimer?.Stop();
		_eyeFade?.Stop();
		_eyeFade = util_fade_timer.Fade(18f, 0f, UnityEngine.Random.Range(60, 90), delegate(float value)
		{
			_focus = value;
		}, delegate
		{
			AudioData data = new AudioData
			{
				pitch = UnityEngine.Random.Range(0.86f, 1.25f),
				distance = 2f,
				volume = 0.05f
			};
			NetController<SoundController>.Instance.Play3DSound("Ingame/Monsters/Roomba/shutter.ogg", base.transform.position, data);
			_pictureTimer = util_timer.Simple(UnityEngine.Random.value, delegate
			{
				_eyeFade = util_fade_timer.Fade(18f, _focus, 0f, delegate(float value)
				{
					_focus = value;
				}, delegate
				{
					_pictureTimer?.Stop();
					if ((bool)NETController.Get<entity_player>(_targetNET.Value))
					{
						_pictureTimer = util_timer.Simple(UnityEngine.Random.value, TakePicture);
					}
				});
			});
		});
	}

	[Server]
	private void OnFocusTarget()
	{
		if (!_target.Value || !_target.Value.CompareTag("Player"))
		{
			_targetNET.Value = null;
			return;
		}
		entity_player component = _target.Value.GetComponent<entity_player>();
		if ((bool)component)
		{
			_targetNET.Value = component;
		}
	}

	private void LookAtPlayer(Vector3 target)
	{
		if ((bool)eyeBone)
		{
			Quaternion b = Quaternion.LookRotation(target - eyeBone.position);
			eyeBone.rotation = Quaternion.Slerp(eyeBone.rotation, b, Time.deltaTime * 5f);
		}
	}

	protected override void __initializeVariables()
	{
		if (_targetNET == null)
		{
			throw new Exception("entity_monster_roomba._targetNET cannot be null. All NetworkVariableBase instances must be initialized.");
		}
		_targetNET.Initialize(this);
		__nameNetworkVariable(_targetNET, "_targetNET");
		NetworkVariableFields.Add(_targetNET);
		base.__initializeVariables();
	}

	protected override void __initializeRpcs()
	{
		base.__initializeRpcs();
	}

	protected internal override string __getTypeName()
	{
		return "entity_monster_roomba";
	}
}
