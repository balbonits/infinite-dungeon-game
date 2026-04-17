# Equipment System

## Summary

Full equipment slotting with 10 slot types (19 equippable slots total including 10 rings). No class restrictions — any class can equip any item. Class-matching gear gets a **+25% stat bonus** (soft affinity). No weight system. Bows require a quiver in the ammo slot to fire. Starting gear provided per class on new game.

Follows the slot layout defined in `docs/inventory/items.md`. Replaces the previous class-restriction rule with class-affinity bonuses.

## Equipment Slots

| Slot | Type | What goes here | Per-class flavor |
|------|------|---------------|-----------------|
| Head | Single | Helmets, bands, crowns, hoods | Warrior: Helmets, Ranger: Bands, Mage: Crowns |
| Body | Single | Chest armor, robes, vests | Warrior: Heavy armor, Ranger: Light armor, Mage: Robes |
| Arms | Single | Gauntlets, braces, bangles | Warrior: Gauntlets, Ranger: Braces, Mage: Bangles |
| Legs | Single | Greaves, breeches, leggings | Warrior: Greaves, Ranger: Chausses, Mage: Leggings |
| Feet | Single | Boots, shoes, sandals | Warrior: Boots, Ranger: Shoes, Mage: Sandals |
| Neck | Single | Necklaces, chokers, amulets | Shared across all classes |
| Rings | **10 slots** | Rings, bands, signets | Shared across all classes. Ring stacking is intentional — major build vector. |
| Main Hand | Single | Weapons | Warrior: Bladed/blunt/polearms, Ranger: Bows/crossbows, Mage: Staves/wands |
| Off Hand | Single | Shields, defensive items, grimoires | Warrior: Shields, Ranger: Defensive melee, Mage: Grimoires/ward orbs |
| Ammo | Single | Quivers, mags, bandoliers | Ranger only (functionally). Other classes can equip but gain nothing. |

**Total: 19 equippable slots** (10 slot types, rings have 10 sub-slots).

## Class Affinity (Replaces Class-Lock)

**No class restrictions.** Any class can equip any item. But items have a `ClassAffinity` field — when the player's class matches, all stat bonuses on the item get **+25%**.

| Item | ClassAffinity | Warrior equips | Mage equips |
|------|--------------|----------------|-------------|
| Iron Greatsword (+5 DMG, +2 STR) | Warrior | +6.25 DMG, +2.5 STR | +5 DMG, +2 STR |
| Oak Staff (+3 DMG, +3 INT) | Mage | +3 DMG, +3 INT | +3.75 DMG, +3.75 INT |
| Chain Mail (+8 DEF) | None | +8 DEF | +8 DEF |
| Mage Robes (+4 DEF, +5 Mana) | Mage | +4 DEF, +5 Mana | +5 DEF, +6.25 Mana |

Items with `ClassAffinity = null` (generic gear) work equally for everyone.

**Recycling stays:** class-mismatched gear is still recyclable at the Blacksmith for materials. It's just not *locked* — the player can choose to wear suboptimal gear if they want.

## Ammo System (Ranger)

- Bows require a quiver/magazine in the Ammo slot to fire ranged. **No quiver = bow becomes a melee weapon** (bash/club attack using the bow as a blunt weapon, reduced damage).
- Ammo is **infinite** — arrows are never consumed. The quiver is a permanent equippable item.
- Quivers carry imbues: fire arrows, poison tips, explosive bolts, etc. Imbues go on the quiver, **not** the bow.
- Bows are stat sticks only (base damage + attack speed). Ranged vs melee is determined by quiver presence.
- Non-Rangers can equip a quiver but it serves no purpose without a bow skill.

## Starting Equipment

Each class begins a new run with gear in Main Hand and Off Hand/Ammo. No starting armor, rings, or accessories — those are found/bought.

