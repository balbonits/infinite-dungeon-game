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
        _isCraftMode = true;
        GetTree().Paused = true;
        Refresh();
        _overlay.Visible = true;
        _center.Visible = true;
    }

    public void Close()
    {
        _isOpen = false;
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
    }

    private void ShowCraftableItems(Inventory inv)
    {
        // List all equipment items in backpack that can receive affixes
        for (int i = 0; i < inv.SlotCount; i++)
        {
            var stack = inv.GetSlot(i);
            if (stack == null) continue;
            if (stack.Item.Category != ItemCategory.Weapon &&
                stack.Item.Category != ItemCategory.Armor &&
                stack.Item.Category != ItemCategory.Accessory)
                continue;

            string label = $"{stack.Item.Name} (Lv.{stack.Item.LevelRequirement})";
            var row = new HBoxContainer();
            row.AddThemeConstantOverride("separation", 6);

            var nameLabel = new Label();
            nameLabel.Text = label;
            UiTheme.StyleLabel(nameLabel, UiTheme.Colors.Ink, UiTheme.FontSizes.Body);
            nameLabel.SizeFlagsHorizontal = SizeFlags.ExpandFill;
            row.AddChild(nameLabel);

            // Show available affix count
            int maxTier = AffixDatabase.GetMaxTier(stack.Item.LevelRequirement);
            var tierLabel = new Label();
            tierLabel.Text = $"T{maxTier}";
            UiTheme.StyleLabel(tierLabel, UiTheme.Colors.Muted, UiTheme.FontSizes.Small);
            row.AddChild(tierLabel);

            _itemList.AddChild(row);
        }

        if (_itemList.GetChildCount() == 0)
        {
            var emptyLabel = new Label();
            emptyLabel.Text = Strings.Blacksmith.NoCraftable;
            UiTheme.StyleLabel(emptyLabel, UiTheme.Colors.Muted, UiTheme.FontSizes.Body);
            _itemList.AddChild(emptyLabel);
        }
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

            var row = new HBoxContainer();
            row.AddThemeConstantOverride("separation", 6);

            var nameLabel = new Label();
            nameLabel.Text = stack.Item.Name;
            UiTheme.StyleLabel(nameLabel, UiTheme.Colors.Ink, UiTheme.FontSizes.Body);
            nameLabel.SizeFlagsHorizontal = SizeFlags.ExpandFill;
            row.AddChild(nameLabel);

            var goldLabel = new Label();
            goldLabel.Text = $"+{recycleGold}g";
            UiTheme.StyleLabel(goldLabel, UiTheme.Colors.Accent, UiTheme.FontSizes.Small);
            row.AddChild(goldLabel);

            var recycleBtn = new Button();
            recycleBtn.Text = Strings.Blacksmith.Recycle;
            recycleBtn.CustomMinimumSize = new Vector2(80, 28);
            UiTheme.StyleButton(recycleBtn, UiTheme.FontSizes.Small);
            recycleBtn.Connect(BaseButton.SignalName.Pressed, Callable.From(() =>
            {
                inv.RemoveAt(slotIndex);
                inv.Gold += recycleGold;
                Toast.Instance?.Success($"Recycled for {recycleGold}g");
                Refresh();
            }));
            row.AddChild(recycleBtn);

            _itemList.AddChild(row);
        }

        if (_itemList.GetChildCount() == 0)
        {
            var emptyLabel = new Label();
            emptyLabel.Text = Strings.Blacksmith.NoRecyclable;
            UiTheme.StyleLabel(emptyLabel, UiTheme.Colors.Muted, UiTheme.FontSizes.Body);
            _itemList.AddChild(emptyLabel);
        }
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (!_isOpen) return;

        if (@event is InputEventKey keyEvent && keyEvent.Pressed && keyEvent.Keycode == Key.Escape)
        {
            Close();
            GetViewport().SetInputAsHandled();
        }
    }
}
