using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace HyenaQuest;

public class ui_dx_selector : MonoBehaviour
{
	public static readonly int screenEdgeMargin = 40;

	public RectTransform canvas;

	public TextMeshProUGUI title;

	public Image image;

	private Bounds _screenBounds;

	private Bounds _worldBounds;

	private readonly Vector3[] _boundsCorners = new Vector3[8];

	private BoundsData[] _renderersList;

	private bool _wasHighlighting;

	private bool _boundsDirty = true;

	private Vector3 _lastCameraPosition;

	private Quaternion _lastCameraRotation;

	private readonly float _cameraMovementThreshold = 0.15f;

	private readonly float _rotationThreshold = 0.5f;

	private void Awake()
	{
		if (!canvas)
		{
			throw new UnityException("No image found in ui_dx_selector.");
		}
		if (!title)
		{
			throw new UnityException("No title found in ui_dx_selector.");
		}
		if (!image)
		{
			throw new UnityException("No image found in ui_dx_selector.");
		}
	}

	public void Highlight(BoundsData[] renderers, string hint, Sprite sprite = null)
	{
		_renderersList = renderers;
		title.text = hint;
		image.sprite = sprite;
		image.enabled = sprite;
		_boundsDirty = true;
	}

	public void ClearAll()
	{
		_renderersList = null;
		title.text = string.Empty;
		canvas.gameObject.SetActive(value: false);
		_wasHighlighting = false;
	}

	private void Update()
	{
		if ((bool)SDK.MainCamera && _renderersList != null && _renderersList.Length != 0)
		{
			bool num = Vector3.Distance(SDK.MainCamera.transform.position, _lastCameraPosition) > _cameraMovementThreshold;
			bool flag = Quaternion.Angle(SDK.MainCamera.transform.rotation, _lastCameraRotation) > _rotationThreshold;
			if (num || flag || _boundsDirty)
			{
				CalculateBounds();
				_lastCameraPosition = SDK.MainCamera.transform.position;
				_lastCameraRotation = SDK.MainCamera.transform.rotation;
				_boundsDirty = false;
			}
			UpdateUIRects();
		}
	}

	private void CalculateBounds()
	{
		if (!SDK.MainCamera || _renderersList.Length == 0)
		{
			return;
		}
		_worldBounds = _renderersList[0].GetBounds();
		for (int i = 1; i < _renderersList.Length; i++)
		{
			_worldBounds.Encapsulate(_renderersList[i].GetBounds());
		}
		Vector3 center = _worldBounds.center;
		Vector3 extents = _worldBounds.extents;
		Vector3 vector = SDK.MainCamera.WorldToViewportPoint(center);
		float num = Mathf.Max(extents.x, extents.y, extents.z);
		if (vector.z < 0f || vector.x < 0f - num || vector.x > 1f + num || vector.y < 0f - num || vector.y > 1f + num)
		{
			canvas.gameObject.SetActive(value: false);
			return;
		}
		_boundsCorners[0] = _worldBounds.min;
		_boundsCorners[1] = _worldBounds.max;
		_boundsCorners[2] = new Vector3(_boundsCorners[0].x, _boundsCorners[0].y, _boundsCorners[1].z);
		_boundsCorners[3] = new Vector3(_boundsCorners[0].x, _boundsCorners[1].y, _boundsCorners[0].z);
		_boundsCorners[4] = new Vector3(_boundsCorners[1].x, _boundsCorners[0].y, _boundsCorners[0].z);
		_boundsCorners[5] = new Vector3(_boundsCorners[0].x, _boundsCorners[1].y, _boundsCorners[1].z);
		_boundsCorners[6] = new Vector3(_boundsCorners[1].x, _boundsCorners[0].y, _boundsCorners[1].z);
		_boundsCorners[7] = new Vector3(_boundsCorners[1].x, _boundsCorners[1].y, _boundsCorners[0].z);
		_boundsCorners[0] = SDK.MainCamera.WorldToScreenPoint(_boundsCorners[0]);
		_boundsCorners[0].z = 10f;
		Bounds screenBounds = new Bounds(_boundsCorners[0], Vector3.zero);
		for (int j = 1; j < 8; j++)
		{
			_boundsCorners[j] = SDK.MainCamera.WorldToScreenPoint(_boundsCorners[j]);
			screenBounds.Encapsulate(_boundsCorners[j]);
		}
		Vector3 min = screenBounds.min;
		Vector3 max = screenBounds.max;
		min.x = Mathf.Max(min.x, screenEdgeMargin);
		min.y = Mathf.Max(min.y, screenEdgeMargin);
		max.x = Mathf.Min(max.x, Screen.width - screenEdgeMargin);
		max.y = Mathf.Min(max.y, Screen.height - screenEdgeMargin);
		min.z = (max.z = 0f);
		screenBounds.SetMinMax(min, max);
		_screenBounds = screenBounds;
	}

	private void UpdateUIRects()
	{
		BoundsData[] renderersList = _renderersList;
		bool flag = renderersList != null && renderersList.Length > 0;
		if (flag != _wasHighlighting)
		{
			canvas.gameObject.SetActive(flag);
			title.gameObject.SetActive(flag);
		}
		_wasHighlighting = flag;
		if (flag)
		{
			Vector3 position = new Vector3(Mathf.Round(_screenBounds.center.x), Mathf.Round(_screenBounds.center.y), _screenBounds.center.z);
			canvas.position = position;
			canvas.sizeDelta = _screenBounds.size;
		}
	}
}
