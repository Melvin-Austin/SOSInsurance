using System.Collections.Generic;
using UnityEngine;

namespace HyenaQuest;

public class ui_inventory : MonoBehaviour
{
	[Header("Templates")]
	public GameObject slot;

	private static readonly float INVENTORY_GAP = 1.2f;

	private readonly List<ui_inventory_slot> _ui_slots = new List<ui_inventory_slot>();

	private int _prev_slot = -1;

	public void Awake()
	{
		if (!slot)
		{
			throw new UnityException("Missing slot template");
		}
		BuildInventory();
	}

	public void UpdateInventorySlot(int index, entity_item_pickable prop)
	{
		if (index >= _ui_slots.Count)
		{
			throw new UnityException("Slots missmatch");
		}
		_ui_slots[index].SetItem(prop);
	}

	public void UpdateInventorySelectedSlot(int newSlot)
	{
		if (_ui_slots != null && _ui_slots.Count != 0)
		{
			if (newSlot < 0 || newSlot >= _ui_slots.Count)
			{
				throw new UnityException("Invalid slot index");
			}
			if (_prev_slot != -1)
			{
				_ui_slots[_prev_slot].SetSelected(select: false);
			}
			_ui_slots[newSlot].SetSelected(select: true);
			_prev_slot = newSlot;
		}
	}

	public void BuildInventory()
	{
		byte b = NetController<IngameController>.Instance?.GetMaxInventorySlots() ?? 1;
		for (byte b2 = (byte)_ui_slots.Count; b2 < b; b2++)
		{
			_ui_slots.Add(CreateSlot(b2));
		}
		if (_prev_slot == -1)
		{
			UpdateInventorySelectedSlot(0);
		}
	}

	private ui_inventory_slot CreateSlot(byte index)
	{
		GameObject obj = Object.Instantiate(slot, base.transform);
		if (!obj)
		{
			throw new UnityException("Invalid ui_inventory_slot template");
		}
		float num = obj.transform.localScale.x * INVENTORY_GAP;
		obj.transform.localPosition = new Vector3(num / 2f + (float)(int)index * num, 0f, 0f);
		ui_inventory_slot component = obj.GetComponent<ui_inventory_slot>();
		if (!component)
		{
			throw new UnityException("Invalid ui_inventory_slot template");
		}
		return component;
	}
}
