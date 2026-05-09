using UnityEngine;

namespace HyenaQuest;

public class entity_button_emission : entity_button
{
	private static readonly int EmissionColor = Shader.PropertyToID("_EmissionColor");

	public MeshRenderer meshRenderer;

	[Range(0f, 255f)]
	public int materialIndex;

	[ColorUsage(true, true)]
	public Color activeColor;

	[ColorUsage(true, true)]
	public Color disabledColor;

	protected override void OnNetworkPostSpawn()
	{
		base.OnNetworkPostSpawn();
		if (!base.IsClient)
		{
			return;
		}
		locked.RegisterOnValueChanged(delegate(bool oldValue, bool newValue)
		{
			if (oldValue != newValue)
			{
				meshRenderer.materials[materialIndex].SetColor(EmissionColor, newValue ? activeColor : disabledColor);
			}
		});
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
		return "entity_button_emission";
	}
}
