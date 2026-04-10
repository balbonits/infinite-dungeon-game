# Game Development Fundamentals

A reference library for building games properly. Read the relevant doc BEFORE touching code.

## Bug Prevention (P0) — Read These First

| Doc | Prevents |
|-----|----------|
| [Sprites and Animation](sprites-and-animation.md) | Sprite misalignment, wrong frame indexing, floating characters |
| [Collision and Physics](collision-and-physics.md) | Walking through walls, collision not working |
| [TileMap and Isometric](tilemap-and-isometric.md) | Missing floors, wall clipping, wrong tile sizes |
| [Rendering Pipeline](rendering-pipeline.md) | Z-order confusion, UI behind game world |
| [UI Design](ui-design.md) | Broken positioning, off-screen panels, hardcoded coordinates |
| [Camera and Viewport](camera-and-viewport.md) | Wrong zoom, UI scaling, coordinate confusion |
| [Game Feel](game-feel.md) | Flat combat, no feedback, unresponsive controls |
| [State Machines](state-machines.md) | State bugs, enemies acting while dead, animation glitches |

## System Foundations (P1) — Read Before Building Systems

| Doc | Covers |
|-----|--------|
| [Isometric ARPG](isometric-arpg.md) | ARPG core loop, session pacing, combat feel, loot psychology |
| [Visual Feedback](visual-feedback.md) | Damage numbers, health bars, telegraphing, level-up fanfare |
| [Audio Fundamentals](audio-fundamentals.md) | Music/SFX, audio buses, spatial audio, volume mixing |
| [Save Systems](save-systems.md) | What to persist, serialization, versioning, auto-save |
| [Pathfinding](pathfinding.md) | A*, AStarGrid2D, navigation, enemy movement |
| [Procedural Generation](procedural-generation.md) | BSP, cellular automata, seeds, level feel |
| [2D Performance](performance-2d.md) | Draw calls, object pooling, culling, profiling |
| [Debugging Games](debugging-games.md) | Visual debugging, common bugs, the "play it and look" rule |

## Godot Mastery (P1) — Deep Dives

| Doc | Covers |
|-----|--------|
| [TileSet Deep Dive](godot-tileset-deep.md) | Physics layers on tiles, terrain sets, isometric offsets |
| [Animation Systems](godot-animation.md) | AnimatedSprite2D, AnimationPlayer, SpriteFrames, tweens |
| [2D Shaders](godot-shaders.md) | Flash, outline, fill effects, CanvasItem shaders |
| [Godot 4 C# Patterns](godot4-patterns.md) | Node lifecycle, signals, resources, exports, pitfalls |

## Design Knowledge (P2)

| Doc | Covers |
|-----|--------|
| [Difficulty Design](difficulty-design.md) | Difficulty curves, zone walls, "hard but fair", tuning |
| [Playtesting](playtesting.md) | Why playtesting > specs, first 5 minutes, tuning methodology |
