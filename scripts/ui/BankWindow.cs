using Godot;
using DungeonGame.Autoloads;

namespace DungeonGame.Ui;

/// <summary>
/// Bank storage UI. Deposit/withdraw items between backpack and bank.
/// Also handles bank slot expansion purchasing.
/// Town-only access.
/// </summary>
public partial class BankWindow : Control
{
    public static BankWindow Instance { get; private set; } = null!;

    private ColorRect _overlay = null!;
    private CenterContainer _center = null!;
    private VBoxContainer _bankList = null!;
    private VBoxContainer _backpackList = null!;
    private Label _bankHeader = null!;
    private Label _backpackHeader = null!;
    private Label _goldLabel = null!;
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
        panel.CustomMinimumSize = new Vector2(550, 0);
        _center.AddChild(panel);

        var margin = new MarginContainer();
        panel.AddChild(margin);

        var outerVbox = new VBoxContainer();
        outerVbox.AddThemeConstantOverride("separation", 8);
        margin.AddChild(outerVbox);

        // Title
        var title = new Label();
        title.Text = Strings.Bank.Title;
        UiTheme.StyleLabel(title, UiTheme.Colors.Accent, UiTheme.FontSizes.Heading);
        title.HorizontalAlignment = HorizontalAlignment.Center;
        outerVbox.AddChild(title);

        _goldLabel = new Label();
        UiTheme.StyleLabel(_goldLabel, UiTheme.Colors.Accent, UiTheme.FontSizes.Body);
        _goldLabel.HorizontalAlignment = HorizontalAlignment.Center;
        outerVbox.AddChild(_goldLabel);

        outerVbox.AddChild(new HSeparator());

        // Two-column layout: Bank | Backpack
        var columns = new HBoxContainer();
        columns.AddThemeConstantOverride("separation", 16);
        outerVbox.AddChild(columns);

        // Bank column
        var bankCol = new VBoxContainer();
        bankCol.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        bankCol.AddThemeConstantOverride("separation", 4);
        columns.AddChild(bankCol);

        _bankHeader = new Label();
        UiTheme.StyleLabel(_bankHeader, UiTheme.Colors.Ink, UiTheme.FontSizes.Body);
        _bankHeader.HorizontalAlignment = HorizontalAlignment.Center;
        bankCol.AddChild(_bankHeader);

        var bankScroll = new ScrollContainer { FollowFocus = true };
        bankScroll.CustomMinimumSize = new Vector2(240, 280);
        bankCol.AddChild(bankScroll);
        _bankList = new VBoxContainer();
        _bankList.AddThemeConstantOverride("separation", 2);
        bankScroll.AddChild(_bankList);

        // Backpack column
        var bpCol = new VBoxContainer();
        bpCol.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        bpCol.AddThemeConstantOverride("separation", 4);
        columns.AddChild(bpCol);

        _backpackHeader = new Label();
        UiTheme.StyleLabel(_backpackHeader, UiTheme.Colors.Ink, UiTheme.FontSizes.Body);
        _backpackHeader.HorizontalAlignment = HorizontalAlignment.Center;
        bpCol.AddChild(_backpackHeader);

        var bpScroll = new ScrollContainer { FollowFocus = true };
        bpScroll.CustomMinimumSize = new Vector2(240, 280);
        bpCol.AddChild(bpScroll);
        _backpackList = new VBoxContainer();
        _backpackList.AddThemeConstantOverride("separation", 2);
        bpScroll.AddChild(_backpackList);

        // Bottom buttons
        var bottomRow = new HBoxContainer();
        bottomRow.AddThemeConstantOverride("separation", 12);
        bottomRow.Alignment = BoxContainer.AlignmentMode.Center;
        outerVbox.AddChild(bottomRow);

        var expandBtn = new Button();
        expandBtn.Text = Strings.Bank.Expand;
        expandBtn.CustomMinimumSize = new Vector2(160, 38);
        UiTheme.StyleButton(expandBtn, UiTheme.FontSizes.Body);
        expandBtn.Connect(BaseButton.SignalName.Pressed, Callable.From(() => OnExpandPressed()));
        bottomRow.AddChild(expandBtn);

