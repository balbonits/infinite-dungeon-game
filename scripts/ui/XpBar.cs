using Godot;
using DungeonGame.Autoloads;

namespace DungeonGame.Ui;

/// <summary>
/// XP progress bar positioned below the skill bar. Shows level and XP numbers.
/// Glows gold on level-up, flashes red on XP loss.
/// </summary>
public partial class XpBar : Control
{
    private ColorRect _bgBar = null!;
    private ColorRect _fillBar = null!;
    private ColorRect _flashOverlay = null!;
    private Label _label = null!;
    private int _lastLevel;
    private int _lastXp;

    private const float BarHeight = 12f;
    private const float BarMargin = 80f; // leave room for orbs on sides

    public override void _Ready()
    {
        MouseFilter = MouseFilterEnum.Ignore;

        // Position at very bottom, full width minus orb margins
        SetAnchorsPreset(LayoutPreset.BottomWide);
        OffsetTop = -BarHeight - 2;
        OffsetLeft = BarMargin;
        OffsetRight = -BarMargin;

        // Background
        _bgBar = new ColorRect();
        _bgBar.Color = new Color(0.06f, 0.06f, 0.10f, 0.8f);
        _bgBar.SetAnchorsPreset(LayoutPreset.FullRect);
        _bgBar.MouseFilter = MouseFilterEnum.Ignore;
        AddChild(_bgBar);

        // Fill
        _fillBar = new ColorRect();
        _fillBar.Color = UiTheme.Colors.Accent;
        _fillBar.AnchorTop = 0;
        _fillBar.AnchorBottom = 1;
        _fillBar.AnchorLeft = 0;
        _fillBar.AnchorRight = 0; // updated in refresh
        _fillBar.OffsetLeft = 1;
        _fillBar.OffsetTop = 1;
        _fillBar.OffsetBottom = -1;
        _fillBar.MouseFilter = MouseFilterEnum.Ignore;
        AddChild(_fillBar);

        // Flash overlay (for level-up glow / XP loss flash)
        _flashOverlay = new ColorRect();
        _flashOverlay.Color = new Color(1, 1, 1, 0);
        _flashOverlay.SetAnchorsPreset(LayoutPreset.FullRect);
        _flashOverlay.MouseFilter = MouseFilterEnum.Ignore;
        AddChild(_flashOverlay);

        // Label
        _label = new Label();
        _label.SetAnchorsPreset(LayoutPreset.FullRect);
        _label.HorizontalAlignment = HorizontalAlignment.Center;
        _label.VerticalAlignment = VerticalAlignment.Center;
        _label.MouseFilter = MouseFilterEnum.Ignore;
        UiTheme.StyleLabel(_label, UiTheme.Colors.Ink, 9);
        AddChild(_label);

        _lastLevel = GameState.Instance.Level;
        _lastXp = GameState.Instance.Xp;

        GameState.Instance.Connect(
            GameState.SignalName.StatsChanged,
            new Callable(this, MethodName.OnStatsChanged));
        OnStatsChanged();
    }

    private void OnStatsChanged()
    {
        var gs = GameState.Instance;
        int xpToNext = Constants.Leveling.GetXpToLevel(gs.Level);
        float ratio = xpToNext > 0 ? Mathf.Clamp((float)gs.Xp / xpToNext, 0f, 1f) : 0f;

        _fillBar.AnchorRight = ratio;
        _fillBar.OffsetRight = -1;
        _label.Text = $"Lv.{gs.Level}  {gs.Xp} / {xpToNext} XP";

        // Level-up glow
        if (gs.Level > _lastLevel)
        {
            _lastLevel = gs.Level;
            FlashColor(UiTheme.Colors.Accent, 0.6f);
        }
        // XP loss flash
        else if (gs.Xp < _lastXp && gs.Level == _lastLevel)
        {
            FlashColor(UiTheme.Colors.Danger, 0.4f);
        }

        _lastXp = gs.Xp;
    }

    private void FlashColor(Color color, float duration)
    {
        _flashOverlay.Color = new Color(color, 0.6f);
        var tween = CreateTween();
        tween.TweenProperty(_flashOverlay, "color:a", 0.0f, duration);
    }
}
