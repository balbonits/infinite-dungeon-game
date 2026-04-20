using Godot;

namespace DungeonGame.Sandbox;

/// <summary>
/// Sandbox: Movement
/// Shows 8-way direction snapping, sprite switching, speed normalization.
/// Headless: asserts correct direction names for all 8 input vectors.
/// Run: make sandbox SCENE=movement
/// </summary>
public partial class MovementSandbox : SandboxBase
{
    protected override string SandboxTitle => "🏃  Movement Sandbox";

    private Label _dirLabel = null!;
    private Label _velLabel = null!;
    private Label _spriteLabel = null!;
    private string _lastDir = "south";
    private Vector2 _position = new(700, 350);
    private float _speed = 190f;

    protected override void _SandboxReady()
    {
        AddSectionLabel("Speed");
        AddSlider("Speed", 50, 400, _speed, v => _speed = v);

        AddSectionLabel("Simulate Input");
        var dirs = new (string Label, Vector2 Input)[]
        {
            ("↑ North",     new Vector2(0, -1)),
            ("↓ South",     new Vector2(0, 1)),
            ("← West",      new Vector2(-1, 0)),
            ("→ East",      new Vector2(1, 0)),
            ("↖ North-West",new Vector2(-1, -1).Normalized()),
            ("↗ North-East",new Vector2(1, -1).Normalized()),
            ("↙ South-West",new Vector2(-1, 1).Normalized()),
            ("↘ South-East",new Vector2(1, 1).Normalized()),
        };
        foreach (var (label, input) in dirs)
        {
            var i = input;
            AddButton(label, () => SimulateMove(i));
        }

        _dirLabel = new Label { Position = new Vector2(500, 80) };
        _velLabel = new Label { Position = new Vector2(500, 110) };
        _spriteLabel = new Label { Position = new Vector2(500, 140) };
        foreach (var l in new[] { _dirLabel, _velLabel, _spriteLabel })
        {
            l.AddThemeFontSizeOverride("font_size", Ui.UiTheme.FontSizes.Body);
            AddChild(l);
        }

        SimulateMove(new Vector2(0, 1)); // default south
    }

    private void SimulateMove(Vector2 screenInput)
    {
        string dir = DirectionalSprite.GetDirection(screenInput * _speed);
        _lastDir = dir;

        _dirLabel.Text = $"Direction:  {dir}";
        _velLabel.Text = $"Input:      ({screenInput.X:F2}, {screenInput.Y:F2})";
        _spriteLabel.Text = $"Speed:      {_speed:F0} px/s";

        Log($"Input ({screenInput.X:F2},{screenInput.Y:F2}) → {dir}");
    }

    protected override void RunHeadlessChecks()
    {
        Log("── Headless checks ──");

        var cases = new (Vector2 Input, string Expected)[]
        {
            (new Vector2(1, 0),               "east"),
            (new Vector2(-1, 0),              "west"),
            (new Vector2(0, -1),              "north"),
            (new Vector2(0, 1),               "south"),
            (new Vector2(1, 1).Normalized(),  "south-east"),
            (new Vector2(-1, 1).Normalized(), "south-west"),
            (new Vector2(1, -1).Normalized(), "north-east"),
            (new Vector2(-1,-1).Normalized(), "north-west"),
        };

        foreach (var (input, expected) in cases)
        {
            string got = DirectionalSprite.GetDirection(input * 100f);
            Assert(got == expected, $"({input.X:F2},{input.Y:F2}) → {expected} (got {got})");
        }

        // Zero input returns default direction (no crash)
        string zeroDir = DirectionalSprite.GetDirection(Vector2.Zero);
        Assert(zeroDir == "south", $"Zero input defaults to south (got {zeroDir})");

        FinishHeadless();
    }
}
