using Godot;

namespace DungeonGame.Sandbox;

/// <summary>
/// Sandbox: Bank System
/// Side-by-side backpack + bank panels. Deposit, withdraw, expand.
/// Headless checks: expansion cost formula, item round-trip, slot growth.
/// Run: make sandbox SCENE=bank
/// </summary>
public partial class BankSandbox : SandboxBase
{
    protected override string SandboxTitle => "🏦  Bank Sandbox";

    private Bank _bank = new();
    private Inventory _backpack = new() { Gold = 10_000 };

    private static ItemDef MakeItem(string id) => new()
    { Id = id, Name = id, Category = ItemCategory.Weapon, BuyPrice = 100, SellPrice = 40 };

    protected override void _SandboxReady()
    {
        AddSectionLabel("Backpack → Bank");
        AddButton("Add item to backpack", () =>
        {
            int n = _backpack.UsedSlots;
            _backpack.TryAdd(MakeItem($"sword_{n}"));
            Refresh("Added item to backpack");
        });
        AddButton("Deposit slot 0 → Bank", () =>
            Refresh(_bank.Deposit(_backpack, 0) ? "Deposited slot 0" : "Deposit failed"));

        AddSectionLabel("Bank → Backpack");
        AddButton("Withdraw bank slot 0", () =>
            Refresh(_bank.Withdraw(_backpack, 0) ? "Withdrew bank slot 0" : "Withdraw failed"));

        AddSectionLabel("Expansion");
        AddButton("Purchase expansion", () =>
        {
            int cost = _bank.GetNextExpansionCost();
            bool ok = _bank.PurchaseExpansion(_backpack);
            Refresh(ok ? $"Expanded! Cost: {cost}g" : $"Can't afford {cost}g");
        });

        Refresh("Ready");
    }

    protected override void _Reset()
    {
        _bank = new Bank();
        _backpack = new Inventory { Gold = 10_000 };
        Refresh("Reset");
    }

    private void Refresh(string action)
    {
        Log($"> {action}");
        Log($"  Backpack: {_backpack.UsedSlots}/{_backpack.SlotCount} slots  |  Gold: {_backpack.Gold}g");
        Log($"  Bank:     {_bank.Storage.UsedSlots}/{_bank.TotalSlots} slots  |  Expansions: {_bank.ExpansionCount}");
        Log($"  Next exp: {_bank.GetNextExpansionCost()}g");
        Log("");
    }

    protected override void RunHeadlessChecks()
    {
        Log("── Headless checks ──");

        var bank = new Bank();
        Assert(bank.GetNextExpansionCost() == 500, "1st expansion costs 500g");

        var inv = new Inventory { Gold = 100_000 };
        bank.PurchaseExpansion(inv);
        Assert(bank.GetNextExpansionCost() == 2000, "2nd expansion costs 2000g");
        Assert(bank.TotalSlots == Bank.StartingSlots + Bank.SlotsPerExpansion,
            $"Slots grew to {bank.TotalSlots}");

        inv.TryAdd(MakeItem("test_sword"));
        bool deposited = bank.Deposit(inv, 0);
        Assert(deposited, "Deposit succeeded");
        Assert(inv.UsedSlots == 0, "Backpack empty after deposit");
        Assert(bank.Storage.UsedSlots == 1, "Bank has 1 item");

        bool withdrew = bank.Withdraw(inv, 0);
        Assert(withdrew, "Withdraw succeeded");
        Assert(inv.UsedSlots == 1, "Item back in backpack");
        Assert(bank.Storage.UsedSlots == 0, "Bank empty after withdraw");

        FinishHeadless();
    }
}
