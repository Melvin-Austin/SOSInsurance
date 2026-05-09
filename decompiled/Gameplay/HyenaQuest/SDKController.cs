using System;
using System.Collections.Generic;
using Pathfinding;
using UnityEngine;
using ZLinq;

namespace HyenaQuest;

[DefaultExecutionOrder(-120)]
[DisallowMultipleComponent]
public class SDKController : MonoController<SDKController>
{
	public List<AudioClip> wood = new List<AudioClip>();

	public List<AudioClip> metal = new List<AudioClip>();

	public List<AudioClip> plastic = new List<AudioClip>();

	public List<AudioClip> fabric = new List<AudioClip>();

	public List<AudioClip> brick = new List<AudioClip>();

	public List<AudioClip> cardboard = new List<AudioClip>();

	public List<AudioClip> woodDamage = new List<AudioClip>();

	public List<AudioClip> metalDamage = new List<AudioClip>();

	public List<AudioClip> plasticDamage = new List<AudioClip>();

	public List<AudioClip> fabricDamage = new List<AudioClip>();

	public List<AudioClip> brickDamage = new List<AudioClip>();

	public List<AudioClip> cardboardDamage = new List<AudioClip>();

	public List<GameObject> scrap = new List<GameObject>();

	public List<GameObject> monsters = new List<GameObject>();

	private bool IsServer
	{
		get
		{
			if ((bool)NETController.Instance)
			{
				return NETController.Instance.IsServer;
			}
			return false;
		}
	}

	public new void Awake()
	{
		base.Awake();
		SDK.PreNetworkTemplateSpawn = PreNetworkTemplateSpawn;
		SDK.PatchSDKEntity = PatchSDKEntity;
	}

	public new void OnDestroy()
	{
		SDK.PreNetworkTemplateSpawn = null;
		SDK.PatchSDKEntity = null;
		base.OnDestroy();
	}

