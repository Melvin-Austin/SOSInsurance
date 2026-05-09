using System.Collections;
using UnityEngine;

namespace HyenaQuest;

[DisallowMultipleComponent]
public class LoadingController : MonoBehaviour
{
	public static readonly int TIMEOUT = 50;

	private float _timeout;

	private bool _timedOut;

	public void Awake()
	{
		StartCoroutine(NetInitialize());
	}

	public void Update()
	{
		if (_timeout != 0f && !_timedOut && !(Time.time < _timeout))
		{
			_timedOut = true;
			NETController.Instance?.Disconnect("SERVER TIMEOUT");
		}
	}

	private IEnumerator NetInitialize()
	{
		yield return new WaitUntil(() => NETController.Instance);
		yield return new WaitForSeconds(2.8f);
		_timeout = Time.time + (float)TIMEOUT;
		yield return NETController.Instance?.StartNetwork();
	}
}
