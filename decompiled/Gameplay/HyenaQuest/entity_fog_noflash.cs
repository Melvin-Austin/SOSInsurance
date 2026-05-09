using FailCake;
using UnityEngine;

namespace HyenaQuest;

public class entity_fog_noflash : MonoBehaviour
{
	public Vector3 actualPosition;

	public Vector3 safeSpawnArea;

	private util_timer _spawnTimer;

	public void Awake()
	{
		base.transform.position = safeSpawnArea;
		_spawnTimer?.Stop();
		_spawnTimer = util_timer.Simple(1f, delegate
		{
			base.transform.localPosition = actualPosition;
		});
	}

	public void OnDestroy()
	{
		_spawnTimer?.Stop();
	}
}
