using Godot;
using DungeonGame.Autoloads;

namespace DungeonGame.Ui;

/// <summary>
/// Load Game screen (UI-02). Three save-slot cards side by side + Load/Back buttons.
/// Spec: docs/flows/load-game.md.
///
/// Keyboard nav:
/// - Left/Right cycle between the three card slots.
/// - Down from cards zone → Load button.
/// - Down from Load → Back button.
/// - Up reverses zones.
/// - S / Enter activates focused control (select card, press Load/Back, open delete).
/// - Delete (or Shift+S) on a populated card → delete confirmation dialog.
/// - D / Esc → back to splash.
/// </summary>
public partial class LoadGameScreen : Control
{
    [Signal] public delegate void LoadSelectedEventHandler(int slotIndex);
    [Signal] public delegate void BackPressedEventHandler();
    /// <summary>
    /// Fired after a save slot is deleted. The caller (Main.ShowLoadGameScreen)
    /// is expected to tear down this instance and create a fresh LoadGameScreen
    /// rather than try to mutate this one in place — in-place rebuild hit
    /// lifecycle bugs where the screen's freed children + CallDeferred(_Ready)
    /// could race with a user pressing Back, orphaning splash focus and
    /// silently breaking the New Game button on return.
    /// </summary>
    [Signal] public delegate void SlotDeletedEventHandler();

    private int _focusedSlot = -1;    // 0..2 when a card is focused; -1 when buttons focused
    private int _buttonZone = 0;      // 0 = Load, 1 = Back
    private readonly SlotEntry[] _slots = new SlotEntry[SaveManager.SlotCount];
    private Button _loadBtn = null!;
    private Button _backBtn = null!;
    private bool _ready;

    private sealed class SlotEntry
    {
        public int Index;
        public bool IsPopulated;
        public Control Root = null!;          // Teardown target (the parent this slot adds to the row).
        public Control Focusable = null!;     // The control that receives GrabFocus() — must have FocusMode != None.
        public CharacterCard? Card;           // Populated slot — the character card.
        public Button? DeleteBtn;             // Red X, only on populated.
    }

    public override void _Ready()
    {
        ProcessMode = ProcessModeEnum.Always;

        var bg = new ColorRect { Color = UiTheme.Colors.BgDark };
        bg.SetAnchorsPreset(LayoutPreset.FullRect);
        AddChild(bg);

        var root = new VBoxContainer();
        root.AddThemeConstantOverride("separation", 16);
        root.SetAnchorsPreset(LayoutPreset.Center);
        root.CustomMinimumSize = new Vector2(780, 0);
        var center = new CenterContainer();
        center.SetAnchorsPreset(LayoutPreset.FullRect);
        center.AddChild(root);
        AddChild(center);

        var title = new Label { Text = "LOAD GAME" };
        UiTheme.StyleLabel(title, UiTheme.Colors.Accent, 32);
        title.HorizontalAlignment = HorizontalAlignment.Center;
        root.AddChild(title);

        var subtitle = new Label { Text = "Choose a character to continue your descent." };
        UiTheme.StyleLabel(subtitle, UiTheme.Colors.Muted, UiTheme.FontSizes.Body);
        subtitle.HorizontalAlignment = HorizontalAlignment.Center;
        root.AddChild(subtitle);

        var cardsRow = new HBoxContainer();
        cardsRow.AddThemeConstantOverride("separation", 16);
        cardsRow.Alignment = BoxContainer.AlignmentMode.Center;
        root.AddChild(cardsRow);

        for (int i = 0; i < SaveManager.SlotCount; i++)
            _slots[i] = BuildSlot(i, cardsRow);

        var buttonsRow = new HBoxContainer();
        buttonsRow.AddThemeConstantOverride("separation", 16);
        buttonsRow.Alignment = BoxContainer.AlignmentMode.Center;
        root.AddChild(buttonsRow);

        _backBtn = new Button { Text = "Back" };
        _backBtn.CustomMinimumSize = new Vector2(140, 40);
        _backBtn.FocusMode = FocusModeEnum.All;
        UiTheme.StyleSecondaryButton(_backBtn, UiTheme.FontSizes.Button);
        _backBtn.Pressed += () => EmitSignal(SignalName.BackPressed);

        _loadBtn = new Button { Text = "Load" };
        _loadBtn.CustomMinimumSize = new Vector2(180, 40);
        _loadBtn.FocusMode = FocusModeEnum.All;
        UiTheme.StyleButton(_loadBtn, UiTheme.FontSizes.Button);
        _loadBtn.Pressed += () => TryLoadFocusedSlot();

        buttonsRow.AddChild(_backBtn);
        buttonsRow.AddChild(_loadBtn);

        var hints = UiTheme.CreateHintBar(
            ("Left/Right", "Slot"),
            ("Up/Down", "Zone"),
            (Constants.InputActions.ActionCross, "Select"),
            ("Delete", "Delete save"),
            (Constants.InputActions.ActionTriangle, "Back"));
        root.AddChild(hints);

        // Focus the first populated slot, falling back to Back if everything is empty.
        // Guard the callback: the user may press Back (freeing this screen) before
        // the 0.15s timer fires. Touching the freed C# wrapper would throw
        // ObjectDisposedException.
        var timer = GetTree().CreateTimer(0.15);
        timer.Timeout += () =>
        {
            if (!IsInstanceValid(this) || !IsInsideTree())
                return;
            _ready = true;
            FocusFirstAvailable();
        };
    }

