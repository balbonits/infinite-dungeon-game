using Godot;
using System.Collections.Generic;
using DungeonGame.Autoloads;
using DungeonGame.Ui;

namespace DungeonGame.Scenes;

public partial class Player : CharacterBody2D
{
    private float _attackTimer;
    private float _graceTimer;
    private bool _isInvincible;
    private int _lastLevel;
    private string _lastDirection = "south";

    private AttackConfig _primaryAttack = null!;
    private AttackConfig? _meleeFallback;

    private Area2D _attackArea = null!;
    private Sprite2D _sprite = null!;
    private Camera2D _camera = null!;
    private Dictionary<string, Texture2D> _rotations = null!;

    public bool IsInvincible => _isInvincible;

    public override void _Ready()
    {
        AddToGroup(Constants.Groups.Player);
        _attackArea = GetNode<Area2D>("AttackRange");
        _sprite = GetNode<Sprite2D>("Sprite");
        _camera = GetNode<Camera2D>("Camera2D");

        // Load class attack configs (data-driven, no class-specific branching)
        var selectedClass = GameState.Instance.SelectedClass;
        _primaryAttack = ClassAttacks.GetPrimary(selectedClass);
        _meleeFallback = ClassAttacks.GetMeleeFallback(selectedClass);

        // Resize attack detection area to match primary range
        var attackShape = _attackArea.GetNode<CollisionShape2D>("AttackShape");
        ((CircleShape2D)attackShape.Shape).Radius = _primaryAttack.Range;

        // Apply player collision config
        var playerConfig = SpeciesDatabase.Player;
        _sprite.Scale = new Vector2(playerConfig.SpriteScale, playerConfig.SpriteScale);
        _sprite.Offset = new Vector2(0, playerConfig.SpriteOffsetY);
        var bodyShape = GetNode<CollisionShape2D>("CollisionShape2D");
        ((CircleShape2D)bodyShape.Shape).Radius = playerConfig.CollisionRadius;

        // Load class sprites
        int classIndex = (int)selectedClass;
        string rotationsPath = Constants.Assets.PlayerClassRotations[classIndex];
        _rotations = DirectionalSprite.LoadRotations(rotationsPath);
        if (_rotations.TryGetValue("south", out var southTex))
            _sprite.Texture = southTex;

        _lastLevel = GameState.Instance.Level;
        GameState.Instance.Connect(
            GameState.SignalName.StatsChanged,
            new Callable(this, MethodName.OnStatsChanged));

        StartGracePeriod();
    }

    public override void _PhysicsProcess(double delta)
    {
        if (GameState.Instance.IsDead)
        {
            Velocity = Vector2.Zero;
            return;
        }

        UpdateGracePeriod(delta);
        HandleHpRegen(delta);
        HandleMovement();
        HandleAttack(delta);
    }

    public void StartGracePeriod()
    {
        _isInvincible = true;
        _graceTimer = Constants.PlayerStats.GracePeriod;
    }

    // --- Flash effects (all delegate to FlashFx for reusability) ---
    public void DamageFlash() => FlashFx.Flash(this, _sprite, FlashFx.Damage);
    public void PoisonFlash() => FlashFx.Linger(this, _sprite, FlashFx.Poison, 0.2f, 0.3f);
    public void CurseFlash() => FlashFx.Linger(this, _sprite, FlashFx.Curse, 0.15f, 0.25f);
    public void BoostFlash() => FlashFx.AlternateFlash(this, _sprite, FlashFx.Boost, FlashFx.Shield);
    public void ShieldFlash() => FlashFx.DoublePulse(this, _sprite, FlashFx.Shield);
    public void FreezeFlash() => FlashFx.Linger(this, _sprite, FlashFx.Freeze, 0.3f, 0.4f);
    public void HealFlash() => FlashFx.DoublePulse(this, _sprite, FlashFx.Heal);
    public void CrazedFlash() => FlashFx.AlternateFlash(this, _sprite, FlashFx.Crazed, FlashFx.Damage, 4, 0.04f);

    private void OnStatsChanged()
    {
        int currentLevel = GameState.Instance.Level;
        if (currentLevel > _lastLevel)
        {
            _lastLevel = currentLevel;
            FloatingText.LevelUp(GetParent(), GlobalPosition);
            BoostFlash();
        }
    }

