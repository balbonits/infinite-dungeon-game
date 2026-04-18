using Godot;
using System.Collections.Generic;
using DungeonGame.Autoloads;
using DungeonGame.Ui;

namespace DungeonGame.Scenes;

public partial class Enemy : CharacterBody2D, IDamageable
{
    // Color gradient anchors: level gap -> color (cool=weak, warm=deadly)
    private static readonly (int gap, Color color)[] GradientAnchors =
    {
        (-10, new Color("9D9D9D")), // Grey — trivial
        (-6,  new Color("4A7DFF")), // Blue — low
        (-3,  new Color("4AE8E8")), // Cyan — low-mid
        (0,   new Color("6BFF89")), // Green — even
        (3,   new Color("FFDE66")), // Yellow — mid-high
        (6,   new Color("F5C86B")), // Gold — high
        (8,   new Color("FF9340")), // Orange — very high
        (10,  new Color("FF6F6F")), // Red — extreme
    };

    [Export] public int Level { get; set; } = 1;
    [Export] public int SpeciesIndex { get; set; } = (int)EnemySpecies.Skeleton;

    private int _hp;
    private float _moveSpeed;
    private int _damage;
    private int _xpReward;
    private Color _currentColor;
    private string _lastDirection = "south";

    private Sprite2D _sprite = null!;
    private Label _levelLabel = null!;
    private Area2D _hitArea = null!;
    private Timer _hitCooldown = null!;
    private Dictionary<string, Texture2D> _rotations = null!;

    public override void _Ready()
    {
        AddToGroup(Constants.Groups.Enemies);

        // Apply zone difficulty multiplier to all stats (spec: dungeon.md)
        int floor = GameState.Instance.FloorNumber;
        int zone = Constants.Zones.GetZone(floor);
        float zoneMult = Constants.Zones.GetDifficultyMultiplier(floor);

        // Endgame stat-multiplier chain: floor * pact * saturation. Intelligence's
        // SpawnRateModifier and AggressionModifier are NOT stat multipliers per
        // their docstrings (DungeonIntelligence.cs:140, :151) — they drive
        // cadence: SpawnRateModifier in Dungeon.OnSpawnTimerTimeout and
        // AggressionModifier in DealDamageTo (read at use, not cached here, so
        // each cycle picks up the latest pressure value).
        var pacts = GameState.Instance.Pacts;
        var sat = GameState.Instance.Saturation;
        float hpMult = zoneMult * pacts.EnemyHpMultiplier * sat.GetHpMultiplier(zone);
        float dmgMult = zoneMult * pacts.EnemyDamageMultiplier * sat.GetDamageMultiplier(zone);
        float spdMult = zoneMult * pacts.EnemySpeedMultiplier * sat.GetSpeedMultiplier(zone);

        _hp = (int)(Constants.EnemyStats.GetHp(Level) * hpMult);
        _moveSpeed = Constants.EnemyStats.GetSpeed(Level) * spdMult;
        _damage = (int)(Constants.EnemyStats.GetDamage(Level) * dmgMult);
        // Enemy XP: base_xp * floor_multiplier * (1 + saturation_bonus + pact_bonus)
        float floorXpMult = 1.0f + (floor - 1) * 0.5f;
        float xpBonus = 1.0f + sat.GetXpBonus(zone) + pacts.XpBonus;
        _xpReward = (int)(Constants.EnemyStats.GetXpReward(Level) * floorXpMult * xpBonus);

        _sprite = GetNode<Sprite2D>("Sprite");
        _levelLabel = GetNode<Label>("LevelLabel");
        _hitArea = GetNode<Area2D>("HitArea");
        _hitCooldown = GetNode<Timer>("HitCooldownTimer");
        // Note: HitCooldown.WaitTime is refreshed per-attack in DealDamageTo so
        // pressure-driven AggressionModifier shifts apply on the next swing
        // (modifier evolves over time via DungeonIntelligence.Update).

        // Apply per-species collision and sprite config
        var speciesConfig = SpeciesDatabase.Get(SpeciesIndex);
        _sprite.Scale = new Godot.Vector2(speciesConfig.SpriteScale, speciesConfig.SpriteScale);
        _sprite.Offset = new Godot.Vector2(0, speciesConfig.SpriteOffsetY);
        _levelLabel.Position = new Godot.Vector2(-20, speciesConfig.LabelOffsetY);

        // Resize collision shapes to match species body
        var bodyShape = GetNode<CollisionShape2D>("CollisionShape2D");
        ((CircleShape2D)bodyShape.Shape).Radius = speciesConfig.CollisionRadius;
        var hitShape = _hitArea.GetNode<CollisionShape2D>("HitShape");
        ((CircleShape2D)hitShape.Shape).Radius = speciesConfig.HitAreaRadius;

        // Load species-specific directional sprites
        if (SpeciesIndex >= 0 && SpeciesIndex < Constants.Assets.EnemySpeciesRotations.Length)
        {
            _rotations = DirectionalSprite.LoadRotations(Constants.Assets.EnemySpeciesRotations[SpeciesIndex]);
            if (_rotations.TryGetValue("south", out var southTex))
                _sprite.Texture = southTex;
        }
        else
        {
            _rotations = new Dictionary<string, Texture2D>();
        }

        UpdateColor();
        _levelLabel.Text = Strings.Enemy.LevelLabel(Level);

        _hitArea.Connect(Area2D.SignalName.BodyEntered, new Callable(this, MethodName.OnHitAreaBodyEntered));
        _hitCooldown.Connect(Timer.SignalName.Timeout, new Callable(this, MethodName.OnHitCooldownTimerTimeout));
        GameState.Instance.Connect(
            GameState.SignalName.StatsChanged,
            new Callable(this, MethodName.UpdateColor));
    }

