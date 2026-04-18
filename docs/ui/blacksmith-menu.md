# Blacksmith Menu — Four-Tab Wireframe

## Summary

The Blacksmith's service menu consolidates four previously-separate services into one tabbed window: **Forge** (apply affixes to equipment), **Craft** (create items from materials), **Recycle** (break down gear), **Shop** (caravan-stocked materials + consumables). SPEC-BLACKSMITH-MERGED-MENU-01 (Phase G). Voice follows [npc-dialogue-voices.md §Blacksmith](../flows/npc-dialogue-voices.md).

## Current State

**Spec status: LOCKED** via SPEC-BLACKSMITH-MERGED-MENU-01 (Phase G). Unblocks `NPC-ROSTER-REWIRE-01` impl for the Blacksmith service-menu wiring. Replaces the current single-service `BlacksmithWindow.Open()` "Open Forge" entry point with a four-tab window.

## Design

### Why four tabs on one NPC

Prior design had four separate NPCs (Shopkeeper, Blacksmith, Guild Master, Banker/Teleporter). The 3-NPC roster consolidation moved everything forge/craft/recycle/shop-adjacent under the Blacksmith, because:

- **Narrative coherence:** in the Maoyuu-homage village, the Blacksmith IS the one who handles metal, materials, and the caravan trade. A separate shopkeeper didn't fit the frontier scale.
- **Player friction:** one NPC visit for all crafting-adjacent work is faster than three.
- **Voice coherence:** Blacksmith's "pioneer smith learning" voice stretches naturally across Forge/Craft/Recycle. The Shop tab adds "caravan trade" which still fits the smith's remit on a frontier.

### Tab layout

```
┌─────────────────────────────────────────────────────────────┐
│  Blacksmith                                           [X]    │
├─────────────────────────────────────────────────────────────┤
│ [ Forge ]  [ Craft ]  [ Recycle ]  [ Shop ]                  │
├─────────────────────────────────────────────────────────────┤
│                                                              │
│                  (tab content area)                          │
│                                                              │
│                                                              │
│                                                              │
│                                                              │
├─────────────────────────────────────────────────────────────┤
│  Gold: 1,420g    [ Cancel / Close — D ]                      │
└─────────────────────────────────────────────────────────────┘
```

- **Default tab:** Forge (most common interaction).
- **Tab order left-to-right:** Forge → Craft → Recycle → Shop (frequency-of-use order, not alphabetical).
- **Tab navigation:** Q / E cycle left / right; 1-4 jump to specific tab; KeyboardNav handles up/down within a tab.

### Tab 1 — Forge