| Class | Main Hand | Off Hand / Ammo |
|-------|-----------|-----------------|
| Warrior | Iron Short Sword | Wooden Small Shield (Off Hand) |
| Ranger | Short Bow | Iron Arrows Quiver (Ammo) |
| Mage | Short Staff | Basic Spellbook (Off Hand) |

Starting items go directly into equipment slots (not inventory). Can be unequipped to inventory later.

## Equip Flow

1. Open pause menu → Equipment tab
2. Slots displayed as a list (slot label + item name or "[empty]")
3. Navigate with Up/Down, press S (action_cross) on a slot
4. Picker opens showing compatible items from inventory, filtered by slot type
5. Navigate picker, press S to equip
6. Previously equipped item swaps back to inventory
7. If inventory is full: direct swap (old item goes to the slot new item came from)

### Unequip

- Select occupied slot, press A (action_square) → Action Menu → "Unequip"
- Item returns to inventory
- If inventory is full, show "Inventory full" feedback

### Ring Slots

Ring section shows 10 sub-slots. Same flow: click slot → pick from inventory rings. Stacking the same ring type across multiple slots is allowed and intentional.

## Stat Contribution

All equipment stats are additive within their category:

```
total_flat_bonus = sum(all equipped item flat bonuses * affinity_multiplier)
total_percent_bonus = sum(all equipped item percent bonuses * affinity_multiplier)
final_stat = (base_stat + total_flat_bonus) * (1 + total_percent_bonus / 100)

affinity_multiplier = (item.ClassAffinity == playerClass) ? 1.25 : 1.0
```

10 ring slots can stack the same affix type across all rings. This is the primary build customization vector.

## Item Categories by Slot

| Slot | Accepted ItemCategory values |
|------|------------------------------|
| Head | `Head` |
| Body | `Body` |
| Arms | `Arms` |
| Legs | `Legs` |
| Feet | `Feet` |
| Neck | `Neck` |
| Rings | `Ring` |
| Main Hand | `Weapon` |
| Off Hand | `Shield`, `Spellbook`, `DefensiveMelee` |
| Ammo | `Quiver` |

## Data Model

### EquipSlot enum
```csharp
public enum EquipSlot
{
    None,       // Not equippable
    Head,
    Body,
    Arms,
    Legs,
    Feet,
    Neck,
    Ring,       // 10 sub-slots (Ring0 through Ring9)
    MainHand,
    OffHand,
    Ammo,
}
```

### ItemDef additions
```csharp
public EquipSlot Slot { get; init; }             // Which slot this goes in
public PlayerClass? ClassAffinity { get; init; }   // Null = universal
```

### EquipmentSet class
```csharp
public class EquipmentSet
{
    // Single slots
    public ItemDef? Head, Body, Arms, Legs, Feet, Neck;
    public ItemDef? MainHand, OffHand, Ammo;
    // Ring array
    public ItemDef?[] Rings { get; } = new ItemDef?[10];

    bool TryEquip(EquipSlot slot, ItemDef item, Inventory backpack, int ringIndex = 0);
    ItemDef? Unequip(EquipSlot slot, Inventory backpack, int ringIndex = 0);
    bool HasQuiver();
    StatBlock GetTotalBonuses(PlayerClass playerClass);
    void ForceEquip(EquipSlot slot, ItemDef item, int ringIndex = 0);
    SavedEquipment CaptureState();
    void RestoreState(SavedEquipment data);
}
```

### GameState addition
```csharp
public EquipmentSet Equipment { get; set; } = new();
```

## Save/Load

```csharp
public record SavedEquipment
{
    public string? Head { get; init; }
    public string? Body { get; init; }
    public string? Arms { get; init; }
    public string? Legs { get; init; }
    public string? Feet { get; init; }
    public string? Neck { get; init; }
    public string?[] Rings { get; init; } = new string?[10];
    public string? MainHand { get; init; }
    public string? OffHand { get; init; }
    public string? Ammo { get; init; }
}
```

