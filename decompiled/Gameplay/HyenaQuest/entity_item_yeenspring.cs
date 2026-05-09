using UnityEngine;

namespace HyenaQuest;

public class entity_item_yeenspring : entity_item_pickable
{
	private static readonly int Falling = Animator.StringToHash("Falling");

	public new void LateUpdate()
	{
		base.LateUpdate();
		if ((bool)_ownerPlayer)
		{
			SkinnedMeshRenderer itemRenderer = _ownerPlayer.GetItemRenderer(PlayerItemRenderer.YEEN_SPRINGS);
			if ((bool)itemRenderer)
			{
				Animator animator = _ownerPlayer.GetAnimator();
				itemRenderer.SetBlendShapeWeight(0, ((object)animator != null && animator.GetBool(Falling)) ? 100 : 0);
			}
		}
	}

	public override string GetID()
	{
		return "item_yeenspring";
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
		return "entity_item_yeenspring";
	}
}
