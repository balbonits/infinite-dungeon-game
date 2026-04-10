using Godot;
using System;

/// <summary>
/// Visual representation of one enemy on the dungeon floor.
/// Holds a MonsterData reference, renders a colored diamond + HP bar,
/// chases the player with simple AI, and handles death/loot/XP.
/// </summary>
public partial class EnemyEntity : Node2D
{
    // Tier colors from sprite-specs.md
    private static readonly Color Tier1Color = new("#6bff89");
    private static readonly Color Tier2Color = new("#ffde66");
    private static readonly Color Tier3Color = new("#ff6f6f");

    // Movement speeds by tier (px/s)
    private const float Tier1Speed = 66f;
    private const float Tier2Speed = 84f;
    private const float Tier3Speed = 102f;

    // AI thresholds
    private const float AggroRange = 384f; // ~6 tiles
    private const float MeleeRange = 30f;
    private const float AttackCooldown = 1.5f;

    // Creature sprites: 1024x1024, 8x8 grid, 128x128 per frame
    private const int CreatureHframes = 8;
    private const int CreatureVframes = 8;
    private const float CreatureScale = 0.3125f; // 40/128 = fits tile
    private static readonly string[] CreatureSprites = {
        "slime", "goblin", "skeleton", "zombie", "ogre", "werewolf", "elemental", "magician"
    };

    private MonsterData _monsterData;
    private Sprite2D _spriteNode;
    private ColorRect _hpBarBg;
    private ColorRect _hpBarFill;
    private float _attackTimer;
    private float _animTimer;
    private int _animFrame;
    private int _direction = 0;

    public MonsterData MonsterData => _monsterData;

    public void Init(MonsterData data)
    {
        _monsterData = data;
    }

    public override void _Ready()
    {
        if (_monsterData == null)
        {
            GD.PrintErr("[EnemyEntity] No MonsterData set — call Init() before adding to tree");
            return;
        }

        // Pick a creature sprite based on monster name or random
        string creatureName = PickCreatureSprite(_monsterData.Name);
        string spritePath = $"res://assets/isometric/enemies/creatures/{creatureName}.png";

        _spriteNode = new Sprite2D();
        _spriteNode.TextureFilter = TextureFilterEnum.Nearest;

        Texture2D tex = null;
        if (ResourceLoader.Exists(spritePath))
            tex = ResourceLoader.Load<Texture2D>(spritePath);
        if (tex == null)
            tex = TestHelper.LoadIssPng(spritePath);

        if (tex != null)
        {
            _spriteNode.Texture = tex;
            _spriteNode.Hframes = CreatureHframes;
            _spriteNode.Vframes = CreatureVframes;
            _spriteNode.Scale = new Vector2(CreatureScale, CreatureScale);
            // Offset so feet land on node position
            float frameH = tex.GetHeight() / CreatureVframes;
            _spriteNode.Offset = new Vector2(0, -frameH * 0.3f);
            // Tint by tier for visual distinction
            _spriteNode.Modulate = _monsterData.Tier switch
            {
                MonsterTier.Tier2 => new Color(1.0f, 0.95f, 0.7f), // slight gold tint
                MonsterTier.Tier3 => new Color(1.0f, 0.7f, 0.7f),  // slight red tint
                _ => Colors.White,
            };
        }
        else
        {
            // Fallback: colored diamond
            var placeholder = new Polygon2D();
            placeholder.Polygon = new Vector2[] {
                new(0, -20), new(16, 0), new(0, 20), new(-16, 0)
            };
            placeholder.Color = _monsterData.Tier switch
            {
                MonsterTier.Tier1 => Tier1Color,
                MonsterTier.Tier2 => Tier2Color,
                MonsterTier.Tier3 => Tier3Color,
                _ => Tier1Color,
            };
            AddChild(placeholder);
        }

        AddChild(_spriteNode);

        // HP bar background (dark) — wide enough to be visible at 2x zoom
        _hpBarBg = new ColorRect();
        _hpBarBg.Size = new Vector2(40, 5);
        _hpBarBg.Position = new Vector2(-20, -28);
        _hpBarBg.Color = new Color(0.15f, 0.15f, 0.15f, 0.8f);
        AddChild(_hpBarBg);

        // HP bar fill (red)
        _hpBarFill = new ColorRect();
        _hpBarFill.Size = new Vector2(40, 5);
        _hpBarFill.Position = new Vector2(-20, -28);
        _hpBarFill.Color = new Color(0.85f, 0.2f, 0.2f);
        AddChild(_hpBarFill);

        UpdateHpBar();
    }

