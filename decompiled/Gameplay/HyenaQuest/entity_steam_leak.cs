using Unity.Netcode;
using UnityEngine;
using UnityEngine.VFX;

namespace HyenaQuest;

public class entity_steam_leak : NetworkBehaviour
{
	public GameObject leak;

	private int _layer;

	private VisualEffect _vfx;

	private readonly RaycastHit[] _hits = new RaycastHit[1];

	public void Awake()
	{
		if (!leak)
		{
			throw new UnityException("Missing leak");
		}
		_layer = LayerMask.GetMask("entity_phys", "entity_phys_item");
		_vfx = leak.GetComponent<VisualEffect>();
		if ((bool)_vfx)
		{
			throw new UnityException("Missing VisualEffect");
		}
	}

	public void Update()
	{
		if (base.IsClient)
		{
			_ = base.transform.position;
			Vector3 halfExtents = Vector3.one * 0.05f;
			float maxDistance = 1f;
			int num = Physics.BoxCastNonAlloc(base.transform.position, halfExtents, -base.transform.up, _hits, base.transform.rotation, maxDistance, _layer, QueryTriggerInteraction.Ignore);
			leak.transform.localScale = new Vector3(leak.transform.localScale.x, (num > 0) ? Mathf.Max(0.01f, _hits[0].distance) : 1f, leak.transform.localScale.z);
		}
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
		return "entity_steam_leak";
	}
}
