# Glossary

Plain-language definitions for terms used throughout this project. Written for someone who directs the game vision but doesn't write code.

---

**Affix** — A prefix or suffix modifier on a piece of equipment, added by the Blacksmith (e.g., "Fiery" or "of Strength").

**Autoload** — A Godot singleton that stays alive across all scenes, like a global manager that never goes away (e.g., GameState).

**BSP (Binary Space Partitioning)** — An algorithm that divides a rectangular space into smaller rooms by repeatedly splitting it in half, used to generate dungeon layouts.

**CanvasLayer** — A UI layer that stays fixed on screen while the game world scrolls underneath it, like a heads-up display painted on glass.

**CharacterBody2D** — A physics body for characters that can collide with walls and other objects and move through the world.

**Diminishing Returns** — A stat formula where each additional point gives less benefit than the last, so you can always get stronger but never become invincible.

**Entity** — Any game object that participates in gameplay: the player, an enemy, or an NPC — they all share the same underlying data model.

**EntityData** — The unified data class that represents any entity in the game, holding their stats, health, equipment, and everything else in one structure.

**Headless** — Running the game without a visible window, used for automated testing so the test suite can run without someone watching the screen.

**Isometric** — The diamond-shaped perspective used in the game, where the camera looks down at an angle so floors appear as flat diamonds (2:1 width-to-height ratio).

**ISS (Isometric Stone Soup)** — The specific tileset by Screaming Brain Studios that defines the game's visual grid standard: 64x32 pixel floor tiles and 64x64 pixel wall blocks.

**Magicule** — The game's fictional magic particle, the source of all supernatural phenomena in the world.

**Node** — Godot's basic building block; everything in a scene is a node, from a character to a timer to a sound effect.

**Proc Gen (Procedural Generation)** — Creating game content (like dungeon floors) using algorithms instead of designing each one by hand, so the dungeon can be infinite.

**Scene** — A reusable tree of nodes bundled together, like a self-contained component (a player scene, an enemy scene, a menu scene).

**Signal** — Godot's event system where one node broadcasts that something happened and other nodes listen and react, without needing to know about each other directly.

**TileMap / TileMapLayer** — The system that renders the isometric floor and wall grid by placing tile images in a repeating pattern across the screen.

**xUnit** — The C# testing framework used for unit tests, letting us verify that game logic (damage formulas, leveling math, etc.) works correctly without launching the full game.
