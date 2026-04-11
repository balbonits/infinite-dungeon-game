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

        public static int GetDamage(int level) => BaseDamage + (int)(level * DamagePerLevel);

        // Spec: level_hp = floor(8 + level * 0.5) per level, cumulative
        public static int GetMaxHp(int level)
        {
            int total = StartingHp;
            for (int l = 1; l <= level; l++)
                total += (int)(8 + l * 0.5f);
            return total;
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
        public const int InitialEnemies = 8;
        public const int EnemySoftCap = 14;
        public const float SpawnInterval = 2.8f;
        public const float RespawnDelay = 1.4f;
        public const float SafeSpawnRadius = 150.0f;
        public const int MaxSpawnRetries = 10;
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
        public static readonly Vector2I TileSize = new(64, 32);
        public static readonly Vector2I TextureRegionSize = new(64, 64);
        public static readonly Vector2I AtlasCoords = new(0, 0);

        // Wall collision polygon (full rectangle for smooth sliding)
        public static readonly Vector2[] WallCollisionPolygon =
        {
            new(-32, -16), new(32, -16), new(32, 16), new(-32, 16)
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
        // Player class rotations (indexed by PlayerClass enum)
        public static readonly string[] PlayerClassRotations =
        {
            "res://assets/characters/player/warrior/rotations",
            "res://assets/characters/player/ranger/rotations",
            "res://assets/characters/player/mage/rotations",
        };

        // Player class display sprites (south-facing, for selection screen)
        public static readonly string[] PlayerClassPreviews =
        {
            "res://assets/characters/player/warrior/rotations/south.png",
            "res://assets/characters/player/ranger/rotations/south.png",
            "res://assets/characters/player/mage/rotations/south.png",
        };

        // Projectiles
        public const string ArrowProjectile = "res://assets/projectiles/arrow.png";
        public const string MagicBoltProjectile = "res://assets/projectiles/magic_bolt.png";

        // Enemies (indexed by EnemySpecies enum)
        public static readonly string[] EnemySpeciesRotations =
        {
            "res://assets/characters/enemies/skeleton/rotations",   // 0 Skeleton
            "res://assets/characters/enemies/goblin/rotations",     // 1 Goblin
            "res://assets/characters/enemies/bat/rotations",        // 2 Bat
            "res://assets/characters/enemies/wolf/rotations",       // 3 Wolf
            "res://assets/characters/enemies/orc/rotations",        // 4 Orc
            "res://assets/characters/enemies/dark_mage/rotations",  // 5 DarkMage
            "res://assets/characters/enemies/spider/rotations",     // 6 Spider
        };

        // Dungeon tiles (zone 1 default — kept for fallback)
        public static readonly string[] DungeonFloorTextures =
        {
            "res://assets/tiles/dungeon/floor.png",
            "res://assets/tiles/dungeon/floor_cracked.png",
            "res://assets/tiles/dungeon/floor_flagstone.png",
            "res://assets/tiles/dungeon/floor_worn.png",
        };
        public const string DungeonWallTexture = "res://assets/tiles/dungeon/wall.png";

        // Zone-themed tilesets: floors[] + wall per zone
        public static (string[] floors, string wall) GetZoneTheme(int zone)
        {
            return zone switch
            {
                1 => (new[]
                {
                    "res://assets/tiles/dungeon_dark/floor_0.png",
                    "res://assets/tiles/dungeon_dark/floor_1.png",
                    "res://assets/tiles/dungeon_dark/floor_2.png",
                    "res://assets/tiles/dungeon_dark/floor_3.png",
                    "res://assets/tiles/dungeon_dark/floor_4.png",
                    "res://assets/tiles/dungeon_dark/floor_5.png",
                }, "res://assets/tiles/dungeon_dark/wall_0.png"),
                2 => (new[]
                {
                    "res://assets/tiles/cathedral/floor_0.png",
                    "res://assets/tiles/cathedral/floor_1.png",
                    "res://assets/tiles/cathedral/floor_2.png",
                    "res://assets/tiles/cathedral/floor_3.png",
                    "res://assets/tiles/cathedral/floor_4.png",
                    "res://assets/tiles/cathedral/floor_5.png",
                }, "res://assets/tiles/cathedral/wall_0.png"),
                3 => (new[]
                {
                    "res://assets/tiles/volcano/floor_0.png",
                    "res://assets/tiles/volcano/floor_1.png",
                    "res://assets/tiles/volcano/floor_2.png",
                    "res://assets/tiles/volcano/floor_3.png",
                    "res://assets/tiles/volcano/floor_4.png",
                    "res://assets/tiles/volcano/floor_5.png",
                }, "res://assets/tiles/volcano/wall_0.png"),
                4 => (new[]
                {
                    "res://assets/tiles/sky_temple/floor_0.png",
                    "res://assets/tiles/sky_temple/floor_1.png",
                    "res://assets/tiles/sky_temple/floor_2.png",
                    "res://assets/tiles/sky_temple/floor_3.png",
                    "res://assets/tiles/sky_temple/floor_4.png",
                    "res://assets/tiles/sky_temple/floor_5.png",
                }, "res://assets/tiles/sky_temple/wall_0.png"),
                5 => (new[]
                {
                    "res://assets/tiles/nether/floor_0.png",
                    "res://assets/tiles/nether/floor_1.png",
                    "res://assets/tiles/nether/floor_2.png",
                    "res://assets/tiles/nether/floor_3.png",
                    "res://assets/tiles/nether/floor_4.png",
                    "res://assets/tiles/nether/floor_5.png",
                }, "res://assets/tiles/nether/wall_0.png"),
                // Zone 6+: cycle through themes
                _ => GetZoneTheme(((zone - 1) % 5) + 1),
            };
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
