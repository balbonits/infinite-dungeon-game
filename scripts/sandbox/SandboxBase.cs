using Godot;
using System.Collections.Generic;

namespace DungeonGame.Sandbox;

/// <summary>
/// Base class for all sandbox scenes.
/// Provides: header, status log panel, reset button,
/// headless mode detection, and pass/fail exit codes.
///
/// Usage:
///   - Extend this class in each sandbox script
///   - Call Log("message") to print to both screen and stdout
///   - Call Pass() / Fail("reason") to exit cleanly in headless mode
///   - Override _SandboxReady() instead of _Ready()
///   - Override _Reset() to handle the Reset button
/// </summary>
public abstract partial class SandboxBase : Control
{
    // ── Headless detection ────────────────────────────────────────────────────

    /// <summary>True when running with --headless flag (CI or make sandbox-headless).</summary>
    protected static bool IsHeadless => DisplayServer.GetName() == "headless";

    // ── Scene structure ───────────────────────────────────────────────────────

    protected Label _titleLabel = null!;
    protected RichTextLabel _logPanel = null!;
    protected Button _resetButton = null!;
    protected VBoxContainer _controlsContainer = null!;

    private readonly List<string> _logLines = new();
    private int _passCount;
    private int _failCount;

    // ── Lifecycle ─────────────────────────────────────────────────────────────

    public override void _Ready()
    {
        BuildUi();
        _SandboxReady();

        if (IsHeadless)
            RunHeadlessChecks();
    }

    /// <summary>Override to initialize sandbox state. Called after UI is built.</summary>
    protected virtual void _SandboxReady() { }

    /// <summary>Override to define headless assertions. Called automatically in headless mode.</summary>
    protected virtual void RunHeadlessChecks() { }

    /// <summary>Override to reset sandbox to initial state.</summary>
    protected virtual void _Reset() { }

    // ── Logging ───────────────────────────────────────────────────────────────

    /// <summary>Print to the on-screen log and stdout.</summary>
    protected void Log(string message)
    {
        _logLines.Add(message);
        GD.Print($"[SANDBOX] {message}");
        if (_logPanel != null)
            _logPanel.Text = string.Join("\n", _logLines);
    }

    /// <summary>Log a passing assertion.</summary>
    protected void Assert(bool condition, string description)
    {
        if (condition)
        {
            _passCount++;
            Log($"  ✅ {description}");
        }
        else
        {
            _failCount++;
            Log($"  ❌ FAIL: {description}");
        }
    }

    /// <summary>Call at end of headless checks to exit with correct code.</summary>
    protected void FinishHeadless()
    {
        Log("");
        Log($"── Results: {_passCount} passed, {_failCount} failed ──");
        GD.Print($"[SANDBOX] Exiting with code {(_failCount > 0 ? 1 : 0)}");
        GetTree().Quit(_failCount > 0 ? 1 : 0);
    }

    // ── UI Builder ────────────────────────────────────────────────────────────

    private void BuildUi()
    {
        // Root layout
        var root = new VBoxContainer();
        root.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        root.AddThemeConstantOverride("separation", 0);
        AddChild(root);

        // Header bar
        var header = new PanelContainer();
        header.CustomMinimumSize = new Vector2(0, 48);
        root.AddChild(header);

        var headerHBox = new HBoxContainer();
        headerHBox.AddThemeConstantOverride("separation", 12);
        header.AddChild(headerHBox);

        // Back button
        var backBtn = new Button { Text = "← Launcher" };
        backBtn.Pressed += () => GetTree().ChangeSceneToFile("res://scenes/sandbox/SandboxLauncher.tscn");
        headerHBox.AddChild(backBtn);

        // Title
        _titleLabel = new Label
        {
            Text = SandboxTitle,
            SizeFlagsHorizontal = SizeFlags.Expand,
        };
        _titleLabel.AddThemeFontSizeOverride("font_size", 18);
        headerHBox.AddChild(_titleLabel);

        // Reset button
        _resetButton = new Button { Text = "↺ Reset" };
        _resetButton.Pressed += () => _Reset();
        headerHBox.AddChild(_resetButton);

        // Body — split: controls left, log right
        var body = new HSplitContainer();
        body.SizeFlagsVertical = SizeFlags.Expand;
        body.SplitOffset = 500;
        root.AddChild(body);

        // Controls panel (left)
        _controlsContainer = new VBoxContainer();
        _controlsContainer.AddThemeConstantOverride("separation", 8);
        var controlsScroll = new ScrollContainer { FollowFocus = true };
        controlsScroll.SizeFlagsHorizontal = SizeFlags.Expand;
        controlsScroll.CustomMinimumSize = new Vector2(300, 0);
        controlsScroll.AddChild(_controlsContainer);
        body.AddChild(controlsScroll);

        // Log panel (right)
        var logScroll = new ScrollContainer { FollowFocus = true };
        logScroll.SizeFlagsHorizontal = SizeFlags.Expand;
        _logPanel = new RichTextLabel
        {
            BbcodeEnabled = false,
            FitContent = true,
            SelectionEnabled = true,
        };
        _logPanel.AddThemeFontSizeOverride("normal_font_size", 12);
        logScroll.AddChild(_logPanel);
        body.AddChild(logScroll);
    }

    /// <summary>Override to set the sandbox title shown in the header.</summary>
    protected abstract string SandboxTitle { get; }

    // ── Helper: labeled section in controls panel ─────────────────────────────

    protected Label AddSectionLabel(string text)
    {
        var label = new Label { Text = text };
        label.AddThemeColorOverride("font_color", new Color(0.6f, 0.8f, 1f));
        label.AddThemeFontSizeOverride("font_size", 13);
        _controlsContainer.AddChild(label);
        return label;
    }

    protected Button AddButton(string text, System.Action onPressed)
    {
        var btn = new Button { Text = text };
        btn.Pressed += onPressed;
        _controlsContainer.AddChild(btn);
        return btn;
    }

    protected HSlider AddSlider(string labelText, float min, float max, float value,
        System.Action<float> onChanged)
    {
        var row = new HBoxContainer();
        _controlsContainer.AddChild(row);

        var lbl = new Label { Text = labelText, CustomMinimumSize = new Vector2(120, 0) };
        row.AddChild(lbl);

        var slider = new HSlider
        {
            MinValue = min,
            MaxValue = max,
            Value = value,
            SizeFlagsHorizontal = SizeFlags.Expand,
        };
        slider.ValueChanged += v =>
        {
            lbl.Text = $"{labelText}: {v:F0}";
            onChanged((float)v);
        };
        row.AddChild(slider);
        return slider;
    }
}
