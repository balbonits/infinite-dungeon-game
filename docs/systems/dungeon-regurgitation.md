# Dungeon Regurgitation

## Summary

When the dungeon consumes equipment (from death loss or backpack wipe), it keeps a record and occasionally regurgitates those exact items back into the world — as monster drops or chest contents — at or below the floor they were lost on. Affixes are preserved. Rate is deliberately brutal-low: lost gear should FEEL lost, and the rare return is meant to be a bittersweet surprise rather than a safety net.

## Current State

**Spec status: DRAFT.**

This is a new system. Nothing is implemented. Depends on:
- [death.md](death.md) — the 5-option sacrifice dialog is the primary source of consumed items.
- [equipment.md](equipment.md) — defines the 1-random-piece equipment loss rule that feeds the pool.
- [monster-drops.md](monster-drops.md) — regurg items hijack the equipment channel.
- [items.md](../inventory/items.md) — defines the item + affix data model that must round-trip through the save file.

## Lore

> *"The dungeon does not forget what it swallows. Each piece of gear it takes — the buckle, the blade, the bright arcane embroidery of a mage's hem — it digests, sorts, and sometimes spits back up, rearranged among the refuse of a deeper floor. A skeleton on floor 3 holds the very sword a warrior lost on floor 8 last month. The dungeon is not giving it back. The dungeon is showing it to the player. It is saying: I have been eating. I have been busy."*

This extends the existing death lore from [death.md](death.md) (§ "The Dungeon's Appetite"). That spec already says the dungeon "regurgitates what it consumed back into its halls, rearranged." This spec makes that mechanic concrete.

**The comedic beat is intended design.** When a low-floor Elite chestpiece that was lost on floor 8 finally regurgitates on floor 37 — eight item-levels behind the player and statistically trash — that is the point. The dungeon does not know or care that the player has outgrown the gear. It returns what it took, on its own schedule, without judgment. Framing it as "overleveled trash drop" in the tooltip text (via a subtle "Recovered" flag — see **Visual Surfacing** below) turns a mechanical downgrade into lore. The player laughs, pockets it for sentimental/recycle value, and moves on. This is celebrated, not patched.

## Design

### R1 — Pool Scope (locked: R1-A)

**Per save slot, per character.** The pool is a property of the active save. Deleting a save slot wipes the pool. New characters in other slots have their own, independent pools. No cross-character inheritance.

### R2 — Preservation Fidelity (locked: R2-A)

**Full preservation.** Items re-enter the pool with every field intact:
- Base item ID
- Base quality (Normal / Superior / Elite / Masterwork / Mythic / Transcendent)
- Item level (the floor it was originally lost on — **not** the current floor when it resurfaces)
- Every affix (name, type, stat, value, tier) — all 6 (or up to 12 for Transcendent)
- Lock flag is **stripped** on regurg (item returns unlocked)

Items are serialized to disk as `GraveyardEntry` records (see **Data Model** below). The save file is the authoritative pool.

### R3 — Re-Entry Channels (locked: R3-C)

**Both channels.** Regurgitated items can surface via:

| Channel | Per-roll weight pulling from pool |
|---|---|
| Monster equipment drop | Low — 0.5% of equipment drops that succeed on eligible floors |
| Treasure room chest | Higher — 3% of chest equipment slots on eligible floors |

Chests are the **primary** channel — the lore fits ("the dungeon sorts its digested goods into containers at deeper floors" per [death.md](death.md)) and the pacing works (chests are already dopamine-moments). Monster drops are the rare surprise bonus channel.

When the regurg roll fires, the system pulls one entry from the eligible-pool subset (see R5) and **replaces** the would-have-been-rolled item. The regurgitated entry is then removed from the pool (no duplicate recoveries).

### R4 — Pity / Rate Gating (locked: R4-C)

**Floor-gated, no pity timer. Pure probability.**

The rate is deliberately brutal-low:
- Monster drop: 0.5% of successful equipment drops check the pool. If the pool is empty **or** has no entries eligible under R5, no regurg occurs and the normal drop proceeds.
- Chest slot: 3% of chest equipment rolls check the pool. Same empty-pool behavior.

