using Godot;

namespace DungeonGame.Sandbox;

/// <summary>
/// Sandbox: Death Penalty
/// Interactive calculator: floor depth → XP loss, item loss, protection costs.
/// Headless checks: cap, idol consumption, item loss count.
/// Run: make sandbox SCENE=death-penalty
/// </summary>
public partial class DeathPenaltySandbox : SandboxBase
{
    protected override string SandboxTitle => "💀  Death Penalty Sandbox";

    private int _deepestFloor = 10;
    private int _currentXp = 1000;
    private bool _hasIdol = false;
    private Inventory _inv = new();

    protected override void _SandboxReady()
    {
        AddSectionLabel("Parameters");
        AddSlider("Deepest Floor", 1, 200, _deepestFloor, v => { _deepestFloor = (int)v; Recalculate(); });
        AddSlider("Current XP", 0, 5000, _currentXp, v => { _currentXp = (int)v; Recalculate(); });

        AddSectionLabel("Inventory");
        AddButton("Fill with 5 items", () =>
        {
            _inv = new Inventory();
            for (int i = 0; i < 5; i++)
                _inv.TryAdd(new ItemDef { Id = $"item_{i}", Name = $"Item {i}", Category = ItemCategory.Weapon });
            Recalculate();
        });
        AddButton("Add Sacrificial Idol", () =>
        {
            _inv.TryAdd(new ItemDef { Id = "consumable_sacrificial_idol", Name = "Sacrificial Idol", Category = ItemCategory.Consumable });
            _hasIdol = true;
            Recalculate();
        });

        AddButton("▶  Apply Penalty", ApplyPenalty);

        Recalculate();
    }

    protected override void _Reset()
    {
        _deepestFloor = 10; _currentXp = 1000; _hasIdol = false; _inv = new();
        Recalculate();
    }

    private void Recalculate()
    {
        Log($"Floor {_deepestFloor} — XP {_currentXp}");
        Log($"  XP loss %:       {DeathPenalty.GetExpLossPercent(_deepestFloor):F1}%");
        Log($"  XP lost:         {DeathPenalty.CalculateXpLoss(_currentXp, _deepestFloor)}");
        Log($"  Items lost:      {DeathPenalty.GetItemsLost(_deepestFloor)}");
        Log($"  XP prot. cost:   {DeathPenalty.GetExpProtectionCost(_deepestFloor)}g");
        Log($"  Item prot. cost: {DeathPenalty.GetBackpackProtectionCost(_deepestFloor)}g");
        Log($"  Has idol:        {DeathPenalty.HasSacrificialIdol(_inv)}");
        Log($"  Inventory slots: {_inv.UsedSlots}");
        Log("");
    }

    private void ApplyPenalty()
    {
        Log("── Applying penalty ──");
        int xpBefore = _currentXp;
        int slotsBefore = _inv.UsedSlots;

        bool idolSaved = DeathPenalty.HasSacrificialIdol(_inv);
        if (idolSaved)
        {
            DeathPenalty.ConsumeSacrificialIdol(_inv);
            Log("  🏺 Idol consumed — items protected");
        }
        else
        {
            int toRemove = DeathPenalty.GetItemsLost(_deepestFloor);
            DeathPenalty.ApplyItemLoss(_inv, toRemove);
            Log($"  Items: {slotsBefore} → {_inv.UsedSlots}");
        }

        int xpLost = DeathPenalty.CalculateXpLoss(_currentXp, _deepestFloor);
        _currentXp -= xpLost;
        Log($"  XP: {xpBefore} → {_currentXp} (-{xpLost})");
        Log("");
    }

    protected override void RunHeadlessChecks()
    {
        Log("── Headless checks ──");

        Assert(DeathPenalty.GetExpLossPercent(125) == 50f, "Cap at floor 125 = 50%");
        Assert(DeathPenalty.GetExpLossPercent(1000) == 50f, "Cap holds above floor 125");
        Assert(DeathPenalty.CalculateXpLoss(1000, 10) == 40, "Floor 10: lose 40 of 1000 XP");
        Assert(DeathPenalty.GetItemsLost(10) == 2, "Floor 10: lose 2 items");
        Assert(DeathPenalty.GetExpProtectionCost(10) == 150, "XP prot cost floor 10 = 150g");
        Assert(DeathPenalty.GetBackpackProtectionCost(10) == 250, "Backpack prot cost floor 10 = 250g");

        var inv = new Inventory();
        var idol = new ItemDef { Id = "consumable_sacrificial_idol", Name = "Sacrificial Idol", Category = ItemCategory.Consumable };
        inv.TryAdd(idol);
        Assert(DeathPenalty.HasSacrificialIdol(inv), "Idol detected in inventory");
        DeathPenalty.ConsumeSacrificialIdol(inv);
        Assert(!DeathPenalty.HasSacrificialIdol(inv), "Idol removed after consumption");
        Assert(inv.UsedSlots == 0, "Inventory empty after idol consumed");

        var inv2 = new Inventory();
        for (int i = 0; i < 4; i++)
            inv2.TryAdd(new ItemDef { Id = $"item_{i}", Name = $"I{i}", Category = ItemCategory.Weapon });
        DeathPenalty.ApplyItemLoss(inv2, 2);
        Assert(inv2.UsedSlots == 2, $"4 items - 2 lost = 2 remaining (got {inv2.UsedSlots})");

        FinishHeadless();
    }
}