    private SlotEntry BuildSlot(int index, HBoxContainer parent)
    {
        var entry = new SlotEntry { Index = index };
        var save = SaveManager.Instance?.PeekSlot(index);
        entry.IsPopulated = save != null;

        if (save != null)
        {
            var card = CharacterCard.Create(save, () => EmitSignal(SignalName.LoadSelected, index));
            card.FocusEntered += () => { _focusedSlot = index; RefreshLoadEnabled(); };
            card.SizeFlagsVertical = SizeFlags.ShrinkBegin;
            entry.Card = card;

            // Wrap card in a container that positions the delete X over its top-right.
            var wrapper = new Control();
            wrapper.CustomMinimumSize = new Vector2(240, 280);
            wrapper.SizeFlagsVertical = SizeFlags.ShrinkBegin;

            card.SetAnchorsPreset(LayoutPreset.FullRect);
            wrapper.AddChild(card);

            var deleteBtn = new Button { Text = "X" };
            deleteBtn.CustomMinimumSize = new Vector2(32, 32);
            deleteBtn.FocusMode = FocusModeEnum.All;
            deleteBtn.AddThemeColorOverride("font_color", Colors.White);
            UiTheme.StyleDangerButton(deleteBtn, UiTheme.FontSizes.Small);
            // Position the X at the top-right corner (inside the card border).
            deleteBtn.OffsetLeft = -40;
            deleteBtn.OffsetTop = 4;
            deleteBtn.SetAnchor(Side.Left, 1.0f);
            deleteBtn.SetAnchor(Side.Right, 1.0f);
            deleteBtn.SetAnchor(Side.Top, 0.0f);
            deleteBtn.SetAnchor(Side.Bottom, 0.0f);
            deleteBtn.Pressed += () => OpenDeleteDialog(index);
            entry.DeleteBtn = deleteBtn;
            wrapper.AddChild(deleteBtn);

            entry.Root = wrapper;
            entry.Focusable = card; // CharacterCard has FocusMode=All; wrapper is a plain Control (unfocusable).
            parent.AddChild(wrapper);
        }
        else
        {
            var empty = new PanelContainer();
            empty.CustomMinimumSize = new Vector2(240, 280);
            empty.SizeFlagsVertical = SizeFlags.ShrinkBegin;
            empty.FocusMode = FocusModeEnum.All;
            empty.AddThemeStyleboxOverride("panel", UiTheme.CreatePanelStyle(0.4f, false));
            empty.FocusEntered += () => { _focusedSlot = index; RefreshLoadEnabled(); };

            var vbox = new VBoxContainer();
            vbox.Alignment = BoxContainer.AlignmentMode.Center;
            vbox.SizeFlagsHorizontal = SizeFlags.ExpandFill;
            vbox.SizeFlagsVertical = SizeFlags.ExpandFill;
            empty.AddChild(vbox);

            var label = new Label { Text = "Empty Slot" };
            UiTheme.StyleLabel(label, UiTheme.Colors.Muted, UiTheme.FontSizes.Body);
            label.HorizontalAlignment = HorizontalAlignment.Center;
            vbox.AddChild(label);

            var sublabel = new Label { Text = $"Slot {index + 1}" };
            UiTheme.StyleLabel(sublabel, UiTheme.Colors.Muted, UiTheme.FontSizes.Small);
            sublabel.HorizontalAlignment = HorizontalAlignment.Center;
            vbox.AddChild(sublabel);

            entry.Root = empty;
            entry.Focusable = empty;
            parent.AddChild(empty);
        }

        return entry;
    }

    private void RefreshLoadEnabled()
    {
        bool populated = _focusedSlot >= 0 && _slots[_focusedSlot].IsPopulated;
        _loadBtn.Disabled = !populated;
    }

