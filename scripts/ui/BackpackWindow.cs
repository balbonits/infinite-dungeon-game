using Godot;
using DungeonGame.Autoloads;

namespace DungeonGame.Ui;

/// <summary>
/// Backpack inventory window. Shows all carried items in a grid/list.
/// Items can be used (consumables), viewed (detail), or assigned via action menu.
/// Accessible from pause menu — available anywhere (dungeon or town).
/// Spec: docs/inventory/backpack.md
/// </summary>
public partial class BackpackWindow : Control
{
    public static BackpackWindow? Instance { get; private set; }

    private ColorRect _overlay = null!;
    private Label _headerLabel = null!;
    private Label _detailLabel = null!;
    private ScrollContainer _scrollContainer = null!;
    private VBoxContainer _itemList = null!;
    private bool _isOpen;

    public bool IsOpen => _isOpen;

    public override void _Ready()
    {
        Instance = this;
        ProcessMode = ProcessModeEnum.Always;
        MouseFilter = MouseFilterEnum.Ignore;
        BuildUi();
    }

    private void BuildUi()
    {
        var (overlay, content) = UiTheme.CreateDialogWindow(400f);
        _overlay = overlay;
        _overlay.Visible = false;
        AddChild(_overlay);

        // Header
        _headerLabel = new Label();
        UiTheme.StyleLabel(_headerLabel, UiTheme.Colors.Accent, UiTheme.FontSizes.Heading);
        _headerLabel.HorizontalAlignment = HorizontalAlignment.Center;
        content.AddChild(_headerLabel);

        // Gold display
        var goldLabel = new Label();
        goldLabel.Text = ""; // updated on refresh
        UiTheme.StyleLabel(goldLabel, UiTheme.Colors.Accent, UiTheme.FontSizes.Body);
        goldLabel.HorizontalAlignment = HorizontalAlignment.Center;
        content.AddChild(goldLabel);
        // Store reference via name for refresh
        goldLabel.Name = "GoldLabel";

        var hint = new Label();
        hint.Text = "S: action | D: close";
        UiTheme.StyleLabel(hint, UiTheme.Colors.Muted, UiTheme.FontSizes.Small);
        hint.HorizontalAlignment = HorizontalAlignment.Center;
        content.AddChild(hint);

        content.AddChild(new HSeparator());

        // Detail area
        _detailLabel = new Label();
        UiTheme.StyleLabel(_detailLabel, UiTheme.Colors.Muted, UiTheme.FontSizes.Small);
        _detailLabel.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        _detailLabel.CustomMinimumSize = new Vector2(0, 36);
        content.AddChild(_detailLabel);

        // Scrollable slot grid
        _scrollContainer = new ScrollContainer();
        _scrollContainer.CustomMinimumSize = new Vector2(0, 320);
        content.AddChild(_scrollContainer);

        _itemList = new VBoxContainer();
        _itemList.AddThemeConstantOverride("separation", 4);
        _scrollContainer.AddChild(_itemList);

        // Close button
        var closeBtn = new Button();
        closeBtn.Text = "Close";
        closeBtn.CustomMinimumSize = new Vector2(200, 38);
        closeBtn.SizeFlagsHorizontal = SizeFlags.ShrinkCenter;
        closeBtn.FocusMode = FocusModeEnum.None; // not in nav — D/Esc closes, this is mouse-only
        UiTheme.StyleSecondaryButton(closeBtn, UiTheme.FontSizes.Body);
        closeBtn.Connect(BaseButton.SignalName.Pressed, Callable.From(Close));
        content.AddChild(closeBtn);
    }

    public void Open()
    {
        if (_isOpen) return;
        _isOpen = true;
        GetTree().Paused = true;
        Refresh();
        _overlay.Visible = true;
    }

    public void Close()
    {
        _isOpen = false;
        _overlay.Visible = false;
        var pauseMenu = GetNodeOrNull<Control>("../PauseMenu");
        if (pauseMenu != null)
        {
            pauseMenu.Visible = true;
            UiTheme.FocusFirstButton(pauseMenu.GetNode<VBoxContainer>("CenterContainer/PanelContainer/MarginContainer/VBoxContainer"));
        }
        else
        {
            GetTree().Paused = false;
        }
    }

