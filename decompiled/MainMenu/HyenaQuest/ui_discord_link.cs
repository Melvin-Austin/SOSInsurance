using TMPro;
using Unity.VectorGraphics;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace HyenaQuest;

public class ui_discord_link : MonoBehaviour
{
	public Button menuButton;

	public GameObject discordOpenCanvas;

	public TextMeshProUGUI discordText;

	public SVGImage discordIcon;

	public GameObject discordCloseCanvas;

	public SVGImage discordCloseIcon;

	public Button serverButton;

	public Button linkButton;

	public TextMeshProUGUI linkButtonText;

	public void Awake()
	{
		if (!discordOpenCanvas)
		{
			throw new UnityException("Missing GameObject for discordOpenCanvas");
		}
		discordOpenCanvas.SetActive(value: true);
		if (!discordCloseCanvas)
		{
			throw new UnityException("Missing GameObject for discordCloseCanvas");
		}
		discordCloseCanvas.SetActive(value: false);
		if (!discordText)
		{
			throw new UnityException("Missing TextMeshProUGUI component for discordText");
		}
		if (!discordIcon)
		{
			throw new UnityException("Missing SVGImage component for discordIcon");
		}
		if (!discordCloseIcon)
		{
			throw new UnityException("Missing SVGImage component for discordCloseIcon");
		}
		if (!menuButton)
		{
			throw new UnityException("Missing Button component for menuButton");
		}
		if (!serverButton)
		{
			throw new UnityException("Missing Button component for serverButton");
		}
		if (!linkButton)
		{
			throw new UnityException("Missing Button component for linkButton");
		}
		if (!linkButtonText)
		{
			throw new UnityException("Missing TextMeshProUGUI component for linkButtonText");
		}
		Object.Destroy(linkButton.gameObject);
		menuButton.onClick.AddListener(OnMenuButtonClicked);
		serverButton.onClick.AddListener(OnServerButtonClicked);
	}

	public void OnDestroy()
	{
		if ((bool)menuButton)
		{
			menuButton.onClick.RemoveAllListeners();
		}
		if ((bool)serverButton)
		{
			serverButton.onClick.RemoveAllListeners();
		}
	}

	public void Update()
	{
		if (!discordOpenCanvas || !discordCloseCanvas)
		{
			return;
		}
		bool flag = ((InputSystemUIInputModule)EventSystem.current.currentInputModule).GetLastRaycastResult(Mouse.current.deviceId).gameObject?.transform.parent?.gameObject == menuButton.gameObject;
		if (discordOpenCanvas.activeInHierarchy)
		{
			if ((bool)discordIcon && (bool)discordText)
			{
				Color color = discordText.color;
				color.a = (flag ? 1f : 0.3f);
				discordText.color = color;
				Color color2 = discordIcon.color;
				color2.a = (flag ? 1f : 0.1f);
				discordIcon.color = color2;
			}
		}
		else if ((bool)discordCloseIcon)
		{
			Color color3 = discordCloseIcon.color;
			color3.a = (flag ? 1f : 0.1f);
			discordCloseIcon.color = color3;
		}
	}

	private void OnServerButtonClicked()
	{
		Application.OpenURL("https://hyena.quest/discord");
	}

	private void OnMenuButtonClicked()
	{
		if ((bool)discordOpenCanvas && (bool)discordCloseCanvas)
		{
			if (!MonoController<MainMenuController>.Instance)
			{
				throw new UnityException("Missing MainMenuController");
			}
			bool activeInHierarchy = discordOpenCanvas.activeInHierarchy;
			discordOpenCanvas.SetActive(!activeInHierarchy);
			discordCloseCanvas.SetActive(activeInHierarchy);
			MonoController<MainMenuController>.Instance.ToggleDiscordMenu(activeInHierarchy);
		}
	}
}
