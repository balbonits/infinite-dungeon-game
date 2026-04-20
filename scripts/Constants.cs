using Godot;

namespace DungeonGame;

/// <summary>
/// All game-wide constants, enums, and magic values in one place.
/// If a number or string appears in gameplay code, it should be defined here.
/// </summary>
public static class Constants
{
    // --- Groups (node group names) ---
    public static class Groups
    {
        public const string Player = "player";
        public const string Enemies = "enemies";
    }

    // --- Input Actions (must match project.godot [input] section) ---
    public static class InputActions
    {
        public const string MoveUp = "move_up";
        public const string MoveDown = "move_down";
        public const string MoveLeft = "move_left";
        public const string MoveRight = "move_right";
        public const string ActionCross = "action_cross";
        public const string ActionCircle = "action_circle";
        public const string ActionSquare = "action_square";
        public const string ActionTriangle = "action_triangle";
        public const string ShoulderLeft = "shoulder_left";
        public const string ShoulderRight = "shoulder_right";
        public const string MapToggle = "map_toggle";
        public const string Start = "start";
    }

    // --- Collision Layers (bit values) ---
    public static class Layers
    {
        public const uint Walls = 1;    // bit 0
        public const uint Player = 2;   // bit 1
        public const uint Enemies = 4;  // bit 2
    }

    // --- Player ---
    public static class PlayerStats
    {
        public const float MoveSpeed = 190.0f;
        public const float GracePeriod = 1.5f;
        public const float GraceFlickerAlpha = 0.4f;
        public const int BaseDamage = 12;
        public const float DamagePerLevel = 1.5f;
        public const int StartingHp = 100;
        public const float HealOnLevelUpPercent = 0.15f; // 15% of max HP (spec: leveling.md)

        // Inventory — see docs/inventory/backpack.md and docs/inventory/bank.md
        public const int BackpackStartingSlots = 15;
        public const int BackpackSlotsPerExpansion = 5;
        public const int StartingGold = 0; // G1: d — zero starting gold, earned from first kills

        public static int GetDamage(int level) => BaseDamage + (int)(level * DamagePerLevel);

        // Spec (magic.md): class base mana pools
        public static int GetClassBaseMana(PlayerClass cls) => cls switch
        {
            PlayerClass.Mage => 200,
            PlayerClass.Ranger => 100,
            PlayerClass.Warrior => 60,
            _ => 60,
        };

        // Spec: level_hp = floor(8 + level * 0.5) per level, cumulative.
        // AUDIT-08: replaced O(level) loop with closed-form O(1). Derivation
        // in integer terms — for positive integer l, the per-level term
        // floor(8 + l/2) equals 8 + (l/2) under C# integer division.
        // Summing (l/2) over l = 1..level yields floor(level² / 4):
        // pair l = 2k-1 and l = 2k contribute k-1 and k (summing to the
        // quarter-square). So:
        //   total = StartingHp + 8*level + floor(level² / 4)
        // Exhaustive parity with the pre-fix loop is verified in
        // ConstantsTests.GetMaxHp_ExhaustiveMatchesLoop_0To200. The earlier
        // comment claimed float-identity ((int)(8 + l * 0.5f) == 8 + l/2)
        // which isn't safe for large l (float precision fails past ~2^24);
        // Copilot PR #41 round-3 asked for the pure-integer framing.
        //
        // level <= 0: the original loop skipped entirely, so we return
        // StartingHp. This preserves the pre-AUDIT-08 contract for any
        // corrupted/debug state that slips through.  Copilot PR #41 R1.
        //
        // All arithmetic is in int64 and saturated back to int so the
        // leveling spec's unbounded level range doesn't silently overflow
        // past level ≈ 92 k (int max). HP is still stored as int, so we
        // clamp instead of widening the API. Copilot PR #41 R2.
        /// <summary>
        /// Level-derived MaxHp in int64 space, no saturation. The int-bound
        /// public API <see cref="GetMaxHp"/> saturates the result; callers
        /// that want to combine this with further math (like a bonus) should
        /// start from this long value so a saturated int.MaxValue doesn't
        /// silently absorb a subsequent negative adjustment. Copilot PR #41
        /// round-4 finding.
        /// </summary>
        private static long GetMaxHpLong(int level)
        {
            if (level <= 0) return StartingHp;
            return (long)StartingHp + 8L * level + (long)level * level / 4L;
        }

        public static int GetMaxHp(int level)
        {
            long total = GetMaxHpLong(level);
            if (total > int.MaxValue) return int.MaxValue;
            return (int)total;
        }

