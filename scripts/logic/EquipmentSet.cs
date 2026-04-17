using System;

namespace DungeonGame;

/// <summary>
/// Player equipment — 10 slot types, 19 equippable slots total (10 ring sub-slots).
/// Pure logic, no Godot dependency. See docs/systems/equipment.md.
///
/// Equip/Unequip hand off swaps with an <see cref="Inventory"/> (the backpack):
/// the previously-equipped item returns to the backpack; if the backpack has no
/// free slot and cannot merge, the swap is refused (caller should show feedback).
///
/// Stat contribution is computed by <see cref="GetTotalBonuses"/>, applying a 1.25×
/// multiplier when item ClassAffinity matches the player's class.
/// </summary>
public class EquipmentSet
{
    public const int RingSlotCount = 10;
    private const float AffinityMultiplier = 1.25f;

    public ItemDef? Head { get; set; }
    public ItemDef? Body { get; set; }
    public ItemDef? Arms { get; set; }
    public ItemDef? Legs { get; set; }
    public ItemDef? Feet { get; set; }
    public ItemDef? Neck { get; set; }
    public ItemDef? MainHand { get; set; }
    public ItemDef? OffHand { get; set; }
    public ItemDef? Ammo { get; set; }
    public ItemDef?[] Rings { get; } = new ItemDef?[RingSlotCount];

    /// <summary>
    /// True if an arrow-bearing quiver is equipped in the Ammo slot.
    /// Rangers need a quiver to fire bows ranged; without one, bow becomes melee.
    /// </summary>
    public bool HasQuiver() => Ammo != null && Ammo.Category == ItemCategory.Quiver;

    /// <summary>
    /// Total number of currently-equipped items across all 19 slots.
    /// </summary>
    public int EquippedCount
    {
        get
        {
            int n = 0;
            if (Head != null) n++;
            if (Body != null) n++;
            if (Arms != null) n++;
            if (Legs != null) n++;
            if (Feet != null) n++;
            if (Neck != null) n++;
            if (MainHand != null) n++;
            if (OffHand != null) n++;
            if (Ammo != null) n++;
            for (int i = 0; i < Rings.Length; i++)
                if (Rings[i] != null) n++;
            return n;
        }
    }

    /// <summary>
    /// Whether the item's category is compatible with the given slot.
    /// Spec: docs/systems/equipment.md §Item Categories by Slot.
    /// </summary>
    public static bool IsCompatible(EquipSlot slot, ItemDef item) => slot switch
    {
        EquipSlot.Head => item.Category == ItemCategory.Head,
        EquipSlot.Body => item.Category == ItemCategory.Body,
        EquipSlot.Arms => item.Category == ItemCategory.Arms,
        EquipSlot.Legs => item.Category == ItemCategory.Legs,
        EquipSlot.Feet => item.Category == ItemCategory.Feet,
        EquipSlot.Neck => item.Category == ItemCategory.Neck,
        EquipSlot.Ring => item.Category == ItemCategory.Ring,
        EquipSlot.MainHand => item.Category == ItemCategory.Weapon,
        EquipSlot.OffHand => item.Category is ItemCategory.Shield
                             or ItemCategory.Spellbook
                             or ItemCategory.DefensiveMelee,
        EquipSlot.Ammo => item.Category == ItemCategory.Quiver,
        _ => false,
    };

    /// <summary>
    /// Equip <paramref name="item"/> into <paramref name="slot"/>.
    /// Removes the item from <paramref name="backpack"/> (must be present there) and
    /// returns any previously-equipped item to the backpack. If the backpack is full
    /// AND cannot merge the returned item, the swap is refused and state is unchanged.
    /// Returns true on success.
    /// </summary>
    public bool TryEquip(EquipSlot slot, ItemDef item, Inventory backpack, int ringIndex = 0)
    {
        if (!IsCompatible(slot, item)) return false;
        if (slot == EquipSlot.Ring && (ringIndex < 0 || ringIndex >= RingSlotCount)) return false;

        int sourceIndex = backpack.FindSlot(item.Id);
        if (sourceIndex < 0) return false; // Item must exist in backpack to equip.

        var previous = GetSlot(slot, ringIndex);

        // Reserve: try placing previous first (requires a free slot or merging target).
        if (previous != null)
        {
            int targetIndex = backpack.FindSlot(previous.Id);
            if (targetIndex < 0 && backpack.FindEmptySlot() < 0)
                return false;
        }

        // Take one item out of backpack.
        if (backpack.RemoveAt(sourceIndex, 1) == null) return false;

        // Set the new equipped item.
        SetSlot(slot, item, ringIndex);

        // Return previous to backpack (guaranteed to succeed by the reserve check above).
        if (previous != null)
            backpack.TryAdd(previous, 1);

        return true;
    }

