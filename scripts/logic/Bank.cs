using System;

namespace DungeonGame;

/// <summary>
/// Bank storage — safe, permanent, town-only. Items and gold in the bank are never lost on death.
/// Wraps an <see cref="Inventory"/> (items) and its own gold pocket (separate from backpack gold).
/// Pure logic — no Godot dependency.
///
/// Expansion: +1 slot per upgrade, cost <c>50 * N gold</c> (N = expansion number).
/// See docs/inventory/bank.md for the full spec.
/// </summary>
public class Bank
{
    public const int StartingSlots = 25;
    public const int SlotsPerExpansion = 1;

    private Inventory _storage;
    private int _expansionCount;

    public Inventory Storage => _storage;
    public int ExpansionCount => _expansionCount;
    public int TotalSlots => _storage.SlotCount;

    /// <summary>
    /// Gold held in the bank pocket (safe on death). Separate from the backpack's gold pocket.
    /// </summary>
    public long Gold { get; set; }

    public Bank()
    {
        _storage = new Inventory(StartingSlots);
        _expansionCount = 0;
    }

    /// <summary>
    /// Gold cost for the next bank expansion. Formula: <c>50 * N</c> (N = expansion number, 1-indexed).
    /// </summary>
    public long GetNextExpansionCost() => 50L * (_expansionCount + 1);

    /// <summary>
    /// Purchase a bank expansion, drawing gold from the provided pockets (backpack first, then
    /// bank — matches <see cref="DeathPenalty.PayBuyout"/> default and the guild-window MVP doc).
    /// Returns true on success.
    /// </summary>
    public bool PurchaseExpansion(Inventory backpack)
    {
        long cost = GetNextExpansionCost();
        if (Gold + backpack.Gold < cost) return false;

        long fromBackpack = Math.Min(backpack.Gold, cost);
        backpack.Gold -= fromBackpack;
        long remaining = cost - fromBackpack;
        if (remaining > 0) Gold -= remaining;

        _expansionCount++;

        // Create a new larger inventory and transfer items over (preserves Locked flags)
        var newStorage = new Inventory(StartingSlots + _expansionCount * SlotsPerExpansion);
        for (int i = 0; i < _storage.SlotCount; i++)
        {
            var stack = _storage.GetSlot(i);
            if (stack != null)
            {
                // TryAdd merges by Id (there won't be duplicates since the old storage also enforced one-per-slot)
                newStorage.TryAdd(stack.Item, stack.Count);
                // Re-apply Locked flag by finding the slot we just added to
                int idx = newStorage.FindSlot(stack.Item.Id);
                if (idx >= 0 && stack.Locked)
                {
                    // Toggle if not already locked
                    var newStack = newStorage.GetSlot(idx);
                    if (newStack != null && !newStack.Locked)
                        newStorage.ToggleLock(idx);
                }
            }
        }
        _storage = newStorage;

        return true;
    }

    /// <summary>
    /// Legacy helper: move the entire stack at backpack[slotIndex] into the bank.
    /// Wraps <see cref="Inventory.Transfer"/>. Kept for existing BankWindow compatibility
    /// until GuildWindow replaces it.
    /// </summary>
    public bool Deposit(Inventory backpack, int backpackSlot)
    {
        var stack = backpack.GetSlot(backpackSlot);
        if (stack == null) return false;
        return backpack.Transfer(backpackSlot, _storage, stack.Count);
    }

    /// <summary>
    /// Legacy helper: move the entire stack at bank[slotIndex] into the backpack.
    /// Wraps <see cref="Inventory.Transfer"/>. Kept for existing BankWindow compatibility
    /// until GuildWindow replaces it.
    /// </summary>
    public bool Withdraw(Inventory backpack, int bankSlot)
    {
        var stack = _storage.GetSlot(bankSlot);
        if (stack == null) return false;
        return _storage.Transfer(bankSlot, backpack, stack.Count);
    }

    /// <summary>Move gold from backpack → bank. Returns true if successful.</summary>
    public bool DepositGold(Inventory backpack, long amount)
    {
        if (amount <= 0 || backpack.Gold < amount) return false;
        backpack.Gold -= amount;
        Gold += amount;
        return true;
    }

    /// <summary>Move gold from bank → backpack. Returns true if successful.</summary>
    public bool WithdrawGold(Inventory backpack, long amount)
    {
        if (amount <= 0 || Gold < amount) return false;
        Gold -= amount;
        backpack.Gold += amount;
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
                items.Add(new SavedItemStack { ItemId = stack.Item.Id, Count = stack.Count, Locked = stack.Locked });
        }
        return new SavedBankData
        {
            ExpansionCount = _expansionCount,
            Gold = Gold,
            Items = items.ToArray(),
        };
    }

    /// <summary>Restore bank state from save data.</summary>
    public void RestoreState(SavedBankData data)
    {
        _expansionCount = Math.Max(0, data.ExpansionCount);
        _storage = new Inventory(StartingSlots + _expansionCount * SlotsPerExpansion);
        Gold = Math.Max(0, data.Gold);
        foreach (var saved in data.Items)
        {
            var itemDef = ItemDatabase.Get(saved.ItemId);
            if (itemDef != null)
            {
                _storage.TryAdd(itemDef, Math.Max(1, saved.Count));
                if (saved.Locked)
                {
                    // SetLocked is idempotent — see Inventory.SetLocked for why this matters
                    // (merged duplicate entries would otherwise flip the flag on repeat toggles).
                    int idx = _storage.FindSlot(itemDef.Id);
                    if (idx >= 0) _storage.SetLocked(idx, true);
                }
            }
        }
    }
}
