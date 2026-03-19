# dungeon-web-game

**Game Title:** A Dungeon in the Middle of Nowhere  
**Repo name:** dungeon-web-game (short & practical URL)  
**Current Status:** Early prototype / design & development phase  
**Tech:** Single-file HTML + Phaser 3 (via CDN) – 100% offline browser game  
**Target platforms:** Desktop + mobile web (PWA potential)

## What is this?

**A Dungeon in the Middle of Nowhere** is a never-ending, real-time action dungeon crawler inspired by Diablo 1's atmosphere and loot chase.

You control a single permanent character (Warrior, Archer, or Mage) that grows stronger over time. The dungeon is infinite downward — monsters respawn endlessly on each floor (with a soft cap), letting you farm the same level forever or push deeper for better rewards (and bigger risks). Death hurts (penalties scale with depth), but there's no full permadeath: on death you choose between returning to town or respawning at the last safe spot. Runs can be farmed safely or risked for glory.

Key prototype features so far:
- Class selection + Diablo-style stats (STR/DEX/STA/INT) with bonuses
- Real-time movement (WASD / virtual joystick / gamepad)
- Easy auto-targeting combat (nearest enemy in range)
- Infinite respawning monsters + danger color cues (green/yellow/red)
- Basic save/export system (localStorage + Base64 copy)

Planned highlights:
- Town hub with NPC interaction (Item Shop, Blacksmith, Adventure Guild, Level Teleporter)
- Blacksmith crafting & risky upgrades (materials-based, can break items)
- Dungeon seeds for sharing/replaying exact floors (with anti-exploit limits)
- Depth-scaled death penalties + safe spots at level entrances/exits
- Limitless skill leveling & materials bank

## Why this repo?

I'm a front-end developer (@balbonits) building my first real game.  
This is a personal learning project: single-file HTML/JS, Phaser 3, responsive controls, PWA basics, procedural generation, state management — all while trying to make something addictive and fun.

No fixed release date or polish promises — it's evolving slowly and thoughtfully.

## How to run (right now)

1. Clone the repo
2. Open `index.html` in a modern browser (Chrome/Firefox/Edge recommended)
3. No server needed — works offline after first load

(Current prototype is one HTML file — we'll keep it that way as long as possible.)

## Roadmap / Next steps (loose & flexible)

1. Design & implement town hub + NPC panels
2. Real procedural rooms/corridors + dungeon seeds
3. Blacksmith crafting & upgrade risk system
4. Death penalties, safe spots, quit/crash logic
5. Mobile polish & PWA manifest/service worker
6. First equipment drops & simple inventory

## Contributing

Solo learning project for now — no formal contributions, but feel free to fork, play, and open issues with feedback or questions.

Made with curiosity in Los Angeles, 2026.