    /// <summary>
    /// Equip without going through the backpack. Used for starting gear and save restore.
    /// Overwrites the slot silently (any prior item is discarded; callers that care must Unequip first).
    /// </summary>
    public void ForceEquip(EquipSlot slot, ItemDef item, int ringIndex = 0)
    {
        if (!IsCompatible(slot, item)) return;
        if (slot == EquipSlot.Ring && (ringIndex < 0 || ringIndex >= RingSlotCount)) return;
        SetSlot(slot, item, ringIndex);
    }

    /// <summary>
    /// Unequip the given slot, returning the item to the backpack.
    /// Returns the unequipped item on success, null if the slot was empty or
    /// the backpack was full and could not accept a new item type.
    /// </summary>
    public ItemDef? Unequip(EquipSlot slot, Inventory backpack, int ringIndex = 0)
    {
        if (slot == EquipSlot.Ring && (ringIndex < 0 || ringIndex >= RingSlotCount)) return null;

        var item = GetSlot(slot, ringIndex);
        if (item == null) return null;

        // Check backpack can accept.
        if (backpack.FindSlot(item.Id) < 0 && backpack.FindEmptySlot() < 0)
            return null;

        backpack.TryAdd(item, 1);
        SetSlot(slot, null, ringIndex);
        return item;
    }

    public ItemDef? GetSlot(EquipSlot slot, int ringIndex = 0) => slot switch
    {
        EquipSlot.Head => Head,
        EquipSlot.Body => Body,
        EquipSlot.Arms => Arms,
        EquipSlot.Legs => Legs,
        EquipSlot.Feet => Feet,
        EquipSlot.Neck => Neck,
        EquipSlot.Ring => (ringIndex >= 0 && ringIndex < Rings.Length) ? Rings[ringIndex] : null,
        EquipSlot.MainHand => MainHand,
        EquipSlot.OffHand => OffHand,
        EquipSlot.Ammo => Ammo,
        _ => null,
    };

    private void SetSlot(EquipSlot slot, ItemDef? item, int ringIndex)
    {
        switch (slot)
        {
            case EquipSlot.Head: Head = item; break;
            case EquipSlot.Body: Body = item; break;
            case EquipSlot.Arms: Arms = item; break;
            case EquipSlot.Legs: Legs = item; break;
            case EquipSlot.Feet: Feet = item; break;
            case EquipSlot.Neck: Neck = item; break;
            case EquipSlot.Ring:
                if (ringIndex >= 0 && ringIndex < Rings.Length) Rings[ringIndex] = item;
                break;
            case EquipSlot.MainHand: MainHand = item; break;
            case EquipSlot.OffHand: OffHand = item; break;
            case EquipSlot.Ammo: Ammo = item; break;
        }
    }

    /// <summary>
    /// Sum of all equipped-item stat bonuses, with the 1.25× class-affinity multiplier applied
    /// to items whose ClassAffinity matches <paramref name="playerClass"/>. Items with null
    /// ClassAffinity get 1.0×.
    /// Returned as a <see cref="StatBlock"/> containing raw STR/DEX/STA/INT and bonus HP/damage
    /// aggregated into the stat fields the caller cares about.
    /// </summary>
    public EquipmentBonuses GetTotalBonuses(PlayerClass playerClass)
    {
        var b = new EquipmentBonuses();
        Accumulate(Head, playerClass, b);
        Accumulate(Body, playerClass, b);
        Accumulate(Arms, playerClass, b);
        Accumulate(Legs, playerClass, b);
        Accumulate(Feet, playerClass, b);
        Accumulate(Neck, playerClass, b);
        Accumulate(MainHand, playerClass, b);
        Accumulate(OffHand, playerClass, b);
        Accumulate(Ammo, playerClass, b);
        for (int i = 0; i < Rings.Length; i++)
            Accumulate(Rings[i], playerClass, b);
        return b;
    }