    private void Refresh()
    {
        var inv = GameState.Instance.PlayerInventory;
        _headerLabel.Text = $"BACKPACK ({inv.UsedSlots}/{inv.SlotCount})";

        var goldLabel = _overlay.GetNodeOrNull<Label>("CenterContainer/PanelContainer/VBoxContainer/GoldLabel");
        if (goldLabel != null)
            goldLabel.Text = $"Gold: {inv.Gold}";

        _detailLabel.Text = "Select an item";

        foreach (Node child in _itemList.GetChildren())
            child.QueueFree();

        // Grid of square slot boxes — 5 columns
        const int columns = 5;
        const float slotSize = 64;

        var grid = new GridContainer();
        grid.Columns = columns;
        grid.AddThemeConstantOverride("h_separation", 6);
        grid.AddThemeConstantOverride("v_separation", 6);
        grid.SizeFlagsHorizontal = SizeFlags.ShrinkCenter;
        _itemList.AddChild(grid);

        for (int i = 0; i < inv.SlotCount; i++)
        {

            var stack = inv.GetSlot(i);
            int slotIdx = i;

            var slotBtn = new Button();
            slotBtn.CustomMinimumSize = new Vector2(slotSize, slotSize);
            slotBtn.SizeFlagsHorizontal = SizeFlags.ShrinkCenter;
            slotBtn.SizeFlagsVertical = SizeFlags.ShrinkCenter;
            slotBtn.FocusMode = FocusModeEnum.All;

            if (stack != null)
            {
                string countStr = stack.Count > 1 ? $"\n x{stack.Count}" : "";
                slotBtn.Text = stack.Item.Name.Length > 6
                    ? stack.Item.Name[..6] + countStr
                    : stack.Item.Name + countStr;

                Color slotColor = stack.Item.Category switch
                {
                    ItemCategory.Consumable => new Color(0.15f, 0.35f, 0.15f, 0.9f),
                    ItemCategory.Material => new Color(0.15f, 0.25f, 0.35f, 0.9f),
                    ItemCategory.Weapon => new Color(0.35f, 0.20f, 0.15f, 0.9f),
                    ItemCategory.Armor => new Color(0.20f, 0.20f, 0.30f, 0.9f),
                    _ => new Color(0.15f, 0.15f, 0.20f, 0.9f),
                };
                slotBtn.AddThemeStyleboxOverride("normal", CreateSlotBox(slotColor, false));
                slotBtn.AddThemeStyleboxOverride("hover", CreateSlotBox(slotColor, true));
                slotBtn.AddThemeStyleboxOverride("focus", CreateSlotBox(slotColor, true));
                slotBtn.AddThemeColorOverride("font_color", UiTheme.Colors.Ink);
                slotBtn.AddThemeColorOverride("font_hover_color", UiTheme.Colors.Ink);
                slotBtn.AddThemeColorOverride("font_focus_color", UiTheme.Colors.Ink);
                slotBtn.AddThemeFontSizeOverride("font_size", 9);

                var itemDef = stack.Item;
                slotBtn.Connect(BaseButton.SignalName.Pressed, Callable.From(() =>
                    ShowItemActions(slotIdx, itemDef, slotBtn.GlobalPosition)));

                string detail = $"{stack.Item.Name}\n{stack.Item.Description}";
                if (stack.Item.HealAmount > 0) detail += $"\nHeals: {stack.Item.HealAmount} HP";
                if (stack.Item.ManaAmount > 0) detail += $"\nRestores: {stack.Item.ManaAmount} MP";
                if (stack.Item.SellPrice > 0) detail += $"\nSell: {stack.Item.SellPrice}g";
                slotBtn.FocusEntered += () => _detailLabel.Text = detail;
            }
            else
            {
                slotBtn.Text = "";
                slotBtn.AddThemeStyleboxOverride("normal", CreateSlotBox(new Color(0.08f, 0.08f, 0.12f, 0.6f), false));
                slotBtn.AddThemeStyleboxOverride("hover", CreateSlotBox(new Color(0.08f, 0.08f, 0.12f, 0.6f), false));
                slotBtn.AddThemeStyleboxOverride("focus", CreateSlotBox(new Color(0.08f, 0.08f, 0.12f, 0.6f), false));
                slotBtn.Disabled = true;
            }

            grid.AddChild(slotBtn);
        }

        _scrollContainer.ScrollVertical = 0;
        CallDeferred(MethodName.FocusFirst);
    }

    private void FocusFirst()
    {
        UiTheme.FocusFirstButton(_itemList);
    }

    private static StyleBoxFlat CreateSlotBox(Color bgColor, bool focused) =>
        UiTheme.CreateSlotStyle(bgColor, focused);

    private void ShowItemActions(int slotIdx, ItemDef item, Vector2 position)
    {
        var actions = new System.Collections.Generic.List<(string label, System.Action action)>();

        // Use (consumables only)
        if (item.Category == ItemCategory.Consumable)
        {
            actions.Add(("Use", () =>
            {
                var gs = GameState.Instance;
                if (item.HealAmount > 0)
                    gs.Hp = System.Math.Min(gs.MaxHp, gs.Hp + item.HealAmount);
                if (item.ManaAmount > 0)
                    gs.Mana = System.Math.Min(gs.MaxMana, gs.Mana + item.ManaAmount);
                gs.PlayerInventory.RemoveAt(slotIdx);
                Toast.Instance?.Success($"Used {item.Name}");
                Refresh();
            }
            ));
        }

        // Drop
        actions.Add(("Drop", () =>
        {
            GameState.Instance.PlayerInventory.RemoveAt(slotIdx);
            Toast.Instance?.Info($"Dropped {item.Name}");
            Refresh();
        }
        ));

        ActionMenu.Instance?.Show(position, actions.ToArray());
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (!_isOpen) return;
        if (ActionMenu.Instance?.IsOpen == true) return;

        if (KeyboardNav.IsCancelPressed(@event))
        {
            Close();
            GetViewport().SetInputAsHandled();
            return;
        }

        if (KeyboardNav.HandleInput(@event, _itemList))
        {
            GetViewport().SetInputAsHandled();
            return;
        }

        if (@event is InputEventKey k && k.Pressed)
            GetViewport().SetInputAsHandled();
    }
}
