using System;
using System.Collections.Generic;
using Godot;
using DungeonGame.Autoloads;

namespace DungeonGame.Ui;

/// <summary>
/// Merged Store + Bank + Transfer window, opened via the Guild Maid NPC.
/// Three tabs: <c>Store</c>, <c>Bank</c>, <c>Transfer</c>. Opens on Bank by default.
/// Replaces the old ShopWindow and BankWindow.
///
/// Spec: docs/ui/guild-window.md, docs/inventory/bank.md, docs/inventory/backpack.md
/// </summary>
public partial class GuildWindow : GameWindow
{
    public static GuildWindow? Instance { get; private set; }

    private const int StoreTabIndex = 0;
    private const int BankTabIndex = 1;
    private const int TransferTabIndex = 2;

    private GameTabPanel _tabs = null!;
    private Label _goldFooter = null!;

    // Sticky choice: last "Send to" target from a Store buy (Bank default, remembered per session)
    private bool _buySendToBank = true;

    public override void _Ready()
    {
        Instance = this;
        WindowWidth = 560f;
        base._Ready();
    }

    protected override void BuildContent(VBoxContainer content)
    {
        var title = new Label();
        title.Text = "Guild";
        UiTheme.StyleLabel(title, UiTheme.Colors.Accent, UiTheme.FontSizes.Heading);
        title.HorizontalAlignment = HorizontalAlignment.Center;
        content.AddChild(title);

        _tabs = new GameTabPanel();
        _tabs.AddTab("Store", BuildStoreTab);
        _tabs.AddTab("Bank", BuildBankTab);
        _tabs.AddTab("Transfer", BuildTransferTab);
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
        _tabs.SelectTab(BankTabIndex); // Y2: c — open on Bank
        UpdateGoldFooter();
    }

    protected override bool HandleTabInput(InputEvent @event) => _tabs.HandleInput(@event);

    private void UpdateGoldFooter()
    {
        var gs = GameState.Instance;
        _goldFooter.Text = $"Bank gold: {NumberFormat.Abbrev(gs.PlayerBank.Gold)}     " +
                           $"Backpack gold: {NumberFormat.Abbrev(gs.PlayerInventory.Gold)}";
    }

    // ──────────────────────── STORE TAB ────────────────────────

