using System.Collections.Generic;

public class BankData
{
    public const int StartingSlots = 50;
    public const int SlotsPerExpansion = 10;
    public const int BaseCostMultiplier = 500;

    public List<ItemData> Items { get; } = new();
    public int MaxSlots { get; set; } = StartingSlots;
    public int ExpansionCount { get; set; } = 0;
}
