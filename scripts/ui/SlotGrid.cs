using System;
using Godot;

namespace DungeonGame.Ui;

/// <summary>
/// Reusable grid of inventory slot buttons. Binds to an <see cref="Inventory"/> and renders:
/// - Item name (truncated to 6 chars) as the button label + abbreviated count on a second line
///   (a lock emoji is prefixed when <c>stack.Locked</c>). True sprite-icon rendering and a
///   separate lock-icon overlay are tracked as a follow-up polish ticket.
/// - Category-colored background + focus highlight (UiTheme.CreateSlotStyle)
/// - Keyboard navigation via built-in button focus (no custom handling)
/// - Click / S / Enter → <see cref="SlotActivated"/> event fires with (slotIndex, stack)
///
/// Used by BackpackWindow (single grid) and GuildWindow (Bank tab + Transfer tab).
/// See docs/ui/guild-window.md for the full UI spec.
/// </summary>
public partial class SlotGrid : VBoxContainer
{
    /// <summary>Fired when a slot is activated (S/Enter/click). <c>stack</c> is null for empty slots.</summary>
    public event Action<int, ItemStack?>? SlotActivated;

    /// <summary>Fired when a slot gains focus (for detail-panel updates).</summary>
    public event Action<int, ItemStack?>? SlotFocused;

    private Inventory? _inventory;
    private int _columns = 5;
    private float _slotSize = 64f;
    private bool _emptySlotsVisible = true;
    private GridContainer _grid = null!;

    public int Columns
    {
        get => _columns;
        set { _columns = value; if (_grid != null) _grid.Columns = value; }
    }

    public float SlotSize
    {
        get => _slotSize;
        set => _slotSize = value;
    }

    /// <summary>If false, empty slots are not rendered (useful for lists that only show items).</summary>
    public bool EmptySlotsVisible
    {
        get => _emptySlotsVisible;
        set => _emptySlotsVisible = value;
    }

    public override void _Ready()
    {
        _grid = new GridContainer { Columns = _columns };
        _grid.AddThemeConstantOverride("h_separation", 6);
        _grid.AddThemeConstantOverride("v_separation", 6);
        _grid.SizeFlagsHorizontal = SizeFlags.ShrinkCenter;
        AddChild(_grid);
    }

    /// <summary>Bind to an inventory and render its slots. Call again to re-bind. Safe to call
    /// before <see cref="_Ready"/> runs — the grid will populate once the node is in-tree.</summary>
    public void SetInventory(Inventory inventory)
    {
        _inventory = inventory;
        Refresh();
    }

    /// <summary>Redraw all slots from the current inventory's state. If <see cref="_Ready"/>
    /// hasn't yet built the grid, defer the render until after _Ready runs.</summary>
    public void Refresh()
    {
        if (_inventory == null) return;
        if (_grid == null)
        {
            // _Ready hasn't run yet. Defer the populate so we don't lose the first SetInventory.
            CallDeferred(MethodName.Refresh);
            return;
        }

        foreach (Node child in _grid.GetChildren())
            child.QueueFree();

        for (int i = 0; i < _inventory.SlotCount; i++)
        {
            var stack = _inventory.GetSlot(i);
            if (stack == null && !_emptySlotsVisible) continue;

            _grid.AddChild(BuildSlotButton(i, stack));
        }
    }

    /// <summary>Grab focus on the first occupied slot (for keyboard-nav entry into the grid).
    /// Falls back to the first focusable slot if every slot is empty.</summary>
    public void FocusFirstSlot()
    {
        if (_grid == null) return;
        Button? firstFocusable = null;
        foreach (Node child in _grid.GetChildren())
        {
            if (child is Button btn && btn.FocusMode != FocusModeEnum.None && !btn.Disabled)
            {
                // Prefer occupied slots (identified by non-empty Text — empty slots have Text = "")
                if (!string.IsNullOrEmpty(btn.Text))
                {
                    btn.CallDeferred(Control.MethodName.GrabFocus);
                    return;
                }
                firstFocusable ??= btn;
            }
        }
        firstFocusable?.CallDeferred(Control.MethodName.GrabFocus);
    }

    private Button BuildSlotButton(int slotIndex, ItemStack? stack)
    {
        var btn = new Button();
        btn.CustomMinimumSize = new Vector2(_slotSize, _slotSize);
        btn.SizeFlagsHorizontal = SizeFlags.ShrinkCenter;
        btn.SizeFlagsVertical = SizeFlags.ShrinkCenter;
        btn.ClipText = true;
        btn.AddThemeFontSizeOverride("font_size", 9);
        btn.AddThemeColorOverride("font_color", UiTheme.Colors.Ink);
        btn.AddThemeColorOverride("font_hover_color", UiTheme.Colors.Ink);
        btn.AddThemeColorOverride("font_focus_color", UiTheme.Colors.Ink);

        if (stack != null)
        {
            btn.FocusMode = FocusModeEnum.All;
            btn.Text = BuildSlotLabel(stack);
            var bg = CategoryColor(stack.Item.Category);
            btn.AddThemeStyleboxOverride("normal", UiTheme.CreateSlotStyle(bg, false));
            btn.AddThemeStyleboxOverride("hover", UiTheme.CreateSlotStyle(bg, true));
            btn.AddThemeStyleboxOverride("focus", UiTheme.CreateSlotStyle(bg, true));
        }
        else
        {
            // Empty slots are non-interactive and skipped by keyboard nav — they render as
            // placeholders only. Matches PauseMenu's existing inventory grid behavior.
            btn.FocusMode = FocusModeEnum.None;
            btn.Disabled = true;
            btn.Text = "";
            var empty = UiTheme.CreateEmptySlotStyle();
            btn.AddThemeStyleboxOverride("normal", empty);
            btn.AddThemeStyleboxOverride("hover", empty);
            btn.AddThemeStyleboxOverride("focus", empty);
            btn.AddThemeStyleboxOverride("disabled", empty);
        }

        // Re-resolve the live ItemStack at click/focus time rather than capturing the snapshot.
        // The underlying Inventory replaces ItemStack records on update (immutable), so a
        // captured value goes stale after any Refresh elsewhere (count, Locked flag).
        int captured = slotIndex;
        btn.Connect(BaseButton.SignalName.Pressed,
            Callable.From(() => SlotActivated?.Invoke(captured, _inventory?.GetSlot(captured))));
        btn.FocusEntered += () => SlotFocused?.Invoke(captured, _inventory?.GetSlot(captured));

        return btn;
    }

    private static string BuildSlotLabel(ItemStack stack)
    {
        string name = stack.Item.Name;
        if (name.Length > 6) name = name[..6];

        string countLine = stack.Count > 1 ? $"\nx{NumberFormat.Abbrev(stack.Count)}" : "";
        string lockPrefix = stack.Locked ? "🔒 " : "";
        return lockPrefix + name + countLine;
    }

    private static Color CategoryColor(ItemCategory cat) => cat switch
    {
        ItemCategory.Consumable => new Color(0.15f, 0.35f, 0.15f, 0.9f),
        ItemCategory.Material => new Color(0.15f, 0.25f, 0.35f, 0.9f),
        ItemCategory.Weapon => new Color(0.35f, 0.20f, 0.15f, 0.9f),
        ItemCategory.Armor => new Color(0.20f, 0.20f, 0.30f, 0.9f),
        ItemCategory.Accessory => new Color(0.30f, 0.15f, 0.30f, 0.9f),
        ItemCategory.Quiver => new Color(0.25f, 0.25f, 0.15f, 0.9f),
        _ => new Color(0.15f, 0.15f, 0.20f, 0.9f),
    };
}
