using UnityEngine;

namespace HyenaQuest;

[RequireComponent(typeof(Collider))]
public class entity_client_usable : MonoBehaviour
{
	public Interaction interactionType;

	public string hint = "USE";

	[Range(0f, 6f)]
	public float clickDistance = 2f;

	public GameEvent<entity_player> OnUse = new GameEvent<entity_player>();

	private Collider _collider;

	private bool _isLocked;

	public void Awake()
	{
		_collider = GetComponent<Collider>();
		if (!_collider)
		{
			_collider = GetComponentInChildren<Collider>();
		}
		if (!_collider)
		{
			throw new UnityException("Collider missing in entity_fake_usable");
		}
	}

	public InteractionData InteractionSelector(Collider _)
	{
		if (!_isLocked)
		{
			return new InteractionData(interactionType, new Bounds[1] { _collider.bounds }, hint);
		}
		return null;
	}

	public void SetLocked(bool state)
	{
		_isLocked = state;
	}

	[Client]
	public virtual void OnPlayerUse(entity_player player)
	{
		if ((bool)player && !IsLocked())
		{
			OnUse?.Invoke(player);
		}
	}

	public bool IsLocked()
	{
		return _isLocked;
	}
}
