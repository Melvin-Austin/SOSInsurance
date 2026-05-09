using System;
using System.Collections.Generic;
using System.Linq;
using FailCake.VIS;
using FailCake.VMF;
using SaintsField;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using ZLinq;

namespace HyenaQuest;

[RequireComponent(typeof(NetworkObject))]
public class entity_room_base : NetworkBehaviour
{
	private static readonly int Blend = Shader.PropertyToID("_Blend");

	private static readonly int BlendTexture = Shader.PropertyToID("_BlendTexture");

	private static readonly int MainTexture = Shader.PropertyToID("_MainTexture");

	private static readonly int Color = Shader.PropertyToID("_MainColor");

	private static readonly int Layer = Shader.PropertyToID("_Layer");

	[SerializeField]
	private Bounds _BOUNDS_;

	public List<RoomLayer> layers = new List<RoomLayer>();

	public SaintsDictionary<VMFLayer, RoomTexture> modifierTextures = new SaintsDictionary<VMFLayer, RoomTexture>();

	[Range(0f, 100f)]
	public int minSpawnRounds;

	protected readonly List<entity_room_base> _adjacentRooms = new List<entity_room_base>();

	protected entity_vis_room _VISRoom;

	protected Renderer[] _renderers;

	protected Light[] _lights;

	protected DecalProjector[] _decals;

	protected bool _isVIS = true;

	protected float _lightTick;

	private readonly List<NetworkObject> _spawnedNetworkTemplates = new List<NetworkObject>();

	protected readonly NetVar<ulong> _combinedLayerData = new NetVar<ulong>(ulong.MaxValue);

	public void Update()
	{
		if (!SDK.MainCamera || Time.time < _lightTick || _lights == null)
		{
			return;
		}
		_lightTick = Time.time + 0.2f;
		Light[] lights = _lights;
		foreach (Light light in lights)
		{
			if ((bool)light)
			{
				light.gameObject.SetActive(_isVIS || Vector3.Distance(light.transform.position, SDK.MainCamera.transform.position) <= 18f);
			}
		}
	}

	public bool IsRoomVisibile()
	{
		return _isVIS;
	}

	public override void OnNetworkSpawn()
	{
		base.OnNetworkSpawn();
		_BOUNDS_ = util_bounds.GetWorldBounds(_BOUNDS_, base.transform);
		_VISRoom = GetComponent<entity_vis_room>();
		if ((bool)_VISRoom)
		{
			_VISRoom.IsInside = (Vector3 point) => _BOUNDS_.Contains(point);
			_VISRoom.OnVisibilityChanged = delegate(bool active)
			{
				_isVIS = active;
				if (_renderers != null)
				{
					Renderer[] renderers = _renderers;
					foreach (Renderer renderer in renderers)
					{
						if ((bool)renderer)
						{
							renderer.enabled = active;
						}
					}
				}
				if (_decals != null)
				{
					DecalProjector[] decals = _decals;
					foreach (DecalProjector decalProjector in decals)
					{
						if ((bool)decalProjector)
						{
							decalProjector.enabled = active;
						}
					}
				}
				if (active)
				{
					_lightTick = Time.time;
				}
			};
		}
		if (base.IsServer)
		{
			PickLayerData();
		}
	}

	public override void OnNetworkDespawn()
	{
		base.OnNetworkDespawn();
		if (base.IsServer)
		{
			DespawnTemplates();
		}
	}

	protected override void OnNetworkPostSpawn()
	{
		if (base.IsServer)
		{
			InternalSpawnTemplates();
		}
		base.OnNetworkPostSpawn();
		if (!base.IsClient)
		{
			return;
		}
		SDK.OnRoomSpawn?.Invoke(this);
		_combinedLayerData.RegisterOnValueChanged(delegate(ulong _, ulong newValue)
		{
			if (newValue != ulong.MaxValue)
			{
				ApplyLayerData(newValue);
			}
		});
	}

	public override void OnNetworkPreDespawn()
	{
		base.OnNetworkPreDespawn();
		if (base.IsClient)
		{
			_combinedLayerData.OnValueChanged = null;
		}
	}

