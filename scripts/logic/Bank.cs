using System;

namespace DungeonGame;

/// <summary>
/// Bank storage system. Safe, permanent, town-only storage.
/// Items in the bank are never lost on death.
/// Uses the same Inventory class as the backpack.
/// Pure logic — no Godot dependency.
/// </summary>
public class Bank
{
    public const int StartingSlots = 50;
    public const int SlotsPerExpansion = 10;

    private Inventory _storage;
    private int _expansionCount;

    public Inventory Storage => _storage;
    public int ExpansionCount => _expansionCount;
    public int TotalSlots => _storage.SlotCount;

    public Bank()
    {
        _storage = new Inventory(StartingSlots);
        _expansionCount = 0;
    }

    /// <summary>
    /// Gold cost for the next bank expansion.
    /// Formula: 500 * N^2 where N is the expansion number (1-indexed).
    /// </summary>
    public int GetNextExpansionCost()
    {
        int n = _expansionCount + 1;
        return 500 * n * n;
    }

    /// <summary>
    /// Purchase a bank expansion. Deducts gold from the player's inventory.
    /// Returns true if successful.
    /// </summary>
    public bool PurchaseExpansion(Inventory playerInventory)
    {
        int cost = GetNextExpansionCost();
        if (playerInventory.Gold < cost)
            return false;

        playerInventory.Gold -= cost;
        _expansionCount++;

        // Create a new larger inventory and transfer all items
        var newStorage = new Inventory(StartingSlots + _expansionCount * SlotsPerExpansion);
        for (int i = 0; i < _storage.SlotCount; i++)
        {
            var stack = _storage.GetSlot(i);
            if (stack != null)
                newStorage.TryAdd(stack.Item, stack.Count);
        }
        _storage = newStorage;

        return true;
    }

    /// <summary>
    /// Deposit an item from backpack slot to bank. Returns true if successful.
    /// </summary>
    public bool Deposit(Inventory backpack, int backpackSlot)
    {
        var stack = backpack.GetSlot(backpackSlot);
        if (stack == null) return false;

        if (!_storage.TryAdd(stack.Item, stack.Count))
            return false;

        backpack.RemoveAt(backpackSlot, stack.Count);
        return true;
    }

    /// <summary>
    /// Withdraw an item from bank slot to backpack. Returns true if successful.
    /// </summary>
    public bool Withdraw(Inventory backpack, int bankSlot)
    {
        var stack = _storage.GetSlot(bankSlot);
        if (stack == null) return false;

        if (!backpack.TryAdd(stack.Item, stack.Count))
            return false;

        _storage.RemoveAt(bankSlot, stack.Count);
        return true;
    }

    /// <summary>Capture bank state for save data.</summary>
    public SavedBankData CaptureState()
    {
        var items = new System.Collections.Generic.List<SavedItemStack>();
        for (int i = 0; i < _storage.SlotCount; i++)
        {
            var stack = _storage.GetSlot(i);
            if (stack != null)
                items.Add(new SavedItemStack { ItemId = stack.Item.Id, Count = stack.Count });
        }
        return new SavedBankData
        {
            ExpansionCount = _expansionCount,
            Items = items.ToArray(),
        };
    }

    /// <summary>Restore bank state from save data.</summary>
    public void RestoreState(SavedBankData data)
    {
        _expansionCount = Math.Max(0, data.ExpansionCount);
        _storage = new Inventory(StartingSlots + _expansionCount * SlotsPerExpansion);
        foreach (var saved in data.Items)
        {
            var itemDef = ItemDatabase.Get(saved.ItemId);
            if (itemDef != null)
                _storage.TryAdd(itemDef, Math.Max(1, saved.Count));
        }
    }
}
