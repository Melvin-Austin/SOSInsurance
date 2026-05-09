using System;
using System.Collections.Generic;
using FailCake;
using TMPro;
using UnityEngine;

namespace HyenaQuest;

public class entity_store_selector : MonoBehaviour
{
	public TextMeshPro itemNameGameObject;

	public TextMeshPro itemPriceGameObject;

	private StoreItem _item;

	private byte _storeIndex;

	private entity_client_button _buyButton;

	private SpriteRenderer _iconRenderer;

	private util_timer _animationTimer;

	private int _animationSprite;

	public void Awake()
	{
		_buyButton = GetComponentInChildren<entity_client_button>(includeInactive: true);
		if (!_buyButton)
		{
			throw new UnityException("Missing entity_client_button");
		}
		_iconRenderer = GetComponentInChildren<SpriteRenderer>(includeInactive: true);
		if (!_iconRenderer)
		{
			throw new UnityException("Missing SpriteRenderer");
		}
		_buyButton.OnUse += new Action<entity_player>(OnUSE);
		SetEnabled(enable: false);
	}

	public void OnDestroy()
	{
		_animationTimer?.Stop();
		if ((bool)_buyButton)
		{
			_buyButton.OnUse -= new Action<entity_player>(OnUSE);
		}
		if ((bool)MonoController<LocalizationController>.Instance)
		{
			MonoController<LocalizationController>.Instance.Cleanup("store.item-" + base.name);
			MonoController<LocalizationController>.Instance.Cleanup("store.item.price-" + base.name);
		}
	}

	public void SetItem(StoreItem item, byte storeIndex)
	{
		_item = item;
		_storeIndex = storeIndex;
		SetEnabled((bool)item && storeIndex != byte.MaxValue);
		UpdateTexts();
		UpdateIcon();
	}

	private void OnUSE(entity_player ply)
	{
		if ((bool)ply && !ply.IsDead() && _storeIndex != byte.MaxValue)
		{
			NetController<StoreController>.Instance.RequestBuyItemRPC(_storeIndex);
		}
	}

	private void UpdateIcon()
	{
		_animationTimer?.Stop();
		if (!_item || !_iconRenderer)
		{
			return;
		}
		_animationSprite = UnityEngine.Random.Range(0, _item.itemSprites.Count);
		_animationTimer = util_timer.Create(-1, 0.5f, delegate
		{
			if ((bool)_iconRenderer && (bool)_item)
			{
				_animationSprite++;
				if (_animationSprite >= _item.itemSprites.Count)
				{
					_animationSprite = 0;
				}
				_iconRenderer.sprite = _item.itemSprites[_animationSprite];
			}
		});
	}

	private void SetEnabled(bool enable)
	{
		if ((bool)itemNameGameObject)
		{
			itemNameGameObject.transform.parent.gameObject.SetActive(enable);
		}
		if ((bool)_buyButton)
		{
			_buyButton.SetLocked(!enable);
		}
	}

	private void UpdateTexts()
	{
		if (!MonoController<LocalizationController>.Instance)
		{
			return;
		}
		MonoController<LocalizationController>.Instance.Cleanup("store.item-" + base.name);
		MonoController<LocalizationController>.Instance.Cleanup("store.item.price-" + base.name);
		if (!_item)
		{
			return;
		}
		if (_item.itemName.StartsWith("ingame."))
		{
			MonoController<LocalizationController>.Instance.Get("store.item-" + base.name, _item.itemName, delegate(string v)
			{
				if ((bool)itemNameGameObject)
				{
					itemNameGameObject.text = v;
				}
			});
		}
		else
		{
			itemNameGameObject.text = _item.itemName;
		}
		MonoController<LocalizationController>.Instance.Get("store.item.price-" + base.name, "ingame.shop.price", delegate(string v)
		{
			if ((bool)itemPriceGameObject)
			{
				itemPriceGameObject.text = v;
			}
		}, new Dictionary<string, string> { 
		{
			"price",
			_item.itemPrice.ToString()
		} });
	}
}
