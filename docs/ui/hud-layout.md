# HUD Layout

## Summary

Where every on-screen gameplay element lives, how the player toggles visibility, and which hotkeys reach which panel. SPEC-HUD-LAYOUT-01 (Phase H). Authored at the locked 1280×720 design resolution per [SPEC-UI-HIGH-DPI-01](high-dpi.md).

## Current State

**Spec status: LOCKED** via SPEC-HUD-LAYOUT-01 (Phase H). Based on the existing `scripts/ui/Hud.cs` Diablo-style orb layout, extended with explicit zone placement, toggles, and a stats-panel hotkey.

## Design

### Zone map (1280 × 720 design canvas)

```
┌─────────────────────────────────────────────────────────────┐
│ TOP-LEFT         TOP-CENTER           TOP-RIGHT              │
│  Floor depth    Active buffs/debuffs   Stairs compass        │
│  (F23)          (Haste/Sense/Fortify)  (distance to stairs)  │
│                                                              │
│                                                              │
│                                                              │
│                                                              │
│                                                              │
│             (game world renders here)                        │
│                                                              │
│                                                              │
│                                                              │
│                                                              │
│                                                              │
│                 MID-CENTER                                   │
│            (floating damage / XP text)                       │
│                                                              │
│                                                              │
├──────────┬────────────────────────────────┬─────────────────┤
│ HP ORB   │  SKILL BAR (4 slots: 1-2-3-4)  │    MP ORB        │
│ (red)    │                                │    (blue)        │
└──────────┴────────────────────────────────┴─────────────────┘
```

### Element-by-element spec

#### HP orb (bottom-left, always visible)

