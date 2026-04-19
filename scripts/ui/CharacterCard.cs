using Godot;
using System;

namespace DungeonGame.Ui;

/// <summary>
/// Reusable character card — same visual style as class select cards.
/// Used on title screen for saved games and anywhere a character summary is needed.
/// PanelContainer with sprite, name, stats grid, and details.
/// </summary>
public partial class CharacterCard : PanelContainer
{
    private static readonly StyleBoxFlat NormalStyle = CreateStyle(false);
    private static readonly StyleBoxFlat FocusedStyle = CreateStyle(true);

    private Action? _onPressed;

    public static CharacterCard Create(SaveData save, Action onPressed)
    {
        var card = new CharacterCard();
        card._onPressed = onPressed;
        card.AddThemeStyleboxOverride("panel", NormalStyle);
        card.CustomMinimumSize = new Vector2(220, 0);
        card.SizeFlagsHorizontal = SizeFlags.ShrinkCenter;
        card.MouseFilter = MouseFilterEnum.Stop;
        card.FocusMode = FocusModeEnum.All;

        card.MouseEntered += () => card.AddThemeStyleboxOverride("panel", FocusedStyle);
        card.MouseExited += () =>
        {
            if (!card.HasFocus())
                card.AddThemeStyleboxOverride("panel", NormalStyle);
        };
        card.FocusEntered += () => card.AddThemeStyleboxOverride("panel", FocusedStyle);
        card.FocusExited += () => card.AddThemeStyleboxOverride("panel", NormalStyle);
        card.GuiInput += card.OnGuiInput;

        var margin = new MarginContainer();
        card.AddChild(margin);

        var vbox = new VBoxContainer();
        vbox.AddThemeConstantOverride("separation", 6);
        vbox.MouseFilter = MouseFilterEnum.Ignore;
        margin.AddChild(vbox);

        // Class name heading
        var nameLabel = new Label();
        nameLabel.Text = save.SelectedClass.ToString();
        UiTheme.StyleLabel(nameLabel, UiTheme.Colors.Accent, UiTheme.FontSizes.Heading);
        nameLabel.HorizontalAlignment = HorizontalAlignment.Center;
        nameLabel.MouseFilter = MouseFilterEnum.Ignore;
        vbox.AddChild(nameLabel);

        // Character sprite — LPC sheets are multi-row animation atlases, so
        // we crop to the south-facing walk frame 0 (neutral standing pose)
        // rather than loading the entire sheet as a raw Texture2D. Without
        // this, the card renders the whole sprite grid tiled into 92x92.
        int classIdx = (int)save.SelectedClass;
        if (classIdx < Constants.Assets.PlayerClassPreviews.Length)
        {
            var portrait = DirectionalSprite.LoadPortraitFrame(Constants.Assets.PlayerClassPreviews[classIdx]);
            if (portrait != null)
            {
                var sprite = new TextureRect();
                sprite.Texture = portrait;
                sprite.TextureFilter = CanvasItem.TextureFilterEnum.Nearest;
                sprite.StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered;
                sprite.CustomMinimumSize = new Vector2(92, 92);
                sprite.SizeFlagsHorizontal = SizeFlags.ShrinkCenter;
                sprite.MouseFilter = MouseFilterEnum.Ignore;
                vbox.AddChild(sprite);
            }
        }

        // Level
        var levelLabel = new Label();
        levelLabel.Text = $"Level {save.Level}";
        UiTheme.StyleLabel(levelLabel, UiTheme.Colors.Ink, UiTheme.FontSizes.Label);
        levelLabel.HorizontalAlignment = HorizontalAlignment.Center;
        levelLabel.MouseFilter = MouseFilterEnum.Ignore;
        vbox.AddChild(levelLabel);

        vbox.AddChild(new HSeparator());

        // Stats grid (same layout as class select)
        var statsGrid = new GridContainer();
        statsGrid.Columns = 4;
        statsGrid.AddThemeConstantOverride("h_separation", 6);
        statsGrid.AddThemeConstantOverride("v_separation", 4);
        statsGrid.SizeFlagsHorizontal = SizeFlags.ShrinkCenter;
        statsGrid.MouseFilter = MouseFilterEnum.Ignore;
        AddStatRow(statsGrid, "STR", save.Str);
        AddStatRow(statsGrid, "DEX", save.Dex);
        AddStatRow(statsGrid, "STA", save.Sta);
        AddStatRow(statsGrid, "INT", save.Int);
        vbox.AddChild(statsGrid);

        vbox.AddChild(new HSeparator());

        // HP / MP
        var hpMp = new Label();
        hpMp.Text = $"HP: {save.Hp}/{save.MaxHp}   MP: {save.Mana}/{save.MaxMana}";
        UiTheme.StyleLabel(hpMp, UiTheme.Colors.Ink, UiTheme.FontSizes.Small);
        hpMp.HorizontalAlignment = HorizontalAlignment.Center;
        hpMp.MouseFilter = MouseFilterEnum.Ignore;
        vbox.AddChild(hpMp);

        // Floor + Gold
        var floorGold = new Label();
        floorGold.Text = $"Floor: {save.FloorNumber}   Deepest: {save.DeepestFloor}   Gold: {save.Gold}";
        UiTheme.StyleLabel(floorGold, UiTheme.Colors.Info, UiTheme.FontSizes.Small);
        floorGold.HorizontalAlignment = HorizontalAlignment.Center;
        floorGold.MouseFilter = MouseFilterEnum.Ignore;
        vbox.AddChild(floorGold);

        // XP progress
        int xpToNext = Constants.Leveling.GetXpToLevel(save.Level);
        float xpPct = xpToNext > 0 ? (float)save.Xp / xpToNext * 100 : 0;
        var xpLabel = new Label();
        xpLabel.Text = $"XP: {xpPct:F0}%";
        UiTheme.StyleLabel(xpLabel, UiTheme.Colors.Accent, UiTheme.FontSizes.Small);
        xpLabel.HorizontalAlignment = HorizontalAlignment.Center;
        xpLabel.MouseFilter = MouseFilterEnum.Ignore;
        vbox.AddChild(xpLabel);

        // Save date
        var dateLabel = new Label();
        dateLabel.Text = save.SaveDate;
        UiTheme.StyleLabel(dateLabel, UiTheme.Colors.Muted, 9);
        dateLabel.HorizontalAlignment = HorizontalAlignment.Center;
        dateLabel.MouseFilter = MouseFilterEnum.Ignore;
        vbox.AddChild(dateLabel);

        return card;
    }