        /// <summary>
        /// Combine a level-derived MaxHp with an additive bonus, clamped so
        /// the sum cannot overflow int. Callers that recompute MaxHp
        /// (GameState recalc, StatAllocDialog, PauseMenu, DebugConsole)
        /// should go through this instead of adding raw
        /// <c>GetMaxHp(level) + bonus</c> — otherwise a saturated GetMaxHp
        /// plus a positive bonus would wrap to a negative int. Works on the
        /// pre-saturation long value so a negative bonus applied to a very
        /// large level can still produce a correctly-clamped result (Copilot
        /// PR #41 round-4: the earlier version saturated first, losing the
        /// true base for negative-bonus cases).
        /// </summary>
        public static int GetEffectiveMaxHp(int level, int bonus)
        {
            long total = GetMaxHpLong(level) + bonus;
            if (total > int.MaxValue) return int.MaxValue;
            if (total < 0) return 0;
            return (int)total;
        }
    }

    // --- Class-specific combat ---
    public static class ClassCombat
    {
        // Attack ranges (pixels)
        public const float WarriorMeleeRange = 78.0f;
        public const float RangerProjectileRange = 250.0f;
        public const float MageMeleeRange = 78.0f;
        public const float MageSpellRange = 200.0f;

        // Attack cooldowns (seconds)
        public const float WarriorCooldown = 0.42f;
        public const float RangerCooldown = 0.55f;
        public const float MageMeleeCooldown = 0.50f;
        public const float MageSpellCooldown = 0.80f;

        // Projectile speeds (pixels/second)
        public const float ArrowSpeed = 400.0f;
        public const float MagicBoltSpeed = 300.0f;

        // Projectile sizes (spec: combat.md)
        public const float ArrowScale = 0.6f;
        public const float MagicBoltScale = 0.8f;

        public static float GetAttackRange(PlayerClass playerClass) => playerClass switch
        {
            PlayerClass.Warrior => WarriorMeleeRange,
            PlayerClass.Ranger => RangerProjectileRange,
            PlayerClass.Mage => MageSpellRange,
            _ => WarriorMeleeRange,
        };

        public static float GetAttackCooldown(PlayerClass playerClass) => playerClass switch
        {
            PlayerClass.Warrior => WarriorCooldown,
            PlayerClass.Ranger => RangerCooldown,
            PlayerClass.Mage => MageMeleeCooldown,
            _ => WarriorCooldown,
        };
    }

    // --- Enemy ---
    public static class EnemyStats
    {
        public const int BaseHp = 20;
        public const int HpPerLevel = 10;
        public const float BaseSpeed = 50.0f;
        public const float SpeedPerLevel = 5.0f;
        public const int BaseDamage = 2;
        public const int DamagePerLevel = 1;
        public const int BaseXp = 8;
        public const int XpPerLevel = 4;
        public const float HitCooldown = 0.7f;
        public const float CollisionRadius = 10.0f;
        public const float HitAreaRadius = 15.0f;

        public static int GetHp(int level) => BaseHp + level * HpPerLevel;
        public static float GetSpeed(int level) => BaseSpeed + level * SpeedPerLevel;
        public static int GetDamage(int level) => BaseDamage + level * DamagePerLevel;
        public static int GetXpReward(int level) => BaseXp + level * XpPerLevel;
    }

    // --- Spawning ---
    public static class Spawning
    {
        public const int InitialEnemies = 10;
        public const int EnemySoftCap = 14;
        public const float SpawnInterval = 2.8f;
        public const float RespawnDelay = 1.4f;
        public const float SafeSpawnRadius = 150.0f;
        public const int MaxSpawnRetries = 50;
        public const int SpawnWallMargin = 5;
        public const float DespawnDistance = 800.0f;
    }

    // --- Floor Scaling ---
    public static class FloorScaling
    {
        public const int MinRoomSize = 50;
        public const int MaxRoomSize = 70;
        public const int StairsWallMargin = 4;
        public const int RoomGrowthPerFloors = 5;
        public const int MaxRoomGrowth = 6;

        public static int GetMinEnemyLevel(int floor) => Mathf.Max(1, floor - 1);
        public static int GetMaxEnemyLevel(int floor) => floor + 2;
    }

    // --- Zones (10-floor blocks with zone-exclusive species) ---
    public static class Zones
    {
        public const int FloorsPerZone = 10;

        /// <summary>Get the zone number for a floor (1-indexed).</summary>
        public static int GetZone(int floor) => (floor - 1) / FloorsPerZone + 1;