**No pity counter.** The pool does not track "floors since last regurg." The dungeon does not owe the player their stuff. If RNG withholds a lost sword for 200 floors, that is working as intended. Pity timers are a concession to player frustration, and this system is designed to **be** frustrating-but-funny. Lean in.

Rates are **tunable** during playtesting — the 0.5%/3% values are starting points. The ratio (chests ≈ 6× drops) is locked.

### R5 — Depth Constraint (locked: R5-C)

**Items can only resurface at or below the floor they were lost on.**

If an item was lost on floor 12, it is eligible on floors 1–12. On floors 13+ it is **not** a regurg candidate. This is the lore mechanism that drives the comedic "overleveled trash drop" — items get **more** likely to return as the player moves away from where they were lost, but only if the player is backtracking or farming shallower floors.

Subtle consequence: pushing endlessly downward means the pool grows forever and never empties (the player never revisits shallow floors). Farming low floors (Zone 1–2) becomes a soft "graveyard sweep" activity. This is fine — the entire system is optional surface area.

### R6 — Pool Cap / Decay (locked: R6-A)

**Unlimited pool, no FIFO, no time decay.**

Entries linger indefinitely. A save slot that has burned through hundreds of items across a long-lived character will carry hundreds of entries. Storage cost is trivial (a single entry is a few hundred bytes of JSON). Save-load performance impact is negligible within any reasonable playtime — if a save ever hits 10,000+ entries, we revisit the cap decision, but we assume that's a cap on the order of the heat-death of the sun.

### Pool Entry Triggers

Items enter the pool when the dungeon consumes them. The full enumeration:

| Trigger | Does item enter regurg pool? | Notes |
|---|---|---|
| **Accept Fate** at death (1 random equipped piece lost) | **Yes** | The consumed equipped item goes into the pool. |
| **Accept Fate** at death (all backpack items destroyed) | **Yes, for equipment-type items.** Consumables and materials do **not** enter. | Consumables and materials are commodity items with no unique state worth resurrecting; resurrecting them would dilute the comedic surprise of recovering *specific* affixed gear. |
| **Quit Game** at death | **Yes** | Same rules as Accept Fate (equipped piece + equipment-type backpack items). |
| **Save Backpack** at death (1 random equipped piece lost) | **Yes** | The consumed equipped item enters the pool. |
| **Save Equipment** at death (all backpack items destroyed) | **Yes, for equipment-type items only.** | Same rule as Accept Fate's backpack destruction. |
| **Save Both** at death | **No** | Nothing is destroyed — nothing to regurg. |
| **Sacrificial Idol** consumed at death | **No** | Nothing was destroyed. Idol eats the loss; the pool sees no entry. Idol itself is consumed (the idol is not equipment and would not enter anyway). |
| **Manual Drop** via backpack item action | **No** | Player-destroyed items are not dungeon-consumed; they're trashed. The dungeon did not eat them. Fits the lore: the dungeon only sorts what dies inside it. |
| **Sell** at the Guild Maid | **No** | Sold = transacted, not consumed. |
| **Recycle** at the Blacksmith | **No** | Recycled = broken into materials, not consumed. |
| **Gold-buyout mitigation** (Save Both / Save Equip / Save Pack) | **No** | Gold paid replaces what would have been taken. Items not destroyed → no pool entry. |
| **Bank item** (any event) | **No, ever.** | Bank items are never destroyed per [bank.md](../inventory/bank.md). |

The rule can be stated in one sentence: **"An item enters the pool if and only if a sacrifice-dialog outcome destroyed it as an equipment-category item."** Consumables and materials never enter; commodity items re-entering would feel random and un-flavorful.

### Re-Entry Rules (selection + surfacing)

When a regurg roll fires:

1. Compute eligible pool: all entries with `LostOnFloor >= currentFloor`.
2. If eligible subset is empty, regurg does not fire; normal drop proceeds.
3. Pick one entry uniformly at random from the eligible subset. (Future hook: bias toward oldest entries to flush the pool; not implemented here.)
4. Instantiate the item exactly as preserved (base + affixes + quality).
5. Tag the item with a `Recovered = true` flag for visual surfacing.
6. Remove the entry from the pool (no duplicate recoveries).
7. Replace the would-have-been-rolled item with the recovered item.
8. Hand off to the normal drop/chest emit path.

