namespace DungeonGame;

/// <summary>
/// Player inventory (backpack) or bank storage. One item type per slot, unlimited stacking per slot.
/// Pure logic — no Godot dependency.
///
/// Invariant: a single item Id occupies at most ONE slot in this inventory. Adding more of an
/// existing item merges into the existing slot; no stack splitting within a single storage.
/// Partial splitting across storages happens via <see cref="Transfer"/>.
///
/// See docs/inventory/backpack.md and docs/inventory/bank.md for spec.
/// </summary>
public class Inventory
{
    private readonly ItemStack?[] _slots;

    public int SlotCount => _slots.Length;
    public int UsedSlots { get; private set; }
    public long Gold { get; set; }

    public Inventory(int slotCount = 25)
    {
        _slots = new ItemStack?[slotCount];
    }

    public ItemStack? GetSlot(int index) =>
        index >= 0 && index < _slots.Length ? _slots[index] : null;

    /// <summary>
    /// Find the slot holding this item Id, or -1 if not present.
    /// Enforces the one-type-per-slot invariant.
    /// </summary>
    public int FindSlot(string itemId)
    {
        for (int i = 0; i < _slots.Length; i++)
            if (_slots[i] != null && _slots[i]!.Item.Id == itemId)
                return i;
        return -1;
    }

    public int FindEmptySlot()
    {
        for (int i = 0; i < _slots.Length; i++)
            if (_slots[i] == null)
                return i;
        return -1;
    }

    /// <summary>
    /// Try to add an item stack. Merges with existing slot if item Id already present (one type per slot).
    /// Returns true if successful, false if no slot is available for a brand-new item type.
    /// </summary>
    public bool TryAdd(ItemDef item, long count = 1)
    {
        if (count <= 0) return true;

        int existing = FindSlot(item.Id);
        if (existing >= 0)
        {
            var stack = _slots[existing]!;
            _slots[existing] = stack with { Count = stack.Count + count };
            return true;
        }

        int empty = FindEmptySlot();
        if (empty < 0) return false;

        _slots[empty] = new ItemStack { Item = item, Count = count };
        UsedSlots++;
        return true;
    }

    /// <summary>
    /// Remove up to <paramref name="count"/> items from the given slot. Returns the removed stack, or null if the slot was empty.
    /// Clears the slot if the removal empties it.
    /// </summary>
    public ItemStack? RemoveAt(int index, long count = 1)
    {
        if (index < 0 || index >= _slots.Length) return null;
        var stack = _slots[index];
        if (stack == null) return null;

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
    /// Toggle the Lock flag on the slot. Returns the new Locked value, or false if the slot is empty.
    /// </summary>
    public bool ToggleLock(int index)
    {
        var stack = GetSlot(index);
        if (stack == null) return false;
        bool newLocked = !stack.Locked;
        _slots[index] = stack with { Locked = newLocked };
        return newLocked;
    }

    /// <summary>
    /// Set the Lock flag directly (idempotent). Returns true if the slot exists.
    /// Prefer this over <see cref="ToggleLock"/> when you know the desired end state —
    /// e.g., when restoring saved data that may have multiple saved entries for the same
    /// item Id which all merge into a single slot (repeated toggles would flip the state).
    /// </summary>
    public bool SetLocked(int index, bool locked)
    {
        var stack = GetSlot(index);
        if (stack == null) return false;
        if (stack.Locked != locked)
            _slots[index] = stack with { Locked = locked };
        return true;
    }

    /// <summary>
    /// Transfer up to <paramref name="count"/> items from this inventory's slot into <paramref name="dest"/>.
    /// Respects dest's Locked flags? NO — locked items can still be transferred (Lock only gates Sell/Drop).
    /// Returns true if any items were moved. Fails if dest cannot accept a new item type (full) AND does not
    /// already have a slot for this item.
    /// </summary>
    public bool Transfer(int fromIndex, Inventory dest, long count)
    {
        var stack = GetSlot(fromIndex);
        if (stack == null || count <= 0) return false;

        long toMove = count > stack.Count ? stack.Count : count;

        // Check destination can accept
        int destExisting = dest.FindSlot(stack.Item.Id);
        if (destExisting < 0 && dest.FindEmptySlot() < 0) return false;

        // Remove from source
        RemoveAt(fromIndex, toMove);

        // Add to destination (carries Locked flag forward only when creating a new slot;
        // merging keeps the destination's existing lock state)
        if (destExisting >= 0)
        {
            var existing = dest._slots[destExisting]!;
            dest._slots[destExisting] = existing with { Count = existing.Count + toMove };
        }
        else
        {
            int empty = dest.FindEmptySlot();
            dest._slots[empty] = new ItemStack { Item = stack.Item, Count = toMove, Locked = stack.Locked };
            dest.UsedSlots++;
        }
        return true;
    }

    /// <summary>
    /// Destroy items from a slot (Drop). Intended for backpack Drop action — caller must enforce
    /// "backpack only" policy. Refuses if the stack is Locked.
    /// Returns true if items were destroyed.
    /// </summary>
    public bool Drop(int index, long count)
    {
        var stack = GetSlot(index);
        if (stack == null || count <= 0 || stack.Locked) return false;
        RemoveAt(index, count);
        return true;
    }

    public bool CanAfford(long price) => Gold >= price;

    /// <summary>
    /// Legacy helper: buy one of the given item, deducting gold and adding to inventory.
    /// Kept for existing ShopWindow compatibility until GuildWindow replaces it.
    /// </summary>
    public bool TryBuy(ItemDef item)
    {
        if (!CanAfford(item.BuyPrice)) return false;
        if (!TryAdd(item)) return false;
        Gold -= item.BuyPrice;
        return true;
    }

    /// <summary>
    /// Legacy helper: sell one of the item in the given slot, crediting gold.
    /// Refuses Locked items.
    /// Kept for existing ShopWindow compatibility until GuildWindow replaces it.
    /// </summary>
    public bool TrySell(int slotIndex)
    {
        var stack = GetSlot(slotIndex);
        if (stack == null || stack.Locked) return false;
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
