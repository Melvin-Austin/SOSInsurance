using UnityEngine;

namespace HyenaQuest;

public class entity_player_tracker : MonoBehaviour
{
	private const float MALFUNCTION_DURATION = 2f;

	private const float NORMAL_DURATION = 1f;

	private const float CYCLE_DURATION = 3f;

	public GameObject arrow;

	private float _glitchSeed;

	private float _cycleOffset;

	protected void Awake()
	{
		if (!arrow)
		{
			throw new UnityException("Arrow GameObject is not assigned in entity_player_tracker.");
		}
		_glitchSeed = Random.Range(0f, 1000f);
		_cycleOffset = Random.Range(0f, 3f);
	}

	public void LateUpdate()
	{
		if (!PlayerController.LOCAL || !NetController<DeliveryController>.Instance || !NetController<ContractController>.Instance)
		{
			return;
		}
		entity_player_physgun physgun = PlayerController.LOCAL.GetPhysgun();
		if (!physgun)
		{
			return;
		}
		entity_phys grabbingObject = physgun.GetGrabbingObject();
		if (!grabbingObject || !(grabbingObject is entity_prop_delivery entity_prop_delivery2))
		{
			arrow.SetActive(value: false);
			return;
		}
		entity_delivery_spot deliverySpotByAddress = NetController<DeliveryController>.Instance.GetDeliverySpotByAddress(entity_prop_delivery2.GetAddress());
		if (!deliverySpotByAddress)
		{
			arrow.SetActive(value: false);
			return;
		}
		Bounds bounds = grabbingObject.GetBounds();
		Transform transform = grabbingObject.transform;
		arrow.SetActive(value: true);
		arrow.transform.position = new Vector3(transform.position.x, Mathf.Max(bounds.max.y, transform.position.y + bounds.size.y * 0.5f) + 0.05f, transform.position.z);
		Quaternion b = Quaternion.LookRotation((deliverySpotByAddress.transform.position - arrow.transform.position).normalized, Vector3.up) * Quaternion.Euler(90f, 90f, 0f);
		if (NetController<ContractController>.Instance.GetPickedContract().modifiers.HasFlag(ContractModifiers.DELIVERY_MALFUNCTION))
		{
			float time = Time.time;
			if ((time + _cycleOffset) % 3f < 2f)
			{
				float num = (Mathf.PerlinNoise(Mathf.Floor(time * 0.6f + Mathf.PerlinNoise(time * 0.4f, _glitchSeed) * 2f), _glitchSeed) - 0.5f) * 240f;
				float num2 = Mathf.Sin(time * 4f) * 15f + Mathf.Sin(time * 7f) * 8f;
				float num3 = Mathf.Max(0f, Mathf.PerlinNoise(time * 0.5f, _glitchSeed + 10f) - 0.65f) * 2.8f;
				float x = (Mathf.PerlinNoise(time * 50f, _glitchSeed) - 0.5f) * 60f * num3;
				float num4 = (Mathf.PerlinNoise(_glitchSeed, time * 50f) - 0.5f) * 90f * num3;
				float num5 = Mathf.PerlinNoise(time * 0.8f, _glitchSeed + 20f);
				float num6 = ((num5 > 0.75f) ? (time * 400f * (num5 - 0.75f) * 4f) : 0f);
				b *= Quaternion.Euler(x, num + num2 + num4 + num6, 0f);
			}
		}
		arrow.transform.rotation = Quaternion.Slerp(arrow.transform.rotation, b, Time.deltaTime * 12f);
	}
}
