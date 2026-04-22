using Godot;

namespace DungeonGame.Ui;

/// <summary>
/// Base selectable-card component (docs/conventions/ui-component-model.md).
/// Owns framing, state styleboxes (Normal/Highlighted/Pressed), focus visuals,
/// and keyboard+mouse activation. Subclasses populate <see cref="Content"/> with
/// their own children via their constructor and leave everything else alone.
/// </summary>
public partial class Card : PanelContainer
{
    [Signal] public delegate void SelectedEventHandler();

    public static readonly Vector2 DefaultSize = new(240, 320);

    private enum State { Normal, Highlighted, Pressed }
    private static readonly StyleBoxFlat NormalStyle = BuildStyle(State.Normal);
    private static readonly StyleBoxFlat HighlightedStyle = BuildStyle(State.Highlighted);
    private static readonly StyleBoxFlat PressedStyle = BuildStyle(State.Pressed);

    protected VBoxContainer Content { get; private set; } = null!;
    private bool _pressed;

    public Card() : this(DefaultSize) { }

    public Card(Vector2 minSize)
    {
        CustomMinimumSize = minSize;
        SizeFlagsHorizontal = SizeFlags.ShrinkBegin;
        // Fill (not ShrinkBegin) so siblings in an HBoxContainer stretch to the
        // tallest card's height — CustomMinimumSize is a floor, not a ceiling,
        // and per-card content varies in length (long vs short descriptions).
        // Without Fill, each card sizes to its own content and the row becomes
        // a staircase.
        SizeFlagsVertical = SizeFlags.Fill;
        MouseFilter = MouseFilterEnum.Stop;
        FocusMode = FocusModeEnum.All;
        ApplyStyle();

        MouseEntered += RefreshStyle;
        MouseExited += RefreshStyle;
        FocusEntered += RefreshStyle;
        FocusExited += RefreshStyle;
        GuiInput += OnGuiInput;

        var margin = new MarginContainer();
        margin.MouseFilter = MouseFilterEnum.Ignore;
        AddChild(margin);

        Content = new VBoxContainer();
        Content.AddThemeConstantOverride("separation", 6);
        Content.MouseFilter = MouseFilterEnum.Ignore;
        Content.SizeFlagsVertical = SizeFlags.ExpandFill;
        margin.AddChild(Content);
    }

    /// <summary>
    /// Builds an HSeparator subclasses can drop into Content without
    /// swallowing mouse clicks. Default HSeparator has MouseFilter=Stop,
    /// which would block the card's GuiInput when the user clicks on the
    /// separator line.
    /// </summary>
    protected static HSeparator NonInteractiveSeparator()
    {
        var sep = new HSeparator();
        sep.MouseFilter = MouseFilterEnum.Ignore;
        return sep;
    }

    /// <summary>
    /// Mark this card as the "pressed" / active selection. Stays visually
    /// highlighted with the accent border until cleared. Screens use this for
    /// two-step flows (click to select, click Confirm to proceed).
    /// </summary>
    public void SetPressed(bool pressed)
    {
        if (_pressed == pressed) return;
        _pressed = pressed;
        RefreshStyle();
    }

    private void RefreshStyle() => ApplyStyle();

    private void ApplyStyle()
    {
        // IsInsideTree guard: ctor calls ApplyStyle before the node has a
        // viewport, and GetGlobalMousePosition/HasFocus both require one.
        bool highlighted = IsInsideTree() && (HasFocus() || IsMouseInside());
        var style = _pressed
            ? PressedStyle
            : highlighted ? HighlightedStyle : NormalStyle;
        AddThemeStyleboxOverride("panel", style);
    }

    private bool IsMouseInside() => GetGlobalRect().HasPoint(GetGlobalMousePosition());

    private void OnGuiInput(InputEvent @event)
    {
        if (@event is InputEventMouseButton mouse && mouse.Pressed && mouse.ButtonIndex == MouseButton.Left)
            EmitSignal(SignalName.Selected);
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (!HasFocus()) return;
        if (@event.IsActionPressed(Constants.InputActions.ActionCross) ||
            @event.IsActionPressed("ui_accept"))
        {
            EmitSignal(SignalName.Selected);
            GetViewport().SetInputAsHandled();
        }
    }

    private static StyleBoxFlat BuildStyle(State state)
    {
        var style = new StyleBoxFlat();
        style.BgColor = new Color(UiTheme.Colors.BgPanel, state == State.Normal ? 0.85f : 0.95f);
        style.BorderColor = state switch
        {
            State.Pressed => UiTheme.Colors.Accent,
            State.Highlighted => UiTheme.Colors.Accent,
            _ => UiTheme.Colors.PanelBorder,
        };
        // Compensate content margin so the inner rect stays fixed across
        // states — Godot StyleBox content rect = outer minus border minus
        // content margin, so a 1-px border-width bump would otherwise shift
        // every child 1 px on focus/hover/press. 22 = 2 + 20 (Normal pair).
        int borderWidth = state == State.Normal ? 2 : 3;
        style.SetBorderWidthAll(borderWidth);
        style.SetCornerRadiusAll(8);
        style.SetContentMarginAll(22 - borderWidth);
        return style;
    }
}
