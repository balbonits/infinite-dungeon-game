using Godot;
using System.Collections.Generic;
using DungeonGame.Autoloads;
using DungeonGame.Ui;

namespace DungeonGame.Scenes;

public partial class Enemy : CharacterBody2D
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

        _hp = Constants.EnemyStats.GetHp(Level);
        _moveSpeed = Constants.EnemyStats.GetSpeed(Level);
        _damage = Constants.EnemyStats.GetDamage(Level);
        _xpReward = Constants.EnemyStats.GetXpReward(Level);

        _sprite = GetNode<Sprite2D>("Sprite");
        _levelLabel = GetNode<Label>("LevelLabel");
        _hitArea = GetNode<Area2D>("HitArea");
        _hitCooldown = GetNode<Timer>("HitCooldownTimer");

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
            FloatingText.Xp(GetParent(), GlobalPosition, _xpReward);
            EventBus.Instance.EmitSignal(EventBus.SignalName.EnemyDefeated, GlobalPosition, Level);
            GameState.Instance.AwardXp(_xpReward);
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
        player.DamageFlash();
        FloatingText.Damage(player.GetParent(), player.GlobalPosition, _damage);
        _hitCooldown.Start();
    }
}