    private void FocusFirstAvailable()
    {
        for (int i = 0; i < _slots.Length; i++)
        {
            if (_slots[i].IsPopulated)
            {
                _focusedSlot = i;
                _slots[i].Focusable.GrabFocus();
                RefreshLoadEnabled();
                return;
            }
        }
        _focusedSlot = -1;
        _backBtn.GrabFocus();
        RefreshLoadEnabled();
    }

    private void CycleSlotFocus(int direction)
    {
        if (_focusedSlot < 0)
        {
            // Coming up from the buttons zone. Focus any slot; prefer the first populated.
            FocusFirstAvailable();
            return;
        }
        int next = (_focusedSlot + direction + _slots.Length) % _slots.Length;
        _focusedSlot = next;
        _slots[next].Focusable.GrabFocus();
        RefreshLoadEnabled();
    }

    private void MoveToButtonZone()
    {
        _focusedSlot = -1;
        if (_buttonZone == 0) _loadBtn.GrabFocus();
        else _backBtn.GrabFocus();
        RefreshLoadEnabled();
    }

    private void TryLoadFocusedSlot()
    {
        if (_focusedSlot < 0 || !_slots[_focusedSlot].IsPopulated) return;
        EmitSignal(SignalName.LoadSelected, _focusedSlot);
    }

    private void OpenDeleteDialog(int slotIndex)
    {
        var save = SaveManager.Instance?.PeekSlot(slotIndex);
        if (save == null) return;
        var dialog = DeleteConfirmDialog.Create(save, () =>
        {
            SaveManager.Instance?.DeleteSlot(slotIndex);
            Toast.Instance?.Success($"Deleted slot {slotIndex + 1}");
            // Emit instead of rebuild-in-place. The Main flow owns the full
            // screen lifecycle and will create a fresh LoadGameScreen on the
            // next frame, avoiding the freed-children / CallDeferred(_Ready)
            // race that used to orphan splash focus on Back.
            EmitSignal(SignalName.SlotDeleted);
        });
        AddChild(dialog);
        dialog.Open();
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (!Visible) return;

        // Escape / D must always back-navigate, even before the 150 ms
        // auto-focus timer has set _ready. Blocking it on _ready let the
        // event fall through to the global PauseMenu.UnhandledInput, which
        // snapshot-opened the pause menu while splash was still the intended
        // target — a player (or a test) pressing Escape immediately after
        // opening LoadGame would land in PauseMenu instead of splash.
        if (@event is InputEventKey keyEvent && keyEvent.Pressed)
        {
            if (keyEvent.Keycode == Key.Escape || @event.IsActionPressed(Constants.InputActions.ActionTriangle))
            {
                EmitSignal(SignalName.BackPressed);
                GetViewport().SetInputAsHandled();
                return;
            }
        }

        if (!_ready) return;

        if (@event is InputEventKey keyEvent2 && keyEvent2.Pressed)
        {
            // Delete key triggers slot-delete on the currently focused populated slot.
            if (keyEvent2.Keycode == Key.Delete && _focusedSlot >= 0 && _slots[_focusedSlot].IsPopulated)
            {
                OpenDeleteDialog(_focusedSlot);
                GetViewport().SetInputAsHandled();
                return;
            }
        }

        if (@event.IsActionPressed("ui_left"))
        {
            // From buttons zone: prefer first populated slot (don't land on empty slot 0).
            if (_focusedSlot < 0) FocusFirstAvailable();
            else CycleSlotFocus(-1);
            GetViewport().SetInputAsHandled();
            return;
        }
        if (@event.IsActionPressed("ui_right"))
        {
            if (_focusedSlot < 0) FocusFirstAvailable();
            else CycleSlotFocus(1);
            GetViewport().SetInputAsHandled();
            return;
        }
        if (@event.IsActionPressed("ui_down"))
        {
            if (_focusedSlot >= 0) { _buttonZone = 0; MoveToButtonZone(); }
            else if (_loadBtn.HasFocus()) { _buttonZone = 1; _backBtn.GrabFocus(); }
            GetViewport().SetInputAsHandled();
            return;
        }
        if (@event.IsActionPressed("ui_up"))
        {
            if (_backBtn.HasFocus()) { _buttonZone = 0; _loadBtn.GrabFocus(); }
            else if (_loadBtn.HasFocus()) { FocusFirstAvailable(); }
            GetViewport().SetInputAsHandled();
            return;
        }

        if (KeyboardNav.HandleConfirm(@event, GetViewport()))
            GetViewport().SetInputAsHandled();
    }
}