        /// <summary>
        /// Zone difficulty multiplier (spec: dungeon.md).
        /// zone_multiplier = 1.0 + (zone - 1) * 0.5
        /// intra_zone_multiplier = 1.0 + (intra_zone_step * 0.05)
        /// total = zone_multiplier * intra_zone_multiplier
        /// </summary>
        public static float GetDifficultyMultiplier(int floor)
        {
            int zone = GetZone(floor);
            int intraStep = (floor - 1) % FloorsPerZone;
            float zoneMult = 1.0f + (zone - 1) * 0.5f;
            float intraMult = 1.0f + intraStep * 0.05f;
            return zoneMult * intraMult;
        }

        /// <summary>Get species indices allowed on a given floor.</summary>
        public static int[] GetZoneSpecies(int floor)
        {
            int zone = GetZone(floor);
            return zone switch
            {
                1 => new[] { (int)EnemySpecies.Skeleton, (int)EnemySpecies.Bat },
                2 => new[] { (int)EnemySpecies.Goblin, (int)EnemySpecies.Wolf },
                3 => new[] { (int)EnemySpecies.Orc, (int)EnemySpecies.Spider },
                4 => new[] { (int)EnemySpecies.DarkMage, (int)EnemySpecies.Skeleton, (int)EnemySpecies.Orc },
                // Zone 5+: all species available, deeper = harder
                _ => new[] {
                    (int)EnemySpecies.Skeleton, (int)EnemySpecies.Goblin, (int)EnemySpecies.Bat,
                    (int)EnemySpecies.Wolf, (int)EnemySpecies.Orc, (int)EnemySpecies.DarkMage,
                    (int)EnemySpecies.Spider
                },
            };
        }
    }

    // --- XP / Leveling (spec: leveling.md) ---
    public static class Leveling
    {
        // Quadratic XP curve: floor(L^2 * 45)
        public static int GetXpToLevel(int level) => level * level * 45;
    }

    // --- Town ---
    public static class Town
    {
        public const int Width = 24;
        public const int Height = 20;
        public const float NpcScale = 0.9f;
        public const float NpcCollisionRadius = 14.0f;
    }

    // --- Floor Wipe Rewards ---
    public static class FloorWipe
    {
        public const int BonusGoldBase = 20;
        public const int BonusGoldPerFloor = 10;
        public const float BonusItemDropChance = 0.5f;
        public const int BonusXpBase = 30;
        public const int BonusXpPerFloor = 15;

        public static int GetBonusGold(int floor) => BonusGoldBase + floor * BonusGoldPerFloor;
        public static int GetBonusXp(int floor) => BonusXpBase + floor * BonusXpPerFloor;
    }

    // --- Tiles ---
    public static class Tiles
    {
        // Top-down 32x32 grid (ADR-007 pivot). Previous iso dimensions
        // (64x32 floor, 64x64 wall) archived alongside the PixelLab sprites.
        public static readonly Vector2I TileSize = new(32, 32);
        public static readonly Vector2I TextureRegionSize = new(32, 32);
        public static readonly Vector2I AtlasCoords = new(0, 0);

        // Wall collision polygon — axis-aligned 32x32 square centered on cell.
        public static readonly Vector2[] WallCollisionPolygon =
        {
            new(-16, -16), new(16, -16), new(16, 16), new(-16, 16)
        };
    }

    // --- Visual Effects ---
    public static class Effects
    {
        public const float SlashWidth = 13.0f;
        public const float SlashHeight = 2.0f;
        public const float SlashFadeDuration = 0.12f;
        public const float SlashRiseAmount = 8.0f;
        public const float SlashMaxRotation = 1.2f;
        public const float SlashAlpha = 0.95f;
        public const float DamageFlashDuration = 0.15f;
        public const float StairsCollisionRadius = 14.0f;
        public const float StairsTriggerRadius = 24.0f;
    }

    // --- Asset Paths ---
    public static class Assets
    {
        // Player class full-sheet atlases (LPC, indexed by PlayerClass enum).
        // Loaded via DirectionalSprite.LoadFromAtlas with LpcCharacterWalk layout.
        public static readonly string[] PlayerClassSheets =
        {
            "res://assets/characters/player/warrior/warrior_full_sheet.png",
            "res://assets/characters/player/ranger/ranger_full_sheet.png",
            "res://assets/characters/player/mage/mage_full_sheet.png",
        };

        // Back-compat alias for code that still reads this name.
        public static readonly string[] PlayerClassRotations = PlayerClassSheets;

        // Player class display sprites (south-facing, for selection screen).
        // Points at the same full sheet — UI uses an AtlasTexture region crop.
        public static readonly string[] PlayerClassPreviews = PlayerClassSheets;

