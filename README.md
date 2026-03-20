# dungeon-web-game

**Game Title:** A Dungeon in the Middle of Nowhere  
**Repo name:** dungeon-web-game (short & practical URL)  
**Current Status:** Early prototype / design & development phase  
**Tech:** Single-file HTML + Phaser 3 (via CDN) – 100% offline browser game  
**Target platforms:** Desktop + mobile web (PWA potential)

## What is this?

**A Dungeon in the Middle of Nowhere** is a persistent, never-ending real-time action dungeon crawler inspired by Diablo 1's atmosphere, loot chase, and town hub feel.

You control a **single permanent character** (Warrior, Archer, or Mage) that grows stronger across all sessions — there are no rerolls. The dungeon descends infinitely with endless monster respawns on each floor (soft cap + timers), allowing safe farming on any level or risky deep pushes for better rewards.

Death hurts (gold buyout to mitigate EXP & loot penalties, scaling with deepest floor achieved), but it's not full permadeath. On death you choose: return to town (reset progress) or respawn at the last safe spot (keep current floor layout). Safe spots exist at every floor entrance/exit.

Key prototype features so far:
- Class selection + Diablo-style stats (STR/DEX/STA/INT) with bonuses
- Real-time movement (WASD / virtual joystick / gamepad)
- Easy auto-targeting combat (nearest enemy in range)
- Infinite respawning monsters + danger color cues (green/yellow/red)
- Basic save/export system (localStorage + Base64 copy)

Planned highlights:
- Town hub with NPC interaction (Item Shop, Blacksmith, Adventure Guild, Level Teleporter)
- Depth-scaled death penalties (EXP loss, backpack loot loss) with gold buyout mitigation
- Sacrificial Idol consumable (shop-bought, negates loot loss on death)
- Backpack (risky carry) vs Bank (permanent safe storage) with expansion mechanics
- Blacksmith crafting & risky upgrades (materials-based, can break items)
- Dungeon seeds for sharing/replaying exact floors (with anti-exploit limits)
- Limitless skill leveling

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

1. Death screen UI + penalties calculation + confirmation dialog
2. Safe spots (auto-checkpoint at floor entrance/exit)
3. Town hub scene + NPC walk-up interaction
4. Backpack & bank storage UI + expansion mechanics
5. Dungeon seeds generation/display + seeded mode restrictions
6. Blacksmith crafting basics
7. Mobile polish & PWA manifest/service worker

## Contributing

Solo learning project for now — no formal contributions, but feel free to fork, play, and open issues with feedback or questions.

Made with curiosity in Los Angeles, 2026.