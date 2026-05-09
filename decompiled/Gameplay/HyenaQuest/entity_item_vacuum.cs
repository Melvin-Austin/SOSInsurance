using System;
using TMPro;
using Unity.Netcode;
using UnityEngine;

namespace HyenaQuest;

public class entity_item_vacuum : entity_item_pickable
{
	public TextMeshPro scrapText;

	public GameEvent<int, bool> OnScrapAdded = new GameEvent<int, bool>();

	private readonly NetVar<int> _totalScrap = new NetVar<int>(0);

	protected override void OnNetworkPostSpawn()
	{
		base.OnNetworkPostSpawn();
		if (!base.IsClient)
		{
			return;
		}
		NetVar<int> totalScrap = _totalScrap;
		totalScrap.OnValueChanged = (NetworkVariable<int>.OnValueChangedDelegate)Delegate.Combine(totalScrap.OnValueChanged, (NetworkVariable<int>.OnValueChangedDelegate)delegate(int _, int newValue)
		{
			if ((bool)scrapText)
			{
				scrapText.text = $"{GetScrapPercentage()} %";
			}
			OnScrapAdded.Invoke(newValue, param2: false);
		});
	}

	public override void OnNetworkPreDespawn()
	{
		base.OnNetworkPreDespawn();
		if (base.IsClient)
		{
			_totalScrap.OnValueChanged = null;
		}
	}

	public override void OnUse(entity_player ply, Collider obj, bool pressing)
	{
		if (!(ply != PlayerController.LOCAL))
		{
			entity_player_vacuum vacuum = PlayerController.LOCAL.GetVacuum();
			if (!vacuum)
			{
				throw new UnityException("Missing entity_player_vacuum");
			}
			if (pressing)
			{
				vacuum.OnScrapStart();
			}
			else
			{
				vacuum.OnScrapEnd();
			}
		}
	}

	public bool IsFull()
	{
		return _totalScrap.Value >= (NetController<ScrapController>.Instance?.GetMaxContainerScrap() ?? 200);
	}

	public int GetTotalScrap()
	{
		return _totalScrap.Value;
	}

	public int GetScrapPercentage()
	{
		return Mathf.FloorToInt((float)_totalScrap.Value / (float)(NetController<ScrapController>.Instance?.GetMaxContainerScrap() ?? 200) * 100f);
	}

	[Server]
	public void AddScrap(int scrap)
	{
		if (!base.IsServer)
		{
			throw new UnityException("AddScrap can only be called on the server.");
		}
		if (!IsFull())
		{
			SetScrap(_totalScrap.Value + scrap);
		}
	}

	[Server]
	public void SetScrap(int scrap)
	{
		if (!base.IsServer)
		{
			throw new UnityException("SetScrap can only be called on the server.");
		}
		_totalScrap.Value = Mathf.Max(scrap, 0);
		OnScrapAdded.Invoke(_totalScrap.Value, param2: true);
	}

	[Server]
	public void Clear()
	{
		if (!base.IsServer)
		{
			throw new UnityException("Clear can only be called on the server.");
		}
		_totalScrap.Value = 0;
		OnScrapAdded.Invoke(0, param2: true);
	}

	public override string GetID()
	{
		return "item_vacuum";
	}

	protected override void Init()
	{
		base.Init();
		if (!scrapText)
		{
			throw new UnityException("scrapText is not set");
		}
		base.name = GetID();
	}

	protected override void __initializeVariables()
	{
		if (_totalScrap == null)
		{
			throw new Exception("entity_item_vacuum._totalScrap cannot be null. All NetworkVariableBase instances must be initialized.");
		}
		_totalScrap.Initialize(this);
		__nameNetworkVariable(_totalScrap, "_totalScrap");
		NetworkVariableFields.Add(_totalScrap);
		base.__initializeVariables();
	}

	protected override void __initializeRpcs()
	{
		base.__initializeRpcs();
	}

	protected internal override string __getTypeName()
	{
		return "entity_item_vacuum";
	}
}