### Interaction with Death Penalty

**Regurgitation does not reduce the death penalty.** The penalty is paid in full at the moment of death (gold + items + EXP). The regurg pool is a **post-hoc lore-flavored second chance**, not a mitigation mechanic.

This keeps [death.md](death.md) authoritative on penalty design — the gold/item tradeoffs are balanced around the assumption that lost items are lost forever. Regurg returns are a bonus surprise at a future unrelated moment, not a recoup.

**Important UX note:** the death dialog and toast messaging do **not** mention the regurg pool. The player should not think "I lost it, but maybe I'll get it back." They should think "I lost it." The regurg return, 40 floors later, is the surprise. Surfacing the pool in the death UI would destroy the core emotional beat.

### Interaction with Equipment-on-Death (SYS-11 + SYS-12)

[equipment.md](equipment.md) § "Equipment on Death" defines the 1-random-equipped-piece rule. That destroyed piece enters the regurg pool per the trigger table above. No change to the SYS-11/SYS-12 surface.

Likewise, [death.md](death.md) § "Equipment Loss Detail" is authoritative on *which* piece is lost. This spec only handles *what happens after*.

### Visual Surfacing (player-facing)

Recovered items surface with subtle flavor in the tooltip and toast:

**Tooltip addition** (appended to standard item tooltip):
```
Recovered
(Last seen on floor 8.)
```

**Toast notification** on pickup:
```
The dungeon returns: Fiery Iron Helmet.
```

No sparkle, no fanfare, no sound cue distinct from normal loot. The dungeon is not celebrating giving it back — it is noting, flatly, that it has returned. The understated surfacing is the joke.

**Lock state:** Recovered items are always unlocked on return, per R2. Player re-locks manually if desired.

### Data Model

