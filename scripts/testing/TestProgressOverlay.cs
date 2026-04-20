#if DEBUG
using Godot;

namespace DungeonGame.Testing;

/// <summary>
/// Top-of-screen banner that shows the currently running test's name while
/// <c>make test-ui</c> is walking the windowed suite. Attaches itself to the
/// scene-tree root so it survives Main scene reloads (ResetToFreshSplash).
///
/// GameTestBase.Expect calls <see cref="SetCurrentTest"/> after stack-walking
/// to find the first [Test] method on the call stack, so updates happen
/// automatically without every test needing a StartTest call.
/// </summary>
public partial class TestProgressOverlay : CanvasLayer
{
    private static TestProgressOverlay? _instance;

    private Label _label = null!;

    /// <summary>Idempotent: attach if not already present. Safe to call multiple times.</summary>
    public static void EnsureAttached(Node host)
    {
        if (_instance != null && IsInstanceValid(_instance))
            return;

        var root = host.GetTree().Root;
        var overlay = new TestProgressOverlay { Name = "TestProgressOverlay", Layer = 128 };
        root.AddChild(overlay);
        _instance = overlay;
    }

    public static void SetCurrentTest(string suite, string testName)
    {
        if (_instance == null || !IsInstanceValid(_instance)) return;
        _instance.CallDeferred(MethodName.UpdateLabel, $"▶ {suite} :: {testName}");
    }

    public override void _Ready()
    {
        ProcessMode = ProcessModeEnum.Always;
        var panel = new PanelContainer
        {
            AnchorLeft = 0,
            AnchorRight = 1,
            AnchorTop = 0,
            AnchorBottom = 0,
            OffsetTop = 0,
            OffsetBottom = 36,
            MouseFilter = Control.MouseFilterEnum.Ignore,
        };
        AddChild(panel);

        var style = new StyleBoxFlat
        {
            BgColor = new Color(0, 0, 0, 0.75f),
        };
        panel.AddThemeStyleboxOverride("panel", style);

        _label = new Label
        {
            Text = "▶ waiting for first test…",
            HorizontalAlignment = HorizontalAlignment.Center,
            MouseFilter = Control.MouseFilterEnum.Ignore,
        };
        _label.AddThemeColorOverride("font_color", new Color(1, 0.9f, 0.3f));
        _label.AddThemeFontSizeOverride("font_size", 18);
        panel.AddChild(_label);
    }

    private void UpdateLabel(string text)
    {
        if (_label != null)
            _label.Text = text;
    }
}
#endif
