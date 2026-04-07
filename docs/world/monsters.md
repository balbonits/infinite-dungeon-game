# Monsters

## Summary

Enemies spawn from screen edges with one of three danger tiers. They chase the player and deal damage on contact. Defeated enemies respawn on timers. In Godot 4, enemies are `CharacterBody2D` scenes with `Area2D` hit detection and timer-based cooldowns.

## Current State

Migrating from Phaser 3 prototype to Godot 4:
- Enemies spawn from random screen edges
- Three danger tiers with distinct colors: green (low), yellow (mid), red (high)
- Each tier has scaling HP (`18 + tier * 12`) and speed (`48 + tier * 18`)
- Enemies chase the player using direct pathfinding (straight-line pursuit)
- Deal `3 + dangerTier` damage on overlap (700ms cooldown per enemy)
- Defeated enemies respawn after 1.4s delay
- Soft cap: spawn timer only adds new enemies if fewer than 14 are active
- 10 enemies spawned on initial game start

## Design

### Danger Tiers (Legacy — Phaser Prototype)

The Phaser prototype used 3 fixed danger tiers with hardcoded colors. This system is **replaced by the unified color gradient** (see [color-system.md](../systems/color-system.md)), where monster color is computed from the level gap between the player and the monster's effective level (based on floor depth). The stats below remain valid for balance reference.

| Tier | Name | Color (Hex) | Color (Godot) | HP Formula | HP | Speed Formula | Speed | Damage Formula | Damage | XP Formula | XP Reward |
|------|------|-------------|---------------|-----------|-----|---------------|-------|----------------|--------|------------|-----------|
| 1 | Low | `#6bff89` | `Color("#6bff89")` | 18 + 1 * 12 | 30 | 48 + 1 * 18 | 66 | 3 + 1 | 4 | 10 + 1 * 4 | 14 |
| 2 | Mid | `#ffde66` | `Color("#ffde66")` | 18 + 2 * 12 | 42 | 48 + 2 * 18 | 84 | 3 + 2 | 5 | 10 + 2 * 4 | 18 |
| 3 | High | `#ff6f6f` | `Color("#ff6f6f")` | 18 + 3 * 12 | 54 | 48 + 3 * 18 | 102 | 3 + 3 | 6 | 10 + 3 * 4 | 22 |

**New color system:** Monster color is a continuous gradient (Grey → Blue → Cyan → Green → Yellow → Gold → Orange → Red) based on the level gap between the player and the monster. Warmer = more dangerous, cooler = less threatening. XP rewards scale with threat — harder (warmer) enemies give bonus XP. See [color-system.md](../systems/color-system.md) for full details.

### Stat Formulas (General)

All formulas are parameterized by `danger_tier` (integer, 1-3):

| Stat | Formula | Notes |
|------|---------|-------|
| HP | `18 + danger_tier * 12` | Linear scaling, tier 1 starts at 30 |
| Speed | `48 + danger_tier * 18` | Pixels per second, higher tiers are faster |
| Contact Damage | `3 + danger_tier` | Damage dealt to player per hit |
| XP Reward | `10 + danger_tier * 4` | XP awarded to player on kill |

### Spawning Rules

| Parameter | Value | Notes |
|-----------|-------|-------|
| Initial spawn count | 10 | Spawned immediately when game/floor starts |
| Soft cap | 14 | Periodic timer will not spawn above this count |
| Periodic spawn interval | 2.8 seconds | Repeating timer checks cap and spawns one enemy |
| Respawn delay (after kill) | 1.4 seconds | Individual timer per killed enemy |
| Tier assignment | Random 1-3 | `randi_range(1, 3)` -- equal probability |

#### Spawn Positions

Enemies spawn at random positions along the four edges of the play area:
- **Edge 0 (Top):** random X across full width, Y = 10
- **Edge 1 (Right):** X = world_width - 10, random Y across full height
- **Edge 2 (Bottom):** random X across full width, Y = world_height - 10
- **Edge 3 (Left):** X = 10, random Y across full height

Edge selection is random with equal probability: `randi_range(0, 3)`.

### AI Behavior

Currently simple: all enemies chase the player in a straight line. No pathfinding, no special behaviors, no state machine.

