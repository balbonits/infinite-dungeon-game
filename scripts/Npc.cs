using Godot;
using DungeonGame.Ui;

namespace DungeonGame.Scenes;

/// <summary>
/// NPC with proximity detection. Shows an interact prompt when player is near.
/// Player must press the action button (S / action_cross) to open the dialog.
/// Walking away dismisses the prompt and any open dialog.
/// </summary>
public partial class Npc : StaticBody2D
{
    [Export] public string NpcName { get; set; } = "";
    [Export] public string Greeting { get; set; } = "";
    [Export] public string SpritePath { get; set; } = "";

    private bool _playerNearby;
    private Label _promptLabel = null!;

    public override void _Ready()
    {
        CollisionLayer = Constants.Layers.Walls;

        // Sprite — NPCs use LPC full sheets; crop south-facing walk frame 0.
        if (!string.IsNullOrEmpty(SpritePath) && ResourceLoader.Exists(SpritePath))
        {
            var textures = DirectionalSprite.LoadFromAtlas(SpritePath, DirectionalSprite.LpcCharacterWalk);
            if (textures.TryGetValue("south", out var southTex))
            {
                var sprite = new Sprite2D();
                sprite.Texture = southTex;
                sprite.TextureFilter = CanvasItem.TextureFilterEnum.Nearest;
                sprite.Offset = new Vector2(0, Constants.Sprite.PlayerSpriteOffsetY);
                sprite.Scale = new Vector2(Constants.Sprite.NpcScale, Constants.Sprite.NpcScale);
                AddChild(sprite);
            }
        }

        // Collision shape
        var bodyShape = new CollisionShape2D();
        var circle = new CircleShape2D();
        circle.Radius = Constants.Town.NpcCollisionRadius;
        bodyShape.Shape = circle;
        AddChild(bodyShape);

        // Name label (always visible)
        var nameLabel = new Label();
        nameLabel.Text = NpcName;
        nameLabel.AddThemeColorOverride("font_color", UiTheme.Colors.Accent);
        nameLabel.AddThemeColorOverride("font_outline_color", Colors.Black);
        nameLabel.AddThemeConstantOverride("outline_size", 3);
        nameLabel.AddThemeFontSizeOverride("font_size", UiTheme.FontSizes.Small);
        nameLabel.HorizontalAlignment = HorizontalAlignment.Center;
        nameLabel.Position = new Vector2(-30, -56);
        AddChild(nameLabel);

        // Interact prompt (only visible when player is near)
        _promptLabel = new Label();
        _promptLabel.Text = Strings.Npc.InteractPrompt;
        _promptLabel.AddThemeColorOverride("font_color", UiTheme.Colors.Ink);
        _promptLabel.AddThemeColorOverride("font_outline_color", Colors.Black);
        _promptLabel.AddThemeConstantOverride("outline_size", 3);
        _promptLabel.AddThemeFontSizeOverride("font_size", UiTheme.FontSizes.Small);
        _promptLabel.HorizontalAlignment = HorizontalAlignment.Center;
        _promptLabel.Position = new Vector2(-30, -68);
        _promptLabel.Visible = false;
        AddChild(_promptLabel);

        // Interaction detection area
        var interactArea = new Area2D();
        interactArea.CollisionLayer = 0;
        interactArea.CollisionMask = Constants.Layers.Player;
        interactArea.Monitoring = true;

        var areaShape = new CollisionShape2D();
        var interactCircle = new CircleShape2D();
        interactCircle.Radius = 40.0f;
        areaShape.Shape = interactCircle;
        interactArea.AddChild(areaShape);

        interactArea.Connect(Area2D.SignalName.BodyEntered,
            new Callable(this, MethodName.OnPlayerEntered));
        interactArea.Connect(Area2D.SignalName.BodyExited,
            new Callable(this, MethodName.OnPlayerExited));

        AddChild(interactArea);
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (!_playerNearby)
            return;

        if (@event.IsActionPressed(Constants.InputActions.ActionCross))
        {
            NpcPanel.Instance?.Show(NpcName, Greeting);
            GetViewport().SetInputAsHandled();
        }
    }

    private void OnPlayerEntered(Node2D body)
    {
        if (!body.IsInGroup(Constants.Groups.Player))
            return;
        _playerNearby = true;
        _promptLabel.Visible = true;
    }

    private void OnPlayerExited(Node2D body)
    {
        if (!body.IsInGroup(Constants.Groups.Player))
            return;
        _playerNearby = false;
        _promptLabel.Visible = false;
        NpcPanel.Instance?.Close();
    }
}
