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

    private MonsterData _monsterData;
    private Polygon2D _sprite;
    private ColorRect _hpBarBg;
    private ColorRect _hpBarFill;
    private float _attackTimer;

    public MonsterData MonsterData => _monsterData;

    /// <summary>
    /// Initialize this enemy with the given MonsterData.
    /// Must be called immediately after instantiation, before _Ready.
    /// </summary>
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

        // Visual: colored diamond sized for isometric tile grid (64x32)
        // ~50% tile width = 32px wide, ~70% equivalent = 40px tall
        _sprite = new Polygon2D();
        _sprite.Polygon = new Vector2[]
        {
            new(0, -20), new(16, 0), new(0, 20), new(-16, 0)
        };
        _sprite.Color = _monsterData.Tier switch
        {
            MonsterTier.Tier1 => Tier1Color,
            MonsterTier.Tier2 => Tier2Color,
            MonsterTier.Tier3 => Tier3Color,
            _ => Tier1Color,
        };
        AddChild(_sprite);

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
        // Walk up to DungeonScene and find the PlayerController
        var scene = GetParent()?.GetParent();
        if (scene == null) return null;

        foreach (var child in scene.GetChildren())
        {
            if (child is PlayerController pc)
                return pc;
        }
        return null;
    }
}
