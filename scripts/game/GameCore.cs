using System;
using System.Collections.Generic;

// ==================== ENUMS ====================

public enum ItemType { Weapon, Armor, Accessory, Consumable, Material }
public enum EquipSlot { MainHand, OffHand, Head, Body, Legs, Feet, Ring, None }
public enum ItemQuality { Normal, Superior, Elite }
public enum MonsterTier { Tier1 = 1, Tier2 = 2, Tier3 = 3 }
public enum TargetPriority { Nearest, Strongest, Tankiest, Weakest, Bosses }
public enum GameLocation { Town, Dungeon }
public enum StatusEffect { None, Poison }

// ==================== DATA MODELS ====================

public class PlayerState
{
    public string Name = "Hero";
    public int Level = 1;
    public int XP = 0;
    public int HP;
    public int MaxHP;
    public int MP;
    public int MaxMP;
    public int STR = 5;
    public int DEX = 5;
    public int INT = 5;
    public int VIT = 5;
    public int Gold = 100;
    public int StatPoints = 0;
    public int SkillPoints = 0;
    public Dictionary<EquipSlot, ItemData> Equipment = new();
    public List<ItemData> Inventory = new();
    public int InventorySize = 25;
    public int BackpackExpansions = 0;
    public bool IsDead = false;
    public StatusEffect Status = StatusEffect.None;
    public int PoisonTicksLeft = 0;
    public int PoisonDamagePerTick = 0;

    // XP curve: L^2 * 45 (integer math, no unnecessary double conversion)
    public int XPToNextLevel => Level * Level * 45;

    // Cached combat stats — recalculated on equipment change
    private int _cachedDamage;
    private int _cachedDefense;
    private bool _statsDirty = true;

    public void InvalidateStats() => _statsDirty = true;

    private void RecalcStats()
    {
        if (!_statsDirty) return;
        _cachedDamage = 12 + (int)(Level * 1.5f);
        if (Equipment.TryGetValue(EquipSlot.MainHand, out var weapon))
            _cachedDamage += weapon.Damage;
        _cachedDefense = 0;
        foreach (var item in Equipment.Values)
            _cachedDefense += item.Defense;
        _statsDirty = false;
    }

    public int TotalDamage { get { RecalcStats(); return _cachedDamage; } }
    public int TotalDefense { get { RecalcStats(); return _cachedDefense; } }

    public PlayerState()
    {
        MaxHP = 100 + Level * 8;
        HP = MaxHP;
        MaxMP = 50 + INT * 3;
        MP = MaxMP;
    }
}

public class ItemData
{
    public string Name;
    public ItemType Type;
    public EquipSlot Slot;
    public int Damage;
    public int Defense;
    public int HPBonus;
    public int MPBonus;
    public int Value;
    public bool Stackable;
    public int StackCount = 1;
    public string Description;
    public int ItemLevel;
    public ItemQuality Quality = ItemQuality.Normal;
    public List<AffixData> Prefixes = new();
    public List<AffixData> Suffixes = new();
    public WeaponType WeaponType = WeaponType.Unarmed;
}

public class MonsterData
{
    public string Name;
    public MonsterTier Tier;
    public int HP;
    public int MaxHP;
    public int Damage;
    public int Defense;
    public int XPReward;
    public int GoldReward;
    public bool IsDead;
    public bool CanPoison;
}

public class SkillData
{
    public string Name;
    public string Description;
    public int ManaCost;
    public int BaseDamage;
    public float Cooldown;
    public float CooldownRemaining;
    public int Level = 1;
}

public class GameSettings
{
    public TargetPriority TargetMode = TargetPriority.Nearest;
    public float MasterVolume = 0.8f;
    public bool ShowDamageNumbers = true;
    public bool ShowMinimap = true;
}

// ==================== GAME STATE (SINGLETON) ====================

public static class GameState
{
    public static PlayerState Player = new();
    public static GameSettings Settings = new();
    public static GameLocation Location = GameLocation.Town;
    public static int DungeonFloor = 0;
    public static List<MonsterData> ActiveMonsters = new();
    public static List<SkillData> PlayerSkills = new();

    public static void Reset()
    {
        Player = new PlayerState();
        Settings = new GameSettings();
        Location = GameLocation.Town;
        DungeonFloor = 0;
        ActiveMonsters.Clear();
        PlayerSkills.Clear();
    }
}

// ==================== GAME SYSTEMS ====================

public static class GameSystems
{
    private static readonly Random Rng = new();

    // ---------- COMBAT ----------

