using Godot;
using System.Collections.Generic;
using DungeonGame.Autoloads;

namespace DungeonGame.Ui;

/// <summary>
/// Class selection screen. Two-step: click card to highlight, click Confirm to proceed.
/// </summary>
public partial class ClassSelect : Control
{
    private record ClassData(
        PlayerClass PlayerClass,
        string Name,
        string Description,
        int Str, int Dex, int Sta, int Int,
        string SkillName,
        string SkillType,
        Color SkillColor,
        string SkillIconPath
    );

    private static readonly ClassData[] Classes =
    {
        new(PlayerClass.Warrior, Strings.Classes.Warrior, Strings.Classes.WarriorDescription,
            3, 0, 2, 0, Strings.Classes.SkillSlash, Strings.Classes.SkillSlashType,
            UiTheme.Colors.Accent, "res://assets/icons/skill_slash.png"),
        new(PlayerClass.Ranger, Strings.Classes.Ranger, Strings.Classes.RangerDescription,
            1, 3, 1, 0, Strings.Classes.SkillArrowShot, Strings.Classes.SkillArrowShotType,
            UiTheme.Colors.Safe, "res://assets/icons/skill_arrow.png"),
        new(PlayerClass.Mage, Strings.Classes.Mage, Strings.Classes.MageDescription,
            0, 1, 1, 3, Strings.Classes.SkillMagicBolt, Strings.Classes.SkillMagicBoltType,
            new Color("4AE8E8"), "res://assets/icons/skill_magic_bolt.png"),
    };

    private PanelContainer? _selectedCard;
    private PlayerClass _selectedClass;
    private Button _confirmButton = null!;
    private Button _backButton = null!;
    private int _focusIndex = -1;
    private int _focusZone; // 0 = cards, 1 = confirm, 2 = back
    private readonly List<PanelContainer> _cards = new();

    private static readonly StyleBoxFlat DefaultCardStyle = CreateCardStyle(false, false);
    private static readonly StyleBoxFlat HoverCardStyle = CreateCardStyle(true, false);
    private static readonly StyleBoxFlat SelectedCardStyle = CreateCardStyle(false, true);

    private static StyleBoxFlat CreateCardStyle(bool hovered, bool selected)
    {
        var style = new StyleBoxFlat();
        style.BgColor = selected
            ? new Color(UiTheme.Colors.BgPanel, 0.95f)
            : new Color(UiTheme.Colors.BgPanel, hovered ? 0.92f : 0.85f);
        style.BorderColor = selected
            ? UiTheme.Colors.Accent
            : hovered ? UiTheme.Colors.PanelBorderBright : UiTheme.Colors.PanelBorder;
        // Fixed border width prevents layout shift on state change
        style.SetBorderWidthAll(2);
        style.SetCornerRadiusAll(8);
        style.SetContentMarginAll(20);
        return style;
    }

    public override void _Ready()
    {
        ProcessMode = ProcessModeEnum.Always;

        var overlay = new ColorRect();
        overlay.Color = UiTheme.Colors.BgDark;
        overlay.SetAnchorsPreset(LayoutPreset.FullRect);
        AddChild(overlay);

        var center = new CenterContainer();
        center.SetAnchorsPreset(LayoutPreset.FullRect);
        AddChild(center);

        var mainVbox = new VBoxContainer();
        mainVbox.AddThemeConstantOverride("separation", 24);
        center.AddChild(mainVbox);

        // Title
        var title = new Label();
        title.Text = Strings.Ui.ChooseClass;
        UiTheme.StyleLabel(title, UiTheme.Colors.Accent, UiTheme.FontSizes.Title);
        title.HorizontalAlignment = HorizontalAlignment.Center;
        mainVbox.AddChild(title);

        // Class cards row
        var cardRow = new HBoxContainer();
        cardRow.AddThemeConstantOverride("separation", 24);
        cardRow.Alignment = BoxContainer.AlignmentMode.Center;
        mainVbox.AddChild(cardRow);

        foreach (var data in Classes)
        {
            var card = CreateClassCard(data);
            _cards.Add(card);
            cardRow.AddChild(card);
        }

        // Confirm button
        _confirmButton = new Button();
        _confirmButton.Text = Strings.Ui.ConfirmSelection;
        _confirmButton.CustomMinimumSize = new Vector2(200, 48);
        _confirmButton.SizeFlagsHorizontal = SizeFlags.ShrinkCenter;
        UiTheme.StyleButton(_confirmButton, UiTheme.FontSizes.Heading);
        _confirmButton.Disabled = true;
        _confirmButton.Connect(BaseButton.SignalName.Pressed,
            Callable.From(OnConfirmPressed));
        mainVbox.AddChild(_confirmButton);

        // Back button
        _backButton = new Button();
        var backBtn = _backButton;
        backBtn.Text = "Back to Main Menu";
        backBtn.CustomMinimumSize = new Vector2(200, 48);
        backBtn.SizeFlagsHorizontal = SizeFlags.ShrinkCenter;
        backBtn.FocusMode = FocusModeEnum.All;
        UiTheme.StyleSecondaryButton(backBtn, UiTheme.FontSizes.Heading);
        backBtn.Connect(BaseButton.SignalName.Pressed, Callable.From(() =>
        {
            Visible = false;
            QueueFree();
            GetTree().Paused = true;
            GetTree().ReloadCurrentScene();
        }));
        mainVbox.AddChild(backBtn);
    }

