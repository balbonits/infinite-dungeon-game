using Godot;

namespace DungeonGame.Ui;

/// <summary>
/// HUD compass arrows pointing toward both staircases.
/// Each arrow sits at the screen edge in the direction of its staircase.
/// Hides individually when that staircase is visible on screen.
/// </summary>
public partial class StairsCompass : Control
{
    public static StairsCompass Instance { get; private set; } = null!;

    private const float EdgeMargin = 60.0f;
    private const float ArrowSize = 16.0f;
    private const float ScreenPadding = 50.0f;

    private CompassIndicator _downIndicator = null!;
    private CompassIndicator _upIndicator = null!;

    public override void _Ready()
    {
        Instance = this;
        MouseFilter = MouseFilterEnum.Ignore;

        _downIndicator = CreateIndicator(UiTheme.Colors.Accent, Strings.Floor.StairsDown);
        _upIndicator = CreateIndicator(UiTheme.Colors.Safe, Strings.Floor.StairsUp);
    }

    public void SetTargets(Vector2 stairsDownWorld, Vector2 stairsUpWorld)
    {
        _downIndicator.WorldPos = stairsDownWorld;
        _downIndicator.Active = true;
        _upIndicator.WorldPos = stairsUpWorld;
        _upIndicator.Active = true;
    }

    public void ClearTargets()
    {
        _downIndicator.Active = false;
        _downIndicator.Arrow.Visible = false;
        _downIndicator.Label.Visible = false;
        _upIndicator.Active = false;
        _upIndicator.Arrow.Visible = false;
        _upIndicator.Label.Visible = false;
    }

    public override void _Process(double delta)
    {
        var camera = GetViewport().GetCamera2D();
        if (camera == null)
            return;

        Vector2 viewportSize = GetViewportRect().Size;
        Vector2 cameraPos = camera.GlobalPosition;
        Vector2 zoom = camera.Zoom;

        UpdateIndicator(_downIndicator, viewportSize, cameraPos, zoom);
        UpdateIndicator(_upIndicator, viewportSize, cameraPos, zoom);
    }

    private void UpdateIndicator(CompassIndicator indicator, Vector2 viewportSize,
        Vector2 cameraPos, Vector2 zoom)
    {
        if (!indicator.Active)
            return;

        // World to screen
        Vector2 screenPos = ((indicator.WorldPos - cameraPos) * zoom) + viewportSize / 2;

        // On screen? Hide compass
        bool onScreen = screenPos.X > ScreenPadding &&
                        screenPos.X < viewportSize.X - ScreenPadding &&
                        screenPos.Y > ScreenPadding &&
                        screenPos.Y < viewportSize.Y - ScreenPadding;

        if (onScreen)
        {
            indicator.Arrow.Visible = false;
            indicator.Label.Visible = false;
            return;
        }

        // Clamp to edge
        Vector2 center = viewportSize / 2;
        Vector2 dir = (screenPos - center).Normalized();

        Vector2 edgePos = new(
            Mathf.Clamp(center.X + dir.X * (viewportSize.X / 2 - EdgeMargin), EdgeMargin, viewportSize.X - EdgeMargin),
            Mathf.Clamp(center.Y + dir.Y * (viewportSize.Y / 2 - EdgeMargin), EdgeMargin, viewportSize.Y - EdgeMargin)
        );

        indicator.Arrow.GlobalPosition = edgePos;
        indicator.Arrow.Rotation = dir.Angle();
        indicator.Arrow.Visible = true;

        indicator.Label.GlobalPosition = edgePos + new Vector2(-24, 14);
        indicator.Label.Visible = true;
    }

    private CompassIndicator CreateIndicator(Color color, string labelText)
    {
        var arrow = new Polygon2D();
        arrow.Polygon = new Vector2[]
        {
            new(-ArrowSize, -ArrowSize * 0.5f),
            new(ArrowSize, 0),
            new(-ArrowSize, ArrowSize * 0.5f),
        };
        arrow.Color = color;
        arrow.Visible = false;
        AddChild(arrow);

        var label = new Label();
        label.Text = labelText;
        UiTheme.StyleLabel(label, color, UiTheme.FontSizes.Small);
        label.AddThemeColorOverride("font_outline_color", Colors.Black);
        label.AddThemeConstantOverride("outline_size", 3);
        label.Visible = false;
        AddChild(label);

        return new CompassIndicator { Arrow = arrow, Label = label };
    }

    private class CompassIndicator
    {
        public Polygon2D Arrow = null!;
        public Label Label = null!;
        public Vector2 WorldPos;
        public bool Active;
    }
}
