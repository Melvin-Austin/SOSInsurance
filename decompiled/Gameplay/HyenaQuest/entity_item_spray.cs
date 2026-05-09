using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace HyenaQuest;

public class entity_item_spray : entity_item_spray_base
{
	private static readonly List<byte> _availableColors = new List<byte>();

	public List<Material> sprayMaterials = new List<Material>();

	private AudioSource _audio;

	private readonly NetVar<byte> _color = new NetVar<byte>(0);

	public override void OnNetworkSpawn()
	{
		base.OnNetworkSpawn();
		if (!base.IsServer)
		{
			return;
		}
		if (_availableColors.Count == 0)
		{
			for (byte b = 0; b < sprayMaterials.Count; b++)
			{
				_availableColors.Add(b);
			}
		}
		byte b2 = (byte)((sprayMaterials.Count > 1) ? _availableColors.OrderBy((byte _) => UnityEngine.Random.value).FirstOrDefault() : 0);
		if (sprayMaterials.Count > 1)
		{
			_availableColors.Remove(b2);
		}
		_color.Value = b2;
	}

	protected override void OnNetworkPostSpawn()
	{
		base.OnNetworkPostSpawn();
		if (!base.IsClient)
		{
			return;
		}
		_spraying.RegisterOnValueChanged(delegate(bool _, bool newVal)
		{
			if ((bool)_audio)
			{
				if (newVal)
				{
					if ((bool)NetController<SoundController>.Instance)
					{
						_audio.clip = NetController<SoundController>.Instance.GetClip($"Ingame/Entities/Spraycan/spray_loop_{UnityEngine.Random.Range(0, 2)}.ogg");
						_audio.Play();
					}
				}
				else
				{
					_audio.Stop();
				}
			}
		});
		_color.RegisterOnValueChanged(delegate(byte _, byte newVal)
		{
			Material material = sprayMaterials[newVal];
			if ((bool)material && (bool)previewMeshRenderer)
			{
				Material[] materials = previewMeshRenderer.materials;
				if (materials != null && materials.Length == 2)
				{
					materials[0].color = material.color;
					previewMeshRenderer.materials = materials;
				}
			}
		});
	}

	public override void OnNetworkPreDespawn()
	{
		base.OnNetworkPreDespawn();
		if (base.IsClient)
		{
			_spraying.OnValueChanged = null;
			_color.OnValueChanged = null;
		}
	}

	[Server]
	public override Dictionary<string, string> Save()
	{
		if (base.IsSpawned && !base.IsServer)
		{
			throw new UnityException("Server only");
		}
		return new Dictionary<string, string> { 
		{
			"color",
			_color.Value.ToString()
		} };
	}

	[Server]
	public override void Load(Dictionary<string, string> data)
	{
		if (base.IsSpawned && !base.IsServer)
		{
			throw new UnityException("Server only");
		}
		if (data.TryGetValue("color", out var value))
		{
			_color.SetSpawnValue(byte.Parse(value));
		}
	}

	public override string GetID()
	{
		return "item_spraycan";
	}

	protected override void Init()
	{
		base.Init();
		_audio = GetComponentInChildren<AudioSource>(includeInactive: true);
		if (!_audio)
		{
			throw new UnityException("Missing AudioSource component");
		}
		_audio.Stop();
	}

	protected override Vector3 GetSpraySize()
	{
		return new Vector3(UnityEngine.Random.Range(0.45f, 0.65f), UnityEngine.Random.Range(0.45f, 0.65f), 0.2f);
	}

	protected override int GetMaxDecals()
	{
		return 100;
	}

	protected override GameObject GetDecalTemplate()
	{
		GameObject obj = new GameObject("Spray");
		DecalProjector decalProjector = obj.AddComponent<DecalProjector>();
		decalProjector.scaleMode = DecalScaleMode.InheritFromHierarchy;
		decalProjector.material = sprayMaterials[_color.Value];
		decalProjector.renderingLayerMask = _renderingLayerMask;
		decalProjector.pivot = Vector3.zero;
		return obj;
	}

	protected override void __initializeVariables()
	{
		if (_color == null)
		{
			throw new Exception("entity_item_spray._color cannot be null. All NetworkVariableBase instances must be initialized.");
		}
		_color.Initialize(this);
		__nameNetworkVariable(_color, "_color");
		NetworkVariableFields.Add(_color);
		base.__initializeVariables();
	}

	protected override void __initializeRpcs()
	{
		base.__initializeRpcs();
	}

	protected internal override string __getTypeName()
	{
		return "entity_item_spray";
	}
}
