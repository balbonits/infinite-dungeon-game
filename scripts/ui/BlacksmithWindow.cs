using Godot;
using System.Linq;
using DungeonGame.Autoloads;

namespace DungeonGame.Ui;

/// <summary>
/// Blacksmith crafting UI. Apply affixes to equipment, recycle gear.
/// Two tabs: Craft (apply affixes) and Recycle (break down gear).
/// </summary>
public partial class BlacksmithWindow : GameWindow
{
    public static BlacksmithWindow Instance { get; private set; } = null!;

    private Label _goldLabel = null!;
    private Label _detailLabel = null!;
    private bool _isCraftMode = true;

    public override void _Ready()
    {
        Instance = this;
        ReturnToPauseMenu = false;
        base._Ready();
    }

    protected override void BuildContent(VBoxContainer content)
    {
        var title = new Label();
        title.Text = Strings.Blacksmith.Title;
        UiTheme.StyleLabel(title, UiTheme.Colors.Accent, UiTheme.FontSizes.Heading);
        title.HorizontalAlignment = HorizontalAlignment.Center;
        content.AddChild(title);

        _goldLabel = new Label();
        UiTheme.StyleLabel(_goldLabel, UiTheme.Colors.Accent, UiTheme.FontSizes.Body);
        _goldLabel.HorizontalAlignment = HorizontalAlignment.Center;
        content.AddChild(_goldLabel);

        // Tab buttons
        var tabRow = new HBoxContainer();
        tabRow.AddThemeConstantOverride("separation", 8);
        tabRow.Alignment = BoxContainer.AlignmentMode.Center;
        content.AddChild(tabRow);

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

        content.AddChild(new HSeparator());

        _detailLabel = new Label();
        UiTheme.StyleLabel(_detailLabel, UiTheme.Colors.Muted, UiTheme.FontSizes.Small);
        _detailLabel.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        _detailLabel.CustomMinimumSize = new Vector2(0, 30);
        content.AddChild(_detailLabel);

        content.AddChild(Scroll);

        var closeBtn = new Button();
        closeBtn.Text = Strings.Ui.Cancel;
        closeBtn.CustomMinimumSize = new Vector2(200, 38);
        closeBtn.SizeFlagsHorizontal = SizeFlags.ShrinkCenter;
        UiTheme.StyleSecondaryButton(closeBtn, UiTheme.FontSizes.Body);
        closeBtn.Connect(BaseButton.SignalName.Pressed, Callable.From(() => Close()));
        content.AddChild(closeBtn);
    }

    public void Open()
    {
        _isCraftMode = true;
        Show();
    }

    protected override void OnShow()
    {
        Refresh();
    }

    protected override bool HandleTabInput(InputEvent @event)
    {
        if (@event.IsActionPressed(Constants.InputActions.ShoulderLeft))
        {
            _isCraftMode = true;
            Refresh();
            return true;
        }
        if (@event.IsActionPressed(Constants.InputActions.ShoulderRight))
        {
            _isCraftMode = false;
            Refresh();
            return true;
        }
        return false;
    }

    private void Refresh()
    {
        foreach (Node child in ScrollContent.GetChildren())
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

        UiTheme.FocusFirstButton(ScrollContent);
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
            ScrollContent.AddChild(btn);
        }

        if (ScrollContent.GetChildCount() == 0)
            ScrollContent.AddChild(CreateEmptyLabel(Strings.Blacksmith.NoCraftable));
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
            ScrollContent.AddChild(btn);
        }

        if (ScrollContent.GetChildCount() == 0)
            ScrollContent.AddChild(CreateEmptyLabel(Strings.Blacksmith.NoRecyclable));
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
}
