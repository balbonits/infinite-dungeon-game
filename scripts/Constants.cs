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
        public const int HpPerLevel = 8;
        public const int HealOnLevelUp = 18;

        public static int GetDamage(int level) => BaseDamage + (int)(level * DamagePerLevel);
        public static int GetMaxHp(int level) => StartingHp + level * HpPerLevel;
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

        // Projectile sizes
        public const float ArrowScale = 1.5f;
        public const float MagicBoltScale = 1.5f;

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

    // --- XP / Leveling ---
    public static class Leveling
    {
        public const int XpPerLevelMultiplier = 90;

        public static int GetXpToLevel(int level) => level * XpPerLevelMultiplier;
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

        // Dungeon tiles
        public static readonly string[] DungeonFloorTextures =
        {
            "res://assets/tiles/dungeon/floor.png",
            "res://assets/tiles/dungeon/floor_cracked.png",
            "res://assets/tiles/dungeon/floor_flagstone.png",
            "res://assets/tiles/dungeon/floor_worn.png",
        };
        public const string DungeonWallTexture = "res://assets/tiles/dungeon/wall.png";
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
