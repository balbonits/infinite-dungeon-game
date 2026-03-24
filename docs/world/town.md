# Town Hub

## Summary

A safe hub scene where players interact with NPCs, manage inventory, and prepare for dungeon runs. No combat in town.

## Current State

Not yet implemented. The prototype only has the dungeon scene.

## Design

### Town as a Scene

Town is a separate Phaser scene (`TownScene`) that the player transitions to from the dungeon. It functions like a safe lobby:
- No enemies, no combat
- Walk around freely
- Interact with NPCs by walking up to them

### NPC List

| NPC | Function |
|-----|----------|
| Item Shop | Buy consumables (Sacrificial Idol, potions, etc.) |
| Blacksmith | Craft and upgrade equipment (materials-based, risky upgrades can break items) |
| Adventure Guild | View quests, achievements, or challenges |
| Level Teleporter | Travel to previously visited dungeon floors |
| Banker | Access bank storage (safe, permanent) |

### Interaction Model

- **Walk-up interaction:** player moves near an NPC → interaction panel appears
- No click-to-talk — proximity triggers the UI
- Panel shows the NPC's services (shop inventory, craft options, etc.)
- Walking away dismisses the panel

### Town Access

- Players can return to town from the dungeon via death (choose "Return to Town")
- A town portal or exit staircase on floor 1 leads back to town
- The Level Teleporter in town allows jumping to previously visited floors

## Open Questions

- Should town have a visual layout (walk around a map) or be menu-based?
- Can players access the bank from the dungeon, or is it town-only?
- Should NPCs have dialogue/personality, or be purely functional?
- How does the Blacksmith's "risky upgrade" system work in detail?
- Should there be a social element (leaderboards, ghost players) in town?