        var closeBtn = new Button();
        closeBtn.Text = Strings.Ui.Cancel;
        closeBtn.CustomMinimumSize = new Vector2(120, 38);
        UiTheme.StyleSecondaryButton(closeBtn, UiTheme.FontSizes.Body);
        closeBtn.Connect(BaseButton.SignalName.Pressed, Callable.From(() => Close()));
        bottomRow.AddChild(closeBtn);
    }

    public void Open()
    {
        if (_isOpen) return;
        _isOpen = true;
        WindowStack.Push(this);
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
        var bank = GameState.Instance.PlayerBank;
        var backpack = GameState.Instance.PlayerInventory;

        _goldLabel.Text = Strings.Shop.GoldDisplay(backpack.Gold);
        _bankHeader.Text = $"Bank ({bank.Storage.UsedSlots}/{bank.TotalSlots})";
        _backpackHeader.Text = $"Backpack ({backpack.UsedSlots}/{backpack.SlotCount})";

        RefreshBankList(bank, backpack);
        RefreshBackpackList(bank, backpack);
        UiTheme.FocusFirstButton(_bankList);
    }

    private void RefreshBankList(Bank bank, Inventory backpack)
    {
        foreach (Node child in _bankList.GetChildren())
            child.QueueFree();

        for (int i = 0; i < bank.Storage.SlotCount; i++)
        {
            var stack = bank.Storage.GetSlot(i);
            if (stack != null)
            {
                int slotIndex = i;
                var row = CreateItemRow($"{i + 1:D2}  {stack.Item.Name}", stack.Count, Strings.Bank.Withdraw, () =>
                {
                    if (bank.Withdraw(backpack, slotIndex))
                        Refresh();
                    else
                        Toast.Instance?.Warning("Backpack is full!");
                });
                _bankList.AddChild(row);
            }
            else
            {
                var emptyRow = new Label();
                emptyRow.Text = $"{i + 1:D2}  — empty —";
                UiTheme.StyleLabel(emptyRow, new Color(UiTheme.Colors.Muted, 0.4f), UiTheme.FontSizes.Small);
                _bankList.AddChild(emptyRow);
            }
        }
    }

    private void RefreshBackpackList(Bank bank, Inventory backpack)
    {
        foreach (Node child in _backpackList.GetChildren())
            child.QueueFree();

        for (int i = 0; i < backpack.SlotCount; i++)
        {
            var stack = backpack.GetSlot(i);
            if (stack != null)
            {
                int slotIndex = i;
                var row = CreateItemRow($"{i + 1:D2}  {stack.Item.Name}", stack.Count, Strings.Bank.Deposit, () =>
                {
                    if (bank.Deposit(backpack, slotIndex))
                        Refresh();
                    else
                        Toast.Instance?.Warning("Bank is full!");
                });
                _backpackList.AddChild(row);
            }
            else
            {
                var emptyRow = new Label();
                emptyRow.Text = $"{i + 1:D2}  — empty —";
                UiTheme.StyleLabel(emptyRow, new Color(UiTheme.Colors.Muted, 0.4f), UiTheme.FontSizes.Small);
                _backpackList.AddChild(emptyRow);
            }
        }
    }

    private static HBoxContainer CreateItemRow(string name, int count, string actionLabel, System.Action action)
    {
        var row = new HBoxContainer();
        row.AddThemeConstantOverride("separation", 6);

        var nameLabel = new Label();
        string displayName = count > 1 ? $"{name} x{count}" : name;
        nameLabel.Text = displayName;
        UiTheme.StyleLabel(nameLabel, UiTheme.Colors.Ink, UiTheme.FontSizes.Small);
        nameLabel.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        row.AddChild(nameLabel);

        var actionBtn = new Button();
        actionBtn.Text = actionLabel;
        actionBtn.CustomMinimumSize = new Vector2(80, 28);
        actionBtn.FocusMode = FocusModeEnum.All;
        UiTheme.StyleButton(actionBtn, UiTheme.FontSizes.Small);
        actionBtn.Connect(BaseButton.SignalName.Pressed, Callable.From(action));
        row.AddChild(actionBtn);

        return row;
    }

    private void OnExpandPressed()
    {
        var bank = GameState.Instance.PlayerBank;
        var backpack = GameState.Instance.PlayerInventory;
        int cost = bank.GetNextExpansionCost();

        if (bank.PurchaseExpansion(backpack))
        {
            Toast.Instance?.Success($"+{Bank.SlotsPerExpansion} bank slots! ({bank.TotalSlots} total)");
            Refresh();
        }
        else
        {
            Toast.Instance?.Warning($"Not enough gold! Need {cost}g.");
        }
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

        // Q/E switch focus between bank list and backpack list
        if (@event.IsActionPressed(Constants.InputActions.ShoulderLeft))
        {
            UiTheme.FocusFirstButton(_bankList);
            GetViewport().SetInputAsHandled();
            return;
        }
        if (@event.IsActionPressed(Constants.InputActions.ShoulderRight))
        {
            UiTheme.FocusFirstButton(_backpackList);
            GetViewport().SetInputAsHandled();
            return;
        }

        // Up/Down + S navigate and confirm within whichever list has focus
        if (KeyboardNav.HandleInput(@event, _bankList) ||
            KeyboardNav.HandleInput(@event, _backpackList))
        {
            GetViewport().SetInputAsHandled();
            return;
        }

        if (@event is InputEventKey k && k.Pressed)
            GetViewport().SetInputAsHandled();
    }
}
