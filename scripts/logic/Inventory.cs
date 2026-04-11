using System.Collections.Generic;

namespace DungeonGame;

/// <summary>
/// Player inventory (backpack). Fixed slot count, stackable consumables/materials.
/// Pure logic — no Godot dependency.
/// </summary>
public class Inventory
{
    private readonly ItemStack?[] _slots;

    public int SlotCount => _slots.Length;
    public int UsedSlots { get; private set; }
    public int Gold { get; set; }

    public Inventory(int slotCount = 25)
    {
        _slots = new ItemStack?[slotCount];
    }

    public ItemStack? GetSlot(int index) => _slots[index];

    /// <summary>
    /// Try to add an item. Returns true if successful.
    /// Stackable items merge into existing stacks first.
    /// </summary>
    public bool TryAdd(ItemDef item, int count = 1)
    {
        // Try to stack with existing
        if (item.Category == ItemCategory.Consumable || item.Category == ItemCategory.Material)
        {
            for (int i = 0; i < _slots.Length; i++)
            {
                if (_slots[i] != null && _slots[i]!.Item.Id == item.Id &&
                    _slots[i]!.Count < _slots[i]!.MaxStack)
                {
                    int space = _slots[i]!.MaxStack - _slots[i]!.Count;
                    int toAdd = System.Math.Min(count, space);
                    _slots[i] = _slots[i]! with { Count = _slots[i]!.Count + toAdd };
                    count -= toAdd;
                    if (count <= 0) return true;
                }
            }
        }

        // Find empty slot for remainder
        for (int i = 0; i < _slots.Length; i++)
        {
            if (_slots[i] == null)
            {
                _slots[i] = new ItemStack { Item = item, Count = count };
                UsedSlots++;
                return true;
            }
        }

        return false; // Backpack full
    }

    /// <summary>
    /// Remove an item by slot index. Returns the removed stack.
    /// </summary>
    public ItemStack? RemoveAt(int index, int count = 1)
    {
        if (_slots[index] == null) return null;

        var stack = _slots[index]!;
        if (count >= stack.Count)
        {
            _slots[index] = null;
            UsedSlots--;
            return stack;
        }

        _slots[index] = stack with { Count = stack.Count - count };
        return stack with { Count = count };
    }

    /// <summary>
    /// Check if the player can afford an item.
    /// </summary>
    public bool CanAfford(int price) => Gold >= price;

    /// <summary>
    /// Buy an item from a shop. Deducts gold, adds to inventory.
    /// </summary>
    public bool TryBuy(ItemDef item)
    {
        if (!CanAfford(item.BuyPrice)) return false;
        if (!TryAdd(item)) return false;
        Gold -= item.BuyPrice;
        return true;
    }

    /// <summary>
    /// Sell an item from a slot. Adds gold, removes from inventory.
    /// </summary>
    public bool TrySell(int slotIndex)
    {
        var stack = _slots[slotIndex];
        if (stack == null) return false;
        Gold += stack.Item.SellPrice;
        RemoveAt(slotIndex);
        return true;
    }

    public void Clear()
    {
        for (int i = 0; i < _slots.Length; i++)
            _slots[i] = null;
        UsedSlots = 0;
    }
}
