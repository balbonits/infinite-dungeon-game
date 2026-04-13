# Game Flows

Step-by-step interaction flows traced from actual code. Each doc covers one system's input sequences, timing, state changes, and signals. Referenced by AutoPilot walkthrough and manual tests.

## Index

| Flow | File | Key timing |
|------|------|-----------|
| [Splash Screen](splash-screen.md) | Buttons, focus, New Game / Continue | — |
| [Class Selection](class-select.md) | 3 focus zones, confirm tween | 0.4s tween + 2.1s transition |
| [Screen Transition](screen-transition.md) | Fade timing, callback | 2.1s total |
| [Town](town.md) | NPC positions, dungeon entrance | — |
| [NPC Interaction](npc-interaction.md) | Panel open/close, service buttons | 0.15s fade in, 0.1s fade out |
| [Shop](shop.md) | Buy/sell tabs, purchase flow | — |
| [Bank](bank.md) | Deposit, withdraw, expand | — |
| [Blacksmith](blacksmith.md) | Craft, recycle | — |
| [Dungeon](dungeon.md) | Floor gen, spawn, stairs, floor wipe | 3s wipe delay |
| [Combat](combat.md) | Auto-attack, skill hotbar | Class-specific cooldowns |
| [Death](death.md) | 3-step UI, penalties, respawn | — |
| [Pause Menu](pause-menu.md) | Open/close, sub-dialogs | — |
| [Progression](progression.md) | XP, level-up, stat/skill points | — |
| [Save/Load](save-load.md) | Auto-save triggers, load flow | — |
