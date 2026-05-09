using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace HyenaQuest;

public class ui_outfit_list : MonoBehaviour
{
	public GameObject outfitPrefab;

	public GameObject outfitColorPrefab;

	public Sprite lockedSprite;

	public Sprite noneSprite;

	public Button speciesButton;

	public Button suitsButton;

	public Button hatButton;

	public Button googlesButton;

	public Button neckButton;

	public Button tailButton;

	public Button maskButton;

	public Transform speciesContainer;

	public Transform suitsContainer;

	public Transform hatContainer;

	public Transform googlesContainer;

	public Transform neckContainer;

	public Transform tailContainer;

	public Transform maskContainer;

	public List<SpeciesButton> speciesButtons = new List<SpeciesButton>();

	private readonly List<Button> _suitSlots = new List<Button>();

	private readonly Dictionary<ACCESSORY_TYPE, List<ui_outfit>> _slots = new Dictionary<ACCESSORY_TYPE, List<ui_outfit>>();

	private static readonly (ACCESSORY_TYPE type, int tabIndex)[] ACCESSORY_TABS = new(ACCESSORY_TYPE, int)[5]
	{
		(ACCESSORY_TYPE.HAT, 2),
		(ACCESSORY_TYPE.GOOGLES, 3),
		(ACCESSORY_TYPE.NECK, 4),
		(ACCESSORY_TYPE.TAIL, 5),
		(ACCESSORY_TYPE.MASK, 6)
	};

	private (Button button, Transform container)[] _tabs;

	private int _activeTab;

	public void Awake()
	{
		if (!outfitColorPrefab)
		{
			throw new UnityException("outfitPrefab not found");
		}
		if (!outfitPrefab)
		{
			throw new UnityException("outfitColorPrefab not found");
		}
		if (!lockedSprite)
		{
			throw new UnityException("lockedSprite not found");
		}
		if (!noneSprite)
		{
			throw new UnityException("noneSprite not found");
		}
		if (!speciesContainer)
		{
			throw new UnityException("speciesContainer not found");
		}
		if (!suitsContainer)
		{
			throw new UnityException("suitsContainer not found");
		}
		if (!hatContainer)
		{
			throw new UnityException("hatContainer not found");
		}
		if (!googlesContainer)
		{
			throw new UnityException("googlesContainer not found");
		}
		if (!neckContainer)
		{
			throw new UnityException("neckContainer not found");
		}
		if (!tailContainer)
		{
			throw new UnityException("tailContainer not found");
		}
		if (!maskContainer)
		{
			throw new UnityException("maskContainer not found");
		}
		if (!speciesButton)
		{
			throw new UnityException("speciesButton not found");
		}
		if (!suitsButton)
		{
			throw new UnityException("suitsButton not found");
		}
		if (!hatButton)
		{
			throw new UnityException("hatButton not found");
		}
		if (!googlesButton)
		{
			throw new UnityException("googlesButton not found");
		}
		if (!neckButton)
		{
			throw new UnityException("neckButton not found");
		}
		if (!tailButton)
		{
			throw new UnityException("tailButton not found");
		}
		if (!maskButton)
		{
			throw new UnityException("maskButton not found");
		}
		_tabs = new(Button, Transform)[7]
		{
			(speciesButton, speciesContainer),
			(suitsButton, suitsContainer),
			(hatButton, hatContainer),
			(googlesButton, googlesContainer),
			(neckButton, neckContainer),
			(tailButton, tailContainer),
			(maskButton, maskContainer)
		};
		for (int i = 0; i < _tabs.Length; i++)
		{
			int tabIndex = i;
			_tabs[i].button.onClick.AddListener(delegate
			{
				SetTab(tabIndex);
			});
		}
		for (int j = 0; j < speciesButtons.Count; j++)
		{
			if ((bool)speciesButtons[j].button)
			{
				int i2 = j;
				speciesButtons[j].button.onClick.RemoveAllListeners();
				speciesButtons[j].button.onClick.AddListener(delegate
				{
					OnSpeciesChanged(i2);
				});
			}
		}
	}

	public void OnDestroy()
	{
		if (_tabs == null)
		{
			return;
		}
		(Button, Transform)[] tabs = _tabs;
		for (int i = 0; i < tabs.Length; i++)
		{
			Button item = tabs[i].Item1;
			if ((bool)item)
			{
				item.onClick.RemoveAllListeners();
			}
		}
	}

	public void OnEnable()
	{
		if ((bool)PlayerController.LOCAL)
		{
			ClearSlots();
			BuildSuits();
			(ACCESSORY_TYPE, int)[] aCCESSORY_TABS = ACCESSORY_TABS;
			for (int i = 0; i < aCCESSORY_TABS.Length; i++)
			{
				var (type, num) = aCCESSORY_TABS[i];
				BuildRow(type, _tabs[num].container);
			}
			SetTab(_activeTab);
			UpdateSpeciesButtons();
		}
	}

	public void OnDisable()
	{
		if ((bool)PlayerController.LOCAL)
		{
			SetTab(0);
		}
	}

	private void ClearSlots()
	{
		foreach (KeyValuePair<ACCESSORY_TYPE, List<ui_outfit>> slot in _slots)
		{
			foreach (ui_outfit item in slot.Value)
			{
				if ((bool)item)
				{
					Object.Destroy(item.gameObject);
				}
			}
			slot.Value.Clear();
		}
	}

