using System.Collections.Generic;
using UnityEngine;

namespace HyenaQuest;

public class entity_mesh_preview : MonoBehaviour
{
	private static readonly int BaseColor = Shader.PropertyToID("_BaseColor");

	private static readonly int Color = Shader.PropertyToID("_Color");

	public float size = 7.5f;

	public bool isTransparent;

	private MeshRenderer _renderer;

	private MeshFilter _filter;

	private readonly List<Material> _materialPool = new List<Material>();

	private Shader _normalUnlitShader;

	public void Awake()
	{
		_renderer = GetComponentInChildren<MeshRenderer>(includeInactive: true);
		if (!_renderer)
		{
			throw new UnityException("Missing MeshRenderer");
		}
		_renderer.enabled = false;
		_filter = GetComponentInChildren<MeshFilter>(includeInactive: true);
		if (!_filter)
		{
			throw new UnityException("Missing MeshFilter");
		}
		_normalUnlitShader = Shader.Find("Universal Render Pipeline/Unlit");
		if (!_normalUnlitShader)
		{
			throw new UnityException("Missing shader 'Universal Render Pipeline/Unlit'!");
		}
	}

	public void OnDestroy()
	{
		Cleanup();
	}

	public void SetMesh(MeshRenderer render, MeshFilter filter)
	{
		if (!render || !filter)
		{
			_renderer.enabled = false;
			return;
		}
		_filter.mesh = filter.sharedMesh;
		Material[] sharedMaterials = render.sharedMaterials;
		if (sharedMaterials == null || sharedMaterials.Length == 0)
		{
			_renderer.enabled = false;
			return;
		}
		int num = 0;
		Material[] array = sharedMaterials;
		foreach (Material material in array)
		{
			if ((bool)material)
			{
				Material material2;
				if (num < _materialPool.Count)
				{
					material2 = _materialPool[num];
				}
				else
				{
					material2 = new Material(_normalUnlitShader);
					_materialPool.Add(material2);
				}
				material2.mainTexture = material.mainTexture;
				if (isTransparent)
				{
					material2.SetOverrideTag("RenderType", "Transparent");
					material2.renderQueue = 3000;
				}
				if (material2.HasColor(BaseColor) && material.HasColor(BaseColor))
				{
					material2.SetColor(BaseColor, material.GetColor(BaseColor) * 1.2f);
				}
				else if (material2.HasColor(Color) && material.HasColor(Color))
				{
					material2.SetColor(Color, material.GetColor(Color) * 1.2f);
				}
				num++;
			}
		}
		Material[] array2 = new Material[num];
		for (int j = 0; j < num; j++)
		{
			array2[j] = _materialPool[j];
		}
		_renderer.materials = array2;
		_renderer.enabled = true;
		FitToSize();
	}

	private void FitToSize()
	{
		if ((bool)_filter?.mesh)
		{
			Bounds bounds = _filter.mesh.bounds;
			float magnitude = bounds.extents.magnitude;
			if (!(magnitude <= 0f))
			{
				float num = size * 0.5f / magnitude;
				_filter.transform.localScale = Vector3.one * num;
				Vector3 localPosition = -bounds.center * num;
				_filter.transform.localPosition = localPosition;
			}
		}
	}

	private void Cleanup()
	{
		foreach (Material item in _materialPool)
		{
			if ((bool)item)
			{
				Object.Destroy(item);
			}
		}
		_materialPool.Clear();
	}
}
