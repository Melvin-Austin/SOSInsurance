using System.Collections.Generic;
using UnityEngine;

namespace HyenaQuest;

public class entity_led_material : MonoBehaviour
{
	public MeshRenderer meshRenderer;

	public bool active;

	[ColorUsage(true, true)]
	public Color activeColor;

	[ColorUsage(true, true)]
	public Color disabledColor;

	public int materialSlot;

	private static readonly int ShaderColor = Shader.PropertyToID("_BaseColor");

	public void Awake()
	{
		if (!meshRenderer)
		{
			throw new UnityException("entity_led requires a MeshRenderer component to work.");
		}
		if (meshRenderer.sharedMaterials.Length <= materialSlot)
		{
			throw new UnityException("entity_led requires a material to work.");
		}
		UpdateMaterial();
	}

	public void SetActive(bool enable)
	{
		if (active != enable)
		{
			active = enable;
			UpdateMaterial();
		}
	}

	private void UpdateMaterial()
	{
		if ((bool)meshRenderer)
		{
			List<Material> list = new List<Material>();
			meshRenderer.GetMaterials(list);
			list[materialSlot].SetColor(ShaderColor, active ? activeColor : disabledColor);
			meshRenderer.SetMaterials(list);
		}
	}
}
