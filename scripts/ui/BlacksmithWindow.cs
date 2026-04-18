using Godot;
using System;
using System.Collections.Generic;
using DungeonGame.Autoloads;

namespace DungeonGame.Ui;

/// <summary>
/// Blacksmith crafting UI. Four tabs per SPEC-BLACKSMITH-MERGED-MENU-01:
/// Forge (apply affixes), Craft (recipe-based, coming soon), Recycle
/// (break down gear), Shop (caravan-stocked materials + consumables).
/// </summary>
public partial class BlacksmithWindow : GameWindow
{
    public static BlacksmithWindow Instance { get; private set; } = null!;

    private const int ForgeTabIndex = 0;
    private const int CraftTabIndex = 1;
    private const int RecycleTabIndex = 2;
    private const int ShopTabIndex = 3;

    private GameTabPanel _tabs = null!;
    private Label _goldFooter = null!;
    // Default to Bank — matches the prior Guild Store's default and the
    // button text. (Copilot R1 on PR #22: field wasn't initialized, so the
    // C# default `false` silently changed behavior from the port.)
    private bool _buySendToBank = true;

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

        _tabs = new GameTabPanel();
        _tabs.AddTab(Strings.Blacksmith.ForgeTab, BuildForgeTab);
        _tabs.AddTab(Strings.Blacksmith.CraftTab, BuildCraftTab);
        _tabs.AddTab(Strings.Blacksmith.RecycleTab, BuildRecycleTab);
        _tabs.AddTab(Strings.Blacksmith.ShopTab, BuildShopTab);
        content.AddChild(_tabs);