    public static (int damage, bool crit) AttackMonster(MonsterData target)
    {
        int baseDamage = GameState.Player.TotalDamage;

        // Get weapon type from equipped weapon (defaults to Unarmed)
        WeaponType weaponType = WeaponType.Unarmed;
        if (GameState.Player.Equipment.TryGetValue(EquipSlot.MainHand, out var weapon))
            weaponType = weapon.WeaponType;

        // Roll crit using the weapon's base crit chance (from CritSystem)
        var critResult = CritSystem.RollCrit(baseDamage, weaponType, Rng);
        int damage = critResult.FinalDamage;
        bool crit = critResult.IsCrit;

        // Apply target defense: diminishing returns DR formula
        float reduction = target.Defense * (100f / (target.Defense + 100f));
        damage = Math.Max(1, damage - (int)(damage * reduction / 100f));

        target.HP = Math.Max(0, target.HP - damage);
        if (target.HP <= 0) target.IsDead = true;

        return (damage, crit);
    }

    public static int MonsterAttackPlayer(MonsterData monster)
    {
        int damage = 3 + (int)monster.Tier;
        int def = GameState.Player.TotalDefense;
        float reduction = def * (100f / (def + 100f));
        damage = Math.Max(1, damage - (int)(damage * reduction / 100f));

        GameState.Player.HP = Math.Max(0, GameState.Player.HP - damage);
        if (GameState.Player.HP <= 0) GameState.Player.IsDead = true;

        return damage;
    }

    public static (int damage, bool success) UseSkill(SkillData skill, MonsterData target)
    {
        if (GameState.Player.MP < skill.ManaCost) return (0, false);
        if (skill.CooldownRemaining > 0) return (0, false);

        GameState.Player.MP -= skill.ManaCost;
        skill.CooldownRemaining = skill.Cooldown;

        int damage = skill.BaseDamage + GameState.Player.INT * 2;
        target.HP = Math.Max(0, target.HP - damage);
        if (target.HP <= 0) target.IsDead = true;

        return (damage, true);
    }

    // ---------- STATUS EFFECTS ----------

    public static void ApplyPoison(int damagePerTick, int ticks)
    {
        GameState.Player.Status = StatusEffect.Poison;
        GameState.Player.PoisonDamagePerTick = damagePerTick;
        GameState.Player.PoisonTicksLeft = ticks;
    }

    public static int TickPoison()
    {
        if (GameState.Player.Status != StatusEffect.Poison || GameState.Player.PoisonTicksLeft <= 0)
            return 0;

        int damage = GameState.Player.PoisonDamagePerTick;
        GameState.Player.HP = Math.Max(0, GameState.Player.HP - damage);
        GameState.Player.PoisonTicksLeft--;

        if (GameState.Player.PoisonTicksLeft <= 0)
            GameState.Player.Status = StatusEffect.None;
        if (GameState.Player.HP <= 0)
            GameState.Player.IsDead = true;

        return damage;
    }

    // ---------- INVENTORY ----------

    public static bool AddToInventory(ItemData item)
    {
        if (item.Stackable)
        {
            var existing = GameState.Player.Inventory.Find(i => i.Name == item.Name && i.Stackable);
            if (existing != null)
            {
                existing.StackCount += item.StackCount;
                return true;
            }
        }
        if (GameState.Player.Inventory.Count >= GameState.Player.InventorySize)
            return false;
        GameState.Player.Inventory.Add(item);
        return true;
    }

    public static (bool success, ItemData previous) EquipItem(ItemData item)
    {
        if (item.Slot == EquipSlot.None) return (false, null);

        ItemData previous = null;
        if (GameState.Player.Equipment.TryGetValue(item.Slot, out var current))
        {
            // Remove old stat bonuses
            if (current.HPBonus > 0) GameState.Player.MaxHP -= current.HPBonus;
            if (current.MPBonus > 0) GameState.Player.MaxMP -= current.MPBonus;
            previous = current;
            GameState.Player.Inventory.Add(current);
        }

        GameState.Player.Equipment[item.Slot] = item;
        GameState.Player.Inventory.Remove(item);

        // Apply new stat bonuses
        if (item.HPBonus > 0) GameState.Player.MaxHP += item.HPBonus;
        if (item.MPBonus > 0) GameState.Player.MaxMP += item.MPBonus;
        GameState.Player.InvalidateStats();

        return (true, previous);
    }