Apply affixes to equipment. Canonical spec: [items.md §Affix System](../inventory/items.md#affix-system), [synergy-bonuses.md](../systems/synergy-bonuses.md).

**Layout:**
- Left column: player's equipment list (filterable by equipment-ready items).
- Right column: selected item's detail + current affix list + "Add Affix" button.
- "Add Affix" opens a sub-panel listing available affixes (per player's item-level → [AffixDatabase](../../scripts/logic/AffixDatabase.cs)) with gold + material cost.

**Voice:** Blacksmith speaks when a notable event happens (first craft, failed attempt, tier-up). Silent on routine ops.

**Edge case — no equipment:** show empty state "Bring me something to work on, Guildmaster" (in Blacksmith voice).

### Tab 2 — Craft

Create items from materials. Canonical spec: [crafting.md](../systems/blacksmith-forge-rng.md) and related.

**Layout:**
- Left column: recipe list (scrollable, filterable by "can afford" / "all").
- Right column: selected recipe's materials + output preview + "Craft" button.

**Voice:** Blacksmith narrates when an unfamiliar material first appears ("Hm. Never worked this one before. Let me see what she teaches me."). Silent thereafter.

### Tab 3 — Recycle

Break down equipment into gold + materials. Formula locked in [depth-gear-tiers.md §Interaction with Other Systems → Recycling](../systems/depth-gear-tiers.md#interaction-with-other-systems).

**Layout:**
- Left column: player's equipment list.
- Right column: selected item's recycle preview (gold yield + material yield if applicable) + "Recycle" button.

**Voice:** Blacksmith silent on routine recycling. Low-voice comment if the item was notable: "Shame. Good piece. You'll find another."

### Tab 4 — Shop

Buy/sell basic materials + consumables. The caravan restocks the Blacksmith's inventory between dungeon runs.

**Layout:**
- Left column: shop inventory (what the Blacksmith is currently stocking).
- Right column: selected item's details + "Buy" / "Sell" button (contextual).
- Separate Buy/Sell mode toggle at the top.

**Voice:** Blacksmith voice for stock-out ("All out. Come back once the caravan rolls through, or bring me what's missing and I'll trade."), for oddity ("That's a strange thing to be selling, Guildmaster — I'll take it"), and for routine transactions: silent (Guild Maid handles the transactional voice; Blacksmith is the craft-voice, so shop transactions happen quickly without line-per-purchase).

### Shared UI conventions

- **Persistent gold display:** bottom-left of the window, updates in real-time as tabs cost gold.
- **Cancel / Close (D):** closes the window from any tab. No save / autosave per-tab transition — the window is a live interaction, not a multi-step transaction.
- **Keyboard-first:** all operations must be doable without mouse (KeyboardNav conventions per [pause-menu-tabs.md](pause-menu-tabs.md)).
- **Wide-layout inheritance:** window sizing follows the standard `GameWindow` convention — centered modal, 70% of screen width, opens on current tab.

### Interactions with other NPCs

- **Shop tab ≠ Guild Maid.** Prior design had Store under Guild (see [guild-window.md](guild-window.md) — note: that spec is partially superseded by this one; the Store tab moves from Guild Maid to Blacksmith per SPEC-BLACKSMITH-MERGED-MENU-01).
- **Shop tab content differs from prior Guild Store.** Blacksmith's Shop is caravan-stocked: basic materials + basic consumables. Higher-rarity items still come from dungeon drops, not Shop. Guild Maid's tabs are now Bank + Teleport only — no Store.

### State on session start

- **Default tab: Forge.** Regardless of last-tab-used.
- **Clear selections per tab on close:** reopening starts fresh. No "you were looking at X" persistence.

---

## Acceptance Criteria

- [ ] Four tabs exist: Forge, Craft, Recycle, Shop. Order matches spec.
- [ ] Default tab on open is Forge.
- [ ] Tab navigation: Q/E cycles, 1-4 jumps, KeyboardNav handles within-tab movement.
- [ ] Persistent gold display updates in real-time across tabs.
- [ ] D / Escape closes the window from any tab.
- [ ] Each tab's layout matches its section's spec.
- [ ] Blacksmith voice lines trigger per the edge cases listed per tab (not on every routine op).
- [ ] Shop tab's inventory is distinct from prior Guild Store's content — caravan-stocked basics only.

## Implementation Notes

- **BlacksmithWindow rewrite scope:** the current `BlacksmithWindow.Open()` is single-service. This spec requires a TabBar-hosting window similar to `GameWindow` but scoped to Blacksmith services. Consider extracting the TabBar + tab-content pattern into a reusable `TabbedServiceWindow` base class if the Guild Maid menu also lands with multiple tabs.
- **Recycle formula reference:** when the Recycle tab renders the gold-yield preview, it calls `Crafting.RecycleItem(item)` which uses the locked geometric ladder from SPEC-CRAFTING-QUALITY-LADDER-01. No duplication of the formula in UI code.
- **Shop tab content source:** the caravan-stocked inventory list lives in an `ItemDatabase` subset tagged as `BlacksmithShopStock`. When the tag list changes, the Shop tab updates automatically without code edit.
- **Save-state effect:** this menu does not write to save state per-interaction (Forge/Craft/Recycle/Shop are transactional with the save system through the existing transaction paths). No new persistence needed.

## Open Questions

None — spec is locked.
