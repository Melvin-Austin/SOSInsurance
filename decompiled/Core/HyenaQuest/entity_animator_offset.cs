using UnityEngine;

namespace HyenaQuest;

public class entity_animator_offset : MonoBehaviour
{
	private static readonly int OFFSETAnim = Animator.StringToHash("OFFSET");

	[Range(0f, 1f)]
	public float offset;

	public bool randomizeOffset = true;

	private Animator _animator;

	public void Awake()
	{
		_animator = GetComponent<Animator>();
		if (!_animator)
		{
			_animator = GetComponentInChildren<Animator>(includeInactive: true);
		}
		if (!_animator)
		{
			throw new UnityException("Missing Animator component");
		}
		_animator.SetFloat(OFFSETAnim, randomizeOffset ? Random.Range(0f, 1f) : offset);
	}

	public void OnEnable()
	{
		if ((bool)_animator)
		{
			_animator.SetFloat(OFFSETAnim, randomizeOffset ? Random.Range(0f, 1f) : offset);
		}
	}
}
