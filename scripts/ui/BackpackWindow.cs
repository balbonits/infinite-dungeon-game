using System;
using Godot;
using DungeonGame.Autoloads;

namespace DungeonGame.Ui;

/// <summary>
/// Backpack inventory window. Shows all carried items in a grid via <see cref="SlotGrid"/>.
/// Clicking a slot opens an item-actions dropdown (Use/Lock/Drop).
/// Gold is displayed as a label (no controls — Deposit/Withdraw live in the Guild window).
/// Accessible anywhere (dungeon or town).
/// Spec: docs/inventory/backpack.md, docs/ui/guild-window.md#item-actions-dropdown-full-reference
/// </summary>
public partial class BackpackWindow : GameWindow
{
    public static BackpackWindow? Instance { get; private set; }

    private Label _headerLabel = null!;
    private Label _goldLabel = null!;
    private Label _detailLabel = null!;
    private ScrollContainer _scrollContainer = null!;
    private SlotGrid _slotGrid = null!;

    public override void _Ready()
    {
        Instance = this;
        WindowWidth = 400f;
        base._Ready();
    }

    protected override void BuildContent(VBoxContainer content)
    {
        _headerLabel = new Label();
        UiTheme.StyleLabel(_headerLabel, UiTheme.Colors.Accent, UiTheme.FontSizes.Heading);
        _headerLabel.HorizontalAlignment = HorizontalAlignment.Center;
        content.AddChild(_headerLabel);

        _goldLabel = new Label();
        UiTheme.StyleLabel(_goldLabel, UiTheme.Colors.Accent, UiTheme.FontSizes.Body);
        _goldLabel.HorizontalAlignment = HorizontalAlignment.Center;
        content.AddChild(_goldLabel);

        var hint = new Label();
        hint.Text = "S: action  |  D: close";
        UiTheme.StyleLabel(hint, UiTheme.Colors.Muted, UiTheme.FontSizes.Small);
        hint.HorizontalAlignment = HorizontalAlignment.Center;
        content.AddChild(hint);

        content.AddChild(new HSeparator());

        _detailLabel = new Label();
        UiTheme.StyleLabel(_detailLabel, UiTheme.Colors.Muted, UiTheme.FontSizes.Small);
        _detailLabel.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        _detailLabel.CustomMinimumSize = new Vector2(0, 36);
        content.AddChild(_detailLabel);

        _scrollContainer = new ScrollContainer { FollowFocus = true };
        _scrollContainer.CustomMinimumSize = new Vector2(0, 320);
        content.AddChild(_scrollContainer);

        _slotGrid = new SlotGrid { Columns = 5, SlotSize = 64f };
        _slotGrid.SlotActivated += OnSlotActivated;
        _slotGrid.SlotFocused += OnSlotFocused;
        _scrollContainer.AddChild(_slotGrid);

        var closeBtn = new Button();
        closeBtn.Text = "Close";
        closeBtn.CustomMinimumSize = new Vector2(200, 38);
        closeBtn.SizeFlagsHorizontal = SizeFlags.ShrinkCenter;
        closeBtn.FocusMode = FocusModeEnum.None;
        UiTheme.StyleSecondaryButton(closeBtn, UiTheme.FontSizes.Body);
        closeBtn.Connect(BaseButton.SignalName.Pressed, Callable.From(Close));
        content.AddChild(closeBtn);
    }

    public void Open() => Show();

    protected override void OnShow()
    {
        Refresh();
    }

    private void Refresh()
    {
        var inv = GameState.Instance.PlayerInventory;
        _headerLabel.Text = $"BACKPACK ({inv.UsedSlots}/{inv.SlotCount})";
        _goldLabel.Text = $"Gold: {NumberFormat.Abbrev(inv.Gold)}";
        _detailLabel.Text = "Select an item";

        _slotGrid.SetInventory(inv);
        _scrollContainer.ScrollVertical = 0;
        CallDeferred(MethodName.FocusFirstSlot);
    }

    private void FocusFirstSlot() => _slotGrid.FocusFirstSlot();

    private void OnSlotFocused(int slotIdx, ItemStack? stack)
    {
        if (stack == null)
        {
            _detailLabel.Text = "";
            return;
        }
        var detail = $"{stack.Item.Name} x{NumberFormat.Full(stack.Count)}\n{stack.Item.Description}";
        if (stack.Item.HealAmount > 0) detail += $"\nHeals: {stack.Item.HealAmount} HP";
        if (stack.Item.ManaAmount > 0) detail += $"\nRestores: {stack.Item.ManaAmount} MP";
        if (stack.Item.SellPrice > 0) detail += $"\nSell: {stack.Item.SellPrice}g each";
        if (stack.Locked) detail += "\n[LOCKED]";
        _detailLabel.Text = detail;
    }

    private void OnSlotActivated(int slotIdx, ItemStack? stack)
    {
        if (stack == null) return;
        ShowItemActions(slotIdx, stack);
    }

    private void ShowItemActions(int slotIdx, ItemStack stack)
    {
        var actions = new System.Collections.Generic.List<(string label, Action action)>();
        var inv = GameState.Instance.PlayerInventory;

        // Use (consumables only)
        if (stack.Item.Category == ItemCategory.Consumable)
        {
            actions.Add(("Use", () => OnUse(slotIdx, stack.Item)));
        }

        // Lock / Unlock toggle
        actions.Add((stack.Locked ? "Unlock" : "Lock", () =>
        {
            inv.ToggleLock(slotIdx);
            Refresh();
        }
        ));

        // Drop (disabled if Locked)
        if (!stack.Locked)
        {
            actions.Add(("Drop", () => ShowDropConfirmation(slotIdx, stack)));
        }

        var pos = GetViewport().GetMousePosition();
        ActionMenu.Instance?.Show(pos, actions.ToArray());
    }

    private void OnUse(int slotIdx, ItemDef item)
    {
        var gs = GameState.Instance;
        var inv = gs.PlayerInventory;
        if (item.HealAmount > 0)
            gs.Hp = Math.Min(gs.MaxHp, gs.Hp + item.HealAmount);
        if (item.ManaAmount > 0)
            gs.Mana = Math.Min(gs.MaxMana, gs.Mana + item.ManaAmount);
        inv.RemoveAt(slotIdx);
        Toast.Instance?.Success($"Used {item.Name}");
        Refresh();
    }

    private void ShowDropConfirmation(int slotIdx, ItemStack stack)
    {
        // Destructive action — second confirmation before the drop goes through.
        // Partial-drop amount picker is spec'd in docs/ui/guild-window.md#drop-backpack-only
        // but deferred; MVP is always "Drop All" with a yes-cancel confirmation.
        var actions = new System.Collections.Generic.List<(string label, Action action)>
        {
            ($"Destroy {NumberFormat.Abbrev(stack.Count)} {stack.Item.Name}",
                () => ExecuteDrop(slotIdx, stack)),
            ("Cancel", () => { }),
        };
        var pos = GetViewport().GetMousePosition();
        ActionMenu.Instance?.Show(pos, actions.ToArray());
    }

    private void ExecuteDrop(int slotIdx, ItemStack stack)
    {
        var inv = GameState.Instance.PlayerInventory;
        if (inv.Drop(slotIdx, stack.Count))
        {
            Toast.Instance?.Warning($"Destroyed {NumberFormat.Abbrev(stack.Count)}x {stack.Item.Name}");
            Refresh();
        }
    }
}