    private static void Accumulate(ItemDef? item, PlayerClass playerClass, EquipmentBonuses b)
    {
        if (item == null) return;
        float mult = (item.ClassAffinity.HasValue && item.ClassAffinity.Value == playerClass)
            ? AffinityMultiplier
            : 1.0f;
        b.Str += item.BonusStr * mult;
        b.Dex += item.BonusDex * mult;
        b.Sta += item.BonusSta * mult;
        b.Int += item.BonusInt * mult;
        b.Hp += item.BonusHp * mult;
        b.Damage += item.BonusDamage * mult;
    }

    /// <summary>
    /// Roll a random equipped-item slot (uniform over filled slots) and destroy the item.
    /// Returns the destroyed item, or null if nothing was equipped.
    /// Used by the death sacrifice flow — Locked flag does NOT protect equipment (spec).
    /// </summary>
    public ItemDef? DestroyRandomEquipped(Random rng)
    {
        var slots = new System.Collections.Generic.List<(EquipSlot slot, int ringIndex)>();
        if (Head != null) slots.Add((EquipSlot.Head, 0));
        if (Body != null) slots.Add((EquipSlot.Body, 0));
        if (Arms != null) slots.Add((EquipSlot.Arms, 0));
        if (Legs != null) slots.Add((EquipSlot.Legs, 0));
        if (Feet != null) slots.Add((EquipSlot.Feet, 0));
        if (Neck != null) slots.Add((EquipSlot.Neck, 0));
        if (MainHand != null) slots.Add((EquipSlot.MainHand, 0));
        if (OffHand != null) slots.Add((EquipSlot.OffHand, 0));
        if (Ammo != null) slots.Add((EquipSlot.Ammo, 0));
        for (int i = 0; i < Rings.Length; i++)
            if (Rings[i] != null) slots.Add((EquipSlot.Ring, i));

        if (slots.Count == 0) return null;
        var (pickSlot, pickRing) = slots[rng.Next(slots.Count)];
        var item = GetSlot(pickSlot, pickRing);
        SetSlot(pickSlot, null, pickRing);
        return item;
    }

    public SavedEquipment CaptureState()
    {
        var data = new SavedEquipment
        {
            Head = Head?.Id,
            Body = Body?.Id,
            Arms = Arms?.Id,
            Legs = Legs?.Id,
            Feet = Feet?.Id,
            Neck = Neck?.Id,
            MainHand = MainHand?.Id,
            OffHand = OffHand?.Id,
            Ammo = Ammo?.Id,
            Rings = new string?[RingSlotCount],
        };
        for (int i = 0; i < Rings.Length; i++)
            data.Rings[i] = Rings[i]?.Id;
        return data;
    }

    public void RestoreState(SavedEquipment data)
    {
        Head = Resolve(data.Head, EquipSlot.Head);
        Body = Resolve(data.Body, EquipSlot.Body);
        Arms = Resolve(data.Arms, EquipSlot.Arms);
        Legs = Resolve(data.Legs, EquipSlot.Legs);
        Feet = Resolve(data.Feet, EquipSlot.Feet);
        Neck = Resolve(data.Neck, EquipSlot.Neck);
        MainHand = Resolve(data.MainHand, EquipSlot.MainHand);
        OffHand = Resolve(data.OffHand, EquipSlot.OffHand);
        Ammo = Resolve(data.Ammo, EquipSlot.Ammo);

        int ringCount = data.Rings?.Length ?? 0;
        for (int i = 0; i < Rings.Length; i++)
            Rings[i] = (i < ringCount) ? Resolve(data.Rings![i], EquipSlot.Ring) : null;
    }

    private static ItemDef? Resolve(string? id, EquipSlot slot)
    {
        if (string.IsNullOrEmpty(id)) return null;
        var def = ItemDatabase.Get(id);
        if (def == null || !IsCompatible(slot, def)) return null;
        return def;
    }
}

/// <summary>
/// Aggregated equipment stat bonuses after the class-affinity multiplier is applied.
/// Fractional because 1.25× may produce non-integer intermediate values; callers floor/round where needed.
/// </summary>
public class EquipmentBonuses
{
    public float Str;
    public float Dex;
    public float Sta;
    public float Int;
    public float Hp;
    public float Damage;
}
