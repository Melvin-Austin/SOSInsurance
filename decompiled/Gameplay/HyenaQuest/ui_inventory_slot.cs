using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Rendering;
using UnityEngine.UI;

namespace HyenaQuest;

[RequireComponent(typeof(Image))]
[DisallowMultipleComponent]
public class ui_inventory_slot : MonoBehaviour
{
	private static readonly int BaseColor = Shader.PropertyToID("_BaseColor");

	private static readonly int Color = Shader.PropertyToID("_Color");

	private static readonly int QueueOffset = Shader.PropertyToID("_QueueOffset");

	public Sprite normal;

	public Sprite selected;

	private Image _renderer;

	private Transform _preview;

	private MeshFilter _meshFilter;

	private MeshRenderer _meshRenderer;

	private Shader _normalUnlitshader;

	private Shader _vmfShader;

	private Shader _vmfUnlitUIShader;

	private readonly List<Material> _clonedMats = new List<Material>();

	public void Awake()
	{
		if (!normal)
		{
			throw new UnityException("Missing normal");
		}
		if (!selected)
		{
			throw new UnityException("Missing selected");
		}
		_renderer = GetComponent<Image>();
		_preview = base.transform.GetChild(0);
		_meshRenderer = GetComponentInChildren<MeshRenderer>(includeInactive: true);
		if (!_meshRenderer)
		{
			throw new UnityException("Missing MeshRenderer");
		}
		_meshFilter = GetComponentInChildren<MeshFilter>(includeInactive: true);
		if (!_meshFilter)
		{
			throw new UnityException("Missing MeshFilter");
		}
		_normalUnlitshader = Shader.Find("Universal Render Pipeline/Unlit");
		if (!_normalUnlitshader)
		{
			throw new UnityException("Missing shader 'Universal Render Pipeline/Unlit'");
		}
		_vmfShader = Shader.Find("FailCake/VMF/VMFLit");
		if (!_vmfShader)
		{
			throw new UnityException("Missing shader 'FailCake/VMF/VMFLit'");
		}
		_vmfUnlitUIShader = Shader.Find("HyenaQuest/VMF_UI_UNLIT");
		if (!_vmfUnlitUIShader)
		{
			throw new UnityException("Missing shader 'HyenaQuest/VMF_UI_UNLIT'");
		}
	}

	public void SetSelected(bool select)
	{
		_renderer.sprite = (select ? selected : normal);
	}

	public void SetItem(entity_item_pickable prop)
	{
		Cleanup();
		_meshRenderer.enabled = prop;
		if (!prop)
		{
			return;
		}
		switch (prop.previewSquish)
		{
		case Axis.X:
			_preview.localScale = new Vector3(0.1f, prop.previewSize.y, prop.previewSize.z);
			break;
		case Axis.Y:
			_preview.localScale = new Vector3(prop.previewSize.x, 0.1f, prop.previewSize.z);
			break;
		default:
			_preview.localScale = new Vector3(prop.previewSize.x, prop.previewSize.y, 0.1f);
			break;
		}
		_preview.localEulerAngles = prop.previewAngle;
		_preview.localPosition = prop.previewPosition;
		_meshRenderer.receiveShadows = false;
		_meshRenderer.shadowCastingMode = ShadowCastingMode.Off;
		_meshFilter.sharedMesh = prop.GetMesh();
		Material[] materials = prop.GetMaterials();
		if (materials == null || materials.Length == 0)
		{
			return;
		}
		for (int i = 0; i < materials.Length; i++)
		{
			Material material = materials[i];
			if ((bool)material)
			{
				bool flag = material.shader == _vmfShader;
				Material material2 = new Material(flag ? _vmfUnlitUIShader : _normalUnlitshader)
				{
					mainTexture = material.mainTexture
				};
				if (!material2)
				{
					return;
				}
				material2.renderQueue = 3000;
				if (!flag)
				{
					material2.SetOverrideTag("RenderType", "Transparent");
				}
				material2.SetFloat(QueueOffset, i);
				float num = 3f;
				if (flag)
				{
					num += 9f;
				}
				if (material2.HasColor(BaseColor))
				{
					material2.SetColor(BaseColor, material.GetColor(BaseColor) * num);
				}
				else if (material2.HasColor(Color))
				{
					material2.SetColor(Color, material.GetColor(Color) * num);
				}
				_clonedMats.Add(material2);
			}
		}
		_meshRenderer.materials = _clonedMats.ToArray();
	}

	public void OnDestroy()
	{
		Cleanup();
	}

	private void Cleanup()
	{
		foreach (Material clonedMat in _clonedMats)
		{
			Object.Destroy(clonedMat);
		}
		_clonedMats.Clear();
	}
}