**Chase Logic:**
1. Every physics frame, get the player's global position
2. Calculate direction vector from enemy to player: `(player.global_position - global_position).normalized()`
3. Set velocity to `direction * move_speed`
4. Call `move_and_slide()` for physics-based movement with collision

**No avoidance:** Enemies do not avoid each other or obstacles. They stack on the player position if unimpeded.

### Damage Dealing

**Contact damage model:** Enemies deal damage when their `HitArea` (Area2D) overlaps the player's body. A per-enemy cooldown timer prevents damage from being dealt every frame.

**Cooldown flow:**
1. `HitArea.body_entered` signal fires when player enters the enemy's hit zone
2. If `HitCooldownTimer` is stopped (not on cooldown): deal `3 + danger_tier` damage, start cooldown timer (0.7s)
3. When `HitCooldownTimer.timeout` fires: check if player is still overlapping via `get_overlapping_bodies()`
4. If player is still overlapping: deal damage again, restart the cooldown timer
5. If player has left the hit zone: do nothing, timer stays stopped

This creates continuous damage at 0.7s intervals while the player remains in contact with an enemy, matching the Phaser prototype behavior where `onPlayerHit` checked `now - enemy.lastHitAt < 700`.

### Player Attack vs. Enemies

Player attacks are auto-targeted (see `docs/systems/combat.md`):

| Player Stat | Value |
|-------------|-------|
| Attack cooldown | 0.42 seconds (420ms) |
| Attack range | 78 pixels |
| Damage formula | `12 + floor(level * 1.5)` |
| Target selection | Nearest enemy within range |

**Damage per level (first 10 levels):**

| Level | Damage Formula | Damage |
|-------|---------------|--------|
| 1 | 12 + floor(1 * 1.5) | 13 |
| 2 | 12 + floor(2 * 1.5) | 15 |
| 3 | 12 + floor(3 * 1.5) | 16 |
| 4 | 12 + floor(4 * 1.5) | 18 |
| 5 | 12 + floor(5 * 1.5) | 19 |
| 6 | 12 + floor(6 * 1.5) | 21 |
| 7 | 12 + floor(7 * 1.5) | 22 |
| 8 | 12 + floor(8 * 1.5) | 24 |
| 9 | 12 + floor(9 * 1.5) | 25 |
| 10 | 12 + floor(10 * 1.5) | 27 |

**Hits to kill by tier and level:**

| Level | Damage | Tier 1 (30 HP) | Tier 2 (42 HP) | Tier 3 (54 HP) |
|-------|--------|-----------------|-----------------|-----------------|
| 1 | 13 | 3 hits | 4 hits | 5 hits |
| 2 | 15 | 2 hits | 3 hits | 4 hits |
| 3 | 16 | 2 hits | 3 hits | 4 hits |
| 5 | 19 | 2 hits | 3 hits | 3 hits |
| 10 | 27 | 2 hits | 2 hits | 2 hits |

### Slash Visual Effect

