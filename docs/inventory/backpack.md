# Backpack

## Summary

The backpack is the player's carried inventory. Items and gold in the backpack are **at risk on death**. Starts with 15 slots, expanded at the Blacksmith. Unlimited stacking per slot — one item type per slot, any quantity.

## Current State

**Spec status: LOCKED.** Implemented in `scripts/logic/Inventory.cs` (backpack invariants: one-type-per-slot, unlimited `long` stacks, `Lock`/`Drop`/`Transfer` ops, `Gold` as at-risk pocket) and `scripts/ui/BackpackWindow.cs` (refactored onto `SlotGrid` with Use/Lock/Drop actions). Backpack expansion at the Blacksmith is spec'd but pending — the current build starts at 15 slots with no in-game upgrade flow yet. This spec supersedes the previous backpack design (25 slots, Item Shop NPC expansion, 99-stack cap).

## Design

### Core Rules

- **Starting slots:** 15
- **Always accessible:** the backpack can be opened anywhere (dungeon or town) via the Pause Menu → Inventory tab
- **At risk on death:** all backpack items and backpack gold are at risk unless mitigated. See [death.md](../systems/death.md).
- **Expansion:** +5 slots per upgrade at the **Blacksmith** (Old Master Blacksmith). Cost: `200 × N² gold + crafting materials`
- **One item type per slot.** Unlimited quantity per slot (stored as a 64-bit signed integer — max 9.2 quintillion, far beyond practical need).
- **No stack splitting within the same storage.** A single item type occupies exactly one slot in the backpack. Move part of a stack via the Transfer tab to split across bank and backpack.
- **Gold pocket:** the backpack carries its own gold balance (separate from bank gold), displayed as a label at the bottom of the inventory grid. No Deposit/Withdraw controls on the backpack — those live in the Guild window (Bank tab + Transfer tab).

### Inventory Lore

The backpack uses a "magical pocket dimension" mechanic. It simply exists. The dungeon's mystery magic is the in-world explanation for everything inexplicable — including why a bag can hold millions of arrows or why dying adventurers come back (see [death.md](../systems/death.md)).

### Slot Expansion (Blacksmith)

Backpack upgrades are earned at the Blacksmith's forge. Cost has two components: gold + crafting materials that scale with expansion tier.

| Expansion # | New Total | Gold Cost | Material Cost |
|-------------|-----------|-----------|---------------|
| 1 | 20 | 200 | Basic tier (Store) |
| 2 | 25 | 800 | Basic tier (Store) |
| 3 | 30 | 1,800 | Basic tier (Store) |
| 5 | 40 | 5,000 | Mid tier (dungeon drops) |
| 10 | 65 | 20,000 | Mid + high tier |
| 20 | 115 | 80,000 | High tier (manufactured) |
| N | 15 + N×5 | `200 × N²` | Tier scales with N |