    private void UpdateGracePeriod(double delta)
    {
        if (!_isInvincible)
            return;

        _graceTimer -= (float)delta;
        if (_graceTimer <= 0)
        {
            _isInvincible = false;
            _sprite.Modulate = Colors.White;
        }
        else
        {
            _sprite.Modulate = ((int)(_graceTimer * 10) % 2 == 0)
                ? new Color(1, 1, 1, Constants.PlayerStats.GraceFlickerAlpha)
                : Colors.White;
        }
    }

    private float _regenAccumulator;

    private void HandleHpRegen(double delta)
    {
        float regen = GameState.Instance.Stats.HpRegen;
        if (regen <= 0 || GameState.Instance.Hp >= GameState.Instance.MaxHp)
            return;

        _regenAccumulator += regen * (float)delta;
        if (_regenAccumulator >= 1.0f)
        {
            int healAmount = (int)_regenAccumulator;
            _regenAccumulator -= healAmount;
            GameState.Instance.Hp = System.Math.Min(
                GameState.Instance.MaxHp,
                GameState.Instance.Hp + healAmount);
        }
    }

    private void HandleMovement()
    {
        Vector2 inputDir = Input.GetVector(
            Constants.InputActions.MoveLeft,
            Constants.InputActions.MoveRight,
            Constants.InputActions.MoveUp,
            Constants.InputActions.MoveDown
        );

        if (inputDir.Length() > 0)
        {
            Velocity = inputDir.Normalized() * Constants.PlayerStats.MoveSpeed;
            DirectionalSprite.UpdateSprite(_sprite, Velocity, _rotations, ref _lastDirection);
        }
        else
        {
            Velocity = Vector2.Zero;
        }

        MoveAndSlide();
    }

    private void HandleAttack(double delta)
    {
        _attackTimer -= (float)delta;
        if (_attackTimer > 0)
            return;

        Node2D? target = FindNearestEnemy();
        if (target == null)
            return;

        // Pick attack: use melee fallback if enemy is close enough, otherwise primary
        float distToTarget = GlobalPosition.DistanceTo(target.GlobalPosition);
        AttackConfig attack = (_meleeFallback != null && distToTarget <= _meleeFallback.Range)
            ? _meleeFallback
            : _primaryAttack;

        ExecuteAttack(attack, target);
        EventBus.Instance.EmitSignal(EventBus.SignalName.PlayerAttacked, target);
    }

    /// <summary>
    /// Unified attack execution. Reads AttackConfig's TargetMode and resolves
    /// targets accordingly. No class or skill branching.
    /// </summary>
    private void ExecuteAttack(AttackConfig attack, Node2D target)
    {
        var stats = GameState.Instance.Stats;
        int baseDamage = Constants.PlayerStats.GetDamage(GameState.Instance.Level);

        // Apply stat bonuses based on attack type
        float statBonus = attack.IsProjectile
            ? stats.SpellDamageMultiplier  // INT affects ranged/spell damage
            : 1.0f + stats.MeleePercentBoost / 100f;  // STR affects melee damage
        float flatBonus = attack.IsProjectile ? 0 : stats.MeleeFlatBonus;

        int finalDamage = (int)((baseDamage + flatBonus) * attack.DamageMultiplier * statBonus);

        // DEX affects cooldown (attack speed)
        _attackTimer = attack.Cooldown / stats.AttackSpeedMultiplier;

        switch (attack.TargetMode)
        {
            case TargetMode.Self:
                // Buffs apply to caster — no target needed
                break;

            case TargetMode.SingleTarget:
                // Damage is ALWAYS instant — melee or ranged, same thing
                if (target is IDamageable singleTarget)
                    singleTarget.TakeDamage(finalDamage);

                if (attack.IsProjectile)
                {
                    // Visual tracer at chest height (cosmetic only, hit already applied)
                    float chestY = Constants.Sprite.ProjectileSpawnOffsetY;
                    Projectile.SpawnTracer(
                        GetParent(),
                        GlobalPosition + new Vector2(0, chestY),
                        target.GlobalPosition + new Vector2(0, chestY),
                        attack.ProjectileSpeed, attack.ProjectileTexture,
                        attack.ProjectileScale, attack.ProjectileTint);
                }
                else
                {
                    DrawSlash(target.GlobalPosition, attack.EffectColor);
                }
                break;

            case TargetMode.AreaOfEffect:
                // Damage all enemies within AoeRadius of the target point
                HitEnemiesInRadius(target.GlobalPosition, attack.AoeRadius,
                    finalDamage, attack.MaxTargets, attack.EffectColor);
                break;

            case TargetMode.MultiTarget:
                // Hit first target, then chain to nearby enemies
                ExecuteChainAttack(target, finalDamage, attack);
                break;

            case TargetMode.PlayerCentricAoe:
                // Damage all enemies within AoeRadius of the player
                HitEnemiesInRadius(GlobalPosition, attack.AoeRadius,
                    finalDamage, attack.MaxTargets, attack.EffectColor);
                break;
        }
    }