    private void OnGuiInput(InputEvent @event)
    {
        if (@event is InputEventMouseButton mouse && mouse.Pressed && mouse.ButtonIndex == MouseButton.Left)
            _onPressed?.Invoke();
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (!HasFocus()) return;
        // Accept S (action_cross) AND Enter/Space (ui_accept). CharacterCard is a
        // PanelContainer, not a Button, so Godot's built-in ui_accept handling on
        // focused Buttons doesn't apply — we have to route Enter ourselves.
        if (@event.IsActionPressed(Constants.InputActions.ActionCross) ||
            @event.IsActionPressed("ui_accept"))
        {
            _onPressed?.Invoke();
            GetViewport().SetInputAsHandled();
        }
    }

    private static void AddStatRow(GridContainer grid, string label, int value)
    {
        var lbl = new Label();
        lbl.Text = label;
        UiTheme.StyleLabel(lbl, UiTheme.Colors.Muted, UiTheme.FontSizes.Small);
        lbl.MouseFilter = MouseFilterEnum.Ignore;
        grid.AddChild(lbl);

        var val = new Label();
        val.Text = value.ToString();
        UiTheme.StyleLabel(val, value > 0 ? UiTheme.Colors.Ink : UiTheme.Colors.Muted, UiTheme.FontSizes.Small);
        val.MouseFilter = MouseFilterEnum.Ignore;
        grid.AddChild(val);
    }

    private static StyleBoxFlat CreateStyle(bool focused)
    {
        var style = new StyleBoxFlat();
        style.BgColor = new Color(UiTheme.Colors.BgPanel, focused ? 0.95f : 0.85f);
        style.BorderColor = focused ? UiTheme.Colors.Accent : UiTheme.Colors.PanelBorder;
        style.SetBorderWidthAll(2);
        style.SetCornerRadiusAll(8);
        style.SetContentMarginAll(20);
        return style;
    }
}