When the player attacks an enemy, a gold-colored slash mark appears at the enemy's position:
- Color: `#f5c86b` (accent gold), 95% opacity
- Size: 26 x 4 pixels (thin rectangle)
- Rotation: random between -1.2 and +1.2 radians
- Animation: fades to 0 alpha while moving 8px upward over 120ms, then destroyed
- One slash per attack (appears at the targeted enemy's position)

### Future Enemy Types

As the dungeon deepens, enemies should become more varied:
- **Ranged enemies** -- shoot projectiles instead of chasing
- **Fast enemies** -- low HP but very high speed
- **Tank enemies** -- slow but high HP and damage
- **Boss enemies** -- appear on milestone floors, unique mechanics
- **Elemental variants** -- resist certain damage types

### Floor-Based Scaling

Enemy stats should scale with floor depth:
- Deeper floors spawn higher-tier enemies more frequently
- Enemy base stats (HP, speed, damage) increase with floor number
- New enemy types unlock at certain floor thresholds

## Implementation Notes

### Enemy Scene (enemy.tscn)

```
Enemy (CharacterBody2D) [Enemy.cs]
├── CollisionShape2D (CircleShape2D, radius=10)
│   Purpose: Physics body for move_and_slide() wall/body collisions
├── Sprite (Polygon2D, diamond shape)
│   Points: [Vector2(0, -10), Vector2(10, 0), Vector2(0, 10), Vector2(-10, 0)]
│   Color: set dynamically based on danger_tier
├── HitArea (Area2D, collision_layer=0, collision_mask=2)
│   Purpose: Detect overlap with player for contact damage
│   └── HitShape (CollisionShape2D, CircleShape2D, radius=15)
│       Slightly larger than body collision to make contact damage reliable
└── HitCooldownTimer (Timer, wait_time=0.7, one_shot=true)
    Purpose: 700ms cooldown between contact damage hits
```

### Collision Layer Setup

| Layer | Name | Used By |
|-------|------|---------|
| 1 | World | Walls, floor boundaries |
| 2 | Player | Player's CharacterBody2D |
| 3 | Enemies | Enemy CharacterBody2D nodes |

The enemy's `HitArea` has:
- `collision_layer = 0` (does not report itself on any layer)
- `collision_mask = 2` (detects bodies on layer 2 = player)

This means the HitArea only detects the player, never other enemies or walls.

### Enemy.cs Script

```csharp
using Godot;
using System.Collections.Generic;

public partial class Enemy : CharacterBody2D
{
    [Export] public int DangerTier { get; set; } = 1;

    public int Hp { get; private set; }
    public float MoveSpeed { get; private set; }
    public int Damage { get; private set; }
    public int XpReward { get; private set; }

    private Area2D _hitArea;
    private Timer _hitCooldown;
    private Polygon2D _sprite;

    private static readonly Dictionary<int, Color> TierColors = new()
    {
        { 1, new Color("#6bff89") },  // Green - low danger
        { 2, new Color("#ffde66") },  // Yellow - mid danger
        { 3, new Color("#ff6f6f") },  // Red - high danger
    };

    public override void _Ready()
    {
        _hitArea = GetNode<Area2D>("HitArea");
        _hitCooldown = GetNode<Timer>("HitCooldownTimer");
        _sprite = GetNode<Polygon2D>("Sprite");

        Hp = 18 + DangerTier * 12;
        MoveSpeed = 48.0f + DangerTier * 18.0f;
        Damage = 3 + DangerTier;
        XpReward = 10 + DangerTier * 4;
        _sprite.Color = TierColors.GetValueOrDefault(DangerTier, Colors.White);

        _hitArea.BodyEntered += OnHitAreaBodyEntered;
        _hitCooldown.Timeout += OnHitCooldownTimeout;
    }

    public override void _PhysicsProcess(double delta)
    {
        var player = GetTree().GetFirstNodeInGroup("player");
        if (player == null)
            return;
        var direction = (((Node2D)player).GlobalPosition - GlobalPosition).Normalized();
        Velocity = direction * MoveSpeed;
        MoveAndSlide();
    }

    public void TakeDamage(int amount)
    {
        Hp -= amount;
        if (Hp <= 0)
            Die();
    }

    public void Die()
    {
        QueueFree();
    }

    private void OnHitAreaBodyEntered(Node2D body)
    {
        if (body.IsInGroup("player") && _hitCooldown.IsStopped())
            DealDamageToPlayer(body);
    }

    private void OnHitCooldownTimeout()
    {
        // Check if player is still overlapping
        foreach (var body in _hitArea.GetOverlappingBodies())
        {
            if (body.IsInGroup("player"))
            {
                DealDamageToPlayer((Node2D)body);
                return;
            }
        }
    }

    private void DealDamageToPlayer(Node2D player)
    {
        GameState.TakeDamage(Damage);
        _hitCooldown.Start();
    }
}
```

### Spawning System

Enemy spawning is managed by the main game scene (not by individual enemies). The spawn manager:

1. **On scene start:** Spawns 10 enemies at random edge positions with random tiers
2. **Periodic timer (2.8s):** Checks if active enemy count < 14; if so, spawns one new enemy
3. **On enemy death:** Starts a 1.4s one-shot timer; on timeout, spawns a replacement enemy

```csharp
// In the main game scene or spawn manager:
using Godot;

public partial class SpawnManager : Node
{
    private PackedScene _enemyScene = GD.Load<PackedScene>("res://scenes/enemy.tscn");

    public override void _Ready()
    {
        // Initial spawn
        for (int i = 0; i < 10; i++)
            SpawnEnemy();

        // Periodic spawn timer
        var spawnTimer = new Timer();
        spawnTimer.WaitTime = 2.8;
        spawnTimer.Autostart = true;
        spawnTimer.Timeout += OnSpawnTimer;
        AddChild(spawnTimer);
    }

    public void SpawnEnemy()
    {
        var enemy = _enemyScene.Instantiate<Enemy>();
        enemy.DangerTier = (int)GD.RandRange(1, 3);
        enemy.GlobalPosition = RandomEdgePosition();
        AddChild(enemy);
        enemy.TreeExiting += () => OnEnemyDied(enemy);
    }

    private void OnSpawnTimer()
    {
        if (GetTree().GetNodesInGroup("enemies").Count < 14)
            SpawnEnemy();
    }

    private void OnEnemyDied(Node enemy)
    {
        // Respawn after 1.4s delay
        GetTree().CreateTimer(1.4).Timeout += SpawnEnemy;
    }

    private Vector2 RandomEdgePosition()
    {
        int edge = (int)GD.RandRange(0, 3);
        return edge switch
        {
            0 => new Vector2((float)GD.RandRange(0, _worldWidth), 10),   // Top
            1 => new Vector2(_worldWidth - 10, (float)GD.RandRange(0, _worldHeight)), // Right
            2 => new Vector2((float)GD.RandRange(0, _worldWidth), _worldHeight - 10), // Bottom
            3 => new Vector2(10, (float)GD.RandRange(0, _worldHeight)),  // Left
            _ => Vector2.Zero,
        };
    }
}
```

### Camera Shake on Hit

When the player takes damage from an enemy, the camera shakes:
- Intensity: +/-3 pixels displacement
- Duration: 0.045 seconds (approximately 90ms total -- 45ms offset, 45ms return)
- Phaser equivalent: `this.cameras.main.shake(90, 0.0035)` -- 90ms duration, 0.0035 intensity (which is ~3.85 pixels on a 1100px canvas)

In Godot, camera shake can be implemented on the `Camera2D` node:
```csharp
public void Shake(float intensity = 3.0f, float duration = 0.045f)
{
    var tween = CreateTween();
    tween.TweenProperty(this, "offset",
        new Vector2((float)GD.RandRange(-intensity, intensity), (float)GD.RandRange(-intensity, intensity)),
        duration);
    tween.TweenProperty(this, "offset", Vector2.Zero, duration);
}
```

### Comparison to Phaser Prototype

| Aspect | Phaser 3 | Godot 4 (C#) |
|--------|----------|--------------|
| Enemy node | `this.add.circle(x, y, 10, tint)` | `CharacterBody2D` + `Polygon2D` (diamond) |
| Physics body | `this.physics.add.existing(enemy)` | Built into `CharacterBody2D` |
| Movement | `this.physics.moveToObject(enemy, player, speed)` | Manual `Velocity = direction * speed` + `MoveAndSlide()` |
| Hit detection | `this.physics.add.overlap(player, enemies, callback)` | `Area2D.BodyEntered` signal |
| Hit cooldown | `enemy.lastHitAt` timestamp comparison | `Timer` node (one_shot, 0.7s) |
| Respawn delay | `this.time.delayedCall(1400, callback)` | `GetTree().CreateTimer(1.4).Timeout` or Timer node |
| Spawn cap check | `this.enemies.countActive(true) < 14` | `GetTree().GetNodesInGroup("enemies").Count < 14` |
| Random tier | `Phaser.Math.Between(1, 3)` | `GD.RandRange(1, 3)` |
| Death | `enemy.disableBody(true, true)` | `QueueFree()` |

## Open Questions

- Should enemies have individual visual identities (different shapes/emoji)?
- How should enemy density scale with floor depth?
- At what floor depth should boss enemies appear?
- Should enemies drop loot directly, or is loot handled separately?
- Should enemy AI ever include pathfinding around obstacles?
- Should enemies be in a "enemies" group for easy counting/querying?
- Should enemy death play a visual effect (fade out, particles)?
- Should there be a maximum total enemy count (hard cap) beyond the soft cap?