    private void HitEnemiesInRadius(Vector2 center, float radius, int damage,
        int maxTargets, Color effectColor)
    {
        int hitCount = 0;
        foreach (Node2D body in _attackArea.GetOverlappingBodies())
        {
            if (!body.IsInGroup(Constants.Groups.Enemies))
                continue;
            if (center.DistanceTo(body.GlobalPosition) > radius)
                continue;
            if (hitCount >= maxTargets)
                break;

            if (body is IDamageable dmg)
                dmg.TakeDamage(damage);
            DrawSlash(body.GlobalPosition, effectColor);
            hitCount++;
        }
    }

    private void ExecuteChainAttack(Node2D firstTarget, int damage, AttackConfig attack)
    {
        var hit = new System.Collections.Generic.HashSet<Node2D>();
        Node2D? current = firstTarget;

        for (int chain = 0; chain < attack.ChainCount && current != null; chain++)
        {
            if (current is IDamageable chainTarget)
                chainTarget.TakeDamage(damage);
            DrawSlash(current.GlobalPosition, attack.EffectColor);
            hit.Add(current);

            // Find nearest unhit enemy within chain range
            Node2D? next = null;
            float nearestDist = float.PositiveInfinity;
            foreach (Node2D body in _attackArea.GetOverlappingBodies())
            {
                if (!body.IsInGroup(Constants.Groups.Enemies) || hit.Contains(body))
                    continue;
                float dist = current.GlobalPosition.DistanceTo(body.GlobalPosition);
                if (dist <= attack.ChainRange && dist < nearestDist)
                {
                    nearestDist = dist;
                    next = body;
                }
            }
            current = next;
        }
    }

    private Node2D? FindNearestEnemy()
    {
        var bodies = _attackArea.GetOverlappingBodies();
        Node2D? nearest = null;
        float nearestDist = float.PositiveInfinity;

        foreach (Node2D body in bodies)
        {
            if (!body.IsInGroup(Constants.Groups.Enemies))
                continue;
            float dist = GlobalPosition.DistanceTo(body.GlobalPosition);
            if (dist < nearestDist)
            {
                nearestDist = dist;
                nearest = body;
            }
        }

        return nearest;
    }

    private void DrawSlash(Vector2 targetPos, Color? color = null)
    {
        float w = Constants.Effects.SlashWidth;
        float h = Constants.Effects.SlashHeight;

        var slash = new Polygon2D();
        slash.Polygon = new Vector2[] { new(-w, -h), new(w, -h), new(w, h), new(-w, h) };
        slash.Color = new Color(color ?? UiTheme.Colors.Accent, Constants.Effects.SlashAlpha);
        slash.GlobalPosition = targetPos;
        slash.Rotation = (float)GD.RandRange(-Constants.Effects.SlashMaxRotation, Constants.Effects.SlashMaxRotation);

        GetParent().AddChild(slash);

        Tween tween = CreateTween();
        tween.SetParallel(true);
        tween.TweenProperty(slash, "modulate:a", 0.0f, Constants.Effects.SlashFadeDuration);
        tween.TweenProperty(slash, "position:y", slash.Position.Y - Constants.Effects.SlashRiseAmount, Constants.Effects.SlashFadeDuration);
        tween.SetParallel(false);
        tween.TweenCallback(Callable.From(slash.QueueFree));
    }
}
