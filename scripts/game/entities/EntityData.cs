using System;
using System.Collections.Generic;

public class EntityData
{
    // Identity
    public string Id;
    public string Name;
    public EntityType Type;
    public string SpriteSheet;
    public string[] SpriteLayers;

    // Vitals
    public int HP;
    public int MaxHP;
    public int MP;
    public int MaxMP;
    public bool IsDead;

    // Stats
    public int STR;
    public int DEX;
    public int INT;
    public int VIT;
    public int Level;

    // Combat
    public int BaseDamage;
    public int BaseDefense;
    public float AttackSpeed;
    public float AttackRange;
    public float HitboxRadius;

    // Movement
    public float MoveSpeed;

    // Effects
    public List<ActiveEffect> Effects = new();

    // Player-only
    public int XP;
    public int Gold;
    public int StatPoints;
    public int SkillPoints;
    public Dictionary<EquipSlot, ItemData> Equipment = new();
    public List<ItemData> Inventory = new();
    public int InventorySize;

    // Enemy-only
    public int XPReward;
    public int GoldReward;
    public int Tier;

    // XP curve: L^2 * 45
    public int XPToNextLevel => Level * Level * 45;

    // Cached combat stats — recalculated on equipment change
    private int _cachedDamage;
    private int _cachedDefense;
    private bool _statsDirty = true;

    public void InvalidateStats() => _statsDirty = true;

    private void RecalcStats()
    {
        if (!_statsDirty) return;
        _cachedDamage = BaseDamage;
        if (Equipment.TryGetValue(EquipSlot.MainHand, out var weapon))
            _cachedDamage += weapon.Damage;
        _cachedDefense = BaseDefense;
        foreach (var item in Equipment.Values)
            _cachedDefense += item.Defense;
        _statsDirty = false;
    }

    public int TotalDamage { get { RecalcStats(); return _cachedDamage; } }
    public int TotalDefense { get { RecalcStats(); return _cachedDefense; } }
}
