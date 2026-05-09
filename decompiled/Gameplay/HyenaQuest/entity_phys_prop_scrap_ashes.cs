using UnityEngine;

namespace HyenaQuest;

public class entity_phys_prop_scrap_ashes : entity_phys_prop_scrap
{
	private SkinnedMeshRenderer _skinnedRenderer;

	public override bool CanGrab()
	{
		return false;
	}

	protected override void Init()
	{
		base.Init();
		_skinnedRenderer = GetComponentInChildren<SkinnedMeshRenderer>(includeInactive: true);
		if (!_skinnedRenderer)
		{
			throw new UnityException("SkinnedMeshRenderer component missing on entity_phys_prop_scrap_ashes!");
		}
		_skinnedRenderer.SetBlendShapeWeight(0, Random.Range(0, 100));
		_skinnedRenderer.SetBlendShapeWeight(1, Random.Range(0, 100));
		_skinnedRenderer.SetBlendShapeWeight(2, Random.Range(0, 100));
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
		return "entity_phys_prop_scrap_ashes";
	}
}
