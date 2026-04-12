using Godot;
using System.Linq;
using DungeonGame.Autoloads;

namespace DungeonGame.Ui;

/// <summary>
/// Blacksmith crafting UI. Apply affixes to equipment, recycle gear.
/// Two tabs: Craft (apply affixes) and Recycle (break down gear).
/// </summary>
public partial class BlacksmithWindow : Control
{
    public static BlacksmithWindow Instance { get; private set; } = null!;

    private ColorRect _overlay = null!;
    private CenterContainer _center = null!;
    private VBoxContainer _contentBox = null!;
    private Label _goldLabel = null!;
    private Label _detailLabel = null!;
    private ScrollContainer _scrollContainer = null!;
    private VBoxContainer _itemList = null!;
    private bool _isOpen;
    private bool _isCraftMode = true;

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
        _overlay.Color = new Color(0, 0, 0, 0.6f);
        _overlay.SetAnchorsPreset(LayoutPreset.FullRect);
        _overlay.MouseFilter = MouseFilterEnum.Stop;
        _overlay.Visible = false;
        AddChild(_overlay);

        _center = new CenterContainer();
        _center.SetAnchorsPreset(LayoutPreset.FullRect);
        _center.Visible = false;
        AddChild(_center);

        var panel = new PanelContainer();
        panel.AddThemeStyleboxOverride("panel", UiTheme.CreatePanelStyle(0.95f, true));
        panel.CustomMinimumSize = new Vector2(420, 0);
        _center.AddChild(panel);

        var margin = new MarginContainer();
        panel.AddChild(margin);

        _contentBox = new VBoxContainer();
        _contentBox.AddThemeConstantOverride("separation", 8);
        margin.AddChild(_contentBox);

        var title = new Label();
        title.Text = Strings.Blacksmith.Title;
        UiTheme.StyleLabel(title, UiTheme.Colors.Accent, UiTheme.FontSizes.Heading);
        title.HorizontalAlignment = HorizontalAlignment.Center;
        _contentBox.AddChild(title);

        _goldLabel = new Label();
        UiTheme.StyleLabel(_goldLabel, UiTheme.Colors.Accent, UiTheme.FontSizes.Body);
        _goldLabel.HorizontalAlignment = HorizontalAlignment.Center;
        _contentBox.AddChild(_goldLabel);

        // Tab buttons
        var tabRow = new HBoxContainer();
        tabRow.AddThemeConstantOverride("separation", 8);
        tabRow.Alignment = BoxContainer.AlignmentMode.Center;
        _contentBox.AddChild(tabRow);

        var craftTab = new Button();
        craftTab.Text = Strings.Blacksmith.CraftTab;
        craftTab.CustomMinimumSize = new Vector2(120, 34);
        UiTheme.StyleButton(craftTab, UiTheme.FontSizes.Body);
        craftTab.Connect(BaseButton.SignalName.Pressed, Callable.From(() => { _isCraftMode = true; Refresh(); }));
        tabRow.AddChild(craftTab);

        var recycleTab = new Button();
        recycleTab.Text = Strings.Blacksmith.RecycleTab;
        recycleTab.CustomMinimumSize = new Vector2(120, 34);
        UiTheme.StyleSecondaryButton(recycleTab, UiTheme.FontSizes.Body);
        recycleTab.Connect(BaseButton.SignalName.Pressed, Callable.From(() => { _isCraftMode = false; Refresh(); }));
        tabRow.AddChild(recycleTab);

        _contentBox.AddChild(new HSeparator());

        _detailLabel = new Label();
        UiTheme.StyleLabel(_detailLabel, UiTheme.Colors.Muted, UiTheme.FontSizes.Small);
        _detailLabel.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        _detailLabel.CustomMinimumSize = new Vector2(0, 30);
        _contentBox.AddChild(_detailLabel);

        _scrollContainer = new ScrollContainer();
        _scrollContainer.CustomMinimumSize = new Vector2(0, 300);
        _contentBox.AddChild(_scrollContainer);

        _itemList = new VBoxContainer();
        _itemList.AddThemeConstantOverride("separation", 4);
        _scrollContainer.AddChild(_itemList);

