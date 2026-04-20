using Godot;
using DungeonGame.Autoloads;
using DungeonGame.Ui;

namespace DungeonGame.Scenes;

/// <summary>
/// Interactable loot container — Jar / Crate / Chest per SPEC-LOOT-01.
/// All containers interact-only (no combat-break path); single-use, sprite
/// swaps to "opened" state on first interaction and stays in the world
/// until the floor unloads.
///
/// Placeholder visuals (ColorRect) until ART-15 replaces with real sprites;
/// the open/closed state swap is implemented via modulate shift so ART-15
/// can slot in without behavior changes.
/// </summary>
public partial class Container : StaticBody2D
{
    [Export]
    public int ContainerTypeIndex { get; set; } = 0; // 0=Jar, 1=Crate, 2=Chest

    public ContainerLootTable.ContainerType Type =>
        (ContainerLootTable.ContainerType)ContainerTypeIndex;

    private bool _playerNearby;
    private bool _opened;
    private ColorRect _visual = null!;
    private Label _promptLabel = null!;
    private Area2D _interactArea = null!;

    public override void _Ready()
    {
        CollisionLayer = Constants.Layers.Walls;
        AddToGroup(Constants.Groups.Containers);

        // Placeholder visuals — ColorRect sized per container type. ART-15
        // replaces this with Sprite2D + real closed/open textures.
        _visual = new ColorRect();
        (Vector2 size, Color color) = Type switch
        {
            ContainerLootTable.ContainerType.Jar => (new Vector2(16, 20), new Color(0.55f, 0.35f, 0.20f)),    // clay brown
            ContainerLootTable.ContainerType.Crate => (new Vector2(24, 24), new Color(0.70f, 0.55f, 0.30f)), // tan wood
            ContainerLootTable.ContainerType.Chest => (new Vector2(32, 22), new Color(0.75f, 0.55f, 0.15f)), // ornate gold
            _ => (new Vector2(16, 16), Colors.White),
        };
        _visual.Size = size;
        _visual.Position = -size / 2;
        _visual.Color = color;
        AddChild(_visual);

        // Collision shape (so the player can't walk through).
        var bodyShape = new CollisionShape2D();
        var rect = new RectangleShape2D { Size = size * 0.9f };
        bodyShape.Shape = rect;
        AddChild(bodyShape);

        // Interact prompt — hidden until player is nearby.
        _promptLabel = new Label();
        _promptLabel.Text = $"[S] Open {TypeLabel()}";
        _promptLabel.AddThemeColorOverride("font_color", UiTheme.Colors.Ink);
        _promptLabel.AddThemeColorOverride("font_outline_color", Colors.Black);
        _promptLabel.AddThemeConstantOverride("outline_size", 3);
        _promptLabel.AddThemeFontSizeOverride("font_size", UiTheme.FontSizes.Small);
        _promptLabel.HorizontalAlignment = HorizontalAlignment.Center;
        _promptLabel.Position = new Vector2(-40, -30);
        _promptLabel.Size = new Vector2(80, 0);
        _promptLabel.Visible = false;
        AddChild(_promptLabel);

        // Interaction area — Area2D watches for the player body.
        _interactArea = new Area2D();
        _interactArea.CollisionLayer = 0;
        _interactArea.CollisionMask = Constants.Layers.Player;
        _interactArea.Monitoring = true;

        var areaShape = new CollisionShape2D();
        var interactCircle = new CircleShape2D { Radius = 28f };
        areaShape.Shape = interactCircle;
        _interactArea.AddChild(areaShape);

        _interactArea.Connect(Area2D.SignalName.BodyEntered,
            new Callable(this, MethodName.OnPlayerEntered));
        _interactArea.Connect(Area2D.SignalName.BodyExited,
            new Callable(this, MethodName.OnPlayerExited));

        AddChild(_interactArea);
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (!_playerNearby || _opened) return;

        if (@event.IsActionPressed(Constants.InputActions.ActionCross))
        {
            Open();
            GetViewport().SetInputAsHandled();
        }
    }

    private void Open()
    {
        if (_opened) return;
        _opened = true;

        int floor = GameState.Instance.FloorNumber;
        int zone = Constants.Zones.GetZone(floor);
        var loot = ContainerLootTable.Roll(Type, floor, zone);

        // Payout gold.
        if (loot.Gold > 0)
        {
            GameState.Instance.PlayerInventory.Gold += loot.Gold;
            GameState.Instance.EmitSignal(GameState.SignalName.StatsChanged);
            FloatingText.Spawn(GetParent(), GlobalPosition + new Vector2(0, -10),
                $"+{loot.Gold}g", UiTheme.Colors.Accent, 12);
        }

        // Payout materials.
        foreach (var mat in loot.Materials)
        {
            if (GameState.Instance.PlayerInventory.TryAdd(mat))
                FloatingText.Spawn(GetParent(), GlobalPosition + new Vector2(0, 0),
                    mat.Name, UiTheme.Colors.Info, 10, 1.2f);
        }

        // Payout equipment with toasts for user visibility.
        foreach (var item in loot.Equipment)
        {
            if (GameState.Instance.PlayerInventory.TryAdd(item))
            {
                FloatingText.Spawn(GetParent(), GlobalPosition + new Vector2(0, 10),
                    item.Name, UiTheme.Colors.Safe, 12, 1.5f);
                Toast.Instance?.Success($"Found: {item.Name}");
            }
        }

        // "Looted" visual — desaturate + half-alpha. Real art via ART-15.
        _visual.Modulate = new Color(0.4f, 0.4f, 0.4f, 0.6f);
        _promptLabel.Visible = false;
        _interactArea.Monitoring = false;
    }

    private void OnPlayerEntered(Node2D body)
    {
        if (_opened) return;
        if (!body.IsInGroup(Constants.Groups.Player)) return;
        _playerNearby = true;
        _promptLabel.Visible = true;
    }

    private void OnPlayerExited(Node2D body)
    {
        if (!body.IsInGroup(Constants.Groups.Player)) return;
        _playerNearby = false;
        _promptLabel.Visible = false;
    }

    private string TypeLabel() => Type switch
    {
        ContainerLootTable.ContainerType.Jar => "Jar",
        ContainerLootTable.ContainerType.Crate => "Crate",
        ContainerLootTable.ContainerType.Chest => "Chest",
        _ => "Container",
    };

    /// <summary>Factory: produce a container node of the given type at the given position.</summary>
    public static Container Create(ContainerLootTable.ContainerType type, Vector2 position)
    {
        var c = new Container
        {
            ContainerTypeIndex = (int)type,
            Position = position,
        };
        return c;
    }
}