	private void PatchSDKEntity(GameObject obj)
	{
		if (IsServer)
		{
			if (obj.name.StartsWith("entity_sdk_nav_add", StringComparison.InvariantCultureIgnoreCase))
			{
				entity_sdk_nav_add component = obj.GetComponent<entity_sdk_nav_add>();
				if (!component)
				{
					throw new UnityException("Missing entity_sdk_nav_add");
				}
				NavmeshAdd navmeshAdd = obj.AddComponent<NavmeshAdd>();
				if (!(UnityEngine.Object)(object)navmeshAdd)
				{
					throw new UnityException("Failed to add NavmeshAdd");
				}
				navmeshAdd.useRotationAndScale = true;
				navmeshAdd.center = Vector3.zero;
				navmeshAdd.rectangleSize = new Vector2(obj.transform.localScale.x, obj.transform.localScale.z);
				UnityEngine.Object.Destroy(component);
			}
			else if (obj.name.StartsWith("entity_sdk_nav_cut", StringComparison.InvariantCultureIgnoreCase))
			{
				entity_sdk_nav_cut component2 = obj.GetComponent<entity_sdk_nav_cut>();
				if (!component2)
				{
					throw new UnityException("Missing entity_sdk_nav_cut");
				}
				NavmeshCut navmeshCut = obj.AddComponent<NavmeshCut>();
				if (!(UnityEngine.Object)(object)navmeshCut)
				{
					throw new UnityException("Failed to add NavmeshCut");
				}
				navmeshCut.useRotationAndScale = true;
				navmeshCut.center = Vector3.zero;
				navmeshCut.height = obj.transform.localScale.y;
				navmeshCut.rectangleSize.x = obj.transform.localScale.x;
				navmeshCut.rectangleSize.y = obj.transform.localScale.z;
				navmeshCut.cutsAddedGeom = true;
				UnityEngine.Object.Destroy(component2);
			}
			else if (obj.name.StartsWith("entity_sdk_nav_link", StringComparison.InvariantCultureIgnoreCase))
			{
				entity_sdk_nav_link component3 = obj.GetComponent<entity_sdk_nav_link>();
				if (!component3)
				{
					throw new UnityException("Missing entity_sdk_nav_link");
				}
				NodeLink2 nodeLink = obj.AddComponent<NodeLink2>();
				if (!(UnityEngine.Object)(object)nodeLink)
				{
					throw new UnityException("Failed to add NodeLink2");
				}
				nodeLink.end = component3.target;
				entity_jump_link obj2 = obj.AddComponent<entity_jump_link>();
				if (!obj2)
				{
					throw new UnityException("Failed to add entity_jump_link");
				}
				obj2.jumpTime = component3.jumpTime;
				obj2.jumpOffset = component3.jumpOffset;
				obj2.jumpDelay = component3.jumpDelay;
				obj2.effect = component3.effect;
				UnityEngine.Object.Destroy(component3);
			}
		}
		if (obj.name.StartsWith("entity_sdk_interior_door", StringComparison.InvariantCultureIgnoreCase))
		{
			entity_sdk_interior_door component4 = obj.GetComponent<entity_sdk_interior_door>();
			if (!component4)
			{
				throw new UnityException("Missing entity_sdk_interior_door");
			}
			obj.AddComponent<entity_volume_affector>();
			Collider collider = obj.GetComponent<Collider>();
			if (!collider)
			{
				collider = obj.GetComponentInChildren<Collider>(includeInactive: true);
			}
			if ((bool)collider)
			{
				NavmeshCut navmeshCut2 = obj.AddComponent<NavmeshCut>();
				if (!(UnityEngine.Object)(object)navmeshCut2)
				{
					throw new UnityException("Failed to add NavmeshCut");
				}
				navmeshCut2.useRotationAndScale = true;
				navmeshCut2.cutsAddedGeom = true;
				navmeshCut2.radiusExpansionMode = NavmeshCut.RadiusExpansionMode.DontExpand;
				Bounds bounds = collider.bounds;
				bounds.Expand(0.1f);
				Transform obj3 = obj.transform;
				Vector3 center = obj3.InverseTransformPoint(bounds.center);
				Vector3 vector = obj3.InverseTransformVector(bounds.size);
				navmeshCut2.center = center;
				navmeshCut2.height = Mathf.Abs(vector.y);
				navmeshCut2.rectangleSize.x = Mathf.Abs(vector.x);
				navmeshCut2.rectangleSize.y = Mathf.Abs(vector.z);
			}
			entity_door_phys entity_door_phys2 = obj.AddComponent<entity_door_phys>();
			if (!entity_door_phys2)
			{
				throw new UnityException("Failed to add entity_door_phys");
			}
			entity_door_phys2.trap = component4.trap;
			entity_door_phys2.layers = component4.layers;
			switch (component4.collisionMaterial)
			{
			default:
				entity_door_phys2.collideSounds = component4.collideSounds;
				entity_door_phys2.damageSounds = component4.damageSounds;
				break;
			case SoundTypes.WOOD:
				entity_door_phys2.collideSounds = wood;
				entity_door_phys2.damageSounds = woodDamage;
				break;
			case SoundTypes.METAL:
				entity_door_phys2.collideSounds = metal;
				entity_door_phys2.damageSounds = metalDamage;
				break;
			case SoundTypes.PLASTIC:
				entity_door_phys2.collideSounds = plastic;
				entity_door_phys2.damageSounds = plasticDamage;
				break;
			case SoundTypes.FABRIC:
				entity_door_phys2.collideSounds = fabric;
				entity_door_phys2.damageSounds = fabricDamage;
				break;
			case SoundTypes.BRICK:
				entity_door_phys2.collideSounds = brick;
				entity_door_phys2.damageSounds = brickDamage;
				break;
			case SoundTypes.CARDBOARD:
				entity_door_phys2.collideSounds = cardboard;
				entity_door_phys2.damageSounds = cardboardDamage;
				break;
			}
			UnityEngine.Object.DestroyImmediate(component4);
		}
		else if (obj.name.StartsWith("entity_sdk_custom_phys_scrap", StringComparison.InvariantCultureIgnoreCase))
		{
			entity_sdk_custom_phys_scrap component5 = obj.GetComponent<entity_sdk_custom_phys_scrap>();
			if (!component5)
			{
				throw new UnityException("Missing entity_sdk_phys_scrap");
			}
			obj.AddComponent<entity_volume_affector>();
			entity_phys_prop_scrap entity_phys_prop_scrap2 = obj.AddComponent<entity_phys_prop_scrap>();
			if (!entity_phys_prop_scrap2)
			{
				throw new UnityException("Failed to add entity_phys_prop_scrap");
			}
			entity_phys_prop_scrap2.scrap = component5.scrap;
			entity_phys_prop_scrap2.viewModel = component5.viewModel;
			switch (component5.collisionMaterial)
			{
			default:
				entity_phys_prop_scrap2.collideSounds = component5.collideSounds;
				break;
			case SoundTypes.WOOD:
				entity_phys_prop_scrap2.collideSounds = wood;
				break;
			case SoundTypes.METAL:
				entity_phys_prop_scrap2.collideSounds = metal;
				break;
			case SoundTypes.PLASTIC:
				entity_phys_prop_scrap2.collideSounds = plastic;
				break;
			case SoundTypes.FABRIC:
				entity_phys_prop_scrap2.collideSounds = fabric;
				break;
			case SoundTypes.BRICK:
				entity_phys_prop_scrap2.collideSounds = brick;
				break;
			case SoundTypes.CARDBOARD:
				entity_phys_prop_scrap2.collideSounds = cardboard;
				break;
			}
			UnityEngine.Object.DestroyImmediate(component5);
		}
	}

	private GameObject PreNetworkTemplateSpawn(GameObject template)
	{
		if (!IsServer)
		{
			return null;
		}
		if (template.name.StartsWith("SDK-entity_scrap", StringComparison.InvariantCultureIgnoreCase))
		{
			GameObject gameObject = scrap.AsValueEnumerable().SingleOrDefault((GameObject s) => string.Equals(s.name, template.name.Replace("SDK-", ""), StringComparison.InvariantCultureIgnoreCase));
			if (!gameObject)
			{
				return template;
			}
			return gameObject;
		}
		if (template.name.StartsWith("SDK-entity_monster", StringComparison.InvariantCultureIgnoreCase))
		{
			GameObject gameObject2 = monsters.AsValueEnumerable().SingleOrDefault((GameObject s) => string.Equals(s.name, template.name.Replace("SDK-", ""), StringComparison.InvariantCultureIgnoreCase));
			if (!gameObject2)
			{
				return template;
			}
			return gameObject2;
		}
		return template;
	}
}
