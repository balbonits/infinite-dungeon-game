using Godot;

namespace DungeonGame.Sandbox;

/// <summary>
/// Sandbox: Inventory
/// Visual 5×5 grid. Add items, test stacking, buy/sell, overflow.
/// Headless checks: stacking logic, overflow rejection, gold accuracy.
/// Run: make sandbox SCENE=inventory
/// </summary>
public partial class InventorySandbox : SandboxBase
{
    protected override string SandboxTitle => "🎒  Inventory Sandbox";

    private Inventory _inv = new(25);
    private Label _goldLabel = null!;
    private Label _slotsLabel = null!;

    private static readonly ItemDef Sword = new()
    { Id = "sword", Name = "Iron Sword", Category = ItemCategory.Weapon, BuyPrice = 100, SellPrice = 40 };

    private static readonly ItemDef Potion = new()
    { Id = "potion_hp_small", Name = "Small Potion", Category = ItemCategory.Consumable, BuyPrice = 25, SellPrice = 10 };

    private static readonly ItemDef Ore = new()
    { Id = "iron_ore", Name = "Iron Ore", Category = ItemCategory.Material, BuyPrice = 10, SellPrice = 4 };

    protected override void _SandboxReady()
    {
        _inv.Gold = 500;

        AddSectionLabel("Add Items");
        AddButton("+ Sword (weapon)", () => { _inv.TryAdd(Sword); Refresh("Added sword"); });
        AddButton("+ Potion ×5", () => { _inv.TryAdd(Potion, 5); Refresh("Added 5 potions"); });
        AddButton("+ Iron Ore ×10", () => { _inv.TryAdd(Ore, 10); Refresh("Added 10 ore"); });

        AddSectionLabel("Economy");
        AddButton("Buy Sword (100g)", () => Refresh(_inv.TryBuy(Sword) ? "Bought sword" : "Can't afford"));
        AddButton("Sell slot 0", () => Refresh(_inv.TrySell(0) ? "Sold slot 0" : "Slot 0 empty"));

        AddSectionLabel("Bulk Tests");
        AddButton("Fill all 25 slots", () => { for (int i = 0; i < 25; i++) _inv.TryAdd(new ItemDef { Id = $"item_{i}", Name = $"Item {i}", Category = ItemCategory.Weapon }); Refresh("Filled inventory"); });
        AddButton("Try add when full", () => Refresh(_inv.TryAdd(Sword) ? "⚠ Added (unexpected)" : "Correctly rejected — full"));

        _goldLabel = new Label(); _controlsContainer.AddChild(_goldLabel);
        _slotsLabel = new Label(); _controlsContainer.AddChild(_slotsLabel);

        Refresh("Ready");
    }

    protected override void _Reset()
    {
        _inv = new Inventory(25) { Gold = 500 };
        Refresh("Reset");
    }

    private void Refresh(string action)
    {
        Log($"> {action}");
        Log($"  Gold: {_inv.Gold}g   Slots: {_inv.UsedSlots}/{_inv.SlotCount}");
        for (int i = 0; i < 6; i++)
        {
            var s = _inv.GetSlot(i);
            if (s != null) Log($"  [{i}] {s.Item.Name} ×{s.Count}");
        }
        Log("");
    }

    protected override void RunHeadlessChecks()
    {
        Log("── Headless checks ──");

        var inv = new Inventory(5) { Gold = 1000 };

        inv.TryAdd(Potion, 10);
        Assert(inv.UsedSlots == 1, "10 potions stack into 1 slot");

        inv.TryAdd(Potion, 99);
        Assert(inv.UsedSlots == 2, "99 more potions overflow to 2nd slot");

        inv.TryAdd(Sword); inv.TryAdd(Sword); inv.TryAdd(Sword);
        Assert(inv.UsedSlots == 5, "3 swords fill remaining slots");

        bool rejected = !inv.TryAdd(Sword);
        Assert(rejected, "Add to full inventory correctly rejected");

        bool bought = inv.TryBuy(new ItemDef { Id = "x", Name = "X", Category = ItemCategory.Weapon, BuyPrice = 50, SellPrice = 20 });
        Assert(!bought, "Buy when full returns false");
        Assert(inv.Gold == 1000, "Gold unchanged after failed buy");

        var freshInv = new Inventory { Gold = 200 };
        freshInv.TryAdd(Sword);
        freshInv.TrySell(0);
        Assert(freshInv.Gold == 240, $"Gold after sell: expected 240, got {freshInv.Gold}");

        FinishHeadless();
    }
}