    public override void _Process(double delta)
    {
        if (_monsterData == null || _monsterData.IsDead) return;

        // Tick attack cooldown
        if (_attackTimer > 0)
            _attackTimer -= (float)delta;

        // Find player — DungeonScene sets it as the first CharacterBody2D child
        var player = FindPlayer();
        if (player == null) return;

        float dist = GlobalPosition.DistanceTo(player.GlobalPosition);

        // Chase if within aggro range
        if (dist <= AggroRange && dist > MeleeRange)
        {
            float speed = _monsterData.Tier switch
            {
                MonsterTier.Tier1 => Tier1Speed,
                MonsterTier.Tier2 => Tier2Speed,
                MonsterTier.Tier3 => Tier3Speed,
                _ => Tier1Speed,
            };
            var dir = (player.GlobalPosition - GlobalPosition).Normalized();
            Position += dir * speed * (float)delta;
            UpdateSpriteAnimation((float)delta, dir);
        }

        // Attack if within melee range and cooldown is ready
        if (dist <= MeleeRange && _attackTimer <= 0)
        {
            int damage = GameSystems.MonsterAttackPlayer(_monsterData);
            _attackTimer = AttackCooldown;

            // Show damage on player
            TestHelper.ShowFloatingText(
                GetParent(), player.GlobalPosition,
                $"-{damage}", new Color(1.0f, 0.3f, 0.3f));
        }
    }

    /// <summary>
    /// Apply damage to this enemy, update HP bar, show floating text.
    /// Called by DungeonScene after GameSystems.AttackMonster().
    /// </summary>
    public void ShowDamage(int damage, bool crit)
    {
        UpdateHpBar();

        var color = crit ? new Color(1.0f, 0.85f, 0.2f) : new Color(1.0f, 1.0f, 1.0f);
        string text = crit ? $"-{damage}!" : $"-{damage}";
        TestHelper.ShowFloatingText(GetParent(), GlobalPosition, text, color);
    }

    /// <summary>
    /// Handle death: award XP, gold, loot, show floating text, fade out, remove.
    /// </summary>
    public void Die()
    {
        if (_monsterData == null) return;

        // Award XP
        var (leveled, xpGained) = GameSystems.GainXP(_monsterData.XPReward);
        TestHelper.ShowFloatingText(
            GetParent(), GlobalPosition + new Vector2(0, -10),
            $"+{xpGained} XP", new Color(0.5f, 0.8f, 1.0f));

        if (leveled)
        {
            TestHelper.ShowFloatingText(
                GetParent(), GlobalPosition + new Vector2(0, -30),
                "LEVEL UP!", new Color(1.0f, 0.85f, 0.2f));
        }

        // Award gold
        GameState.Player.Gold += _monsterData.GoldReward;
        if (_monsterData.GoldReward > 0)
        {
            TestHelper.ShowFloatingText(
                GetParent(), GlobalPosition + new Vector2(20, -10),
                $"+{_monsterData.GoldReward}g", new Color(1.0f, 0.85f, 0.2f));
        }

        // Loot drop
        var rng = new Random();
        var loot = ItemGenerator.RollLootDrop((int)_monsterData.Tier, GameState.DungeonFloor, rng);
        if (loot != null)
        {
            bool added = GameSystems.AddToInventory(loot);
            if (added)
            {
                TestHelper.ShowFloatingText(
                    GetParent(), GlobalPosition + new Vector2(0, -50),
                    loot.Name, new Color(0.3f, 1.0f, 0.5f));
            }
        }

        // Fade out tween, then remove
        var tween = CreateTween();
        tween.TweenProperty(this, "modulate:a", 0.0, 0.4);
        tween.TweenCallback(Callable.From(QueueFree));
    }

    private void UpdateHpBar()
    {
        if (_monsterData == null || _hpBarFill == null) return;
        float pct = Mathf.Clamp((float)_monsterData.HP / _monsterData.MaxHP, 0f, 1f);
        _hpBarFill.Size = new Vector2(40 * pct, 5);
    }

    private PlayerController FindPlayer()
    {
        var scene = GetParent()?.GetParent();
        if (scene == null) return null;

        foreach (var child in scene.GetChildren())
        {
            if (child is PlayerController pc)
                return pc;
        }
        return null;
    }

    /// <summary>Pick a creature sprite name based on monster name, falling back to random.</summary>
    private static string PickCreatureSprite(string monsterName)
    {
        string lower = monsterName.ToLowerInvariant();
        foreach (var name in CreatureSprites)
        {
            if (lower.Contains(name))
                return name;
        }
        // Deterministic pick based on name hash
        int idx = Math.Abs(monsterName.GetHashCode()) % CreatureSprites.Length;
        return CreatureSprites[idx];
    }

    /// <summary>Update sprite animation frame based on movement direction.</summary>
    private void UpdateSpriteAnimation(float delta, Vector2 dirToPlayer)
    {
        if (_spriteNode?.Texture == null) return;

        _animTimer += delta;
        if (_animTimer < 0.12f) return;
        _animTimer = 0;

        // Determine direction from movement
        float angle = Mathf.Atan2(dirToPlayer.Y, dirToPlayer.X);
        int dir = Mathf.RoundToInt((angle / Mathf.Tau + 1.0f) * 8) % 8;
        _direction = dir switch
        {
            0 => 6,  // right → E
            1 => 7,  // down-right → SE
            2 => 0,  // down → S
            3 => 1,  // down-left → SW
            4 => 2,  // left → W
            5 => 3,  // up-left → NW
            6 => 4,  // up → N
            7 => 5,  // up-right → NE
            _ => 0,
        };

        // Walk animation: frames 1-3
        _animFrame = (_animFrame + 1) % 3;
        int col = 1 + _animFrame; // walk frames
        int frame = _direction * CreatureHframes + col;
        if (frame < CreatureHframes * CreatureVframes)
            _spriteNode.Frame = frame;
    }
}
