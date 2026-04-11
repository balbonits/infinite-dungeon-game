using Godot;
using System;

namespace DungeonGame.Ui;

/// <summary>
/// Reusable screen transition overlay. Fade to black, show message, execute callback, fade in.
/// Added as a child of UILayer (CanvasLayer) so it renders above everything.
/// Usage: ScreenTransition.Instance.Play("Floor 2", () => { /* do work */ });
/// </summary>
public partial class ScreenTransition : Control
{
    public static ScreenTransition Instance { get; private set; } = null!;

    private const float FadeOutDuration = 0.3f;
    private const float HoldDuration = 0.6f;
    private const float FadeInDuration = 0.4f;

    private ColorRect _overlay = null!;
    private Label _messageLabel = null!;
    private Label _subLabel = null!;
    private bool _isTransitioning = false;

    public bool IsTransitioning => _isTransitioning;

    public override void _Ready()
    {
        Instance = this;

        // Full-screen dark overlay
        _overlay = new ColorRect();
        _overlay.Color = new Color(0, 0, 0, 0);
        _overlay.SetAnchorsPreset(LayoutPreset.FullRect);
        _overlay.MouseFilter = MouseFilterEnum.Ignore;
        AddChild(_overlay);

        // Main message (e.g., "Floor 5")
        var center = new CenterContainer();
        center.SetAnchorsPreset(LayoutPreset.FullRect);
        center.MouseFilter = MouseFilterEnum.Ignore;
        AddChild(center);

        var vbox = new VBoxContainer();
        vbox.AddThemeConstantOverride("separation", 8);
        center.AddChild(vbox);

        _messageLabel = new Label();
        _messageLabel.AddThemeColorOverride("font_color", UiTheme.Colors.Accent);
        _messageLabel.AddThemeFontSizeOverride("font_size", 28);
        _messageLabel.HorizontalAlignment = HorizontalAlignment.Center;
        _messageLabel.Modulate = new Color(1, 1, 1, 0);
        vbox.AddChild(_messageLabel);

        _subLabel = new Label();
        _subLabel.AddThemeColorOverride("font_color", UiTheme.Colors.Muted);
        _subLabel.AddThemeFontSizeOverride("font_size", 14);
        _subLabel.HorizontalAlignment = HorizontalAlignment.Center;
        _subLabel.Modulate = new Color(1, 1, 1, 0);
        vbox.AddChild(_subLabel);

        ProcessMode = ProcessModeEnum.Always;
        MouseFilter = MouseFilterEnum.Ignore;
    }

    /// <summary>
    /// Play a transition: fade out → show message → run callback → fade in.
    /// </summary>
    public void Play(string message, Action onMidpoint, string subMessage = "")
    {
        if (_isTransitioning)
            return;

        _isTransitioning = true;
        _messageLabel.Text = message;
        _subLabel.Text = subMessage;
        _overlay.MouseFilter = MouseFilterEnum.Stop; // Block input during transition

        var tween = CreateTween();

        // Phase 1: Fade to black
        tween.TweenProperty(_overlay, "color:a", 1.0f, FadeOutDuration);

        // Phase 2: Show text
        tween.TweenProperty(_messageLabel, "modulate:a", 1.0f, 0.15f);
        tween.TweenProperty(_subLabel, "modulate:a", 1.0f, 0.1f);

        // Phase 3: Execute callback + force GC while screen is black
        tween.TweenCallback(Callable.From(() =>
        {
            onMidpoint?.Invoke();
            System.GC.Collect();
            System.GC.WaitForPendingFinalizers();
        }));
        tween.TweenInterval(HoldDuration);

        // Phase 4: Fade text out
        tween.TweenProperty(_messageLabel, "modulate:a", 0.0f, 0.15f);
        tween.TweenProperty(_subLabel, "modulate:a", 0.0f, 0.1f);

        // Phase 5: Fade from black
        tween.TweenProperty(_overlay, "color:a", 0.0f, FadeInDuration);

        // Cleanup
        tween.TweenCallback(Callable.From(() =>
        {
            _isTransitioning = false;
            _overlay.MouseFilter = MouseFilterEnum.Ignore;
        }));
    }
}
