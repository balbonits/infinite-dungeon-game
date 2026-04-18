# Guild Maid Menu — Two-Tab Wireframe

## Summary

Guild Maid's service menu: **Bank** (gold + item storage, transfers between backpack and bank) and **Teleport** (floor-fast-travel). SPEC-GUILD-MAID-MERGED-MENU-01 (Phase G). Voice follows [npc-dialogue-voices.md §Guild Maid](../flows/npc-dialogue-voices.md).

## Current State

**Spec status: LOCKED** via SPEC-GUILD-MAID-MERGED-MENU-01 (Phase G). Unblocks `NPC-ROSTER-REWIRE-01` impl for the Guild Maid service-menu wiring. Partially supersedes [guild-window.md](guild-window.md) — **the Store tab moves OUT of Guild Maid to the Blacksmith's Shop tab** per SPEC-BLACKSMITH-MERGED-MENU-01; the Transfer tab's item-move function collapses into the Bank tab; the Teleport tab is NEW (moved from the prior separate Teleporter NPC).

## Design

### Why two tabs (down from three)

Prior design gave Guild Maid Store + Bank + Transfer tabs (see [guild-window.md](guild-window.md)). The 3-NPC consolidation:
- **Store → Blacksmith** (caravan-stocked materials + consumables fit the smith's trade better than a front-desk Maid).
- **Transfer → collapsed into Bank** (moving items between backpack and bank is what a Bank UI does; a separate Transfer tab was redundant).
- **Teleport → NEW** (absorbed from the prior Teleporter NPC who is removed from the town roster).

Final: **Bank + Teleport**, two tabs. No quest pickup.

### Tab layout

```
┌─────────────────────────────────────────────────────────────┐
│  Guild Maid                                           [X]    │
├─────────────────────────────────────────────────────────────┤
│ [ Bank ]  [ Teleport ]                                       │
├─────────────────────────────────────────────────────────────┤
│                                                              │
│                  (tab content area)                          │
│                                                              │
│                                                              │
│                                                              │
├─────────────────────────────────────────────────────────────┤
│  Gold: 1,420g   Bank: 14,200g    [ Cancel / Close — D ]      │
└─────────────────────────────────────────────────────────────┘
```

- **Default tab:** Bank (by far the more common interaction).
- **Tab order left-to-right:** Bank → Teleport (frequency).
- **Tab navigation:** Q / E toggles; 1-2 jumps; KeyboardNav within-tab.

### Tab 1 — Bank

Gold + item storage, plus backpack ↔ bank transfers. Canonical detail spec: [bank.md](../inventory/bank.md).

**Layout:**
- Top strip: gold balance (backpack + bank), with Withdraw / Deposit buttons.
- Main body: two columns — **Bank contents** (left) + **Backpack contents** (right). Player moves items between them.
- Bottom strip: action-menu for selected item (Transfer 1 / Transfer All / Sell / Lock / Cancel).

**Transfer behavior:** click-to-move. The previous "Transfer tab" (which was a dedicated tab) collapses into this — Bank tab inherently supports transfer via the two-column layout.

**Voice:** Guild Maid voice on open ("Bank, {Class} Guildmaster. Where would you like to begin?"); silent on routine ops; confirmation line on high-value transactions ("Noted. 84,000 gold deposited."). Neutral tone regardless of balance size — Guild Maid doesn't react to wealth.

**Sell button:** items can be sold directly from the Bank tab, same as the old Guild Window. Sell prices use the existing Shop-sell formula; the sold item's gold lands in bank (not backpack) since the transaction happens at Guild.

### Tab 2 — Teleport

Floor fast-travel. Replaces the prior separate `TeleportDialog` UI opened by a Teleporter NPC.

**Layout:**
- Top strip: current floor indicator + "Stone status" indicator (aligned / aligning / idle).
- Main body: floor-list selector showing all floors the player has previously reached. Highlighted entry = current floor.
- Bottom strip: "Teleport to selected" button (disabled while not in alignment).

**Alignment behavior:** teleport requires the Stone to "align" to a target floor. Alignment takes 1–2 seconds of real time (visual indicator) per [systems/teleport.md](../systems/teleport.md) — spec reference; detailed mechanics live there.

**Voice:** Guild Maid voice on open ("Teleport, Guildmaster. Which floor?"); confirmation on alignment ("The Stone is aligned to Floor 14. Press through when ready."); no celebration on arrival.

**Cost:** teleport has a gold cost per tier of floor. Deducted from bank (if sufficient) or backpack (fallback). No confirmation dialog for cost — it's charged on button press. Insufficient gold shows a Guild Maid line: "Not enough for this destination, {Class} Guildmaster. Would you like to return to the Bank?"

### Shared UI conventions

- **Persistent gold + bank display:** bottom-left, updates in real-time.
- **Cancel / Close (D):** closes the window from any tab.
- **Keyboard-first:** all ops doable without mouse.
- **Modal:** standard `GameWindow` sizing — centered, 70% width, opens on Bank by default.

### Cross-NPC routing

- **Quest questions route to Chief:** if the player mentions quests ("Any work?" "Can I turn in...?"), Guild Maid responds in-voice: "Village Chief handles that, {Class} Guildmaster. He's usually by the fountain." Does NOT open the Chief's menu — player walks over.
- **Forge/Craft/Recycle/Shop route to Blacksmith:** "That's the Blacksmith's domain, Guildmaster. Down the road, hammer in hand." Same pattern as above.

### State on session start

- **Default tab: Bank.**
- **Clear selections per tab on close.**

---

## Acceptance Criteria

- [ ] Two tabs exist: Bank, Teleport. Order matches spec.
- [ ] Default tab on open is Bank.
- [ ] Tab navigation: Q/E toggles, 1-2 jumps, KeyboardNav within-tab.
- [ ] Persistent gold + bank balance display updates in real-time.
- [ ] D / Escape closes from any tab.
- [ ] Bank tab supports Withdraw / Deposit / Transfer (1 or All) / Sell / Lock.
- [ ] Teleport tab lists only previously-reached floors.
- [ ] Teleport cost deducts from bank-first, backpack-fallback (matches [death.md §Gold split](../systems/death.md) convention).
- [ ] Guild Maid voice lines trigger per the edge cases: on open (per tab), on high-value transaction confirm, on routing to another NPC, on insufficient-gold.
- [ ] Cross-NPC routing lines (Chief for quests, Blacksmith for forge/craft/shop) use Guild Maid's crisp-service voice.
- [ ] No quest pickup under Guild Maid (all quest interactions route to Chief).

## Implementation Notes

- **Rename / absorb `GuildWindow`:** the existing `scripts/ui/GuildWindow.cs` is the code-side of the prior 3-tab Store/Bank/Transfer window. Rename to `GuildMaidWindow` and restructure to 2 tabs; migrate Store-tab logic to `BlacksmithWindow` (per SPEC-BLACKSMITH-MERGED-MENU-01); migrate Transfer-tab logic into Bank's two-column layout.
- **Teleport UI absorption:** the existing `TeleportDialog.Instance.Show()` (opened by the removed Teleporter NPC per [npc-interaction.md](../flows/npc-interaction.md)) is absorbed into this window's Teleport tab. The underlying teleport logic stays; only the entry point changes.
- **Save-state effect:** no new persistence. Bank state lives in the existing save; teleport state is runtime.
- **Guild Window partial supersession:** [guild-window.md](guild-window.md) remains LOCKED for the parts of Bank behavior it specifies (sort/filter/search deferrals, amount-input dialogs, etc.). This spec supersedes ONLY the tab-count decision and the Store-tab routing. Do not delete guild-window.md; update it with a pointer at the top noting Store moved to Blacksmith + Teleport added.

## Open Questions

None — spec is locked.
