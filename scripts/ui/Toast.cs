using Godot;
using System.Collections.Generic;

namespace DungeonGame.Ui;

/// <summary>
/// Toast notification system. Shows brief messages that stack and fade.
/// Usage: Toast.Instance.Show("Floor cleared!", UiTheme.Colors.Accent);
/// Toasts appear at the bottom-center and slide up as new ones arrive.
/// </summary>
public partial class Toast : Control
{
    private const float DefaultDuration = 3.0f;
    private const float FadeOutTime = 0.5f;
    private const float SlideDistance = 30.0f;
    private const int MaxVisible = 5;
    private const int BottomOffset = 120;

    public static Toast Instance { get; private set; } = null!;

    private readonly List<Control> _activeToasts = new();

    public override void _Ready()
    {
        Instance = this;
        MouseFilter = MouseFilterEnum.Ignore;
        // Toasts must animate while the tree is paused (splash/menu flows pause the tree
        // before invoking paths that can fail, e.g., Load-Game failure). Without this,
        // fade-in tweens never tick and the toast sits at alpha 0 for its whole lifetime.
        ProcessMode = ProcessModeEnum.Always;
    }

    public void Show(string message, Color? color = null, float duration = DefaultDuration)
    {
        Color textColor = color ?? UiTheme.Colors.Ink;

        // Enforce max visible
        while (_activeToasts.Count >= MaxVisible)
        {
            DismissOldest();
        }

        // Create toast container
        var toast = new PanelContainer();
        toast.AddThemeStyleboxOverride("panel", CreateToastStyle());
        toast.MouseFilter = MouseFilterEnum.Ignore;

        var label = new Label();
        label.Text = message;
        UiTheme.StyleLabel(label, textColor, UiTheme.FontSizes.Body);
        label.HorizontalAlignment = HorizontalAlignment.Center;
        label.MouseFilter = MouseFilterEnum.Ignore;
        toast.AddChild(label);

        AddChild(toast);

        // Position at bottom-center
        toast.SetDeferred("position", new Vector2(
            GetViewportRect().Size.X / 2 - 150,
            GetViewportRect().Size.Y - BottomOffset - _activeToasts.Count * 40
        ));
        toast.CustomMinimumSize = new Vector2(300, 0);

        // Slide existing toasts up
        foreach (var existing in _activeToasts)
        {
            if (IsInstanceValid(existing))
            {
                var tween = CreateTween();
                tween.TweenProperty(existing, "position:y",
                    existing.Position.Y - 40, 0.2f);
            }
        }

        _activeToasts.Add(toast);

        // Entrance animation
        toast.Modulate = new Color(1, 1, 1, 0);
        var enterTween = CreateTween();
        enterTween.TweenProperty(toast, "modulate:a", 1.0f, 0.2f);

        // Auto-dismiss after duration
        var timer = GetTree().CreateTimer(duration);
        timer.Connect(SceneTreeTimer.SignalName.Timeout,
            Callable.From(() => DismissToast(toast)));
    }

    public void Info(string message) => Show(message, UiTheme.Colors.Ink);
    public void Success(string message) => Show(message, UiTheme.Colors.Safe);
    public void Warning(string message) => Show(message, UiTheme.Colors.Accent);
    public void Error(string message) => Show(message, UiTheme.Colors.Danger);

    private void DismissToast(Control toast)
    {
        if (!IsInstanceValid(toast))
            return;

        _activeToasts.Remove(toast);

        var tween = CreateTween();
        tween.SetParallel(true);
        tween.TweenProperty(toast, "modulate:a", 0.0f, FadeOutTime);
        tween.TweenProperty(toast, "position:y", toast.Position.Y - SlideDistance, FadeOutTime);
        tween.SetParallel(false);
        tween.TweenCallback(Callable.From(toast.QueueFree));
    }

    private void DismissOldest()
    {
        if (_activeToasts.Count > 0)
        {
            DismissToast(_activeToasts[0]);
        }
    }

    private static StyleBoxFlat CreateToastStyle()
    {
        var style = new StyleBoxFlat();
        style.BgColor = new Color(UiTheme.Colors.BgPanel, 0.9f);
        style.BorderColor = UiTheme.Colors.PanelBorder;
        style.SetBorderWidthAll(1);
        style.SetCornerRadiusAll(6);
        style.ContentMarginLeft = 16;
        style.ContentMarginRight = 16;
        style.ContentMarginTop = 8;
        style.ContentMarginBottom = 8;
        return style;
    }
}
