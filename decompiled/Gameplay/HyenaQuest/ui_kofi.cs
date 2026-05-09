using System;
using TMPro;
using UnityEngine;

namespace HyenaQuest;

public class ui_kofi : MonoBehaviour
{
	public static readonly float SCROLL_SPEED = 300f;

	public TextMeshProUGUI creditsText;

	public TextMeshProUGUI titleText;

	private Vector3 _originalPosition;

	public void Awake()
	{
		if (!creditsText)
		{
			throw new UnityException("Missing TextMeshProUGUI component for creditsText");
		}
		if (!titleText)
		{
			throw new UnityException("Missing TextMeshProUGUI component for titleText");
		}
		CoreController.WaitFor(delegate(KOFIController ctrl)
		{
			ctrl.OnPatronsLoaded += new Action(OnPatronsLoaded);
		});
	}

	public void OnDestroy()
	{
		if ((bool)MonoController<KOFIController>.Instance)
		{
			MonoController<KOFIController>.Instance.OnPatronsLoaded -= new Action(OnPatronsLoaded);
		}
	}

	public void Update()
	{
		if ((bool)creditsText)
		{
			Vector3 localPosition = creditsText.transform.localPosition;
			localPosition.y += SCROLL_SPEED * Time.deltaTime;
			creditsText.transform.localPosition = localPosition;
			float preferredHeight = creditsText.preferredHeight;
			if (localPosition.y >= preferredHeight)
			{
				creditsText.transform.localPosition = _originalPosition;
			}
		}
	}

	private void OnPatronsLoaded()
	{
		if ((bool)creditsText)
		{
			creditsText.text = MonoController<KOFIController>.Instance?.GetIngameCredits() ?? "";
			creditsText.ForceMeshUpdate();
			RectTransform rectTransform = creditsText.rectTransform;
			rectTransform.sizeDelta = new Vector2(rectTransform.sizeDelta.x, creditsText.preferredHeight);
			creditsText.transform.localPosition = _originalPosition;
		}
	}
}
