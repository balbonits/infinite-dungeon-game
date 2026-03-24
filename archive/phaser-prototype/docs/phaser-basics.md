# Phaser 3 Basics

## Summary

A practical reference for how Phaser 3 works, written for web developers who are new to game engines.

## Current State

The game uses Phaser 3.90.0 with arcade physics, one scene (`DungeonScene`), and basic input handling.

## Design

### Game Configuration

The `Phaser.Game` constructor takes a config object:

```js
const game = new Phaser.Game({
  type: Phaser.AUTO,       // WebGL if available, falls back to Canvas
  parent: "app",           // DOM element ID to inject the canvas into
  width: 1100,             // Game world width in pixels
  height: 700,             // Game world height in pixels
  backgroundColor: "#111827",
  physics: {
    default: "arcade",     // Simple AABB/circle collision — fast and sufficient
    arcade: { debug: false }
  },
  scale: {
    mode: Phaser.Scale.FIT,           // Scale canvas to fit container
    autoCenter: Phaser.Scale.CENTER_BOTH  // Center horizontally and vertically
  },
  scene: [DungeonScene]    // Array of scene classes to register
});
```

### Scene Lifecycle

Scenes are like routes in a web app — each has its own lifecycle:

1. **`constructor()`** — called once when the scene is registered. Initialize instance variables here.
2. **`preload()`** — load assets (images, audio, spritesheets). Runs before `create()`.
3. **`create()`** — set up the scene: create game objects, configure physics, bind input. Runs once after `preload()` completes.
4. **`update(time, delta)`** — the game loop. Called every frame (~60fps). All per-frame logic goes here: movement, AI, combat checks.

Think of it as: `constructor` = component mount setup, `create` = `componentDidMount`, `update` = `requestAnimationFrame` callback.

### Physics Bodies

Phaser's arcade physics gives game objects a "body" that handles velocity, acceleration, and collision:

```js
// Create a visual circle, then make it physical
this.player = this.add.circle(x, y, 12, 0x8ed6ff);
this.physics.add.existing(this.player);

// Now you can set velocity, collide with world bounds, etc.
this.player.body.setMaxVelocity(220, 220);
this.player.setCollideWorldBounds(true);
```

### Input

Phaser abstracts input across devices:

```js
// Keyboard — cursor keys (arrows)
this.cursors = this.input.keyboard.createCursorKeys();
// Custom keys
this.keys = this.input.keyboard.addKeys("W,A,S,D");

// Check in update():
if (this.cursors.left.isDown || this.keys.A.isDown) { /* move left */ }

// Pointer (mouse/touch)
this.input.on("pointerdown", (pointer) => { /* handle */ });
```

### Groups

Collections of similar game objects, managed together:

```js
this.enemies = this.physics.add.group();
// Add objects to the group:
this.enemies.add(enemy);
// Iterate over all active members:
this.enemies.children.iterate((enemy) => { /* per-enemy logic */ });
```

Groups enable batch collision detection:
```js
this.physics.add.overlap(this.player, this.enemies, this.onPlayerHit, null, this);
```

### Tweens

Phaser's animation system for smooth property transitions:

```js
this.tweens.add({
  targets: slash,          // Game object to animate
  alpha: 0,               // Fade out
  y: y - 8,               // Move up slightly
  duration: 120,           // Milliseconds
  onComplete: () => slash.destroy()  // Clean up after
});
```

### Timers

Delayed or repeated actions without blocking the game loop:

```js
// One-shot delay
this.time.delayedCall(1400, () => { this.spawnEnemy(); });

// Repeating timer
this.time.addEvent({
  delay: 2800,
  loop: true,
  callback: () => { /* spawn logic */ }
});
```

### Camera

Built-in camera effects:

```js
this.cameras.main.shake(90, 0.0035);  // Duration (ms), intensity
this.cameras.main.startFollow(this.player);  // Follow a target
this.cameras.main.setZoom(1.2);  // Zoom in
```

### Scale Manager

The `FIT` + `CENTER_BOTH` combination handles responsive sizing:
- The canvas scales to fit its parent container while maintaining aspect ratio
- It centers both horizontally and vertically
- Combined with the CSS responsive layout, this works on desktop and mobile

## Open Questions

- Should we use Phaser's built-in scene transitions for moving between dungeon and town?
- When should we switch from `add.circle()` / `add.rectangle()` to proper sprites?