- No hard cap. Infinite expansion fits the infinite dungeon theme.
- Gold paid from bank or backpack pocket (player's choice).
- Materials must be in the backpack at time of upgrade (they're "delivered" to the smith).

### Material Tiers (Leather & Cloth)

Backpacks in-world are built from leathers and cloths. Both categories scale across three tiers:

| Tier | Source | Examples |
|------|--------|----------|
| **Basic** | Store (Guild Maid) — always in stock | Raw leather, coarse cloth |
| **Mid** | Monster drops in dungeon | Tanned leather, dyed cloth, treated hide |
| **High** | **Manufactured** at the Blacksmith's Workshop tab (combines mid-tier mats + rare ingredients) | Enchanted leather, mystic weave, dungeon-silk |

High-tier materials are crafted at the Blacksmith using the Workshop tab — a new sub-interface beside the existing Forge tab. Recipes are always available (no unlock gate); the player just needs the ingredients.

### Unlimited Stacking

One slot = one item type, any quantity. No 99-cap. This applies to all categories:

| Category | Stackable? | Stack limit |
|----------|-----------|-------------|
| Consumables | Yes | Unlimited |
| Materials | Yes | Unlimited |
| Equipment | Yes (per unique item) | Unlimited per unique — but rolled affixes make most gear unique, so stacks of equipment are rare in practice |
| Special | Yes | Unlimited |

**Storage:** `long` (64-bit signed integer). Max = 9,223,372,036,854,775,807 — eight orders of magnitude beyond the biggest stacks observed in reference games (e.g., Melvor Idle ~10¹⁰).

**Display:** abbreviated (K / M / B / T / Qa / Qi / etc.) with the exact number in the tooltip. See [items.md](items.md#number-display) for formatting rules.

### Gold Pocket (Label Only)

- Backpack displays its gold as a label at the bottom of the inventory grid.
- **No Deposit/Withdraw controls on the backpack UI.** Gold transfer between pockets happens in the Guild window (Bank tab or Transfer tab).
- Backpack gold is **at risk on death** (see [death.md](../systems/death.md)).
- When the player picks up gold in the dungeon, it always goes to the backpack pocket (never directly to the bank).

### Item Actions (dropdown on Backpack slots)

Clicking a slot opens an item-actions dropdown. Actions available from the backpack:

| Action | Effect |
|--------|--------|
| Inspect | Show full tooltip (affixes, item level, value) |
| Use | Only if consumable |
| Equip | Only if equippable (item goes to an equipment slot; previously equipped item returns to backpack) |
| Lock / Unlock | Toggles lock state. Locked items cannot be sold or dropped. Lock does **not** protect from death-loss. |
| Transfer | Navigation shortcut — opens the Guild window → Transfer tab with the item pre-selected |
| **Drop** | Destroys the item permanently. Single-confirmation dialog. For stacks: amount input + "Drop All". |

**Drop = destroy.** There is no "drop on ground, pick up later" mechanic. Dropping an item removes it from existence. This is an intentionally weighty decision — use it to free a slot when the bank is inaccessible and the item is truly worthless.

### Death Penalty Interaction

On death, the player is presented a 5-option dialog (see [death.md](../systems/death.md) for full mechanics). The two options that affect the backpack:

- **Save Backpack** — pay gold, keep all backpack items and backpack gold. Lose 1 random equipped item instead.
- **Save Nothing** / **Accept Fate** / **Quit Game** — lose **all** backpack items, **all** backpack gold, and 1 random equipped item.

**Locked items are NOT protected from death-loss.** Lock only prevents accidental Sell/Drop.

### Sacrificial Idol (unchanged from original spec)

If a Sacrificial Idol is in the backpack at death, it acts as a free "Save Both" — the player keeps all equipment and all backpack contents at no gold cost. The idol is consumed on use (single-use).

See [death.md](../systems/death.md) for full idol rules.

### Starting State

- **15 backpack slots** on new game
- **0 backpack gold** on new game (starting gold rule G1: player starts with zero)
- Starting equipment per class goes directly into equipment slots, not the backpack. See [equipment.md](../systems/equipment.md).

## Resolved Questions

| Question | Decision |
|----------|----------|
| Weight system? | No. Slot-based only. |
| Stack caps? | Unlimited per slot. Storage in `long` (9.2 quintillion max). |
| Stack splitting within backpack? | No. Transfer tab is the only split mechanism (move part of a stack to the bank's slot of that item type). |
| Expansion NPC? | Blacksmith (changed from Item Shop — Store is now merged into Guild window, and the Blacksmith thematically owns backpack-craft). |
| Protected slot? | No. All backpack slots are at risk. Lock flag is for anti-accidental-sale, not death protection. |
| Can you drop items? | Yes, in the backpack only. Drop = destroy. Single confirmation. |
| Sell from backpack? | No. Transfer to bank first, then Sell from the Bank tab. |
| Gold UI on backpack? | Display label only. No controls. Deposit/Withdraw in Guild window. |
| Max slots? | No hard cap. Cost scales quadratically + material tier. |