    public static bool UnequipItem(EquipSlot slot)
    {
        if (!GameState.Player.Equipment.ContainsKey(slot)) return false;
        if (GameState.Player.Inventory.Count >= GameState.Player.InventorySize) return false;

        var item = GameState.Player.Equipment[slot];
        if (item.HPBonus > 0) GameState.Player.MaxHP -= item.HPBonus;
        if (item.MPBonus > 0) GameState.Player.MaxMP -= item.MPBonus;
        GameState.Player.Inventory.Add(item);
        GameState.Player.Equipment.Remove(slot);
        GameState.Player.InvalidateStats();

        return true;
    }

    public static (bool success, string effect) UseItem(ItemData item)
    {
        if (item.Type != ItemType.Consumable) return (false, "Not a consumable");

        string effect = "";
        if (item.HPBonus > 0)
        {
            int healed = Math.Min(item.HPBonus, GameState.Player.MaxHP - GameState.Player.HP);
            GameState.Player.HP += healed;
            effect = $"Restored {healed} HP";
        }
        if (item.MPBonus > 0)
        {
            int restored = Math.Min(item.MPBonus, GameState.Player.MaxMP - GameState.Player.MP);
            GameState.Player.MP += restored;
            effect += (effect.Length > 0 ? ", " : "") + $"Restored {restored} MP";
        }

        item.StackCount--;
        if (item.StackCount <= 0)
            GameState.Player.Inventory.Remove(item);

        return (true, effect);
    }

    // ---------- SHOP ----------

    public static (bool success, string reason) BuyItem(ItemData shopItem, int quantity = 1)
    {
        int totalCost = shopItem.Value * quantity;
        if (GameState.Player.Gold < totalCost)
            return (false, $"Not enough gold (need {totalCost}, have {GameState.Player.Gold})");

        var bought = new ItemData
        {
            Name = shopItem.Name, Type = shopItem.Type, Slot = shopItem.Slot,
            Damage = shopItem.Damage, Defense = shopItem.Defense,
            HPBonus = shopItem.HPBonus, MPBonus = shopItem.MPBonus,
            Value = shopItem.Value, Stackable = shopItem.Stackable,
            StackCount = quantity, Description = shopItem.Description,
        };

        if (!AddToInventory(bought))
            return (false, "Inventory full");

        GameState.Player.Gold -= totalCost;
        return (true, $"Bought {quantity}x {shopItem.Name} for {totalCost}g");
    }

    public static (int gold, string result) SellItem(ItemData item)
    {
        int sellPrice = Math.Max(1, item.Value / 2) * item.StackCount;
        GameState.Player.Gold += sellPrice;
        GameState.Player.Inventory.Remove(item);
        return (sellPrice, $"Sold {item.Name} for {sellPrice}g");
    }

    // ---------- LEVELING ----------

    public static (bool leveled, int xpGained) GainXP(int amount)
    {
        GameState.Player.XP += amount;
        if (GameState.Player.XP >= GameState.Player.XPToNextLevel)
        {
            LevelUp();
            return (true, amount);
        }
        return (false, amount);
    }

    public static (int hpGain, int mpGain, int statPts, int skillPts) LevelUp()
    {
        GameState.Player.XP -= GameState.Player.XPToNextLevel;
        if (GameState.Player.XP < 0) GameState.Player.XP = 0;
        GameState.Player.Level++;
        GameState.Player.InvalidateStats();

        // HP increase: floor(8 + level * 0.5)
        int hpGain = (int)Math.Floor(8 + GameState.Player.Level * 0.5);
        GameState.Player.MaxHP += hpGain;
        int healAmount = (int)Math.Floor(GameState.Player.MaxHP * 0.15);
        GameState.Player.HP = Math.Min(GameState.Player.MaxHP, GameState.Player.HP + healAmount);

        int mpGain = 5;
        GameState.Player.MaxMP += mpGain;
        GameState.Player.MP = Math.Min(GameState.Player.MaxMP, GameState.Player.MP + 10);

        // Stat/skill points: 3 stat + 2 skill (bonus at milestones)
        int statPts = 3;
        int skillPts = 2;
        if (GameState.Player.Level % 10 == 0)
        {
            statPts += 2;
            skillPts += 1;
        }
        GameState.Player.StatPoints += statPts;
        GameState.Player.SkillPoints += skillPts;

        return (hpGain, mpGain, statPts, skillPts);
    }

