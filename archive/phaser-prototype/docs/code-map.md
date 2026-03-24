# Code Map

## Summary

An annotated walkthrough of `index.html`, mapping each section to the Phaser concept it uses and the game system it implements.

## Current State

The entire game is ~450 lines in one file. This map describes the structure as of the initial prototype. Line numbers may shift as the code evolves.

## Design

### High-Level Flow

```
Browser loads index.html
  → <style> block sets up UI theme and responsive layout
  → <script> loads Phaser 3 from CDN
  → IIFE runs:
    → Define constants (GAME_WIDTH, GAME_HEIGHT, COLORS)
    → Define game state object
    → Define DungeonScene class
    → Create Phaser.Game instance → boots the engine
    → DungeonScene.create() runs → sets up the game world
    → DungeonScene.update() runs every frame → movement, AI, combat
```

### Section-by-Section

#### CSS Theme & Layout (lines ~7–112)

- **CSS custom properties** (`:root`) — dark fantasy color palette
- **Global reset** — `box-sizing`, zero margins
- **Body layout** — CSS Grid centering with safe-area inset padding (mobile)
- **`#app` container** — max-width/height with border, rounded corners, shadow
- **`#overlay` / `#hud`** — positioned absolutely over the canvas, `pointer-events: none` so clicks pass through to the game
- **Responsive breakpoint** (`@media max-width: 900px`) — full-viewport on mobile, shows touch instructions

**Phaser concepts:** None — this is pure HTML/CSS for the UI shell around the canvas.

#### HTML Structure (lines ~114–123)

- `<main id="app">` — Phaser injects its `<canvas>` here
- `<section id="overlay">` — HUD with `aria-live="polite"` for screen readers
- `<p id="hud">` — updated by JavaScript each frame

**Phaser concepts:** `parent: "app"` in game config tells Phaser where to put the canvas.

#### Constants & State (lines ~128–148)

- `GAME_WIDTH`, `GAME_HEIGHT` — world dimensions
- `COLORS` — hex color constants for game objects
- `state` — centralized game state (`hp`, `xp`, `level`)

**Phaser concepts:** None — plain JS. State is read/written by scene methods.

#### DungeonScene Class (lines ~150–425)

##### constructor() (lines ~151–159)
Initializes instance variables to null. These get populated in `create()`.

##### create() (lines ~163–208)
Sets up the entire game world:
- Resets state
- Calls `createBackground()` — draws grid
- Creates player circle with physics body
- Creates enemy group and spawns initial enemies
- Sets up keyboard input (cursors + WASD)
- Registers player-enemy overlap collision
- Sets up pointer (mouse/touch) input handlers
- Starts repeating enemy spawn timer
- Updates HUD

**Phaser concepts:** `this.add.circle()`, `this.physics.add.existing()`, `this.physics.add.group()`, `this.input.keyboard`, `this.physics.add.overlap()`, `this.time.addEvent()`

##### update(time) (lines ~210–218)
The game loop — runs every frame:
1. `handleMovement()` — reads input, sets player velocity
2. `handleEnemyAI()` — moves enemies toward player
3. `tryAutoAttack(time)` — checks cooldown, finds nearest enemy, deals damage

**Phaser concepts:** Scene lifecycle `update()`, `time` parameter for cooldown tracking.

##### createBackground() (lines ~220–237)
Draws a dark grid using Phaser graphics. Sets physics world bounds.

**Phaser concepts:** `this.add.graphics()`, `this.physics.world.setBounds()`

##### spawnEnemy() (lines ~239–274)
Creates an enemy at a random screen edge with a random danger tier (1–3). Each tier has different HP, speed, and color.

**Phaser concepts:** `Phaser.Math.Between()`, `this.add.circle()`, `this.physics.add.existing()`

##### handleMovement() (lines ~276–308)
Reads keyboard and pointer input, normalizes the direction vector, and sets player velocity.

**Phaser concepts:** `this.cursors`, `this.keys`, `Phaser.Math.Vector2`, `body.setVelocity()`

##### handleEnemyAI() (lines ~310–315)
Iterates all enemies and moves each toward the player.

**Phaser concepts:** `this.physics.moveToObject()`, group iteration

##### tryAutoAttack(time) (lines ~317–349)
Finds the nearest enemy within range, applies damage, draws a slash effect, and defeats the enemy if HP reaches zero.

**Phaser concepts:** `Phaser.Math.Distance.Between()`, tweens (via `drawSlash`)

##### drawSlash(x, y) (lines ~352–362)
Creates a small rotated rectangle and tweens it to fade out — a visual hit effect.

**Phaser concepts:** `this.add.rectangle()`, `this.tweens.add()`, `Phaser.Math.FloatBetween()`

##### defeatEnemy(enemy) (lines ~364–381)
Disables the enemy, awards XP, checks for level-up, updates HUD, and schedules a respawn.

**Phaser concepts:** `disableBody()`, `this.time.delayedCall()`

##### onPlayerHit(player, enemy) (lines ~383–399)
Handles damage when player overlaps an enemy. Uses a per-enemy hit cooldown to prevent instant death. Shakes camera on hit.

**Phaser concepts:** `this.cameras.main.shake()`, overlap callback

##### gameOver() (lines ~401–420)
Disables the player, stops all enemies, shows a death panel with restart instructions. Listens for R key or tap to restart.

**Phaser concepts:** `this.add.rectangle()`, `this.add.text()`, `this.scene.restart()`, one-time input listeners

##### updateHud() (lines ~422–424)
Writes current state to the DOM HUD element.

**Phaser concepts:** None — direct DOM manipulation for the overlay.

#### Game Initialization (lines ~427–448)

Creates the `Phaser.Game` instance with the config object. Stores a reference on `window.__dungeonGame` for debugging.

**Phaser concepts:** `new Phaser.Game(config)`, scale manager configuration.

## Open Questions

- Should this map be auto-generated from code comments instead of maintained manually?
- As the codebase grows, should each system get its own annotated section?
