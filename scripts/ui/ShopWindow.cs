using Godot;
using System.Collections.Generic;
using DungeonGame.Autoloads;

namespace DungeonGame.Ui;

/// <summary>
/// JRPG-style shop window. Two-panel layout: item list (left) + description (right).
/// Buy tab shows shop inventory, Sell tab shows player backpack.
/// </summary>
public partial class ShopWindow : Control
{
    public static ShopWindow Instance { get; private set; } = null!;

    private ColorRect _overlay = null!;
    private CenterContainer _center = null!;
    private Label _goldLabel = null!;
    private VBoxContainer _itemList = null!;
    private Label _descName = null!;
    private Label _descText = null!;
    private Label _descStats = null!;
    private Button _actionButton = null!;
    private Label _tabIndicator = null!;

    private List<ItemDef> _shopItems = new();
    private ItemDef? _selectedItem;
    private int _selectedIndex = -1;
    private bool _isBuyMode = true;
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
        _overlay = new ColorRect();
        _overlay.Color = new Color(0, 0, 0, 0.5f);
        _overlay.SetAnchorsPreset(LayoutPreset.FullRect);
        _overlay.MouseFilter = MouseFilterEnum.Stop;
        _overlay.Visible = false;
        AddChild(_overlay);

        _center = new CenterContainer();
        _center.SetAnchorsPreset(LayoutPreset.FullRect);
        _center.Visible = false;
        AddChild(_center);

        var mainPanel = new PanelContainer();
        mainPanel.AddThemeStyleboxOverride("panel", UiTheme.CreatePanelStyle(0.95f, true));
        mainPanel.CustomMinimumSize = new Vector2(600, 400);
        _center.AddChild(mainPanel);

        var margin = new MarginContainer();
        mainPanel.AddChild(margin);

        var outerVbox = new VBoxContainer();
        outerVbox.AddThemeConstantOverride("separation", 8);
        margin.AddChild(outerVbox);

        // Header: title + gold
        var header = new HBoxContainer();
        outerVbox.AddChild(header);

        var shopTitle = new Label();
        shopTitle.Text = Strings.Shop.Title;
        shopTitle.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        UiTheme.StyleLabel(shopTitle, UiTheme.Colors.Accent, UiTheme.FontSizes.Heading);
        header.AddChild(shopTitle);

        _goldLabel = new Label();
        UiTheme.StyleLabel(_goldLabel, UiTheme.Colors.Accent, UiTheme.FontSizes.Button);
        header.AddChild(_goldLabel);

        outerVbox.AddChild(new HSeparator());

        // Two-panel layout (Buy/Sell buttons moved to right panel)
        var panels = new HBoxContainer();
        panels.AddThemeConstantOverride("separation", 12);
        panels.SizeFlagsVertical = SizeFlags.ExpandFill;
        outerVbox.AddChild(panels);

        // Left: item list (scrollable)
        var leftPanel = new PanelContainer();
        leftPanel.AddThemeStyleboxOverride("panel", UiTheme.CreatePanelStyle(0.5f));
        leftPanel.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        leftPanel.SizeFlagsStretchRatio = 1.2f;
        panels.AddChild(leftPanel);

        var scroll = new ScrollContainer { FollowFocus = true };
        scroll.SizeFlagsVertical = SizeFlags.ExpandFill;
        leftPanel.AddChild(scroll);

        _itemList = new VBoxContainer();
        _itemList.AddThemeConstantOverride("separation", 2);
        _itemList.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        scroll.AddChild(_itemList);

        // Right: description panel (fixed height to prevent resizing)
        var rightPanel = new PanelContainer();
        rightPanel.AddThemeStyleboxOverride("panel", UiTheme.CreatePanelStyle(0.5f));
        rightPanel.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        rightPanel.SizeFlagsVertical = SizeFlags.ShrinkBegin;
        rightPanel.CustomMinimumSize = new Vector2(0, 260);
        panels.AddChild(rightPanel);

        var rightMargin = new MarginContainer();
        rightPanel.AddChild(rightMargin);

        var descVbox = new VBoxContainer();
        descVbox.AddThemeConstantOverride("separation", 8);
        rightMargin.AddChild(descVbox);

        _descName = new Label();
        UiTheme.StyleLabel(_descName, UiTheme.Colors.Accent, UiTheme.FontSizes.Button);
        descVbox.AddChild(_descName);

        descVbox.AddChild(new HSeparator());

        _descText = new Label();
        UiTheme.StyleLabel(_descText, UiTheme.Colors.Ink, UiTheme.FontSizes.Body);
        _descText.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        descVbox.AddChild(_descText);