- Position: flush with bottom-left corner, 8 px margin on left + 6 px margin on bottom.
- Size: 64 × 64 px.
- Render: liquid-fill gradient per [OrbDisplay.cs](../../scripts/ui/OrbDisplay.cs). Red (#CC2222) when above 25% HP; pulse to bright red (#FF4444) when at/below 25% HP.
- Text overlay: current HP / max HP (e.g., "42 / 120"), centered, 16 px font, white with 1 px black outline for readability against the red orb.
- **Never hidden.** HP is core gameplay feedback; no toggle.

#### MP orb (bottom-right, always visible)

- Position: flush with bottom-right corner, 8 px margin on right + 6 px margin on bottom.
- Size: 64 × 64 px.
- Render: liquid-fill gradient. Blue (#2244CC). No low-mana pulse (running out of mana is not a life-or-death moment like low HP).
- Text overlay: current MP / max MP, same styling as HP.
- **Never hidden.** Mana is core gameplay feedback.

#### Skill bar (bottom-center, always visible)

- Position: centered at bottom, between the two orbs. 16 px margin above the bottom edge.
- Size: 4 slots × 56 px = 224 px wide, 56 px tall. Matches [SkillBarHud.cs](../../scripts/ui/SkillBarHud.cs).
- Each slot shows: assigned ability icon + hotkey number (1/2/3/4) + cooldown overlay (if on cooldown).
- Empty slot shows the slot number only (muted) — invites the player to assign something.
- **Never hidden.** The skill bar is the ability UX core.

#### Floor depth indicator (top-left, always visible)

- Position: top-left, 16 px margin.
- Format: "F{floor}" (e.g., "F23"). 20-px font, Accent color (gold #F5C86B) for visibility.
- **Never hidden.** Floor depth is the primary progression signal.

#### Stairs compass (top-right, always visible while dungeon-exploring)

- Position: top-right, 16 px margin.
- Render: small arrow sprite + distance label (e.g., "↖ 8"). Matches [StairsCompass.cs](../../scripts/ui/StairsCompass.cs).
- Hidden: in Town (no stairs).
- **Never hidden while dungeon-exploring.** Essential wayfinding.

#### Active buff/debuff bar (top-center, contextual)

- Position: top-center, 16 px below the top edge.
- Render: horizontal row of buff/debuff icons, each 32 × 32 px with a small timer text underneath.
- Order: Innate toggles (Haste/Sense/Fortify) leftmost; temporary buffs/debuffs right-aligned.
- Hidden when the list is empty.
- **Cannot be user-toggled off.** If a buff is active, the player needs to see it; hiding buffs silently risks missed mana drain.

#### Floating damage / XP text (mid-center)

- Position: rises from the source (enemy for damage-dealt, player for damage-taken, killed enemy for XP).
- Render per [FloatingText.cs](../../scripts/ui/FloatingText.cs). 13-px → 16-px font under the new PS2P size ladder.
- Duration: 0.8s fade + rise.
- **Cannot be user-toggled off.** Critical gameplay feedback.

### Stats panel — toggle + hotkey

The **Stats panel** is a full-stats breakdown (STR/DEX/STA/INT + derived values) that's more detail than the HUD orbs show.

- **Not always on HUD.** Showing the full stat block on every screen is clutter.
- **Hotkey: `Tab`** opens a stats overlay. Press `Tab` again or `D` / `Escape` closes it.
- **Pause-menu access:** the same panel content is available via Pause Menu → Stats tab (per [pause-menu-tabs.md](pause-menu-tabs.md)).
- **When Tab-overlay is open:** game pauses (same convention as other modals). Overlay is centered, semi-transparent backdrop, displays stats + equipment-derived bonuses + innate-progression.
- **Toggle behavior:** Tab is a momentary show while held (hold-to-peek); releasing hides the overlay. Alternative: Tab click-to-toggle + Tab-again-to-close. Picking **hold-to-peek** for faster check-stats-during-combat feel (doesn't require explicit close).

Hotkey table for all HUD-adjacent toggles:

| Key | Action |
|-----|--------|
| `Tab` | Hold-to-peek Stats overlay |
| `M` | Open map overlay (future — SPEC-MAP-01 if authored) |
| `I` | Open Inventory (Pause Menu → Inventory tab; doesn't open whole pause menu) |
| `Esc` / `P` | Open Pause Menu |
| `1` / `2` / `3` / `4` | Trigger skill-bar slot |
| `Q` / `E` | Tab-cycle within service menus (Blacksmith, Guild Maid) |
| `D` | Close topmost modal (uniform "cancel") |

### Visual-design notes

- **Orbs flush with screen edges** — not floating. The Diablo convention. Communicates "this is where the core numbers live; look here at a glance."
- **Skill bar flush with bottom between orbs** — centered visually. Same row as orbs for one consistent "gameplay strip."
- **Top row is lighter-touch** — floor / compass / buffs; players glance at top once in a while, not constantly.
- **Middle stays clean** — game world renders uncluttered; floating damage is the only mid-screen element.

---

## Acceptance Criteria

- [ ] At 1280×720, every element fits in its zone without overlap.
- [ ] HP orb, MP orb, skill bar, floor indicator, stairs compass all render at the specified positions.
- [ ] Active-buff bar appears when any Innate/buff is active; disappears when list empties.
- [ ] Floating text renders from correct source (enemy/player/killed enemy) and fades in 0.8s.
- [ ] Tab hold-to-peek opens Stats overlay; release hides it.
- [ ] Tab-overlay pauses the game while open.
- [ ] No element can be toggled off except the implicitly-contextual buff bar.
- [ ] All HUD text uses the PS2P font from [font.md](font.md) at correct size-ladder values.
- [ ] Under SPEC-UI-HIGH-DPI-01's integer-scale rule, HUD renders crisp at 1×, 2×, 3×.

## Implementation Notes

- **Existing code state:** orbs + skill bar + stairs compass + floating text already implemented. The new additions: buff bar (top-center) and Stats hold-to-peek overlay (Tab).
- **Buff bar source:** pull from `PlayerStats` active-effects list; subscribe to a "buff added/removed" signal to trigger re-layout of the bar.
- **Stats overlay:** extract the existing Pause Menu → Stats tab content into a standalone `Control` that the Hud can instantiate on `Tab` press. Avoid opening the full Pause Menu for a peek.
- **Hold-to-peek mechanics:** use `Input.IsActionPressed("toggle_stats")` in `_Process` to read continuous state; show overlay when true, hide when false. Input action binding: Tab by default, rebindable via SPEC-GAMEPAD-INPUT-01 when that lands.
- **Pause while peeking:** while Tab is held, set `GetTree().Paused = true`; release unsets. Same pattern as modal dialogs.

## Open Questions

None — spec is locked.