    public static string AllocateStatPoint(string stat)
    {
        if (GameState.Player.StatPoints <= 0) return "No stat points available";
        GameState.Player.StatPoints--;
        switch (stat.ToUpperInvariant())
        {
            case "STR": GameState.Player.STR++; return $"STR -> {GameState.Player.STR}";
            case "DEX": GameState.Player.DEX++; return $"DEX -> {GameState.Player.DEX}";
            case "INT":
                GameState.Player.INT++;
                GameState.Player.MaxMP += 3;
                return $"INT -> {GameState.Player.INT} (+3 MaxMP)";
            case "VIT":
                GameState.Player.VIT++;
                GameState.Player.MaxHP += 3;
                return $"VIT -> {GameState.Player.VIT} (+3 MaxHP)";
            default: GameState.Player.StatPoints++; return "Invalid stat";
        }
    }

    // ---------- DUNGEON ----------

    public static void EnterDungeon()
    {
        GameState.Location = GameLocation.Dungeon;
        GameState.DungeonFloor = 1;
    }

    public static void ExitDungeon()
    {
        GameState.Location = GameLocation.Town;
        GameState.DungeonFloor = 0;
        GameState.ActiveMonsters.Clear();
    }

    public static MonsterData SpawnMonster(string name, MonsterTier tier, bool canPoison = false)
    {
        float floorMult = 1 + (GameState.DungeonFloor - 1) * 0.5f;
        int baseHP = tier switch { MonsterTier.Tier1 => 30, MonsterTier.Tier2 => 42, MonsterTier.Tier3 => 54, _ => 30 };
        int baseXP = tier switch { MonsterTier.Tier1 => 10, MonsterTier.Tier2 => 15, MonsterTier.Tier3 => 20, _ => 10 };

        var monster = new MonsterData
        {
            Name = name,
            Tier = tier,
            HP = (int)(baseHP * floorMult),
            MaxHP = (int)(baseHP * floorMult),
            Damage = 3 + (int)tier,
            Defense = (int)tier * 2,
            XPReward = (int)(baseXP * floorMult),
            GoldReward = (int)(5 * floorMult * (int)tier),
            CanPoison = canPoison,
        };
        GameState.ActiveMonsters.Add(monster);
        return monster;
    }

    // ---------- DEATH & RESPAWN ----------

    public static void PlayerDie()
    {
        GameState.Player.IsDead = true;
    }

    public static void PlayerRespawn()
    {
        GameState.Player.IsDead = false;
        GameState.Player.HP = GameState.Player.MaxHP / 2;
        GameState.Player.MP = GameState.Player.MaxMP / 2;
        GameState.Player.Status = StatusEffect.None;
        GameState.Player.PoisonTicksLeft = 0;
        GameState.Location = GameLocation.Town;
        GameState.DungeonFloor = 0;
        GameState.ActiveMonsters.Clear();
    }

    // ---------- SETTINGS ----------

    public static string ChangeTargetPriority(TargetPriority mode)
    {
        var old = GameState.Settings.TargetMode;
        GameState.Settings.TargetMode = mode;
        return $"Target priority: {old} -> {mode}";
    }

    // ---------- MANA REGEN ----------

    public static int RegenMana(int amount = 5)
    {
        int restored = Math.Min(amount, GameState.Player.MaxMP - GameState.Player.MP);
        GameState.Player.MP += restored;
        return restored;
    }

    // ---------- SAVE ----------

    public static Dictionary<string, string> SaveGame()
    {
        var p = GameState.Player;
        return new Dictionary<string, string>
        {
            ["name"] = p.Name,
            ["level"] = p.Level.ToString(),
            ["hp"] = $"{p.HP}/{p.MaxHP}",
            ["mp"] = $"{p.MP}/{p.MaxMP}",
            ["gold"] = p.Gold.ToString(),
            ["stats"] = $"STR:{p.STR} DEX:{p.DEX} INT:{p.INT} VIT:{p.VIT}",
            ["location"] = GameState.Location.ToString(),
            ["floor"] = GameState.DungeonFloor.ToString(),
            ["inventory"] = $"{p.Inventory.Count} items",
            ["equipment"] = $"{p.Equipment.Count} slots",
        };
    }

    // ---------- ITEM FACTORY ----------

    public static ItemData CreateItem(string name, ItemType type, EquipSlot slot,
        int damage = 0, int defense = 0, int hpBonus = 0, int mpBonus = 0,
        int value = 0, bool stackable = false, string desc = "")
    {
        return new ItemData
        {
            Name = name, Type = type, Slot = slot,
            Damage = damage, Defense = defense,
            HPBonus = hpBonus, MPBonus = mpBonus,
            Value = value, Stackable = stackable,
            StackCount = 1, Description = desc,
        };
    }
}
