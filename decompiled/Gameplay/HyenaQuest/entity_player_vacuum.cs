using System;
using System.Collections.Generic;
using FailCake;
using TMPro;
using Unity.Netcode;
using UnityEngine;

namespace HyenaQuest;

[RequireComponent(typeof(NetworkObject))]
public class entity_player_vacuum : NetworkBehaviour
{
	public TextMeshPro scrapWatchText;

	public MeshRenderer vaccumSwirlRenderer;

	public AudioSource vacuumAudioSource;

	public ParticleSystem vacuumDustParticles;

	private entity_player _owner;

	private entity_item_vacuum _vacuumHolder;

	private entity_player_vacuum_trigger _coneTrigger;

	private util_fade_timer _vacuumAudioFade;

	private int _groundMask;

	private readonly HashSet<entity_phys_prop_scrap> _vacuumingObjects = new HashSet<entity_phys_prop_scrap>();

	private readonly HashSet<entity_phys_prop_scrap> _removeBuffer = new HashSet<entity_phys_prop_scrap>();

	private readonly NetVar<bool> _vacuuming = new NetVar<bool>(value: false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

	protected void Awake()
	{
		if (!scrapWatchText)
		{
			throw new UnityException("Missing scrapWatchText");
		}
		if (!vaccumSwirlRenderer)
		{
			throw new UnityException("Missing vaccumSwirlRenderer");
		}
		if (!vacuumAudioSource)
		{
			throw new UnityException("Missing vacuumAudioSource");
		}
		if (!vacuumDustParticles)
		{
			throw new UnityException("Missing vacuumDustParticles");
		}
		_coneTrigger = GetComponentInChildren<entity_player_vacuum_trigger>(includeInactive: true);
		if (!_coneTrigger)
		{
			throw new UnityException("Missing entity_player_vacuum_trigger");
		}
		_owner = GetComponent<entity_player>();
		if (!_owner)
		{
			throw new UnityException("entity_player_scrapper must be attached to an entity_player");
		}
		_groundMask = LayerMask.GetMask("entity_ground");
		_owner.OnHealthStatusUpdate += new Action<bool>(OnHealthStatusUpdate);
		_owner.GetInventory().OnInventoryUpdate += new Action<int, entity_item_pickable, bool>(OnInventoryUpdate);
	}

	public override void OnDestroy()
	{
		base.OnDestroy();
		if ((bool)_owner)
		{
			_owner.OnHealthStatusUpdate -= new Action<bool>(OnHealthStatusUpdate);
			_owner.GetInventory().OnInventoryUpdate -= new Action<int, entity_item_pickable, bool>(OnInventoryUpdate);
		}
	}

	public override void OnNetworkDespawn()
	{
		_vacuumAudioFade?.Stop();
		ClearVacuumObjects();
		NetController<ShakeController>.Instance?.StopControllerVibration("vacuum");
	}

	protected override void OnNetworkPostSpawn()
	{
		base.OnNetworkPostSpawn();
		if (base.IsClient)
		{
			_vacuuming.RegisterOnValueChanged(delegate(bool _, bool newValue)
			{
				SetVacuumSFX(newValue);
			});
		}
	}

	public override void OnNetworkPreDespawn()
	{
		base.OnNetworkPreDespawn();
		if (base.IsClient)
		{
			_vacuuming.OnValueChanged = null;
		}
	}

	public entity_item_vacuum GetVacuumHolder()
	{
		return _vacuumHolder;
	}

	public void Update()
	{
		if (!base.IsOwner)
		{
			return;
		}
		UpdateInfo();
		entity_player_physgun physgun = _owner.GetPhysgun();
		if (!physgun || physgun.IsGrabbing() || !_vacuumHolder || _vacuumHolder.IsFull())
		{
			SetVacuuming(vacuum: false);
			NetController<ShakeController>.Instance?.StopControllerVibration("vacuum");
		}
		else if (IsVacuuming())
		{
			ScrapController instance = NetController<ScrapController>.Instance;
			if ((object)instance != null && instance.HasVacuumUpgrade())
			{
				UpdateVacuumUpgraded();
			}
			else
			{
				UpdateVacuumDefault();
			}
		}
		else
		{
			ClearVacuumObjects();
			NetController<ShakeController>.Instance?.StopControllerVibration("vacuum");
		}
	}

	[Client]
	public bool IsVacuuming()
	{
		return _vacuuming.Value;
	}

	[Client]
	public void OnScrapStart()
	{
		if (base.IsOwner && (bool)_owner && !_owner.IsDead())
		{
			if (!MonoController<LocalizationController>.Instance)
			{
				throw new UnityException("Missing LocalizationController");
			}
			if (!_vacuumHolder)
			{
				NetController<NotificationController>.Instance.CreateNotification(new NotificationData
				{
					id = "vacuum-error",
					text = "ingame.ui.notification.vacuum.no-container",
					duration = 2f,
					soundEffect = "Ingame/Entities/Terminal/142608__autistic-lucario__error.ogg",
					soundVolume = 0.25f
				});
			}
			else if (_vacuumHolder.IsFull())
			{
				NetController<NotificationController>.Instance.CreateNotification(new NotificationData
				{
					id = "vacuum-full",
					text = "ingame.ui.notification.vacuum.full",
					duration = 2f,
					soundEffect = "Ingame/Entities/Terminal/142608__autistic-lucario__error.ogg",
					soundVolume = 0.25f
				});
			}
			else
			{
				SetVacuuming(vacuum: true);
			}
		}
	}

	[Client]
	public void OnScrapEnd()
	{
		if (base.IsOwner && (bool)_owner)
		{
			SetVacuuming(vacuum: false);
		}
	}

	private void OnHealthStatusUpdate(bool dead)
	{
		if (base.IsOwner)
		{
			SetVacuuming(vacuum: false);
		}
	}

	private void OnInventoryUpdate(int slot, entity_item item, bool server)
	{
		entity_player_inventory inventory = _owner.GetInventory();
		if ((bool)inventory)
		{
			_vacuumHolder = inventory.FindItemByID("item_vacuum") as entity_item_vacuum;
		}
	}

	[Client]
	private bool HasLineOfSight(Vector3 target)
	{
		return true;
	}

	[Client]
	private void UpdateVacuumDefault()
	{
		RaycastHit? aimingHit = _owner.GetAimingHit();
		if (aimingHit.HasValue && (bool)aimingHit.Value.collider && aimingHit.Value.collider.TryGetComponent<entity_phys_prop_scrap>(out var component, 1) && component.CanScrap(_owner))
		{
			SetVacuumTarget(component);
			NetController<ShakeController>.Instance?.SetControllerVibration("vacuum", 0.15f, 0.1f + UnityEngine.Random.Range(0f, 0.05f));
		}
		else
		{
			ClearVacuumObjects();
			NetController<ShakeController>.Instance?.StopControllerVibration("vacuum");
		}
	}

	[Client]
	private void UpdateVacuumUpgraded()
	{
		if (!_coneTrigger)
		{
			return;
		}
		_coneTrigger.RemoveDead();
		HashSet<entity_phys_prop_scrap> contents = _coneTrigger.GetContents();
		if (contents.Count == 0)
		{
			ClearVacuumObjects();
			NetController<ShakeController>.Instance?.StopControllerVibration("vacuum");
			return;
		}
		_removeBuffer.Clear();
		_removeBuffer.UnionWith(_vacuumingObjects);
		foreach (entity_phys_prop_scrap item in contents)
		{
			if ((bool)item && item.CanScrap(_owner) && HasLineOfSight(item.transform.position))
			{
				_removeBuffer.Remove(item);
				if (_vacuumingObjects.Add(item))
				{
					item.SetVacuumingRPC(_owner, scrapping: true);
				}
			}
		}
		foreach (entity_phys_prop_scrap item2 in _removeBuffer)
		{
			if ((bool)item2)
			{
				item2.SetVacuumingRPC(_owner, scrapping: false);
			}
			_vacuumingObjects.Remove(item2);
		}
		if (_vacuumingObjects.Count > 0)
		{
			NetController<ShakeController>.Instance?.SetControllerVibration("vacuum", 0.15f, 0.1f + UnityEngine.Random.Range(0f, 0.05f));
		}
		else
		{
			NetController<ShakeController>.Instance?.StopControllerVibration("vacuum");
		}
	}

	[Client]
	private void SetVacuumTarget(entity_phys_prop_scrap scrap)
	{
		if (_vacuumingObjects.Count != 1 || !_vacuumingObjects.Contains(scrap))
		{
			ClearVacuumObjects();
			_vacuumingObjects.Add(scrap);
			scrap.SetVacuumingRPC(_owner, scrapping: true);
		}
	}

	[Client]
	private void ClearVacuumObjects()
	{
		if (_vacuumingObjects.Count == 0)
		{
			return;
		}
		foreach (entity_phys_prop_scrap vacuumingObject in _vacuumingObjects)
		{
			if ((bool)vacuumingObject && vacuumingObject.IsSpawned)
			{
				vacuumingObject.SetVacuumingRPC(_owner, scrapping: false);
			}
		}
		_vacuumingObjects.Clear();
	}

	[Client]
	private void SetVacuumSFX(bool enable)
	{
		if (!vaccumSwirlRenderer)
		{
			throw new UnityException("vacuumEffect is not set");
		}
		if (!vacuumDustParticles)
		{
			throw new UnityException("vacuumDustParticles is not set");
		}
		if (!vacuumAudioSource)
		{
			throw new UnityException("vacuumAudioSource is not set");
		}
		vaccumSwirlRenderer.enabled = enable;
		if (enable)
		{
			vacuumDustParticles.Play();
		}
		else
		{
			vacuumDustParticles.Stop();
		}
		if (enable)
		{
			vacuumAudioSource.Play();
		}
		_vacuumAudioFade?.Stop();
		_vacuumAudioFade = util_fade_timer.Fade(1.2f, enable ? 0f : 1f, enable ? 1f : 0f, delegate(float f)
		{
			if ((bool)vacuumAudioSource)
			{
				vacuumAudioSource.pitch = f;
			}
		}, delegate
		{
			if (!enable)
			{
				vacuumAudioSource.Stop();
			}
		});
	}

	[Client]
	private void SetVacuuming(bool vacuum)
	{
		if (!base.IsOwner)
		{
			throw new UnityException("SetVacuuming can only be called on the owner.");
		}
		if (_vacuuming.Value != vacuum)
		{
			_vacuuming.Value = vacuum;
			_owner.SetLookingAtArmItem(vacuum);
			ClearVacuumObjects();
		}
	}

	[Client]
	private void UpdateInfo()
	{
		if (!MonoController<LocalizationController>.Instance)
		{
			throw new UnityException("Missing LocalizationController");
		}
		if ((bool)scrapWatchText)
		{
			float f = _vacuumHolder?.GetScrapPercentage() ?? 0;
			scrapWatchText.text = string.Format("{0}\n{1} %", MonoController<LocalizationController>.Instance.Get("ingame.ui.hints.scrap"), Mathf.FloorToInt(f));
		}
	}

	protected override void __initializeVariables()
	{
		if (_vacuuming == null)
		{
			throw new Exception("entity_player_vacuum._vacuuming cannot be null. All NetworkVariableBase instances must be initialized.");
		}
		_vacuuming.Initialize(this);
		__nameNetworkVariable(_vacuuming, "_vacuuming");
		NetworkVariableFields.Add(_vacuuming);
		base.__initializeVariables();
	}

	protected override void __initializeRpcs()
	{
		base.__initializeRpcs();
	}

	protected internal override string __getTypeName()
	{
		return "entity_player_vacuum";
	}
}
