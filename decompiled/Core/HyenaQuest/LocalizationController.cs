using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using ZLinq;

namespace HyenaQuest;

[DisallowMultipleComponent]
[DefaultExecutionOrder(-120)]
public class LocalizationController : MonoController<LocalizationController>
{
	public static readonly Dictionary<LOCALE, string> LOCALE_MAPPING = new Dictionary<LOCALE, string>
	{
		{
			LOCALE.EN,
			"en"
		},
		{
			LOCALE.NL,
			"nl"
		},
		{
			LOCALE.DE,
			"de"
		},
		{
			LOCALE.PT,
			"pt-PT"
		},
		{
			LOCALE.ES,
			"es-ES"
		},
		{
			LOCALE.PIR,
			"pir"
		},
		{
			LOCALE.RU,
			"ru"
		},
		{
			LOCALE.ZH,
			"zh-CN"
		},
		{
			LOCALE.FR,
			"fr"
		},
		{
			LOCALE.IT,
			"it"
		},
		{
			LOCALE.JA,
			"ja"
		},
		{
			LOCALE.CS,
			"cs"
		},
		{
			LOCALE.DA,
			"da"
		},
		{
			LOCALE.EL,
			"el"
		},
		{
			LOCALE.PT_BR,
			"pt-BR"
		},
		{
			LOCALE.TR,
			"tr"
		},
		{
			LOCALE.UK,
			"uk"
		}
	};

	public static readonly Dictionary<string, string> INPUT_MAPPING = new Dictionary<string, string>
	{
		{ "use", "Use" },
		{ "useitem", "Use Item" },
		{ "drop", "Drop" },
		{ "grab", "Grab" },
		{ "zoom", "Zoom" },
		{ "crouch", "Crouch" },
		{ "jump", "Jump" },
		{ "throw", "Throw" },
		{ "rotate", "Rotate Prop" },
		{ "inventory", "Cycle" }
	};

	private readonly Dictionary<string, I18N> _localization = new Dictionary<string, I18N>();

	public new void Awake()
	{
		base.Awake();
		UnityEngine.Object.DontDestroyOnLoad(base.gameObject);
	}

	public void Init()
	{
		LocalizationSettings.SelectedLocaleChanged += SelectedLocaleChanged;
		if (!MonoController<SettingsController>.Instance)
		{
			throw new UnityException("Missing SettingsController");
		}
		MonoController<SettingsController>.Instance.OnSettingsUpdated += new Action(OnSettingsUpdated);
	}

	public new void OnDestroy()
	{
		LocalizationSettings.SelectedLocaleChanged -= SelectedLocaleChanged;
		base.OnDestroy();
	}

	public string Get(string key, Dictionary<string, string> args)
	{
		return LocalizationSettings.StringDatabase.GetLocalizedString(key, new object[1] { args });
	}

	public string Get(string key)
	{
		(string, Dictionary<string, string>) tuple = ParseKey(key);
		return Get(tuple.Item1, tuple.Item2);
	}

	public Locale GetLocale(string language)
	{
		Locale locale = LocalizationSettings.AvailableLocales.GetLocale(language);
		if ((bool)locale)
		{
			return locale;
		}
		throw new UnityException("Invalid locale " + language);
	}

	public void SetLanguage(LOCALE language)
	{
		if (!MonoController<SettingsController>.Instance)
		{
			throw new UnityException("Missing SettingsController");
		}
		PlayerSettings currentSettings = MonoController<SettingsController>.Instance.CurrentSettings;
		currentSettings.localization = language;
		MonoController<SettingsController>.Instance.CurrentSettings = currentSettings;
	}

	public List<string> GetLanguages(bool fullName = false)
	{
		return (from a in LocalizationSettings.AvailableLocales.Locales.AsValueEnumerable()
			select (!fullName) ? a.Identifier.Code : a.LocaleName).ToList();
	}

	public string GetLanguageName(LOCALE locale)
	{
		if (LOCALE_MAPPING.TryGetValue(locale, out var value))
		{
			return LocalizationSettings.AvailableLocales.GetLocale(value)?.LocaleName;
		}
		return locale.ToString();
	}

