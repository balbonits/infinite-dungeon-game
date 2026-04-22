using Godot;
using System.Collections.Generic;
using DungeonGame.Autoloads;

namespace DungeonGame.Ui;

/// <summary>
/// Class selection screen. Keyboard nav is handled at the screen level in
/// <see cref="_Input"/> because <see cref="Card"/>'s FocusMode=All lets
/// Godot's GUI focus system intercept arrow keys before _UnhandledInput
/// can see them — _Input runs first in the pipeline.
/// </summary>
public partial class ClassSelect : Control
{
    private static readonly ClassPreview[] Previews =
    {
        new(PlayerClass.Warrior, Strings.Classes.Warrior, Strings.Classes.WarriorDescription,
            3, 0, 2, 0, Strings.Classes.SkillSlash, Strings.Classes.SkillSlashType,
            UiTheme.Colors.Accent, "res://assets/icons/skill_slash.png"),
        new(PlayerClass.Ranger, Strings.Classes.Ranger, Strings.Classes.RangerDescription,
            1, 3, 1, 0, Strings.Classes.SkillArrowShot, Strings.Classes.SkillArrowShotType,
            UiTheme.Colors.Safe, "res://assets/icons/skill_arrow.png"),
        new(PlayerClass.Mage, Strings.Classes.Mage, Strings.Classes.MageDescription,
            0, 1, 1, 3, Strings.Classes.SkillMagicBolt, Strings.Classes.SkillMagicBoltType,
            UiTheme.Colors.Info, "res://assets/icons/skill_magic_bolt.png"),
    };

    private ClassCard? _selectedCard;
    private PlayerClass _selectedClass;
    private Button _confirmButton = null!;
    private Button _backButton = null!;
    private int _focusIndex = -1;
    private int _focusZone; // 0 = cards, 1 = confirm, 2 = back
    private readonly List<ClassCard> _cards = new();

    public override void _Ready()
    {
        ProcessMode = ProcessModeEnum.Always;

        var overlay = new ColorRect { Color = UiTheme.Colors.BgDark };
        overlay.SetAnchorsPreset(LayoutPreset.FullRect);
        AddChild(overlay);

        var center = new CenterContainer();
        center.SetAnchorsPreset(LayoutPreset.FullRect);
        AddChild(center);

        var mainVbox = new VBoxContainer();
        mainVbox.AddThemeConstantOverride("separation", 24);
        center.AddChild(mainVbox);

        var title = new Label { Text = Strings.Ui.ChooseClass };
        UiTheme.StyleLabel(title, UiTheme.Colors.Accent, UiTheme.FontSizes.Title);
        title.HorizontalAlignment = HorizontalAlignment.Center;
        mainVbox.AddChild(title);

        var cardRow = new HBoxContainer();
        cardRow.AddThemeConstantOverride("separation", 24);
        cardRow.Alignment = BoxContainer.AlignmentMode.Center;
        mainVbox.AddChild(cardRow);

        for (int i = 0; i < Previews.Length; i++)
        {
            int capturedIndex = i;
            var preview = Previews[i];
            var card = ClassCard.Create(preview, () => OnCardActivated(capturedIndex));
            _cards.Add(card);
            cardRow.AddChild(card);
        }

        _confirmButton = new Button { Text = Strings.Ui.ConfirmSelection };
        _confirmButton.CustomMinimumSize = new Vector2(200, 48);
        _confirmButton.SizeFlagsHorizontal = SizeFlags.ShrinkCenter;
        UiTheme.StyleButton(_confirmButton, UiTheme.FontSizes.Heading);
        _confirmButton.Disabled = true;
        _confirmButton.Pressed += OnConfirmPressed;
        mainVbox.AddChild(_confirmButton);

        _backButton = new Button { Text = Strings.Ui.BackToMainMenu };
        _backButton.CustomMinimumSize = new Vector2(200, 48);
        _backButton.SizeFlagsHorizontal = SizeFlags.ShrinkCenter;
        UiTheme.StyleSecondaryButton(_backButton, UiTheme.FontSizes.Heading);
        _backButton.Pressed += OnBackPressed;
        mainVbox.AddChild(_backButton);
    }

    public override void _Input(InputEvent @event)
    {
        if (!Visible || !IsInsideTree()) return;

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
        if (@event.IsActionPressed(Constants.InputActions.MoveDown))
        {
            _focusZone = System.Math.Min(_focusZone + 1, 2);
            UpdateZoneFocus();
            GetViewport()?.SetInputAsHandled();
            return;
        }
        if (@event.IsActionPressed(Constants.InputActions.MoveUp))
        {
            _focusZone = System.Math.Max(_focusZone - 1, 0);
            UpdateZoneFocus();
            GetViewport()?.SetInputAsHandled();
            return;
        }

        // S (action_cross) activates the focused zone's button. Godot's Button
        // fires Pressed on ui_accept (Enter/Space) natively, but not on
        // action_cross — without this shortcut, S-key confirm becomes a no-op.
        // Only consume the event for button zones (1/2). Zone 0 (cards) lets
        // the event fall through to the focused Card's _UnhandledInput so card
        // activation via S still works through the Card's own handler.
        if (@event.IsActionPressed(Constants.InputActions.ActionCross))
        {
            if (_focusZone == 2)
            {
                _backButton.EmitSignal(BaseButton.SignalName.Pressed);
                GetViewport()?.SetInputAsHandled();
                return;
            }
            if (_focusZone == 1 && _selectedCard != null)
            {
                OnConfirmPressed();
                GetViewport()?.SetInputAsHandled();
                return;
            }
        }

        if (KeyboardNav.IsCancelPressed(@event))
        {
            OnBackPressed();
            GetViewport()?.SetInputAsHandled();
        }
    }

    private void MoveFocus(int direction)
    {
        if (_cards.Count == 0) return;

        if (_focusIndex < 0)
            _focusIndex = direction > 0 ? 0 : _cards.Count - 1;
        else
            _focusIndex = (_focusIndex + direction + _cards.Count) % _cards.Count;

        _cards[_focusIndex].CallDeferred(Control.MethodName.GrabFocus);
        OnCardActivated(_focusIndex);
    }

    private void UpdateZoneFocus()
    {
        // CallDeferred because calling GrabFocus from inside _Input runs while
        // Godot is still dispatching the triggering key event — the focus
        // change gets discarded and reported as "nothing focused" on the next
        // frame. Deferring to idle lets the change land cleanly.
        switch (_focusZone)
        {
            case 0:
                if (_focusIndex < 0 && _cards.Count > 0) _focusIndex = 0;
                if (_focusIndex >= 0) _cards[_focusIndex].CallDeferred(Control.MethodName.GrabFocus);
                break;
            case 1:
                _confirmButton.CallDeferred(Control.MethodName.GrabFocus);
                break;
            case 2:
                _backButton.CallDeferred(Control.MethodName.GrabFocus);
                break;
        }
    }

    private void OnCardActivated(int index)
    {
        var card = _cards[index];
        var preview = Previews[index];

        if (_selectedCard != null && _selectedCard != card)
            _selectedCard.SetPressed(false);

        _selectedCard = card;
        _selectedClass = preview.Class;
        card.SetPressed(true);
        _confirmButton.Disabled = false;
    }

    private void OnBackPressed()
    {
        Visible = false;
        QueueFree();
        GetTree().Paused = true;
        GetTree().ReloadCurrentScene();
    }

    private void OnConfirmPressed()
    {
        if (_selectedCard == null)
            return;

        GameState.Instance.SelectedClass = _selectedClass;
        GameState.Instance.Reset();

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