	public void UpdateBounds()
	{
		_BOUNDS_ = util_bounds.GetWorldBounds(_BOUNDS_, base.transform);
	}

	public Bounds GetBounds()
	{
		return _BOUNDS_;
	}

	public entity_room_exit[] GetExits()
	{
		return (from a in GetComponentsInChildren<entity_room_exit>(includeInactive: false).AsValueEnumerable()
			where !(a is entity_interior_exit)
			orderby (a.order == -1) ? int.MaxValue : a.order
			select a).ToArray();
	}

	public void SetWindowColor(Color color, float outsideIntensity)
	{
		entity_window_light[] componentsInChildren = GetComponentsInChildren<entity_window_light>(includeInactive: true);
		foreach (entity_window_light entity_window_light2 in componentsInChildren)
		{
			if ((bool)entity_window_light2)
			{
				entity_window_light2.SetColor(color, outsideIntensity);
			}
		}
	}

	public void SetWindowStatus(string seed, bool forceClosed = false)
	{
		entity_room_window[] componentsInChildren = GetComponentsInChildren<entity_room_window>(includeInactive: true);
		foreach (entity_room_window entity_room_window2 in componentsInChildren)
		{
			if ((bool)entity_room_window2)
			{
				entity_room_window2.SetStatus(seed, forceClosed);
			}
		}
	}

	public void SetSkyboxColor(Color color)
	{
		Renderer[] array = (from r in GetComponentsInChildren<Renderer>(includeInactive: true).AsValueEnumerable()
			where r.transform.parent == base.transform
			select r).ToArray();
		if (array.Length == 0)
		{
			return;
		}
		Renderer[] array2 = array;
		foreach (Renderer renderer in array2)
		{
			if (!renderer)
			{
				continue;
			}
			Material[] materials = renderer.materials;
			if (materials.Length == 0)
			{
				continue;
			}
			Material[] array3 = materials;
			foreach (Material material in array3)
			{
				if ((bool)material && !(material.shader.name != "Shader Graph/FakeSkyboxFX") && material.HasColor(Color))
				{
					material.SetColor(Color, color);
				}
			}
		}
	}

	public void SetModifierTexture(Texture2D modifier, float blend)
	{
		Renderer[] array = (from r in GetComponentsInChildren<Renderer>(includeInactive: true).AsValueEnumerable()
			where r.transform.parent == base.transform
			select r).ToArray();
		if (array.Length == 0)
		{
			return;
		}
		Shader shader = Shader.Find("FailCake/VMF/VMFLit");
		if (!shader)
		{
			throw new UnityException("Shader 'FailCake/VMF/VMFLit' not found!");
		}
		Shader shader2 = Shader.Find("FailCake/VMF/VMFLitLayer");
		if (!shader2)
		{
			throw new UnityException("Shader 'FailCake/VMF/VMFLitLayer' not found!");
		}
		Renderer[] array2 = array;
		foreach (Renderer renderer in array2)
		{
			if (!renderer)
			{
				continue;
			}
			Material[] materials = renderer.materials;
			if (materials.Length == 0)
			{
				continue;
			}
			Material[] array3 = materials;
			foreach (Material material in array3)
			{
				if ((bool)material && (!(material.shader != shader) || !(material.shader != shader2)) && material.HasTexture(BlendTexture) && material.HasFloat(Blend))
				{
					material.SetTexture(BlendTexture, modifier);
					material.SetFloat(Blend, blend);
				}
			}
		}
	}

	protected virtual void DespawnTemplates()
	{
		if (!base.IsServer)
		{
			throw new UnityException("DespawnTemplates can only be called on the server");
		}
		if (_spawnedNetworkTemplates.Count == 0)
		{
			return;
		}
		foreach (NetworkObject spawnedNetworkTemplate in _spawnedNetworkTemplates)
		{
			if ((bool)spawnedNetworkTemplate && spawnedNetworkTemplate.IsSpawned)
			{
				spawnedNetworkTemplate.Despawn();
			}
		}
		_spawnedNetworkTemplates.Clear();
	}