        _goldFooter = new Label();
        UiTheme.StyleLabel(_goldFooter, UiTheme.Colors.Accent, UiTheme.FontSizes.Small);
        _goldFooter.HorizontalAlignment = HorizontalAlignment.Center;
        content.AddChild(_goldFooter);
    }

    public void Open()
    {
        Show();
    }

    protected override void OnShow()
    {
        _tabs.SelectTab(ForgeTabIndex); // default to Forge per spec
        UpdateGoldFooter();
    }

    protected override bool HandleTabInput(InputEvent @event) => _tabs.HandleInput(@event);

    private void UpdateGoldFooter()
    {
        // Show both pools because Shop-tab purchases spend backpack-first
        // then bank (see DoBuy). Displaying only backpack would hide bank
        // spends and make a successful buy look like nothing happened.
        // (Copilot R1 on PR #22: footer showed only backpack gold despite
        // combined spending.)
        var gs = GameState.Instance;
        _goldFooter.Text = $"Bank: {NumberFormat.Abbrev(gs.PlayerBank.Gold)}g     " +
                           $"Backpack: {NumberFormat.Abbrev(gs.PlayerInventory.Gold)}g";
    }

    // ──────────────────────── FORGE TAB ────────────────────────

    private void BuildForgeTab()
    {
        var scroll = _tabs.ScrollContent;

        var hint = new Label();
        hint.Text = Strings.Blacksmith.ForgeHint;
        UiTheme.StyleLabel(hint, UiTheme.Colors.Muted, UiTheme.FontSizes.Small);
        hint.HorizontalAlignment = HorizontalAlignment.Center;
        hint.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        scroll.AddChild(hint);

        var inv = GameState.Instance.PlayerInventory;
        bool any = false;
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
            // Forge buttons are disabled until the affix-apply dialog ships
            // — visible so the player can see what's upcoming, but no-op
            // clicks would give confusing feedback. (Copilot R1 on PR #22.)
            var btn = CreateItemButton(label, () => { });
            btn.Disabled = true;
            btn.TooltipText = "Affix forging coming soon.";
            scroll.AddChild(btn);
            any = true;
        }

        if (!any)
            scroll.AddChild(CreateEmptyLabel(Strings.Blacksmith.NoForgeable));
    }

    // ──────────────────────── CRAFT TAB (placeholder) ────────────────────────

    private void BuildCraftTab()
    {
        var scroll = _tabs.ScrollContent;

        var hint = new Label();
        hint.Text = Strings.Blacksmith.CraftHint;
        UiTheme.StyleLabel(hint, UiTheme.Colors.Muted, UiTheme.FontSizes.Small);
        hint.HorizontalAlignment = HorizontalAlignment.Center;
        hint.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        scroll.AddChild(hint);

        var placeholder = new Label();
        placeholder.Text = "Material-to-item recipes will appear here once the crafting system lands.";
        UiTheme.StyleLabel(placeholder, UiTheme.Colors.Muted, UiTheme.FontSizes.Small);
        placeholder.HorizontalAlignment = HorizontalAlignment.Center;
        placeholder.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        scroll.AddChild(placeholder);
    }

    // ──────────────────────── RECYCLE TAB ────────────────────────

    private void BuildRecycleTab()
    {
        var scroll = _tabs.ScrollContent;

        var hint = new Label();
        hint.Text = Strings.Blacksmith.RecycleHint;
        UiTheme.StyleLabel(hint, UiTheme.Colors.Muted, UiTheme.FontSizes.Small);
        hint.HorizontalAlignment = HorizontalAlignment.Center;
        hint.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        scroll.AddChild(hint);

        var inv = GameState.Instance.PlayerInventory;
        bool any = false;
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

            scroll.AddChild(CreateItemButton(label, () =>
            {
                inv.RemoveAt(slotIndex);
                inv.Gold += recycleGold;
                Toast.Instance?.Success($"Recycled for {recycleGold}g");
                _tabs.SelectTab(RecycleTabIndex); // rebuild list
                UpdateGoldFooter();
            }));
            any = true;
        }

        if (!any)
            scroll.AddChild(CreateEmptyLabel(Strings.Blacksmith.NoRecyclable));
    }

    // ──────────────────────── SHOP TAB ────────────────────────

    private void BuildShopTab()
    {
        var scroll = _tabs.ScrollContent;

        var hint = new Label();
        hint.Text = Strings.Blacksmith.ShopHint;
        UiTheme.StyleLabel(hint, UiTheme.Colors.Muted, UiTheme.FontSizes.Small);
        hint.HorizontalAlignment = HorizontalAlignment.Center;
        hint.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        scroll.AddChild(hint);

        // Caravan stock: everything in ItemDatabase with BuyPrice > 0 and Consumable
        // category. Full `BlacksmithShopStock` tag is a content ticket — for now we
        // surface consumables, matching the previous Guild Store MVP.
        bool any = false;
        foreach (var item in ItemDatabase.All)
        {
            if (item.Category != ItemCategory.Consumable || item.BuyPrice <= 0) continue;

            var btn = new Button();
            btn.Text = $"{item.Name}  —  {item.BuyPrice}g";
            btn.CustomMinimumSize = new Vector2(0, 32);
            btn.FocusMode = FocusModeEnum.All;
            UiTheme.StyleListItemButton(btn);
            var captured = item;
            btn.Connect(BaseButton.SignalName.Pressed,
                Callable.From(() => ShowBuyDialog(captured)));
            scroll.AddChild(btn);
            any = true;
        }

        if (!any)
            scroll.AddChild(CreateEmptyLabel(Strings.Blacksmith.NoShopStock));
    }

    private void ShowBuyDialog(ItemDef item)
    {
        var actions = new List<(string, Action)>
        {
            ($"Buy 1 ({item.BuyPrice}g) → {(_buySendToBank ? "Bank" : "Backpack")}",
                () => DoBuy(item, 1)),
            ($"Buy 10 ({item.BuyPrice * 10}g) → {(_buySendToBank ? "Bank" : "Backpack")}",
                () => DoBuy(item, 10)),
            ($"Toggle target: now {(_buySendToBank ? "Bank (→switch to Backpack)" : "Backpack (→switch to Bank)")}",
                () => { _buySendToBank = !_buySendToBank; ShowBuyDialog(item); }),
        };

        var pos = GetViewport().GetMousePosition();
        ActionMenu.Instance?.Show(pos, actions.ToArray());
    }

    private void DoBuy(ItemDef item, long amount)
    {
        var gs = GameState.Instance;
        long totalCost = item.BuyPrice * amount;
        long available = gs.PlayerInventory.Gold + gs.PlayerBank.Gold;
        if (available < totalCost)
        {
            Toast.Instance?.Error("Not enough gold (combined bank + backpack)");
            return;
        }

        var target = _buySendToBank ? gs.PlayerBank.Storage : gs.PlayerInventory;
        if (!target.TryAdd(item, amount))
        {
            Toast.Instance?.Warning($"{(_buySendToBank ? "Bank" : "Backpack")} is full");
            return;
        }

        long fromBackpack = Math.Min(gs.PlayerInventory.Gold, totalCost);
        gs.PlayerInventory.Gold -= fromBackpack;
        long remaining = totalCost - fromBackpack;
        if (remaining > 0) gs.PlayerBank.Gold -= remaining;

        Toast.Instance?.Success(
            $"Bought {amount}x {item.Name} → {(_buySendToBank ? "Bank" : "Backpack")}");
        UpdateGoldFooter();
    }

    // ──────────────────────── HELPERS ────────────────────────

    private static Button CreateItemButton(string text, Action onPress)
    {
        var btn = new Button();
        btn.Text = $"  {text}";
        btn.CustomMinimumSize = new Vector2(0, 32);
        UiTheme.StyleListItemButton(btn);
        btn.Connect(BaseButton.SignalName.Pressed, Callable.From(onPress));
        return btn;
    }

    private static Label CreateEmptyLabel(string text)
    {
        var label = new Label();
        label.Text = text;
        UiTheme.StyleLabel(label, UiTheme.Colors.Muted, UiTheme.FontSizes.Body);
        label.HorizontalAlignment = HorizontalAlignment.Center;
        return label;
    }
}