## UI: Equipment Tab (in Pause Menu)

```
┌────────────────────────────────┐
│  EQUIPMENT                     │
├────────────────────────────────┤
│  Main Hand: [Iron Short Sword] │
│  Off Hand:  [Wooden Shield]    │
│  Ammo:      [empty]            │
│  ─────────────────────────     │
│  Head:      [empty]            │
│  Body:      [empty]            │
│  Arms:      [empty]            │
│  Legs:      [empty]            │
│  Feet:      [empty]            │
│  ─────────────────────────     │
│  Neck:      [empty]            │
│  Rings: 0/10 equipped     [►]  │
├────────────────────────────────┤
│  STR +2  DEX +0  DMG +5       │
│  DEF +3  (Warrior bonus ✓)    │
└────────────────────────────────┘
```

Rings row opens a sub-view showing all 10 ring slots.

## Files to Create

- `scripts/logic/EquipmentSet.cs` — pure logic, slot management, stat calculation, quiver check
- `scripts/ui/EquipmentPanel.cs` — UI panel for the Equipment tab
- Unit tests in `tests/unit/EquipmentSetTests.cs`

## Equipment on Death

Equipment interacts with the death sacrifice dialog (see [death.md](death.md) for the full flow).

### Loss Rule

If the player chooses **Save Backpack**, **Accept Fate**, or **Quit Game** (any option that does not save equipment), the dungeon destroys **exactly 1 random equipped item**. The random roll is uniform across the **19 equipped slots** (Head, Body, Arms, Legs, Feet, Neck, Main Hand, Off Hand, Ammo, + 10 Ring slots), with empty slots skipped (only currently-equipped items are eligible for the roll).

- If nothing is equipped (e.g., just after starting a fresh character before picking up gear), no equipment is lost.
- The lost item is **destroyed** — it does not go to the bank, it does not reappear as loot.
- **Locked equipped items are NOT protected.** The Lock flag on equipment only prevents accidental unequip via the item-actions dropdown; it does nothing during the death roll.

### Saving Equipment (gold cost)

```
equipBuyoutCost = deepestFloor × 25
```

This is ~40% of the backpack buyout cost (`deepestFloor × 60`). Equipment is cheaper to save because long-term investment in affixed gear is worth more than mass-consumable backpack contents.

See [death.md](death.md#buyout-cost-formulas) for the full cost table.

### Interaction with Sacrificial Idol

A Sacrificial Idol in the backpack acts as a free "Save Both" — equipment is kept, backpack is kept, idol is consumed. See [death.md](death.md#sacrificial-idol).

### Locking Equipped Items

Right-click (or dropdown) on any equipped slot exposes **Lock / Unlock**:
- Locked: Unequip is greyed out in the dropdown (prevents accidental unequip)
- Locked: No effect on the death-loss roll (NOT protected)
- Unlocked (default): All actions available

This makes the Lock flag useful for "don't accidentally swap my best weapon while browsing the picker" — not as a death-mitigation tool.

## Files to Modify

- `scripts/logic/Item.cs` — add `EquipSlot` enum, add `Slot`/`ClassAffinity` to `ItemDef`, add new `ItemCategory` values
- `scripts/logic/ItemDatabase.cs` — add `Slot`/`ClassAffinity` to existing items, add starting gear + armor/ring items
- `scripts/autoloads/GameState.cs` — add `Equipment` property, equip starting gear in `Reset()`
- `scripts/logic/SaveData.cs` — add `SavedEquipment`
- `scripts/logic/SaveSystem.cs` — save/load equipment
- `scripts/logic/PlayerStats.cs` — factor in equipment bonuses
- `scripts/Player.cs` — check `HasQuiver()` before firing bow
- `docs/systems/classes.md` — update "Equipment Restrictions" section: remove class-lock, add class-affinity rule
- `docs/inventory/items.md` — update `class_restriction` to `class_affinity` in data model
