using UnityEngine;
using UnityEngine.Scripting;

namespace HyenaQuest;

[Preserve]
public class entity_room_exit : MonoBehaviour
{
	public Vector3 direction;

	public string biomeID;

	public int order = -1;

	private entity_room _owner;

	public entity_room GetOwner()
	{
		if (!_owner)
		{
			_owner = GetComponentInParent<entity_room>(includeInactive: true);
		}
		return _owner;
	}
}