	protected virtual void InternalSpawnTemplates()
	{
		if (!base.IsServer)
		{
			throw new UnityException("SpawnTemplates can only be called on the server");
		}
		if (_spawnedNetworkTemplates.Count > 0)
		{
			throw new UnityException("Templates already spawned");
		}
		entity_network_template_chance[] componentsInChildren = GetComponentsInChildren<entity_network_template_chance>(includeInactive: false);
		foreach (entity_network_template_chance entity_network_template_chance2 in componentsInChildren)
		{
			if (entity_network_template_chance2.CanSpawn())
			{
				(GameObject, NetworkObject) tuple = entity_network_template_chance2.NetworkSpawn();
				if ((bool)tuple.Item1 && (bool)tuple.Item2)
				{
					tuple.Item2.Spawn();
					_spawnedNetworkTemplates.Add(tuple.Item2);
				}
			}
		}
	}

	public void AddAdjacentRoom(entity_room_base room)
	{
		if (!_adjacentRooms.Contains(room))
		{
			_adjacentRooms.Add(room);
		}
	}

	public List<entity_room_base> GetAdjacentRooms()
	{
		return _adjacentRooms;
	}

	protected virtual void PickLayerData()
	{
		if (!base.IsServer)
		{
			throw new UnityException("PickCombinedLayerData can only be called on the server");
		}
		byte b = SDK.GetCurrentRound?.Invoke() ?? 0;
		byte b2 = 0;
		uint num = 0u;
		List<RoomLayer> list = layers;
		if (list != null && list.Count > 0)
		{
			float num2 = 0f;
			foreach (RoomLayer layer in layers)
			{
				if (layer.round <= b)
				{
					num2 += layer.weight;
				}
			}
			float num3 = UnityEngine.Random.value * num2;
			for (int i = 0; i < layers.Count; i++)
			{
				RoomLayer roomLayer = layers[i];
				if (roomLayer.round <= b)
				{
					if (num3 < roomLayer.weight)
					{
						b2 = (byte)i;
						break;
					}
					num3 -= roomLayer.weight;
				}
			}
		}
		SaintsDictionary<VMFLayer, RoomTexture> saintsDictionary = modifierTextures;
		if (saintsDictionary != null && saintsDictionary.Count > 0)
		{
			int num4 = 0;
			int num5 = TextureLayerSeed();
			if (num5 != -1)
			{
				UnityEngine.Random.InitState(num5);
			}
			foreach (KeyValuePair<VMFLayer, RoomTexture> modifierTexture in modifierTextures)
			{
				if (num4 >= 16)
				{
					break;
				}
				List<Texture2D> texture = modifierTexture.Value.texture;
				byte b3 = 0;
				if (texture != null && texture.Count > 0)
				{
					b3 = (byte)UnityEngine.Random.Range(0, texture.Count);
				}
				num |= (uint)((b3 & 0xFF) << num4 * 8);
				num4++;
			}
			UnityEngine.Random.InitState((int)DateTime.Now.Ticks);
		}
		_combinedLayerData.SetSpawnValue(((ulong)num << 8) | b2);
		ApplyLayerData(_combinedLayerData.Value);
	}

	protected virtual void CleanUnselectedLayers(byte selectedLayer)
	{
		List<RoomLayer> list = layers;
		if (list == null || list.Count <= 0)
		{
			return;
		}
		for (int i = 0; i < layers.Count; i++)
		{
			RoomLayer roomLayer = layers[i];
			if ((bool)roomLayer.layer)
			{
				if (i == selectedLayer)
				{
					roomLayer.layer.SetActive(value: true);
				}
				else
				{
					UnityEngine.Object.DestroyImmediate(roomLayer.layer);
				}
			}
		}
		_renderers = (from r in GetComponentsInChildren<Renderer>(includeInactive: false).AsValueEnumerable()
			where !string.Equals(r.name, "world", StringComparison.InvariantCultureIgnoreCase) && !r.CompareTag("OCCLUDER/IGNORE")
			select r).ToArray();
		_lights = GetComponentsInChildren<Light>(includeInactive: false);
		_decals = GetComponentsInChildren<DecalProjector>(includeInactive: false);
	}

