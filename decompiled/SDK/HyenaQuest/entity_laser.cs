using UnityEngine;

namespace HyenaQuest;

[RequireComponent(typeof(LineRenderer))]
public class entity_laser : MonoBehaviour
{
	public GameObject hitEnd;

	private LineRenderer _lineRenderer;

	private int _layerMask;

	public void Awake()
	{
		if (!hitEnd)
		{
			throw new UnityException("entity_laser requires a hitEnd GameObject to work.");
		}
		_lineRenderer = GetComponent<LineRenderer>();
		if (!_lineRenderer)
		{
			throw new UnityException("entity_laser requires a LineRenderer component to work.");
		}
		_lineRenderer.tag = "OCCLUDER/IGNORE";
		_lineRenderer.useWorldSpace = true;
		_lineRenderer.positionCount = 2;
		_layerMask = LayerMask.GetMask("entity_phys", "entity_ground", "entity_player", "entity_enemy", "entity_phys_item");
	}

	public void Update()
	{
		if (!_lineRenderer || !hitEnd)
		{
			return;
		}
		bool flag = (SDK.GetCurrentRound?.Invoke() ?? 1) >= 2;
		_lineRenderer.enabled = flag;
		hitEnd.SetActive(flag);
		if (!flag)
		{
			return;
		}
		Vector3 position = base.transform.position;
		Vector3 vector = position + base.transform.forward * 1000f;
		if (Physics.Linecast(position, vector, out var hitInfo, _layerMask))
		{
			vector = hitInfo.point;
			if ((bool)hitInfo.rigidbody && hitInfo.collider.gameObject.CompareTag("Player"))
			{
				SDK.OnKillRequest?.Invoke(DamageType.ELECTRIC_ASHES, hitInfo.collider);
			}
		}
		_lineRenderer.SetPosition(0, position);
		_lineRenderer.SetPosition(1, vector);
		hitEnd.transform.position = vector + Vector3.left * 0.01f;
	}

	private void OnRoundUpdate(byte round, bool server)
	{
		bool active = round >= 2;
		_lineRenderer.enabled = active;
		hitEnd.SetActive(active);
	}
}
