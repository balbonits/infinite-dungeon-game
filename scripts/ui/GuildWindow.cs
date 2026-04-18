using System;
using System.Collections.Generic;
using Godot;
using DungeonGame.Autoloads;

namespace DungeonGame.Ui;

/// <summary>
/// Guild Maid's service window. Two tabs per SPEC-GUILD-MAID-MERGED-MENU-01:
/// <c>Bank</c> (storage + two-column transfer layout + gold controls) and
/// <c>Teleport</c> (floor fast-travel, absorbed from the retired Teleporter NPC).
/// Opens on Bank by default. Store tab moved OUT to the Blacksmith's Shop tab
/// per SPEC-BLACKSMITH-MERGED-MENU-01. Transfer tab collapsed INTO Bank.
///
/// Spec: docs/ui/guild-maid-menu.md (new), docs/ui/guild-window.md (partially
/// superseded), docs/inventory/bank.md, docs/inventory/backpack.md.
/// </summary>
public partial class GuildWindow : GameWindow
{
    public static GuildWindow? Instance { get; private set; }

    private const int BankTabIndex = 0;
    private const int TeleportTabIndex = 1;

    private GameTabPanel _tabs = null!;
    private Label _goldFooter = null!;

    private SlotGrid _bankGrid = null!;
    private SlotGrid _backpackGrid = null!;

    public override void _Ready()
    {
        Instance = this;
        WindowWidth = 560f;
        base._Ready();
    }

    protected override void BuildContent(VBoxContainer content)
    {
        var title = new Label();
        title.Text = Strings.Guild.Title;
        UiTheme.StyleLabel(title, UiTheme.Colors.Accent, UiTheme.FontSizes.Heading);
        title.HorizontalAlignment = HorizontalAlignment.Center;
        content.AddChild(title);

        _tabs = new GameTabPanel();
        _tabs.AddTab(Strings.Guild.BankTab, BuildBankTab);
        _tabs.AddTab(Strings.Guild.TeleportTab, BuildTeleportTab);
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
        _tabs.SelectTab(BankTabIndex);
        UpdateGoldFooter();
    }

    protected override bool HandleTabInput(InputEvent @event) => _tabs.HandleInput(@event);

    private void UpdateGoldFooter()
    {
        var gs = GameState.Instance;
        _goldFooter.Text = $"Bank gold: {NumberFormat.Abbrev(gs.PlayerBank.Gold)}     " +
                           $"Backpack gold: {NumberFormat.Abbrev(gs.PlayerInventory.Gold)}";
    }

    // ──────────────────────── BANK TAB ────────────────────────
    // Merges the prior Bank + Transfer tabs: gold controls on top, then
    // two-column Bank ⇄ Backpack layout, then an upgrade button.

    private void BuildBankTab()
    {
        var scroll = _tabs.ScrollContent;
        var gs = GameState.Instance;

        // Gold row (Withdraw/Deposit All).
        var goldRow = new HBoxContainer();
        goldRow.AddThemeConstantOverride("separation", 8);
        goldRow.Alignment = BoxContainer.AlignmentMode.Center;
        scroll.AddChild(goldRow);

        var bankGoldLabel = new Label();
        bankGoldLabel.Text = $"Bank: {NumberFormat.Abbrev(gs.PlayerBank.Gold)}g";
        UiTheme.StyleLabel(bankGoldLabel, UiTheme.Colors.Accent, UiTheme.FontSizes.Body);
        goldRow.AddChild(bankGoldLabel);

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

        scroll.AddChild(new HSeparator());

        // Two-column Bank ⇄ Backpack.
        var row = new HBoxContainer();
        row.AddThemeConstantOverride("separation", 12);
        scroll.AddChild(row);

        var bankSide = new VBoxContainer();
        var bankLbl = new Label();
        bankLbl.Text = $"BANK ({gs.PlayerBank.Storage.UsedSlots}/{gs.PlayerBank.TotalSlots})";
        UiTheme.StyleLabel(bankLbl, UiTheme.Colors.Accent, UiTheme.FontSizes.Small);
        bankSide.AddChild(bankLbl);
        _bankGrid = new SlotGrid { Columns = 3, SlotSize = 48f };
        _bankGrid.SlotActivated += (idx, stack) => OnSlotClicked(idx, stack, fromBank: true);
        bankSide.AddChild(_bankGrid);
        _bankGrid.SetInventory(gs.PlayerBank.Storage);
        row.AddChild(bankSide);

        var arrow = new Label();
        arrow.Text = "⇄";
        UiTheme.StyleLabel(arrow, UiTheme.Colors.Muted, UiTheme.FontSizes.Title);
        arrow.VerticalAlignment = VerticalAlignment.Center;
        row.AddChild(arrow);

        var bpSide = new VBoxContainer();
        var bpLbl = new Label();
        bpLbl.Text = $"BACKPACK ({gs.PlayerInventory.UsedSlots}/{gs.PlayerInventory.SlotCount})";
        UiTheme.StyleLabel(bpLbl, UiTheme.Colors.Accent, UiTheme.FontSizes.Small);
        bpSide.AddChild(bpLbl);
        _backpackGrid = new SlotGrid { Columns = 3, SlotSize = 48f };
        _backpackGrid.SlotActivated += (idx, stack) => OnSlotClicked(idx, stack, fromBank: false);
        bpSide.AddChild(_backpackGrid);
        _backpackGrid.SetInventory(gs.PlayerInventory);
        row.AddChild(bpSide);

        // Upgrade button.
        scroll.AddChild(new HSeparator());
        var upgradeBtn = new Button();
        upgradeBtn.Text = $"Upgrade Bank (+1 slot) — {gs.PlayerBank.GetNextExpansionCost()}g";
        upgradeBtn.CustomMinimumSize = new Vector2(0, 32);
        UiTheme.StyleButton(upgradeBtn, UiTheme.FontSizes.Small);
        upgradeBtn.Connect(BaseButton.SignalName.Pressed, Callable.From(OnUpgradePressed));
        scroll.AddChild(upgradeBtn);
    }

