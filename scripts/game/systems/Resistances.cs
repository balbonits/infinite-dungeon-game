using System;

public class Resistances
{
    public int Fire { get; set; }
    public int Water { get; set; }
    public int Air { get; set; }
    public int Earth { get; set; }
    public int Light { get; set; }
    public int Dark { get; set; }

    public int GetResistance(DamageType type) => type switch
    {
        DamageType.Fire => Fire,
        DamageType.Water => Water,
        DamageType.Air => Air,
        DamageType.Earth => Earth,
        DamageType.Light => Light,
        DamageType.Dark => Dark,
        _ => 0 // Physical uses Defense, not resistance
    };

    public int GetEffective(DamageType type, int floorNumber)
    {
        int baseRes = GetResistance(type);
        int penalty = Math.Max(0, floorNumber) / 2;
        return Math.Max(baseRes - penalty, -100);
    }
}