    public override void _PhysicsProcess(double delta)
    {
        var player = GetTree().GetFirstNodeInGroup(Constants.Groups.Player) as Node2D;
        if (player == null)
            return;

        Vector2 direction = (player.GlobalPosition - GlobalPosition).Normalized();
        Velocity = direction * _moveSpeed;
        MoveAndSlide();

        // Safety net: despawn if somehow escaped room bounds
        if (GlobalPosition.DistanceTo(player.GlobalPosition) > Constants.Spawning.DespawnDistance)
            QueueFree();

        DirectionalSprite.UpdateSprite(_sprite, Velocity, _rotations, ref _lastDirection);
    }

    public void TakeDamage(int amount)
    {
        _hp -= amount;

        FlashFx.Flash(this, _sprite, Colors.White, 0.1f);
        FloatingText.Damage(GetParent(), GlobalPosition, amount);

        if (_hp <= 0)
        {
            // XP reward
            FloatingText.Xp(GetParent(), GlobalPosition, _xpReward);
            GameState.Instance.AwardXp(_xpReward);

            // Loot drops
            int gold = LootTable.GetGoldDrop(Level);
            GameState.Instance.PlayerInventory.Gold += gold;
            FloatingText.Spawn(GetParent(), GlobalPosition + new Vector2(0, 10),
                $"+{gold}g", UiTheme.Colors.Accent, 11);

            // ITEM-02 — species-aware equipment + material drops.
            var species = (EnemySpecies)SpeciesIndex;
            int floor = GameState.Instance.FloorNumber;

            var equip = MonsterDropTable.RollEquipment(species, floor);
            if (equip != null && GameState.Instance.PlayerInventory.TryAdd(equip))
            {
                FloatingText.Spawn(GetParent(), GlobalPosition + new Vector2(0, 20),
                    equip.Name, UiTheme.Colors.Safe, 12, 1.5f);
                if (Toast.Instance != null)
                    Toast.Instance.Success($"Found: {equip.Name}");
            }

            foreach (var mat in MonsterDropTable.RollMaterials(species, floor))
            {
                if (GameState.Instance.PlayerInventory.TryAdd(mat))
                {
                    FloatingText.Spawn(GetParent(), GlobalPosition + new Vector2(0, 30),
                        mat.Name, UiTheme.Colors.Info, 10, 1.2f);
                }
            }

            // Track achievement counters
            GameState.Instance.Achievements.IncrementCounter("enemies_killed");
            GameState.Instance.Achievements.IncrementCounter("gold_earned", gold);

            // Endgame system hooks
            int zone = Constants.Zones.GetZone(GameState.Instance.FloorNumber);
            GameState.Instance.Saturation.RecordKill(zone);
            GameState.Instance.Intelligence.RecordKill();

            EventBus.Instance.EmitSignal(EventBus.SignalName.EnemyDefeated, GlobalPosition, Level);
            GameState.Instance.CheckAchievements();
            QueueFree();
        }
        else
        {
            var tween = CreateTween();
            tween.TweenProperty(_sprite, "modulate", _currentColor, 0.1);
        }
    }

    private void UpdateColor()
    {
        int gap = Level - GameState.Instance.Level;
        _currentColor = GetGradientColor(gap);
        _sprite.Modulate = _currentColor;
        _levelLabel.Modulate = _currentColor;
    }

    private static Color GetGradientColor(int gap)
    {
        if (gap <= GradientAnchors[0].gap)
            return GradientAnchors[0].color;
        if (gap >= GradientAnchors[^1].gap)
            return GradientAnchors[^1].color;

        for (int i = 0; i < GradientAnchors.Length - 1; i++)
        {
            var (gapA, colorA) = GradientAnchors[i];
            var (gapB, colorB) = GradientAnchors[i + 1];
            if (gap >= gapA && gap <= gapB)
            {
                float t = (float)(gap - gapA) / (gapB - gapA);
                return colorA.Lerp(colorB, t);
            }
        }
        return GradientAnchors[3].color;
    }

    private void OnHitAreaBodyEntered(Node2D body)
    {
        if (!body.IsInGroup(Constants.Groups.Player) || !_hitCooldown.IsStopped())
            return;
        DealDamageTo(body);
    }

    private void OnHitCooldownTimerTimeout()
    {
        foreach (Node2D body in _hitArea.GetOverlappingBodies())
        {
            if (body.IsInGroup(Constants.Groups.Player))
            {
                DealDamageTo(body);
                return;
            }
        }
    }

    private void DealDamageTo(Node2D playerNode)
    {
        if (playerNode is not Player player)
            return;
        if (player.IsInvincible)
            return;

        GameState.Instance.TakeDamage(_damage);
        GameState.Instance.Intelligence.RecordDamageTaken(_damage);
        player.DamageFlash();
        FloatingText.Damage(player.GetParent(), player.GlobalPosition, _damage);
        // Refresh per-attack so the cooldown tracks the current Intelligence pressure.
        _hitCooldown.WaitTime = Constants.EnemyStats.HitCooldown / GameState.Instance.Intelligence.AggressionModifier;
        _hitCooldown.Start();
    }
}