        _descStats = new Label();
        UiTheme.StyleLabel(_descStats, UiTheme.Colors.Safe, UiTheme.FontSizes.Small);
        descVbox.AddChild(_descStats);

        var spacer = new Control();
        spacer.SizeFlagsVertical = SizeFlags.ExpandFill;
        descVbox.AddChild(spacer);

        _actionButton = new Button();
        _actionButton.CustomMinimumSize = new Vector2(140, 36);
        _actionButton.SizeFlagsHorizontal = SizeFlags.ShrinkCenter;
        UiTheme.StyleButton(_actionButton, UiTheme.FontSizes.Body);
        _actionButton.Connect(BaseButton.SignalName.Pressed, Callable.From(OnActionPressed));
        _actionButton.Visible = false;
        descVbox.AddChild(_actionButton);

        descVbox.AddChild(new HSeparator());

        // Buy/Sell toggle buttons
        var modeRow = new HBoxContainer();
        modeRow.AddThemeConstantOverride("separation", 8);
        modeRow.Alignment = BoxContainer.AlignmentMode.Center;
        descVbox.AddChild(modeRow);

        var buyTab = new Button();
        buyTab.Text = Strings.Shop.BuyTab;
        buyTab.CustomMinimumSize = new Vector2(80, 28);
        UiTheme.StyleButton(buyTab, UiTheme.FontSizes.Small);
        buyTab.Connect(BaseButton.SignalName.Pressed, Callable.From(() => SetMode(true)));
        modeRow.AddChild(buyTab);

        var sellTab = new Button();
        sellTab.Text = Strings.Shop.SellTab;
        sellTab.CustomMinimumSize = new Vector2(80, 28);
        UiTheme.StyleButton(sellTab, UiTheme.FontSizes.Small);
        sellTab.Connect(BaseButton.SignalName.Pressed, Callable.From(() => SetMode(false)));
        modeRow.AddChild(sellTab);

        _tabIndicator = new Label();
        UiTheme.StyleLabel(_tabIndicator, UiTheme.Colors.Muted, UiTheme.FontSizes.Small);
        _tabIndicator.HorizontalAlignment = HorizontalAlignment.Center;
        descVbox.AddChild(_tabIndicator);

