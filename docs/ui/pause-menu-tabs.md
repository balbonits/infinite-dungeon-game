# Pause Menu — Tabbed Redesign

## Summary

Replace the flat button list in the pause menu with an 8-tab layout. Each major system gets its own dedicated tab. Navigation via Q/E shoulder buttons (consistent with existing tabbed UI).

## Layout

```
┌──────────────────────────────────────────────────────────────────────────────┐
│                                   PAUSED                                     │
│ [Inventory] [Equipment] [Skills] [Warrior Arts*] [Quests] [Ledger] [Stats] [System] │
│              Q ◀                                                    ▶ E      │
├──────────────────────────────────────────────────────────────────────────────┤
│                                                                              │
│                            (tab content here)                                │
│                                                                              │
├──────────────────────────────────────────────────────────────────────────────┤
│                                [Resume]                                      │
└──────────────────────────────────────────────────────────────────────────────┘
```

*Tab 4 label changes per class: **Warrior Arts** / **Ranger Crafts** / **Arcane Spells**

## Tabs

### Tab 1: Inventory
Backpack view directly inside the pause panel (5-column slot grid, item details, action menu for Use/Drop).

### Tab 2: Equipment
10 slot types / 19 equippable positions. Click slot to open picker with compatible inventory items. Combined stat bonuses shown at bottom with class affinity indicator. See `docs/systems/equipment.md`.

### Tab 3: Skills
- Header: **SKILLS** + "SP: N available"
- Class masteries grouped by category (e.g., Body: Unarmed, Bladed... / Mind: Discipline, Intimidation)
- Each mastery row: name, level, XP bar, passive bonus value, [+] allocate SP button
- Cross-tab link: each mastery shows which Abilities it unlocks (links to Tab 4)
- Separator + **INNATE** sub-section: Haste, Sense, Fortify, Armor
- Note: hotbar assignment has moved to the Abilities tab (Tab 4)

### Tab 4: Abilities (class-specific)
- Tab label changes per class: **Warrior Arts** / **Ranger Crafts** / **Arcane Spells**
- Header: class-specific name + "AP: N available"
- Abilities grouped by parent Skill mastery
- Each ability row: name, level, XP bar, mana cost, cooldown, [+] allocate AP, [hotbar] assign button
- Locked abilities: grayed out, "Requires [Skill Name] Lv.1"
- Mage-specific: unlearned spells show name + "Unknown Spell"
- Cross-tab link: each ability shows parent Skill mastery level + bonus
- Distinct visual theming per class on this tab

### Tab 5: Quests
Active quest list — 3 radiant quests from the Adventure Guild. Shows quest type, target, progress bar, and rewards (gold + XP). Completed quests marked with checkmark.

### Tab 6: Ledger
The Dungeon Ledger — a sniper's field notes of previous encounters. Tracks:
- Achievements across 5 categories (Combat, Exploration, Progression, Economy, Mastery)
- Per-achievement progress bars and unlock status
- Lifetime stats (total kills, deepest floor, gold earned, deaths, etc.)

### Tab 7: Stats
Stat allocation panel — spend free points on STR/DEX/STA/INT. Shows current values, derived stats (melee bonus, dodge chance, spell damage, etc.), and class level bonuses.

### Tab 8: System
- **Tutorial** — 4-section reference guide
- **Settings** — Gameplay/Display/Audio/Controls
- **Back to Main Menu** — save and return to splash
- **Quit Game** — exit application

## Navigation

- **Q/E** (shoulder buttons): cycle tabs left/right (8 tabs in the cycle)
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