        var closeBtn = new Button();
        closeBtn.Text = Strings.Ui.Cancel;
        closeBtn.CustomMinimumSize = new Vector2(200, 38);
        closeBtn.SizeFlagsHorizontal = SizeFlags.ShrinkCenter;
        UiTheme.StyleSecondaryButton(closeBtn, UiTheme.FontSizes.Body);
        closeBtn.Connect(BaseButton.SignalName.Pressed, Callable.From(() => Close()));
        _contentBox.AddChild(closeBtn);
    }

    public void Open()
    {
        if (_isOpen) return;
        _isOpen = true;
        WindowStack.Push(this);
        _isCraftMode = true;
        GetTree().Paused = true;
        Refresh();
        _overlay.Visible = true;
        _center.Visible = true;
    }

    public void Close()
    {
        _isOpen = false;
        WindowStack.Pop(this);
        GetTree().Paused = false;
        _overlay.Visible = false;
        _center.Visible = false;
    }

    private void Refresh()
    {
        foreach (Node child in _itemList.GetChildren())
            child.QueueFree();

        var inv = GameState.Instance.PlayerInventory;
        _goldLabel.Text = Strings.Shop.GoldDisplay(inv.Gold);
        _detailLabel.Text = _isCraftMode
            ? Strings.Blacksmith.CraftHint
            : Strings.Blacksmith.RecycleHint;

        if (_isCraftMode)
            ShowCraftableItems(inv);
        else
            ShowRecyclableItems(inv);

        UiTheme.FocusFirstButton(_itemList);
    }

    private void ShowCraftableItems(Inventory inv)
    {
        for (int i = 0; i < inv.SlotCount; i++)
        {
            var stack = inv.GetSlot(i);
            if (stack == null) continue;
            if (stack.Item.Category != ItemCategory.Weapon &&
                stack.Item.Category != ItemCategory.Armor &&
                stack.Item.Category != ItemCategory.Accessory)
                continue;

            int maxTier = AffixDatabase.GetMaxTier(stack.Item.LevelRequirement);
            string label = $"{stack.Item.Name} (Lv.{stack.Item.LevelRequirement})  T{maxTier}";

            var btn = CreateItemButton(label, () =>
            {
                _detailLabel.Text = $"{stack.Item.Name}\n{stack.Item.Description}\nMax affix tier: {maxTier}";
            });
            _itemList.AddChild(btn);
        }

        if (_itemList.GetChildCount() == 0)
            _itemList.AddChild(CreateEmptyLabel(Strings.Blacksmith.NoCraftable));
    }

    private void ShowRecyclableItems(Inventory inv)
    {
        for (int i = 0; i < inv.SlotCount; i++)
        {
            var stack = inv.GetSlot(i);
            if (stack == null) continue;
            if (stack.Item.Category != ItemCategory.Weapon &&
                stack.Item.Category != ItemCategory.Armor &&
                stack.Item.Category != ItemCategory.Accessory)
                continue;

            int slotIndex = i;
            int recycleGold = 5 + stack.Item.LevelRequirement * 2;
            string label = $"{stack.Item.Name}    +{recycleGold}g";

            var btn = CreateItemButton(label, () =>
            {
                inv.RemoveAt(slotIndex);
                inv.Gold += recycleGold;
                Toast.Instance?.Success($"Recycled for {recycleGold}g");
                Refresh();
            });
            _itemList.AddChild(btn);
        }

        if (_itemList.GetChildCount() == 0)
            _itemList.AddChild(CreateEmptyLabel(Strings.Blacksmith.NoRecyclable));
    }

    private static Button CreateItemButton(string text, System.Action onPress)
    {
        var btn = new Button();
        btn.Text = $"  {text}";
        btn.Alignment = HorizontalAlignment.Left;
        btn.CustomMinimumSize = new Vector2(0, 32);
        btn.FocusMode = FocusModeEnum.All;

        var normal = new StyleBoxFlat();
        normal.BgColor = new Color(0, 0, 0, 0.01f);
        normal.SetCornerRadiusAll(4);
        normal.ContentMarginLeft = 8;
        btn.AddThemeStyleboxOverride("normal", normal);

        var hover = new StyleBoxFlat();
        hover.BgColor = new Color(UiTheme.Colors.Accent, 0.15f);
        hover.SetCornerRadiusAll(4);
        hover.ContentMarginLeft = 8;
        btn.AddThemeStyleboxOverride("hover", hover);

        var focus = new StyleBoxFlat();
        focus.BgColor = new Color(UiTheme.Colors.Accent, 0.25f);
        focus.SetCornerRadiusAll(4);
        focus.ContentMarginLeft = 8;
        btn.AddThemeStyleboxOverride("focus", focus);

        btn.AddThemeColorOverride("font_color", UiTheme.Colors.Ink);
        btn.AddThemeFontSizeOverride("font_size", UiTheme.FontSizes.Body);
        btn.Connect(BaseButton.SignalName.Pressed, Callable.From(onPress));
        return btn;
    }

    private static Label CreateEmptyLabel(string text)
    {
        var label = new Label();
        label.Text = text;
        UiTheme.StyleLabel(label, UiTheme.Colors.Muted, UiTheme.FontSizes.Body);
        return label;
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (!_isOpen) return;

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

        // Q/E switch Craft/Recycle tabs
        if (@event.IsActionPressed(Constants.InputActions.ShoulderLeft))
        {
            _isCraftMode = true;
            Refresh();
            GetViewport().SetInputAsHandled();
            return;
        }
        if (@event.IsActionPressed(Constants.InputActions.ShoulderRight))
        {
            _isCraftMode = false;
            Refresh();
            GetViewport().SetInputAsHandled();
            return;
        }

        if (@event is InputEventKey k && k.Pressed)
            GetViewport().SetInputAsHandled();
    }
}