    private void BuildStoreTab()
    {
        var scroll = _tabs.ScrollContent;

        // Store stocks basic consumables. Full catalog (materials, ammo) is a content ticket;
        // today we surface everything from ItemDatabase that has a BuyPrice and is Consumable.
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
        }
    }

    private void ShowBuyDialog(ItemDef item)
    {
        // Simple modal: current amount + +/- buttons + confirm "Send to Bank/Backpack"
        // Full slider UI spec'd in guild-window.md; MVP uses preset buttons.
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

        // Draw from backpack pocket first, then bank
        long fromBackpack = Math.Min(gs.PlayerInventory.Gold, totalCost);
        gs.PlayerInventory.Gold -= fromBackpack;
        long remaining = totalCost - fromBackpack;
        if (remaining > 0) gs.PlayerBank.Gold -= remaining;

        Toast.Instance?.Success(
            $"Bought {amount}x {item.Name} → {(_buySendToBank ? "Bank" : "Backpack")}");
        UpdateGoldFooter();
    }

    // ──────────────────────── BANK TAB ────────────────────────

    private SlotGrid _bankGrid = null!;

    private void BuildBankTab()
    {
        var scroll = _tabs.ScrollContent;
        var gs = GameState.Instance;

        var header = new Label();
        header.Text = $"Bank ({gs.PlayerBank.Storage.UsedSlots}/{gs.PlayerBank.TotalSlots})";
        UiTheme.StyleLabel(header, UiTheme.Colors.Accent, UiTheme.FontSizes.Body);
        header.HorizontalAlignment = HorizontalAlignment.Center;
        scroll.AddChild(header);

        _bankGrid = new SlotGrid { Columns = 5, SlotSize = 56f };
        _bankGrid.SlotActivated += OnBankSlotActivated;
        scroll.AddChild(_bankGrid);
        _bankGrid.SetInventory(gs.PlayerBank.Storage);

        // Gold row
        var goldRow = new HBoxContainer();
        goldRow.AddThemeConstantOverride("separation", 8);
        goldRow.Alignment = BoxContainer.AlignmentMode.Center;
        scroll.AddChild(goldRow);

        var goldLabel = new Label();
        goldLabel.Text = $"Bank gold: {NumberFormat.Abbrev(gs.PlayerBank.Gold)}";
        UiTheme.StyleLabel(goldLabel, UiTheme.Colors.Accent, UiTheme.FontSizes.Body);
        goldRow.AddChild(goldLabel);

        var withdrawBtn = new Button();
        withdrawBtn.Text = "Withdraw All";
        UiTheme.StyleSecondaryButton(withdrawBtn, UiTheme.FontSizes.Small);
        withdrawBtn.Connect(BaseButton.SignalName.Pressed, Callable.From(OnWithdrawAll));
        goldRow.AddChild(withdrawBtn);

        var depositBtn = new Button();
        depositBtn.Text = "Deposit All";
        UiTheme.StyleSecondaryButton(depositBtn, UiTheme.FontSizes.Small);
        depositBtn.Connect(BaseButton.SignalName.Pressed, Callable.From(OnDepositAll));
        goldRow.AddChild(depositBtn);

        // Upgrade button
        var upgradeBtn = new Button();
        upgradeBtn.Text = $"Upgrade (+1 slot) — {gs.PlayerBank.GetNextExpansionCost()}g";
        UiTheme.StyleButton(upgradeBtn, UiTheme.FontSizes.Small);
        upgradeBtn.Connect(BaseButton.SignalName.Pressed, Callable.From(OnUpgradePressed));
        scroll.AddChild(upgradeBtn);
    }

    private void OnBankSlotActivated(int slotIdx, ItemStack? stack)
    {
        if (stack == null) return;
        var gs = GameState.Instance;
        var actions = new List<(string, Action)>();

        // Sell (greyed out via not-adding if Locked)
        if (!stack.Locked)
        {
            actions.Add(($"Sell 1 ({stack.Item.SellPrice}g)", () =>
            {
                gs.PlayerBank.Storage.RemoveAt(slotIdx, 1);
                gs.PlayerBank.Gold += stack.Item.SellPrice;
                Toast.Instance?.Info($"Sold 1 {stack.Item.Name}");
                _tabs.SelectTab(BankTabIndex); // rebuild
                UpdateGoldFooter();
            }
            ));
            actions.Add(($"Sell All ({NumberFormat.Abbrev(stack.Count * stack.Item.SellPrice)}g)", () =>
            {
                long total = stack.Count * stack.Item.SellPrice;
                gs.PlayerBank.Storage.RemoveAt(slotIdx, stack.Count);
                gs.PlayerBank.Gold += total;
                Toast.Instance?.Info($"Sold {NumberFormat.Abbrev(stack.Count)} {stack.Item.Name}");
                _tabs.SelectTab(BankTabIndex);
                UpdateGoldFooter();
            }
            ));
        }

        actions.Add((stack.Locked ? "Unlock" : "Lock", () =>
        {
            gs.PlayerBank.Storage.ToggleLock(slotIdx);
            _tabs.SelectTab(BankTabIndex);
        }
        ));

        actions.Add(("Transfer to Backpack", () =>
        {
            if (gs.PlayerBank.Storage.Transfer(slotIdx, gs.PlayerInventory, stack.Count))
            {
                Toast.Instance?.Info($"Moved to backpack");
                _tabs.SelectTab(BankTabIndex);
            }
            else
            {
                Toast.Instance?.Warning("Backpack full");
            }
        }
        ));

        var pos = GetViewport().GetMousePosition();
        ActionMenu.Instance?.Show(pos, actions.ToArray());
    }

    private void OnWithdrawAll()
    {
        var gs = GameState.Instance;
        if (gs.PlayerBank.Gold <= 0) return;
        gs.PlayerBank.WithdrawGold(gs.PlayerInventory, gs.PlayerBank.Gold);
        _tabs.SelectTab(BankTabIndex);
        UpdateGoldFooter();
    }

    private void OnDepositAll()
    {
        var gs = GameState.Instance;
        if (gs.PlayerInventory.Gold <= 0) return;
        gs.PlayerBank.DepositGold(gs.PlayerInventory, gs.PlayerInventory.Gold);
        _tabs.SelectTab(BankTabIndex);
        UpdateGoldFooter();
    }

    private void OnUpgradePressed()
    {
        var gs = GameState.Instance;
        long cost = gs.PlayerBank.GetNextExpansionCost();
        if (gs.PlayerBank.PurchaseExpansion(gs.PlayerInventory))
        {
            Toast.Instance?.Success($"+1 bank slot ({gs.PlayerBank.TotalSlots} total)");
            _tabs.SelectTab(BankTabIndex);
            UpdateGoldFooter();
        }
        else
        {
            Toast.Instance?.Error($"Not enough gold. Need {cost}g total.");
        }
    }

    // ──────────────────────── TRANSFER TAB ────────────────────────

    private SlotGrid _transferBankGrid = null!;
    private SlotGrid _transferBackpackGrid = null!;

    private void BuildTransferTab()
    {
        var scroll = _tabs.ScrollContent;
        var gs = GameState.Instance;

        var row = new HBoxContainer();
        row.AddThemeConstantOverride("separation", 12);
        scroll.AddChild(row);

        // Bank side
        var bankSide = new VBoxContainer();
        var bankLbl = new Label();
        bankLbl.Text = $"BANK ({gs.PlayerBank.Storage.UsedSlots}/{gs.PlayerBank.TotalSlots})";
        UiTheme.StyleLabel(bankLbl, UiTheme.Colors.Accent, UiTheme.FontSizes.Small);
        bankSide.AddChild(bankLbl);
        _transferBankGrid = new SlotGrid { Columns = 3, SlotSize = 48f };
        _transferBankGrid.SlotActivated += (idx, stack) => OnTransferSlotClicked(stack, idx, fromBank: true);
        bankSide.AddChild(_transferBankGrid);
        _transferBankGrid.SetInventory(gs.PlayerBank.Storage);
        row.AddChild(bankSide);

        var arrow = new Label();
        arrow.Text = "⇄";
        UiTheme.StyleLabel(arrow, UiTheme.Colors.Muted, UiTheme.FontSizes.Title);
        arrow.VerticalAlignment = VerticalAlignment.Center;
        row.AddChild(arrow);

        // Backpack side
        var bpSide = new VBoxContainer();
        var bpLbl = new Label();
        bpLbl.Text = $"BACKPACK ({gs.PlayerInventory.UsedSlots}/{gs.PlayerInventory.SlotCount})";
        UiTheme.StyleLabel(bpLbl, UiTheme.Colors.Accent, UiTheme.FontSizes.Small);
        bpSide.AddChild(bpLbl);
        _transferBackpackGrid = new SlotGrid { Columns = 3, SlotSize = 48f };
        _transferBackpackGrid.SlotActivated += (idx, stack) => OnTransferSlotClicked(stack, idx, fromBank: false);
        bpSide.AddChild(_transferBackpackGrid);
        _transferBackpackGrid.SetInventory(gs.PlayerInventory);
        row.AddChild(bpSide);

        // Gold transfer row
        scroll.AddChild(new HSeparator());
        var goldRow = new HBoxContainer();
        goldRow.Alignment = BoxContainer.AlignmentMode.Center;
        goldRow.AddThemeConstantOverride("separation", 8);
        scroll.AddChild(goldRow);

        var sendToBank = new Button();
        sendToBank.Text = "Backpack → Bank (all)";
        UiTheme.StyleSecondaryButton(sendToBank, UiTheme.FontSizes.Small);
        sendToBank.Connect(BaseButton.SignalName.Pressed, Callable.From(TransferGoldToBank));
        goldRow.AddChild(sendToBank);

        var sendToBackpack = new Button();
        sendToBackpack.Text = "Bank → Backpack (all)";
        UiTheme.StyleSecondaryButton(sendToBackpack, UiTheme.FontSizes.Small);
        sendToBackpack.Connect(BaseButton.SignalName.Pressed, Callable.From(TransferGoldToBackpack));
        goldRow.AddChild(sendToBackpack);
    }

    private void TransferGoldToBank()
    {
        var gs = GameState.Instance;
        if (gs.PlayerInventory.Gold <= 0) return;
        gs.PlayerBank.DepositGold(gs.PlayerInventory, gs.PlayerInventory.Gold);
        _tabs.SelectTab(TransferTabIndex); // stay on Transfer tab after a transfer-tab action
        UpdateGoldFooter();
    }

    private void TransferGoldToBackpack()
    {
        var gs = GameState.Instance;
        if (gs.PlayerBank.Gold <= 0) return;
        gs.PlayerBank.WithdrawGold(gs.PlayerInventory, gs.PlayerBank.Gold);
        _tabs.SelectTab(TransferTabIndex); // stay on Transfer tab after a transfer-tab action
        UpdateGoldFooter();
    }

    private void OnTransferSlotClicked(ItemStack? stack, int slotIdx, bool fromBank)
    {
        if (stack == null) return;
        var gs = GameState.Instance;

        // Simple MVP: Transfer All. Full amount-input dialog per B1: a is a follow-up ticket.
        var source = fromBank ? gs.PlayerBank.Storage : gs.PlayerInventory;
        var dest = fromBank ? gs.PlayerInventory : gs.PlayerBank.Storage;

        if (source.Transfer(slotIdx, dest, stack.Count))
        {
            Toast.Instance?.Info($"Moved {NumberFormat.Abbrev(stack.Count)} {stack.Item.Name}");
            _tabs.SelectTab(TransferTabIndex); // rebuild both grids
        }
        else
        {
            Toast.Instance?.Warning($"{(fromBank ? "Backpack" : "Bank")} full");
        }
    }
}