	public void Get(string id, string key, Action<string> callback, Dictionary<string, string> args)
	{
		if (callback == null)
		{
			throw new UnityException("Callback cannot be null");
		}
		if (string.IsNullOrEmpty(key))
		{
			throw new UnityException("Key cannot be empty");
		}
		if (string.IsNullOrEmpty(id))
		{
			throw new UnityException("ID cannot be empty");
		}
		_localization[id] = new I18N
		{
			key = key,
			args = args,
			callback = callback
		};
		callback(Get(key, args));
	}

	public void Get(string id, string key, Action<string> callback)
	{
		if (callback == null)
		{
			throw new UnityException("Callback cannot be null");
		}
		if (string.IsNullOrEmpty(key))
		{
			throw new UnityException("Key cannot be empty");
		}
		if (string.IsNullOrEmpty(id))
		{
			throw new UnityException("ID cannot be empty");
		}
		(string, Dictionary<string, string>) tuple = ParseKey(key);
		_localization[id] = new I18N
		{
			key = tuple.Item1,
			args = tuple.Item2,
			callback = callback
		};
		callback(Get(tuple.Item1, tuple.Item2));
	}

	public void Cleanup(string id)
	{
		if (string.IsNullOrEmpty(id))
		{
			throw new UnityException("ID cannot be empty");
		}
		_localization.Remove(id);
	}

	public string GetKeybindingText(string actionID, string compositePart = null, InputBinding.DisplayStringOptions options = (InputBinding.DisplayStringOptions)0)
	{
		if (!MonoController<StartupController>.Instance)
		{
			throw new UnityException("Missing StartupController instance");
		}
		if (INPUT_MAPPING.TryGetValue(actionID.ToLowerInvariant(), out var value))
		{
			actionID = value;
		}
		InputAction inputAction = MonoController<StartupController>.Instance.GetIngameActions().FindAction(actionID);
		if (inputAction == null)
		{
			return actionID;
		}
		for (int i = 0; i < inputAction.bindings.Count; i++)
		{
			InputBinding inputBinding = inputAction.bindings[i];
			if (inputBinding.isComposite)
			{
				continue;
			}
			if (compositePart != null)
			{
				if (!inputBinding.isPartOfComposite || !inputBinding.name.Equals(compositePart, StringComparison.OrdinalIgnoreCase))
				{
					continue;
				}
			}
			else if (inputBinding.isPartOfComposite)
			{
				continue;
			}
			string bindingDisplayString = inputAction.GetBindingDisplayString(i, options);
			if (!string.IsNullOrEmpty(bindingDisplayString))
			{
				return bindingDisplayString;
			}
		}
		return actionID;
	}

	private void OnSettingsUpdated()
	{
		if ((bool)MonoController<SettingsController>.Instance)
		{
			PlayerSettings currentSettings = MonoController<SettingsController>.Instance.CurrentSettings;
			Locale locale = LocalizationSettings.AvailableLocales.GetLocale(LOCALE_MAPPING[currentSettings.localization]);
			if (!locale)
			{
				currentSettings.localization = LOCALE.EN;
				MonoController<SettingsController>.Instance.CurrentSettings = currentSettings;
			}
			else
			{
				LocalizationSettings.SelectedLocale = locale;
			}
		}
	}

	private (string, Dictionary<string, string>) ParseKey(string key)
	{
		string[] array = key.Split(new string[1] { "||" }, StringSplitOptions.None);
		string item = array[0];
		Dictionary<string, string> item2 = array.AsValueEnumerable().Skip(1).Select((string param, int index) => new
		{
			Key = index.ToString(),
			Value = param
		})
			.ToDictionary(x => x.Key, x => x.Value);
		return (item, item2);
	}

	private void SelectedLocaleChanged(Locale obj)
	{
		foreach (KeyValuePair<string, I18N> item in _localization)
		{
			if (item.Value.callback == null)
			{
				_localization.Remove(item.Key);
				continue;
			}
			item.Value.callback(LocalizationSettings.StringDatabase.GetLocalizedString(item.Value.key, new object[1] { item.Value.args }));
		}
	}
}