        // Close button
        outerVbox.AddChild(new HSeparator());
        var closeBtn = new Button();
        closeBtn.Text = Strings.Shop.Close;
        closeBtn.CustomMinimumSize = new Vector2(120, 36);
        closeBtn.SizeFlagsHorizontal = SizeFlags.ShrinkCenter;
        UiTheme.StyleButton(closeBtn, UiTheme.FontSizes.Body);
        closeBtn.Connect(BaseButton.SignalName.Pressed, Callable.From(Close));
        outerVbox.AddChild(closeBtn);
    }

    public void Open(List<ItemDef> shopInventory)
    {
        _shopItems = shopInventory;
        _isOpen = true;
        WindowStack.Push(this);
        _isBuyMode = true;
        GetTree().Paused = true;
        _overlay.Visible = true;
        _center.Visible = true;
        RefreshList();
        UiTheme.FocusFirstButton(_itemList);
    }

    public void Close()
    {
        _isOpen = false;
        WindowStack.Pop(this);
        GetTree().Paused = false;
        _overlay.Visible = false;
        _center.Visible = false;
    }

    private void SetMode(bool buyMode)
    {
        _isBuyMode = buyMode;
        _selectedItem = null;
        _selectedIndex = -1;
        RefreshList();
        UiTheme.FocusFirstButton(_itemList);
    }

    private void RefreshList()
    {
        foreach (Node child in _itemList.GetChildren())
            child.QueueFree();

        UpdateGold();
        _tabIndicator.Text = _isBuyMode ? Strings.Shop.BuyMode : Strings.Shop.SellMode;
        _descName.Text = "";
        _descText.Text = Strings.Shop.SelectItem;
        _descStats.Text = "";
        _actionButton.Visible = false;

        if (_isBuyMode)
        {
            for (int i = 0; i < _shopItems.Count; i++)
                AddItemRow(_shopItems[i], i, $"{_shopItems[i].BuyPrice}g");
        }
        else
        {
            var inventory = GameState.Instance.PlayerInventory;
            for (int i = 0; i < inventory.SlotCount; i++)
            {
                var stack = inventory.GetSlot(i);
                if (stack == null) continue;
                AddItemRow(stack.Item, i, $"{stack.Item.SellPrice}g");
            }
        }
    }

    private void AddItemRow(ItemDef item, int index, string price)
    {
        var row = new Button();
        row.Text = $"  {item.Name}    {price}";
        row.Alignment = HorizontalAlignment.Left;
        row.CustomMinimumSize = new Vector2(0, 28);
        row.FocusMode = FocusModeEnum.All;

        var normal = new StyleBoxFlat();
        normal.BgColor = new Color(0, 0, 0, 0.01f);
        normal.SetCornerRadiusAll(4);
        normal.ContentMarginLeft = 8;
        row.AddThemeStyleboxOverride("normal", normal);

        var hover = new StyleBoxFlat();
        hover.BgColor = new Color(UiTheme.Colors.Accent, 0.15f);
        hover.SetCornerRadiusAll(4);
        hover.ContentMarginLeft = 8;
        row.AddThemeStyleboxOverride("hover", hover);

        // FF-style: focused row is clearly highlighted (cursor)
        var focus = new StyleBoxFlat();
        focus.BgColor = new Color(UiTheme.Colors.Accent, 0.25f);
        focus.SetCornerRadiusAll(4);
        focus.ContentMarginLeft = 8;
        row.AddThemeStyleboxOverride("focus", focus);

        row.AddThemeColorOverride("font_color", UiTheme.Colors.Ink);
        row.AddThemeFontSizeOverride("font_size", UiTheme.FontSizes.Body);

        // FF-style: description updates on FOCUS (cursor move), not on press
        int capturedIndex = index;
        ItemDef capturedItem = item;
        row.FocusEntered += () => UpdateDescription(capturedItem, capturedIndex);

        // Press (S/cross or click) = confirm buy/sell
        row.Connect(BaseButton.SignalName.Pressed, Callable.From(() =>
        {
            UpdateDescription(capturedItem, capturedIndex);
            OnActionPressed();
        }));

        _itemList.AddChild(row);
    }

    private void UpdateDescription(ItemDef item, int index)
    {
        _selectedItem = item;
        _selectedIndex = index;
        _descName.Text = item.Name;
        _descText.Text = item.Description;

        var stats = new System.Text.StringBuilder();
        if (item.BonusDamage > 0) stats.AppendLine($"Damage: +{item.BonusDamage}");
        if (item.BonusStr > 0) stats.AppendLine($"STR: +{item.BonusStr}");
        if (item.BonusDex > 0) stats.AppendLine($"DEX: +{item.BonusDex}");
        if (item.BonusSta > 0) stats.AppendLine($"STA: +{item.BonusSta}");
        if (item.BonusInt > 0) stats.AppendLine($"INT: +{item.BonusInt}");
        if (item.HealAmount > 0) stats.AppendLine($"Heals: {item.HealAmount} HP");
        if (item.ProjectileDamageMultiplier > 1) stats.AppendLine($"Damage: x{item.ProjectileDamageMultiplier:F1}");
        _descStats.Text = stats.ToString();

        _actionButton.Visible = true;
        _actionButton.Text = _isBuyMode
            ? Strings.Shop.Buy(item.BuyPrice)
            : Strings.Shop.Sell(item.SellPrice);
    }

    private void OnActionPressed()
    {
        if (_selectedItem == null) return;

        var inventory = GameState.Instance.PlayerInventory;

        if (_isBuyMode)
        {
            if (inventory.TryBuy(_selectedItem))
            {
                Toast.Instance.Success($"Bought {_selectedItem.Name}");
                UpdateGold();
            }
            else
            {
                Toast.Instance.Error(Strings.Shop.CannotAfford);
            }
        }
        else
        {
            if (inventory.TrySell(_selectedIndex))
            {
                Toast.Instance.Success($"Sold {_selectedItem.Name}");
                _selectedItem = null;
                RefreshList();
            }
        }
    }

    private void UpdateGold()
    {
        _goldLabel.Text = Strings.Shop.GoldDisplay(GameState.Instance.PlayerInventory.Gold);
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (!_isOpen) return;

        // FF-style: D (circle) or Esc = cancel/close
        if (KeyboardNav.IsCancelPressed(@event))
        {
            Close();
            GetViewport().SetInputAsHandled();
            return;
        }

        // Up/Down navigate the item list (cursor), description follows via FocusEntered
        if (KeyboardNav.HandleInput(@event, _itemList))
        {
            GetViewport().SetInputAsHandled();
            return;
        }

        // Q/E (shoulder) switch Buy/Sell tabs
        if (@event.IsActionPressed(Constants.InputActions.ShoulderLeft))
        {
            SetMode(true);
            GetViewport().SetInputAsHandled();
            return;
        }
        if (@event.IsActionPressed(Constants.InputActions.ShoulderRight))
        {
            SetMode(false);
            GetViewport().SetInputAsHandled();
            return;
        }

        // Block ALL input when open — nothing passes through to game or panels behind
        if (@event is InputEventKey k && k.Pressed)
            GetViewport().SetInputAsHandled();
    }
}
