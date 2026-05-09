using System.Collections.Generic;
using UnityEngine;

namespace HyenaQuest;

public class entity_player_vacuum_trigger : MonoBehaviour
{
	private readonly HashSet<entity_phys_prop_scrap> _contents = new HashSet<entity_phys_prop_scrap>();

	private int _layer;

	private MeshCollider _collider;

	public void Awake()
	{
		_layer = LayerMask.NameToLayer("entity_phys");
	}

	public void RemoveDead()
	{
		_contents.RemoveWhere((entity_phys_prop_scrap s) => !s);
	}

	public HashSet<entity_phys_prop_scrap> GetContents()
	{
		return _contents;
	}

	private void OnTriggerEnter(Collider other)
	{
		if ((bool)other && (bool)other.gameObject && other.gameObject.layer == _layer && other.TryGetComponent<entity_phys_prop_scrap>(out var component, 1))
		{
			_contents.Add(component);
		}
	}

	private void OnTriggerExit(Collider other)
	{
		if ((bool)other && (bool)other.gameObject && other.gameObject.layer == _layer && other.TryGetComponent<entity_phys_prop_scrap>(out var component, 1))
		{
			_contents.Remove(component);
		}
	}
}
