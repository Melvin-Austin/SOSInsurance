using UnityEngine;

namespace HyenaQuest;

public class entity_monster_eyes : MonoBehaviour
{
	public GameObject eyes;

	public GameObject pupilL;

	public GameObject pupilR;

	public Vector2 area;

	public Vector2 eyeBounds;

	public Vector2 pupilBounds;

	private float _shakeAgreeTimer;

	private float _shakeDisagreeTimer;

	private Vector3 _targetEyePosition;

	private Vector3 _originalEyePosition;

	private Vector3 _originalPupilLPosition;

	private Vector3 _originalPupilRPosition;

	public void Awake()
	{
		if (!eyes)
		{
			throw new UnityException("Missing eyes");
		}
		if (!pupilL)
		{
			throw new UnityException("Missing pupilL");
		}
		if (!pupilR)
		{
			throw new UnityException("Missing pupilR");
		}
		_originalEyePosition = eyes.transform.localPosition;
		_targetEyePosition = _originalEyePosition;
		_originalPupilLPosition = pupilL.transform.localPosition;
		_originalPupilRPosition = pupilR.transform.localPosition;
	}

	public void Update()
	{
		FollowNearPlayer();
		UpdatePupils();
		UpdateShake();
	}

	public void Agree()
	{
		_shakeAgreeTimer = Time.time + Random.Range(0.3f, 0.8f);
	}

	public void Disagree()
	{
		_shakeDisagreeTimer = Time.time + Random.Range(0.3f, 0.8f);
	}

	private void FollowNearPlayer()
	{
		entity_player lOCAL = PlayerController.LOCAL;
		Transform transform = (eyes.transform.parent ? eyes.transform.parent : base.transform);
		if (!lOCAL)
		{
			_targetEyePosition = _originalEyePosition;
			eyes.transform.localPosition = Vector3.Lerp(eyes.transform.localPosition, _targetEyePosition, Time.deltaTime * 5f);
			return;
		}
		Vector3 normalized = (lOCAL.view.transform.position - base.transform.position).normalized;
		Vector3 vector = transform.InverseTransformDirection(normalized);
		Vector3 targetEyePosition = _originalEyePosition + new Vector3(vector.x * eyeBounds.x / 2f, vector.y * eyeBounds.y / 2f, 0f);
		_targetEyePosition = targetEyePosition;
		eyes.transform.localPosition = Vector3.Lerp(eyes.transform.localPosition, _targetEyePosition, Time.deltaTime * 8f);
	}

	private void UpdatePupils()
	{
		entity_player lOCAL = PlayerController.LOCAL;
		if (!lOCAL)
		{
			pupilL.transform.localPosition = Vector3.Lerp(pupilL.transform.localPosition, _originalPupilLPosition, Time.deltaTime * 8f);
			pupilR.transform.localPosition = Vector3.Lerp(pupilR.transform.localPosition, _originalPupilRPosition, Time.deltaTime * 8f);
			return;
		}
		Vector3 normalized = (lOCAL.view.transform.position - eyes.transform.position).normalized;
		Vector3 vector = eyes.transform.InverseTransformDirection(normalized);
		Vector3 vector2 = new Vector3(vector.x * pupilBounds.x / 2f, vector.y * pupilBounds.y / 2f, 0f);
		vector2.x = Mathf.Clamp(vector2.x, (0f - pupilBounds.x) / 2f, pupilBounds.x / 2f);
		vector2.y = Mathf.Clamp(vector2.y, (0f - pupilBounds.y) / 2f, pupilBounds.y / 2f);
		vector2.y += 0.05f;
		Vector3 vector3 = vector2;
		pupilL.transform.localPosition = Vector3.Lerp(pupilL.transform.localPosition, _originalPupilLPosition + vector3, Time.deltaTime * 8f);
		Vector3 vector4 = vector2;
		pupilR.transform.localPosition = Vector3.Lerp(pupilR.transform.localPosition, _originalPupilRPosition + vector4, Time.deltaTime * 8f);
	}

	private void UpdateShake()
	{
		if (Time.time < _shakeAgreeTimer)
		{
			float y = Mathf.Sin(Time.time * 26f) * 0.01f;
			Vector3 vector = Vector3.Lerp(eyes.transform.localPosition, _targetEyePosition, Time.deltaTime * 8f);
			eyes.transform.localPosition = vector + new Vector3(0f, y, 0f);
		}
		if (Time.time < _shakeDisagreeTimer)
		{
			float x = Mathf.Sin(Time.time * 26f) * 0.01f;
			Vector3 vector2 = Vector3.Lerp(eyes.transform.localPosition, _targetEyePosition, Time.deltaTime * 8f);
			eyes.transform.localPosition = vector2 + new Vector3(x, 0f, 0f);
		}
	}
}
