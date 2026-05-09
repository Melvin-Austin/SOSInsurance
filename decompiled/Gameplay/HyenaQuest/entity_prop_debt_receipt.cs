using System;
using TMPro;
using Unity.Collections;
using UnityEngine;

namespace HyenaQuest;

public class entity_prop_debt_receipt : entity_phys_prop_scrap
{
	public TextMeshPro receiptText;

	private readonly NetVar<FixedString4096Bytes> _receiptText = new NetVar<FixedString4096Bytes>();

	public override int GetReward()
	{
		return 0;
	}

	public override void OnNetworkDespawn()
	{
		base.OnNetworkDespawn();
		if (base.IsClient)
		{
			MonoController<LocalizationController>.Instance?.Cleanup($"receipt-{base.NetworkObjectId}");
		}
	}

	protected override void OnNetworkPostSpawn()
	{
		base.OnNetworkPostSpawn();
		if (base.IsClient)
		{
			_receiptText.RegisterOnValueChanged(delegate(FixedString4096Bytes _, FixedString4096Bytes newValue)
			{
				UpdateText(newValue.ToString());
			});
		}
	}

	public override void OnNetworkPreDespawn()
	{
		base.OnNetworkPreDespawn();
		if (base.IsClient)
		{
			_receiptText.OnValueChanged = null;
		}
	}

	[Server]
	public void SetText(string text)
	{
		if (!base.IsServer)
		{
			throw new UnityException("SetText can only be called on the server!");
		}
		_receiptText.Value = new FixedString4096Bytes(text);
	}

	public override bool CanScrap(entity_player scrapper)
	{
		return !IsLocked();
	}

	protected override void Init()
	{
		base.Init();
		if (!receiptText)
		{
			throw new UnityException("Missing receiptText");
		}
	}

	private void UpdateText(string text)
	{
		if (!MonoController<LocalizationController>.Instance)
		{
			throw new UnityException("Missing LocalizationController");
		}
		if (!receiptText)
		{
			return;
		}
		MonoController<LocalizationController>.Instance.Get($"receipt-{base.NetworkObjectId}", "ingame.receipt.items", delegate(string s)
		{
			if ((bool)receiptText)
			{
				string[] array = s.Split(new string[1] { "<##>" }, StringSplitOptions.None);
				string text2 = text;
				for (int i = 0; i < array.Length; i++)
				{
					text2 = text2.Replace($"%%{i}%%", array[i].Truncate(38));
				}
				receiptText.text = text2;
			}
		});
	}

	protected override void __initializeVariables()
	{
		if (_receiptText == null)
		{
			throw new Exception("entity_prop_debt_receipt._receiptText cannot be null. All NetworkVariableBase instances must be initialized.");
		}
		_receiptText.Initialize(this);
		__nameNetworkVariable(_receiptText, "_receiptText");
		NetworkVariableFields.Add(_receiptText);
		base.__initializeVariables();
	}

	protected override void __initializeRpcs()
	{
		base.__initializeRpcs();
	}

	protected internal override string __getTypeName()
	{
		return "entity_prop_debt_receipt";
	}
}