	private void BuildSuits()
	{
		if (_suitSlots.Count != 0 || !PlayerController.LOCAL)
		{
			return;
		}
		List<PLAYER_JUMPSUITS> jumpsuitMaterials = PlayerController.LOCAL.jumpsuitMaterials;
		for (int i = 0; i < jumpsuitMaterials.Count - 1; i++)
		{
			GameObject obj = Object.Instantiate(outfitColorPrefab, suitsContainer);
			if (!obj)
			{
				throw new UnityException("Failed to instance suit");
			}
			Button component = obj.GetComponent<Button>();
			if (!component || !(component.targetGraphic is Image image))
			{
				throw new UnityException("Missing Button");
			}
			image.color = jumpsuitMaterials[i].color;
			int i2 = i;
			component.onClick.RemoveAllListeners();
			component.onClick.AddListener(delegate
			{
				if ((bool)PlayerController.LOCAL)
				{
					PlayerController.LOCAL.SetSuitSkin((byte)i2);
					RefreshSuits();
				}
			});
			_suitSlots.Add(component);
		}
		RefreshSuits();
	}

	private void RefreshSuits()
	{
		if ((bool)PlayerController.LOCAL)
		{
			byte suitSkin = PlayerController.LOCAL.SuitSkin;
			for (int i = 0; i < _suitSlots.Count; i++)
			{
				ColorBlock colors = _suitSlots[i].colors;
				colors.normalColor = ((i == suitSkin) ? Color.white : new Color(0.14f, 0.14f, 0.14f));
				_suitSlots[i].colors = colors;
			}
		}
	}

	private void UpdateSpeciesButtons()
	{
		PlayerSpecies species = PlayerController.LOCAL.GetSpecies();
		for (int i = 0; i < speciesButtons.Count; i++)
		{
			Button button = speciesButtons[i].button;
			Image icon = speciesButtons[i].icon;
			if ((bool)button && (bool)icon)
			{
				bool flag2 = (button.interactable = PlayerController.LOCAL.CanPlaySpecies((PlayerSpecies)i));
				icon.sprite = (flag2 ? speciesButtons[i].species : lockedSprite);
				ColorBlock colors = button.colors;
				colors.normalColor = ((i == (int)species) ? Color.white : new Color(0.14f, 0.14f, 0.14f));
				button.colors = colors;
			}
		}
	}

	private void SetTab(int index)
	{
		if (!PlayerController.LOCAL)
		{
			return;
		}
		_activeTab = index;
		PlayerController.LOCAL.OutfitModeFlip(index == 5);
		for (int i = 0; i < _tabs.Length; i++)
		{
			bool flag = i == index;
			_tabs[i].container.gameObject.SetActive(flag);
			_tabs[i].button.image.color = (flag ? new Color(0.32f, 0.32f, 0.32f) : new Color(0.14f, 0.14f, 0.14f));
		}
		(ACCESSORY_TYPE, int)[] aCCESSORY_TABS = ACCESSORY_TABS;
		for (int j = 0; j < aCCESSORY_TABS.Length; j++)
		{
			(ACCESSORY_TYPE, int) tuple = aCCESSORY_TABS[j];
			var (type, _) = tuple;
			if (tuple.Item2 == index)
			{
				RefreshRow(type);
			}
		}
	}

	private void OnSpeciesChanged(int indx)
	{
		if ((bool)PlayerController.LOCAL)
		{
			PlayerSpecies species = (PlayerSpecies)indx;
			if (PlayerController.LOCAL.CanPlaySpecies(species))
			{
				PlayerController.LOCAL.SpeciesTF(species);
				UpdateSpeciesButtons();
			}
		}
	}

	private static bool IsSelected(AccessoryData item, byte raw)
	{
		if (item.index == 31)
		{
			if (raw != 31)
			{
				return raw == 0;
			}
			return true;
		}
		if (raw != 31 && raw != 0)
		{
			return item.index == raw - 1;
		}
		return false;
	}

	private void RefreshRow(ACCESSORY_TYPE type)
	{
		if (!PlayerController.LOCAL || !_slots.TryGetValue(type, out var value))
		{
			return;
		}
		List<AccessoryData> allAccessories = PlayerController.LOCAL.GetAllAccessories(type);
		if (allAccessories != null && allAccessories.Count > 0)
		{
			byte accessoryChoice = PlayerController.LOCAL.GetAccessoryChoice(type);
			for (int i = 0; i < value.Count; i++)
			{
				value[i].SetSelected(IsSelected(allAccessories[i], accessoryChoice));
				value[i].SetAccessory(allAccessories[i], lockedSprite, noneSprite);
			}
		}
	}

	private void BuildRow(ACCESSORY_TYPE type, Transform container)
	{
		if (!container)
		{
			throw new UnityException("Missing container");
		}
		if (!_slots.ContainsKey(type))
		{
			_slots.Add(type, new List<ui_outfit>());
		}
		List<AccessoryData> allAccessories = PlayerController.LOCAL.GetAllAccessories(type);
		for (int i = 0; i < allAccessories.Count; i++)
		{
			GameObject obj = Object.Instantiate(outfitPrefab, container);
			if (!obj)
			{
				throw new UnityException("Failed to instance outfit");
			}
			ui_outfit component = obj.GetComponent<ui_outfit>();
			if (!component)
			{
				throw new UnityException("Missing ui_outfit");
			}
			AccessoryData capturedItem = allAccessories[i];
			int i2 = i;
			component.button.onClick.RemoveAllListeners();
			component.button.onClick.AddListener(delegate
			{
				if ((bool)PlayerController.LOCAL)
				{
					PlayerController.LOCAL.SetAccessory(capturedItem.type, (byte)capturedItem.index);
					UpdateSelected(type, i2);
				}
			});
			_slots[type].Add(component);
		}
	}

	private void UpdateSelected(ACCESSORY_TYPE type, int index)
	{
		if (_slots.TryGetValue(type, out var value))
		{
			for (int i = 0; i < value.Count; i++)
			{
				value[i].SetSelected(i == index);
			}
		}
	}
}