    private void OnSlotClicked(int slotIdx, ItemStack? stack, bool fromBank)
    {
        if (stack == null) return;
        var gs = GameState.Instance;
        var source = fromBank ? gs.PlayerBank.Storage : gs.PlayerInventory;
        var dest = fromBank ? gs.PlayerInventory : gs.PlayerBank.Storage;
        var actions = new List<(string, Action)>();

        // Transfer to the other side (primary action for both sides).
        actions.Add(($"Transfer to {(fromBank ? "Backpack" : "Bank")} ({NumberFormat.Abbrev(stack.Count)})", () =>
        {
            if (source.Transfer(slotIdx, dest, stack.Count))
            {
                Toast.Instance?.Info($"Moved to {(fromBank ? "backpack" : "bank")}");
                _tabs.SelectTab(BankTabIndex);
            }
            else
            {
                Toast.Instance?.Warning($"{(fromBank ? "Backpack" : "Bank")} full");
            }
        }
        ));

        // Sell — only from bank side (backpack-side sell flows through a different path).
        if (fromBank && !stack.Locked)
        {
            actions.Add(($"Sell 1 ({stack.Item.SellPrice}g)", () =>
            {
                gs.PlayerBank.Storage.RemoveAt(slotIdx, 1);
                gs.PlayerBank.Gold += stack.Item.SellPrice;
                Toast.Instance?.Info($"Sold 1 {stack.Item.Name}");
                _tabs.SelectTab(BankTabIndex);
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

        // Lock toggle (bank side only — backpack lock is managed elsewhere).
        if (fromBank)
        {
            actions.Add((stack.Locked ? "Unlock" : "Lock", () =>
            {
                gs.PlayerBank.Storage.ToggleLock(slotIdx);
                _tabs.SelectTab(BankTabIndex);
            }
            ));
        }

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

    // ──────────────────────── TELEPORT TAB ────────────────────────
    // Absorbed from the prior TeleportDialog / retired Teleporter NPC.

    private void BuildTeleportTab()
    {
        var scroll = _tabs.ScrollContent;

        var hint = new Label();
        hint.Text = Strings.Teleport.Subtitle;
        UiTheme.StyleLabel(hint, UiTheme.Colors.Muted, UiTheme.FontSizes.Small);
        hint.HorizontalAlignment = HorizontalAlignment.Center;
        hint.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        scroll.AddChild(hint);

        int deepestFloor = GameState.Instance.FloorNumber;
        if (deepestFloor <= 1)
        {
            var noFloors = new Label();
            noFloors.Text = Strings.Teleport.NoFloorsVisited;
            UiTheme.StyleLabel(noFloors, UiTheme.Colors.Muted, UiTheme.FontSizes.Body);
            noFloors.HorizontalAlignment = HorizontalAlignment.Center;
            scroll.AddChild(noFloors);
            return;
        }

        for (int floor = deepestFloor; floor >= 1; floor--)
        {
            int targetFloor = floor;
            string label = Strings.Floor.FloorNumber(targetFloor);
            int zone = Constants.Zones.GetZone(targetFloor);
            string zoneLabel = $"{label}  (Zone {zone})";

            var btn = new Button();
            btn.Text = zoneLabel;
            btn.CustomMinimumSize = new Vector2(0, 34);
            UiTheme.StyleButton(btn, UiTheme.FontSizes.Body);
            btn.Connect(BaseButton.SignalName.Pressed,
                Callable.From(() => TeleportToFloor(targetFloor)));
            scroll.AddChild(btn);
        }
    }

    private void TeleportToFloor(int floor)
    {
        GameState.Instance.FloorNumber = floor;
        ScreenTransition.Instance.Play(
            Strings.Floor.FloorNumber(floor),
            () =>
            {
                Close();
                Scenes.Main.Instance.LoadDungeon();
            },
            Strings.Teleport.Teleporting);
    }
}