    private PanelContainer CreateClassCard(ClassData data)
    {
        var panel = new PanelContainer();
        panel.AddThemeStyleboxOverride("panel", DefaultCardStyle);
        panel.CustomMinimumSize = new Vector2(220, 340);
        panel.MouseFilter = MouseFilterEnum.Stop;

        panel.MouseEntered += () => OnCardHovered(panel);
        panel.MouseExited += () => OnCardUnhovered(panel);
        panel.GuiInput += (@event) =>
        {
            if (@event is InputEventMouseButton mouseEvent &&
                mouseEvent.Pressed && mouseEvent.ButtonIndex == MouseButton.Left)
                OnCardClicked(panel, data.PlayerClass);
        };

        var margin = new MarginContainer();
        panel.AddChild(margin);

        var vbox = new VBoxContainer();
        vbox.AddThemeConstantOverride("separation", 8);
        vbox.MouseFilter = MouseFilterEnum.Ignore;
        vbox.SizeFlagsVertical = SizeFlags.ExpandFill;
        margin.AddChild(vbox);

        // --- Class name ---
        var nameLabel = new Label();
        nameLabel.Text = data.Name;
        UiTheme.StyleLabel(nameLabel, UiTheme.Colors.Accent, UiTheme.FontSizes.Heading);
        nameLabel.HorizontalAlignment = HorizontalAlignment.Center;
        nameLabel.MouseFilter = MouseFilterEnum.Ignore;
        vbox.AddChild(nameLabel);

        // --- Character sprite ---
        // LPC sheets are multi-row animation atlases. Crop to south-facing
        // walk frame 0 (standing pose) via LoadPortraitFrame — loading the
        // full sheet would tile the entire animation grid into the card.
        int classIndex = (int)data.PlayerClass;
        if (classIndex < Constants.Assets.PlayerClassPreviews.Length)
        {
            var portrait = DirectionalSprite.LoadPortraitFrame(Constants.Assets.PlayerClassPreviews[classIndex]);
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

        // --- Description ---
        var descLabel = new Label();
        descLabel.Text = data.Description;
        UiTheme.StyleLabel(descLabel, UiTheme.Colors.Muted, UiTheme.FontSizes.Body);
        descLabel.HorizontalAlignment = HorizontalAlignment.Center;
        descLabel.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        descLabel.MouseFilter = MouseFilterEnum.Ignore;
        vbox.AddChild(descLabel);

        // --- Separator ---
        vbox.AddChild(new HSeparator());

        // --- Stats grid (4 columns: label, value) ---
        var statsGrid = new GridContainer();
        statsGrid.Columns = 4;
        statsGrid.AddThemeConstantOverride("h_separation", 6);
        statsGrid.AddThemeConstantOverride("v_separation", 4);
        statsGrid.SizeFlagsHorizontal = SizeFlags.ShrinkCenter;
        statsGrid.MouseFilter = MouseFilterEnum.Ignore;

        AddStatRow(statsGrid, "STR", data.Str, data.Str >= 3);
        AddStatRow(statsGrid, "DEX", data.Dex, data.Dex >= 3);
        AddStatRow(statsGrid, "STA", data.Sta, data.Sta >= 3);
        AddStatRow(statsGrid, "INT", data.Int, data.Int >= 3);
        vbox.AddChild(statsGrid);

        // --- Separator ---
        vbox.AddChild(new HSeparator());

        // --- Skill section ---
        var skillRow = new HBoxContainer();
        skillRow.AddThemeConstantOverride("separation", 8);
        skillRow.SizeFlagsHorizontal = SizeFlags.ShrinkCenter;
        skillRow.MouseFilter = MouseFilterEnum.Ignore;

        // Skill icon (PixelLab art or colored placeholder)
        var iconPanel = new PanelContainer();
        var iconStyle = new StyleBoxFlat();
        iconStyle.BgColor = new Color(data.SkillColor, 0.2f);
        iconStyle.BorderColor = data.SkillColor;
        iconStyle.SetBorderWidthAll(1);
        iconStyle.SetCornerRadiusAll(4);
        iconStyle.SetContentMarginAll(4);
        iconPanel.AddThemeStyleboxOverride("panel", iconStyle);
        iconPanel.CustomMinimumSize = new Vector2(36, 36);
        iconPanel.MouseFilter = MouseFilterEnum.Ignore;

        if (ResourceLoader.Exists(data.SkillIconPath))
        {
            var skillIcon = new TextureRect();
            skillIcon.Texture = GD.Load<Texture2D>(data.SkillIconPath);
            skillIcon.TextureFilter = CanvasItem.TextureFilterEnum.Nearest;
            skillIcon.StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered;
            skillIcon.ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize;
            skillIcon.MouseFilter = MouseFilterEnum.Ignore;
            iconPanel.AddChild(skillIcon);
        }

        skillRow.AddChild(iconPanel);

        // Skill name + type
        var skillVbox = new VBoxContainer();
        skillVbox.AddThemeConstantOverride("separation", 0);
        skillVbox.MouseFilter = MouseFilterEnum.Ignore;

        var skillNameLabel = new Label();
        skillNameLabel.Text = data.SkillName;
        UiTheme.StyleLabel(skillNameLabel, data.SkillColor, UiTheme.FontSizes.Body);
        skillNameLabel.MouseFilter = MouseFilterEnum.Ignore;
        skillVbox.AddChild(skillNameLabel);

        var skillTypeLabel = new Label();
        skillTypeLabel.Text = data.SkillType;
        UiTheme.StyleLabel(skillTypeLabel, UiTheme.Colors.Muted, UiTheme.FontSizes.Small);
        skillTypeLabel.MouseFilter = MouseFilterEnum.Ignore;
        skillVbox.AddChild(skillTypeLabel);

        skillRow.AddChild(skillVbox);
        vbox.AddChild(skillRow);

        // --- Spacer ---
        var spacer = new Control();
        spacer.SizeFlagsVertical = SizeFlags.ExpandFill;
        spacer.MouseFilter = MouseFilterEnum.Ignore;
        vbox.AddChild(spacer);

        // --- Hint ---
        var hintLabel = new Label();
        hintLabel.Text = Strings.Ui.ClickToSelect;
        UiTheme.StyleLabel(hintLabel, UiTheme.Colors.Muted, UiTheme.FontSizes.Small);
        hintLabel.HorizontalAlignment = HorizontalAlignment.Center;
        hintLabel.MouseFilter = MouseFilterEnum.Ignore;
        vbox.AddChild(hintLabel);

        return panel;
    }

    private static void AddStatRow(GridContainer grid, string statName, int value, bool isPrimary)
    {
        var nameLabel = new Label();
        nameLabel.Text = statName;
        UiTheme.StyleLabel(nameLabel, isPrimary ? UiTheme.Colors.Accent : UiTheme.Colors.Muted, UiTheme.FontSizes.Body);
        nameLabel.MouseFilter = MouseFilterEnum.Ignore;
        grid.AddChild(nameLabel);

        var valueLabel = new Label();
        valueLabel.Text = $"+{value}";
        Color valueColor = value == 0
            ? new Color(UiTheme.Colors.Muted, 0.4f)
            : isPrimary ? UiTheme.Colors.Accent : UiTheme.Colors.Ink;
        UiTheme.StyleLabel(valueLabel, valueColor, UiTheme.FontSizes.Body);
        valueLabel.MouseFilter = MouseFilterEnum.Ignore;
        grid.AddChild(valueLabel);
    }

    private void OnCardHovered(PanelContainer card)
    {
        if (card != _selectedCard)
            card.AddThemeStyleboxOverride("panel", HoverCardStyle);
    }

    private void OnCardUnhovered(PanelContainer card)
    {
        if (card != _selectedCard)
            card.AddThemeStyleboxOverride("panel", DefaultCardStyle);
    }

    private void OnCardClicked(PanelContainer card, PlayerClass playerClass)
    {
        if (_selectedCard != null)
            _selectedCard.AddThemeStyleboxOverride("panel", DefaultCardStyle);

        _selectedCard = card;
        _selectedClass = playerClass;
        card.AddThemeStyleboxOverride("panel", SelectedCardStyle);

        // Enable confirm button (no layout-affecting animation)
        _confirmButton.Disabled = false;
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        // Guard against input fired after the node has been QueueFree-d or
        // detached from the tree — the back-button handler transitions away
        // from ClassSelect and Input.FlushBufferedEvents re-enters this method
        // while the node is mid-teardown. Without the IsInsideTree() check,
        // GetViewport() returns null on the trailing SetInputAsHandled call.
        if (!Visible || !IsInsideTree())
            return;

        // Left/Right — navigate within cards (zone 0)
        if (@event.IsActionPressed(Constants.InputActions.MoveLeft))
        {
            _focusZone = 0;
            MoveFocus(-1);
            GetViewport()?.SetInputAsHandled();
            return;
        }
        if (@event.IsActionPressed(Constants.InputActions.MoveRight))
        {
            _focusZone = 0;
            MoveFocus(1);
            GetViewport()?.SetInputAsHandled();
            return;
        }

        // Down — move to next zone (cards → confirm → back)
        if (@event.IsActionPressed(Constants.InputActions.MoveDown))
        {
            _focusZone = System.Math.Min(_focusZone + 1, 2);
            UpdateZoneFocus();
            GetViewport()?.SetInputAsHandled();
            return;
        }

        // Up — move to previous zone (back → confirm → cards)
        if (@event.IsActionPressed(Constants.InputActions.MoveUp))
        {
            _focusZone = System.Math.Max(_focusZone - 1, 0);
            UpdateZoneFocus();
            GetViewport()?.SetInputAsHandled();
            return;
        }

        // Confirm
        if (@event.IsActionPressed(Constants.InputActions.ActionCross) ||
            (@event is InputEventKey key && key.Pressed &&
             (key.Keycode == Key.Enter || key.Keycode == Key.Space)))
        {
            if (_focusZone == 2)
            {
                _backButton.EmitSignal(BaseButton.SignalName.Pressed);
            }
            else if (_focusZone == 1 && _selectedCard != null)
            {
                OnConfirmPressed();
            }
            else if (_focusZone == 0 && _focusIndex >= 0)
            {
                OnCardClicked(_cards[_focusIndex], Classes[_focusIndex].PlayerClass);
            }
            GetViewport()?.SetInputAsHandled();
            return;
        }

        // Cancel — back to main menu
        if (KeyboardNav.IsCancelPressed(@event))
        {
            _backButton?.EmitSignal(BaseButton.SignalName.Pressed);
            GetViewport()?.SetInputAsHandled();
        }
    }

    private void UpdateZoneFocus()
    {
        // Remove button focus highlights
        _confirmButton.ReleaseFocus();
        _backButton.ReleaseFocus();

        switch (_focusZone)
        {
            case 0: // Cards
                if (_focusIndex < 0 && _cards.Count > 0)
                {
                    _focusIndex = 0;
                    _cards[0].AddThemeStyleboxOverride("panel", HoverCardStyle);
                }
                break;
            case 1: // Confirm
                _confirmButton.GrabFocus();
                break;
            case 2: // Back
                _backButton.GrabFocus();
                break;
        }
    }

    private void MoveFocus(int direction)
    {
        if (_cards.Count == 0)
            return;

        // Unhover previous
        if (_focusIndex >= 0 && _cards[_focusIndex] != _selectedCard)
            _cards[_focusIndex].AddThemeStyleboxOverride("panel", DefaultCardStyle);

        // Move
        if (_focusIndex < 0)
            _focusIndex = direction > 0 ? 0 : _cards.Count - 1;
        else
            _focusIndex = (_focusIndex + direction + _cards.Count) % _cards.Count;

        // Apply hover or select the focused card
        var card = _cards[_focusIndex];
        if (card != _selectedCard)
            card.AddThemeStyleboxOverride("panel", HoverCardStyle);

        // Auto-select on focus for immediate feedback
        OnCardClicked(card, Classes[_focusIndex].PlayerClass);
    }

    private void OnConfirmPressed()
    {
        if (_selectedCard == null)
            return;

        GameState.Instance.SelectedClass = _selectedClass;
        GameState.Instance.Reset();

        // Let ScreenTransition cover ClassSelect with the fade-to-black.
        // At midpoint (overlay fully opaque), hide ClassSelect and swap in the town.
        // This prevents the town from flashing into view before the loading screen.
        ScreenTransition.Instance.Play(
            Strings.Town.Title,
            () =>
            {
                Visible = false;
                QueueFree();
                GetTree().Paused = false;
                Scenes.Main.Instance.LoadTown();
            },
            Strings.Town.Arriving);
    }
}