        // Projectiles — 8-direction sheets where available, single-frame fallback otherwise
        public const string ArrowProjectile = "res://assets/projectiles/arrow_8dir.png";
        public const string MagicArrowProjectile = "res://assets/projectiles/magic_arrow_8dir.png";
        public const string MagicBoltProjectile = "res://assets/projectiles/magic_bolt_8dir.png";
        public const string FireballProjectile = "res://assets/projectiles/fireball_8dir.png";
        public const string FrostBoltProjectile = "res://assets/projectiles/frost_bolt_8dir.png";
        public const string LightningProjectile = "res://assets/projectiles/lightning_8dir.png";
        public const string StoneSpikeProjectile = "res://assets/projectiles/stone_spike_8dir.png";
        public const string EnergyBlastProjectile = "res://assets/projectiles/energy_blast_8dir.png";
        public const string ShadowBoltProjectile = "res://assets/projectiles/shadow_bolt_8dir.png";

        // HUD orbs
        public const string OrbHp = "res://assets/ui/orb_hp.png";
        public const string OrbMp = "res://assets/ui/orb_mp.png";

        // Enemies (indexed by EnemySpecies enum) — tech-demo LPC monster mapping.
        // Fiction-loose; SPEC-SPECIES-LPC-REWRITE-01 will tighten this.
        // Loaded via DirectionalSprite.LoadFromAtlas with LpcMonster layout.
        public static readonly string[] EnemySpeciesSheets =
        {
            "res://assets/characters/enemies/lpc/ghost.png",       // 0 Skeleton → ghost
            "res://assets/characters/enemies/lpc/small_worm.png",  // 1 Goblin → small worm
            "res://assets/characters/enemies/lpc/bat.png",         // 2 Bat (direct match)
            "res://assets/characters/enemies/lpc/snake.png",       // 3 Wolf → snake
            "res://assets/characters/enemies/lpc/pumpking.png",    // 4 Orc → pumpking
            "res://assets/characters/enemies/lpc/ghost.png",       // 5 DarkMage → ghost
            "res://assets/characters/enemies/lpc/eyeball.png",     // 6 Spider → eyeball
        };

        // Back-compat alias
        public static readonly string[] EnemySpeciesRotations = EnemySpeciesSheets;

        // Dungeon tiles (zone 1 default — kept for fallback)
        public static readonly string[] DungeonFloorTextures =
        {
            "res://assets/tiles/dungeon/floor.png",
            "res://assets/tiles/dungeon/floor_cracked.png",
            "res://assets/tiles/dungeon/floor_flagstone.png",
            "res://assets/tiles/dungeon/floor_worn.png",
        };
        public const string DungeonWallTexture = "res://assets/tiles/dungeon/wall.png";

        // Zone-themed tilesets: floors[] + wall per zone.
        // Tech-demo (ADR-007 pivot): all zones fall back to the shared
        // top-down dungeon tile pair (floor.png + wall.png). Re-theming
        // per zone with top-down art is a post-tech-demo task — the iso
        // dungeon_dark/cathedral/volcano/sky_temple/nether/ sources still
        // exist at res:// but aren't loaded here until they're rebuilt
        // for the top-down grid.
        public static (string[] floors, string wall) GetZoneTheme(int zone)
        {
            _ = zone; // ignored until per-zone top-down art exists
            return (DungeonFloorTextures, DungeonWallTexture);
        }
        public const string StairsDownTexture = "res://assets/tiles/dungeon/stairs_down.png";
        public const string StairsUpTexture = "res://assets/tiles/dungeon/stairs_up.png";

        // Town tiles
        public const string TownFloorTexture = "res://assets/tiles/town/town_floor.png";
        public const string TownWallTexture = "res://assets/tiles/town/town_wall.png";
        public const string CaveEntranceTexture = "res://assets/tiles/town/cave_entrance.png";

        // Scenes
        public const string PlayerScene = "res://scenes/player.tscn";
        public const string EnemyScene = "res://scenes/enemy.tscn";
        public const string TownScene = "res://scenes/town.tscn";
        public const string DungeonScene = "res://scenes/dungeon.tscn";
    }

    // --- Sprite ---
    public static class Sprite
    {
        public const float PlayerScale = 1.0f;
        public const float EnemyScale = 0.7f;
        public const float BossScale = 1.2f;
        public const float NpcScale = 0.9f;
        public const float PlayerSpriteOffsetY = -30.0f;
        public const float EnemySpriteOffsetY = -26.0f;
        public const float ProjectileSpawnOffsetY = -25.0f;
    }
}

// Enums live in scripts/logic/ — one per file (Direction.cs, EnemySpecies.cs)