Sketch (C# — ITEM-02 / SYS-13 implementing team writes the real thing):

```csharp
public record GraveyardEntry
{
    public string ItemId { get; init; } = "";                  // Base item catalog ID
    public BaseQuality Quality { get; init; }
    public int ItemLevel { get; init; }                         // Floor it was crafted at
    public int LostOnFloor { get; init; }                       // Floor where it entered the pool
    public SavedAffix[] Affixes { get; init; } = System.Array.Empty<SavedAffix>();
    public long EntryTimestamp { get; init; }                   // Unix seconds — for future "oldest first" bias
}

public record SavedAffix
{
    public string AffixId { get; init; } = "";
    public int Tier { get; init; }
    public float Value { get; init; }
}

public record GraveyardPool
{
    public List<GraveyardEntry> Entries { get; init; } = new();
    public int LifetimeConsumed { get; init; }                  // Stat tracking — how many items ever entered
    public int LifetimeRecovered { get; init; }                 // Stat tracking — how many ever returned
}
```

`SaveData` extension:

```csharp
// In SaveData.cs — add optional field, backward-compatible
public GraveyardPool? Graveyard { get; init; }
```

**Backward compatibility:** the field is nullable. Existing saves without `Graveyard` deserialize to `null` and are treated as "empty pool." No migration needed. Version bump is **not** required.

### Recovery Stats (Dungeon Ledger hook)

Two counters fit naturally into the existing Dungeon Ledger ([achievements.md](achievements.md)):

| Counter | Fires |
|---|---|
| Items Consumed by the Dungeon | On every pool entry. |
| Items Returned by the Dungeon | On every successful regurg pull. |

Future achievement candidates (not defined here — hand off to whoever owns the Ledger): "Recovered 1 / 10 / 100 items," "Survived a run where the dungeon returned your starting sword," etc. Do not block on these.

## Edge Cases

| Case | Resolution |
|---|---|
| Starting gear destroyed on death | Enters pool. The player's floor-1 Iron Short Sword can resurface on floor 1 forever after. Flavorful. |
| Item's base was removed from the catalog in a later patch | Entry is skipped silently during the regurg roll; stays in pool indefinitely but never fires. Log a warning. Future "garbage collect unknown IDs on load" pass can clean these up. |
| Affix was removed from `AffixDatabase` in a later patch | Entry still deserializes; unknown affix IDs are dropped at load time with a warning. Item returns with fewer affixes than it was lost with. Rare; not worth versioning the pool around. |
| Player dies with 0 equipped items (fresh char) | Nothing to destroy → nothing enters pool. This is already handled by [equipment.md](equipment.md). |
| Sacrificial Idol consumed but player still picks Accept Fate afterward | Contradictory: Idol auto-consumes **before** the dialog outcome per [death.md](death.md). If the Idol fires, Save Both is applied and Accept Fate is not an option. Not an edge case in practice. |
| Player manually drops an item from the backpack | Does not enter pool (see trigger table). Intentional. |
| Player sells gear, then dies | Sold items are not in the pool. Player can't "recover" something they chose to liquidate. |
| Pool grows to 10,000+ entries | Unlimited per R6-A. Revisit only if save-load times become measurably painful. |
| Regurg pulls an item with an item-level above the player's current level | Fine — the player gets a powerful surprise. The bittersweet-comedy is usually the opposite direction (low-level gear on high-floor), but high-level-gear-on-low-floor regurg is a nice inverse surprise. Ship as-is. |
| Player is on floor 150+ and the pool has no entry with `LostOnFloor >= 150` | Pool fires no regurg. Normal drop proceeds. Eventually an item lost deep enough will land there. |
| Player imports an old save without a `Graveyard` field | Pool starts empty on first load. No migration. |

## Acceptance Criteria

- [ ] Equipment items destroyed via Save Backpack / Save Equipment / Accept Fate / Quit Game enter a per-save-slot `GraveyardPool` with full fidelity (base + affixes + quality + `LostOnFloor`).
- [ ] Consumables, materials, and manually-dropped items do **not** enter the pool.
- [ ] Sacrificial Idol blocks pool entry entirely for that death.
- [ ] Save Both blocks pool entry entirely for that death.
- [ ] Regurg rolls fire at 0.5% per successful monster equipment drop and 3% per chest equipment slot.
- [ ] Regurg only selects entries with `LostOnFloor >= currentFloor`.
- [ ] A regurgitated entry is removed from the pool on surface.
- [ ] Recovered items surface with the `Recovered` tooltip line and the "The dungeon returns: X." toast.
- [ ] Save file is backward-compatible: saves without a `Graveyard` field load with an empty pool.
- [ ] Lock flag is stripped on regurg.
- [ ] Death dialog and death toast do **not** reference the regurg pool.
- [ ] Ledger counters "Items Consumed by the Dungeon" and "Items Returned by the Dungeon" increment correctly.

## Implementation Notes

- Pool lives on `GameState` as `GameState.Graveyard` (nullable, mirroring `SaveData.Graveyard`). Create on first pool entry if null.
- Hook consumption in `DeathPenalty.cs` (the existing destroy-equipment and wipe-backpack code paths). Add a single call `Graveyard.Consume(ItemInstance)` at the destruction site; the helper handles the trigger-table filtering (skip non-equipment).
- Hook regurg rolls in:
  - `MonsterDropTables.Roll(...)` (post-equipment-channel success): call `Graveyard.TryRegurgitate(currentFloor, rng)` with a 0.5% gate. If it returns a `GraveyardEntry`, replace the drop.
  - Treasure chest roll (currently in `ItemGenerator.GenerateCrateLoot` per [item-generation.md](item-generation.md), subject to refactor in ITEM-02): same call at a 3% gate per equipment slot.
- Keep `GraveyardPool` pure-logic (no Godot dependency). Serialize via `System.Text.Json` consistent with the rest of `SaveData`.
- `EntryTimestamp` is reserved for a future "flush oldest first" bias. Implementation team can leave the field written but not read for now.
- Item instantiation from a `GraveyardEntry` rehydrates through the same pipeline that crafted/saved the original item. If that pipeline's API shifts (it will, during ITEM-01 catalog adoption), the `GraveyardEntry` → item hydration call is the single coupling point — document it.
- `Recovered = true` flag: add `bool IsRecovered` to whatever runtime item class wraps `ItemDef` + affixes. Not persisted on re-stash; this is a transient UI tag, not a permanent item property. Once the player transfers the recovered item to the bank or equips it, the flag may be stripped — it has served its narrative purpose. (Implementation detail; if persisting is easier, persist it.)

## Open Questions

None.