	protected virtual int TextureLayerSeed()
	{
		return -1;
	}

	protected virtual void ApplyLayerData(ulong combinedData)
	{
		byte selectedLayer = (byte)(combinedData & 0xFF);
		uint encodedTextures = (uint)(combinedData >> 8);
		CleanUnselectedLayers(selectedLayer);
		ApplyTextureLayer(encodedTextures);
	}

	protected byte GetSelectedLayerModel()
	{
		return (byte)(_combinedLayerData.Value & 0xFF);
	}

	protected virtual void ApplyTextureLayer(uint encodedTextures)
	{
		SaintsDictionary<VMFLayer, RoomTexture> saintsDictionary = modifierTextures;
		if (saintsDictionary == null || saintsDictionary.Count <= 0)
		{
			return;
		}
		Renderer[] array = (from r in GetComponentsInChildren<Renderer>(includeInactive: true).AsValueEnumerable()
			where r.name == "func_wall_layer" || r.transform.parent == base.transform
			select r).ToArray();
		VMFLayerMaterial[] componentsInChildren = GetComponentsInChildren<VMFLayerMaterial>(includeInactive: true);
		if (array.Length == 0 && componentsInChildren.Length == 0)
		{
			Debug.LogWarning("No VMF layer materials found, but have modifierTextures set!");
			return;
		}
		Shader shader = Shader.Find("FailCake/VMF/VMFLitLayer");
		if (!shader)
		{
			throw new UnityException("Shader 'FailCake/VMF/VMFLitLayer' not found!");
		}
		List<Material> list = new List<Material>();
		Renderer[] array2 = array;
		foreach (Renderer renderer in array2)
		{
			if (!renderer)
			{
				continue;
			}
			Material[] materials = renderer.materials;
			foreach (Material material in materials)
			{
				if ((bool)material && string.Equals(material.shader.name, shader.name, StringComparison.InvariantCultureIgnoreCase) && material.HasTexture(MainTexture) && material.HasFloat(Layer))
				{
					list.Add(material);
				}
			}
		}
		Dictionary<VMFLayer, IGrouping<VMFLayer, VMFLayerMaterial>> dictionary = (from v in componentsInChildren.AsValueEnumerable()
			where v
			group v by v.layer).ToDictionary((IGrouping<VMFLayer, VMFLayerMaterial> g) => g.Key, (IGrouping<VMFLayer, VMFLayerMaterial> g) => g);
		int num = 0;
		foreach (KeyValuePair<VMFLayer, RoomTexture> modifierTexture in modifierTextures)
		{
			if (num >= 4)
			{
				break;
			}
			VMFLayer key = modifierTexture.Key;
			List<Texture2D> texture = modifierTexture.Value.texture;
			byte b = (byte)((encodedTextures >> num * 8) & 0xFF);
			if (texture != null && texture.Count > 0 && b < texture.Count)
			{
				Texture2D texture2D = texture[b];
				if ((bool)texture2D)
				{
					float b2 = (int)key;
					foreach (Material item in list)
					{
						if (Mathf.Approximately(item.GetFloat(Layer), b2))
						{
							item.SetTexture(MainTexture, texture2D);
						}
					}
				}
			}
			if (dictionary.TryGetValue(key, out var value))
			{
				foreach (VMFLayerMaterial item2 in value)
				{
					item2.materialType = modifierTexture.Value.material;
				}
			}
			num++;
		}
	}

	protected override void __initializeVariables()
	{
		if (_combinedLayerData == null)
		{
			throw new Exception("entity_room_base._combinedLayerData cannot be null. All NetworkVariableBase instances must be initialized.");
		}
		_combinedLayerData.Initialize(this);
		__nameNetworkVariable(_combinedLayerData, "_combinedLayerData");
		NetworkVariableFields.Add(_combinedLayerData);
		base.__initializeVariables();
	}

	protected override void __initializeRpcs()
	{
		base.__initializeRpcs();
	}

	protected internal override string __getTypeName()
	{
		return "entity_room_base";
	}
}
