using UnityEngine;

namespace HyenaQuest;

public class entity_animator_random_offset : StateMachineBehaviour
{
	public float minOffset = 0.3f;

	public float maxOffset = 1f;

	public static int playedState;

	public override void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
	{
		if (!(stateInfo.normalizedTime > 0.001f) && playedState != stateInfo.fullPathHash)
		{
			float normalizedTime = Random.Range(minOffset, maxOffset);
			animator.Play(stateInfo.fullPathHash, layerIndex, normalizedTime);
			playedState = stateInfo.fullPathHash;
		}
	}
}
