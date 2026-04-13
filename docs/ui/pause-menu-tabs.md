# Pause Menu — Tabbed Redesign

## Summary

Replace the flat button list in the pause menu with a 7-tab layout. Each major system gets its own dedicated tab. Navigation via Q/E shoulder buttons (consistent with existing tabbed UI).

## Layout

```
┌──────────────────────────────────────────────────────────────────┐
│                            PAUSED                                │
│ [Inventory] [Equipment] [Skills] [Quests] [Ledger] [Stats] [System] │
│              Q ◀                                  ▶ E            │
├──────────────────────────────────────────────────────────────────┤
│                                                                  │
│                      (tab content here)                          │
│                                                                  │
├──────────────────────────────────────────────────────────────────┤
│                          [Resume]                                │
└──────────────────────────────────────────────────────────────────┘
```

## Tabs

### Tab 1: Inventory
Backpack view directly inside the pause panel (5-column slot grid, item details, action menu for Use/Drop).

### Tab 2: Equipment
10 slot types / 19 equippable positions. Click slot to open picker with compatible inventory items. Combined stat bonuses shown at bottom with class affinity indicator. See `docs/systems/equipment.md`.

### Tab 3: Skills
Skill tree directly inside the pause panel (class skill graph, passive bonuses, spend points, assign to hotbar).

### Tab 4: Quests
Active quest list — 3 radiant quests from the Adventure Guild. Shows quest type, target, progress bar, and rewards (gold + XP). Completed quests marked with checkmark.

### Tab 5: Ledger
The Dungeon Ledger — a sniper's field notes of previous encounters. Tracks:
- Achievements across 5 categories (Combat, Exploration, Progression, Economy, Mastery)
- Per-achievement progress bars and unlock status
- Lifetime stats (total kills, deepest floor, gold earned, deaths, etc.)

### Tab 6: Stats
Stat allocation panel — spend free points on STR/DEX/STA/INT. Shows current values, derived stats (melee bonus, dodge chance, spell damage, etc.), and class level bonuses.

### Tab 7: System
- **Tutorial** — 4-section reference guide
- **Settings** — Gameplay/Display/Audio/Controls
- **Back to Main Menu** — save and return to splash
- **Quit Game** — exit application

## Navigation

- **Q/E** (shoulder buttons): cycle tabs left/right
- **Up/Down**: navigate buttons within current tab
- **S** (action_cross): confirm / press focused button
- **D** (action_circle): close pause menu (same as Resume)
- **Esc** (start): toggle pause menu open/close
- Resume button always visible at bottom, regardless of tab

## Files to Modify

- `scripts/ui/PauseMenu.cs` — rewrite to use TabBar + per-tab content containers
- `scenes/pause_menu.tscn` — rebuild scene tree with tabs
- Reuse existing `TabBar.cs` component from `scripts/ui/TabBar.cs`

## Notes

- Inventory, Equipment, and Skills tabs embed their existing windows (BackpackWindow, EquipmentPanel, SkillTreeDialog) directly rather than opening them as separate overlays
- The Journal tab has sub-buttons (Stats, Ledger, Quests) that open their respective dialogs
- The System tab has sub-buttons that trigger their existing actions
- WindowStack input blocking still applies — pause menu is topmost when open
