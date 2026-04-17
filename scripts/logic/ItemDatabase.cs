using System.Collections.Generic;

namespace DungeonGame;

/// <summary>
/// Central item registry — 259 base items per docs/inventory/item-catalog.md (ITEM-01).
///
/// Catalog structure (totals):
/// - Armor: 5 slots (Head/Body/Arms/Legs/Feet) × 5 tiers × 3 class voices = 75
/// - Main Hand weapons: Warrior 3 + Ranger 3 + Mage 2 archetypes × 5 tiers = 40
/// - Off Hand: Shield (10) + DefensiveMelee (10) + Spellbook (10) = 30
/// - Ammo / Quivers: 9 imbue types (no tiers — ammo is infinite) = 9
/// - Neck: 5 tiers × 3 focuses = 15
/// - Ring: 5 tiers × 8 focuses (4 stats + 4 combat) = 40
/// - Consumables: 28 (HP/MP potions, scrolls, bombs, food, bandages, antidotes,
///   teleport stones, Sacrificial Idol, elixirs)
/// - Materials: 15 tiered generic (ore/bone/hide × 5 tiers) + 7 signature = 22
///
/// Stats here are **baseline per tier** — item generation at drop time may override
/// via item-generation.md formulas and BaseQuality/affix rolls; see ItemGenerator.
/// </summary>
public static class ItemDatabase
{
    private static readonly Dictionary<string, ItemDef> Items = new();

    // ─── Tier-name ladders (from item-catalog.md) ────────────────────────

    private static readonly string[] WarriorAdj = { "Iron", "Steel", "Hard", "Super", "Mega" };
    private static readonly string[] RangerAdj = { "Rough", "Standard", "Fine", "Fancy", "Top-Shelf" };
    private static readonly string[] MageDescriptor = { "Novice", "Apprentice", "Adept", "Master", "Archmage" };
    private static readonly string[] MetalLadder = { "Iron", "Steel", "Mithril", "Orichalcum", "Dragonite" };

    static ItemDatabase()
    {
        RegisterArmor();
        RegisterMainHandWeapons();
        RegisterOffHand();
        RegisterQuivers();
        RegisterNeck();
        RegisterRings();
        RegisterConsumables();
        RegisterMaterials();
    }

    private static void Register(ItemDef item) => Items[item.Id] = item;

    public static ItemDef? Get(string id) => Items.GetValueOrDefault(id);

    public static IEnumerable<ItemDef> GetByCategory(ItemCategory category)
    {
        foreach (var item in Items.Values)
            if (item.Category == category) yield return item;
    }

    public static IEnumerable<ItemDef> GetByTier(int tier)
    {
        foreach (var item in Items.Values)
            if (item.Tier == tier) yield return item;
    }

    public static IEnumerable<ItemDef> All => Items.Values;

    // ─── Armor (75 items) ────────────────────────────────────────────────
    // Head / Body / Arms / Legs / Feet × 5 tiers × 3 class voices.
    // Stat payload is shared across class-voice variants at the same (slot, tier).

    private static void RegisterArmor()
    {
        // Head: Helmet / Hood / Crown (Mage names are long, see MageHeadName).
        for (int t = 1; t <= 5; t++)
        {
            int ts = HeadSta(t);
            Register(Armor($"head_warrior_helmet_t{t}", $"{WarriorAdj[t - 1]} Helmet",
                ItemCategory.Head, EquipSlot.Head, t, PlayerClass.Warrior, sta: ts));
            Register(Armor($"head_ranger_hood_t{t}", $"{RangerAdj[t - 1]} Hood",
                ItemCategory.Head, EquipSlot.Head, t, PlayerClass.Ranger, sta: ts));
            Register(Armor($"head_mage_crown_t{t}", MageHeadName(t),
                ItemCategory.Head, EquipSlot.Head, t, PlayerClass.Mage, sta: ts));
        }

        // Body (primary armor slot — extra Sta + bonus HP).
        for (int t = 1; t <= 5; t++)
        {
            int ts = BodySta(t);
            int hp = BodyHp(t);
            Register(Armor($"body_warrior_armor_t{t}", $"{WarriorAdj[t - 1]} Armor",
                ItemCategory.Body, EquipSlot.Body, t, PlayerClass.Warrior, sta: ts, hp: hp));
            Register(Armor($"body_ranger_vest_t{t}", $"{RangerAdj[t - 1]} Vest",
                ItemCategory.Body, EquipSlot.Body, t, PlayerClass.Ranger, sta: ts, hp: hp));
            Register(Armor($"body_mage_robe_t{t}", MageBodyName(t),
                ItemCategory.Body, EquipSlot.Body, t, PlayerClass.Mage, sta: ts, hp: hp));
        }

        // Arms.
        for (int t = 1; t <= 5; t++)
        {
            int ts = ArmsSta(t);
            Register(Armor($"arms_warrior_gauntlets_t{t}", $"{WarriorAdj[t - 1]} Gauntlets",
                ItemCategory.Arms, EquipSlot.Arms, t, PlayerClass.Warrior, sta: ts));
            Register(Armor($"arms_ranger_braces_t{t}", $"{RangerAdj[t - 1]} Braces",
                ItemCategory.Arms, EquipSlot.Arms, t, PlayerClass.Ranger, sta: ts));
            Register(Armor($"arms_mage_bangles_t{t}", MageArmsName(t),
                ItemCategory.Arms, EquipSlot.Arms, t, PlayerClass.Mage, sta: ts));
        }

        // Legs.
        for (int t = 1; t <= 5; t++)
        {
            int ts = LegsSta(t);
            Register(Armor($"legs_warrior_greaves_t{t}", $"{WarriorAdj[t - 1]} Greaves",
                ItemCategory.Legs, EquipSlot.Legs, t, PlayerClass.Warrior, sta: ts));
            Register(Armor($"legs_ranger_breeches_t{t}", $"{RangerAdj[t - 1]} Breeches",
                ItemCategory.Legs, EquipSlot.Legs, t, PlayerClass.Ranger, sta: ts));
            Register(Armor($"legs_mage_leggings_t{t}", MageLegsName(t),
                ItemCategory.Legs, EquipSlot.Legs, t, PlayerClass.Mage, sta: ts));
        }

        // Feet (light armor slot — DEX instead of STA for Ranger-flavored movement speed flavor).
        for (int t = 1; t <= 5; t++)
        {
            int td = FeetDex(t);
            Register(Armor($"feet_warrior_boots_t{t}", $"{WarriorAdj[t - 1]} Boots",
                ItemCategory.Feet, EquipSlot.Feet, t, PlayerClass.Warrior, dex: td));
            Register(Armor($"feet_ranger_shoes_t{t}", $"{RangerAdj[t - 1]} Shoes",
                ItemCategory.Feet, EquipSlot.Feet, t, PlayerClass.Ranger, dex: td));
            Register(Armor($"feet_mage_sandals_t{t}", MageFeetName(t),
                ItemCategory.Feet, EquipSlot.Feet, t, PlayerClass.Mage, dex: td));
        }
    }

    private static ItemDef Armor(string id, string name, ItemCategory cat, EquipSlot slot,
        int tier, PlayerClass affinity, int str = 0, int dex = 0, int sta = 0, int @int = 0, int hp = 0) => new()
        {
            Id = id,
            Name = name,
            Description = $"Tier-{tier} {cat.ToString().ToLowerInvariant()} gear.",
            Category = cat,
            Slot = slot,
            Tier = tier,
            ClassAffinity = affinity,
            BuyPrice = TierBuyPrice(tier),
            SellPrice = TierSellPrice(tier),
            LevelRequirement = TierLevelReq(tier),
            BonusStr = str,
            BonusDex = dex,
            BonusSta = sta,
            BonusInt = @int,
            BonusHp = hp,
        };

    private static int HeadSta(int t) => t;                // 1..5
    private static int BodySta(int t) => t + 1;            // 2..6
    private static int BodyHp(int t) => t * 5;             // 5..25
    private static int ArmsSta(int t) => t;                // 1..5
    private static int LegsSta(int t) => t;                // 1..5
    private static int FeetDex(int t) => t;                // 1..5

    private static string MageHeadName(int t) => t switch
    {
        1 => "The Novice's Coronet of Preliminary Warding",
        2 => "The Apprentice's Diadem of Moderate Arcane Protection",
        3 => "The Adept's Circlet of Greater Warding",
        4 => "The Master's Crown of Supreme Ward",
        5 => "The Archmage's Diadem of Absolute Theurgic Supremacy",
        _ => "Coronet",
    };
    private static string MageBodyName(int t) => t switch
    {
        1 => "Novice Robes of Basic Magery",
        2 => "Apprentice Vestments of Intermediate Study",
        3 => "Adept's Mantle of Advanced Theurgy",
        4 => "Master's Garb of Superior Magical Conduction",
        5 => "Archmage's Robes of Transcendent Theurgism",
        _ => "Robe",
    };
    private static string MageArmsName(int t) => t switch
    {
        1 => "Novice Bangles of Minor Focus",
        2 => "Apprentice Bangles of Steady Incantation",
        3 => "Adept's Bangles of Disciplined Channeling",
        4 => "Master's Bangles of Superior Gestural Arcana",
        5 => "Archmage's Bangles of Transcendent Somatic Command",
        _ => "Bangles",
    };
    private static string MageLegsName(int t) => t switch
    {
        1 => "Novice Leggings of Basic Ambulatory Warding",
        2 => "Apprentice Leggings of Moderate Lower Theurgy",
        3 => "Adept's Leggings of Greater Peripatetic Ward",
        4 => "Master's Leggings of Superior Locomotive Arcana",
        5 => "Archmage's Leggings of Transcendent Ambulatory Supremacy",
        _ => "Leggings",
    };
    private static string MageFeetName(int t) => t switch
    {
        1 => "Novice Sandals of Quiet Tread",
        2 => "Apprentice Sandals of Steady Gait",
        3 => "Adept's Sandals of Disciplined Pace",
        4 => "Master's Sandals of Superior Ambulation",
        5 => "Archmage's Sandals of Transcendent Peripateia",
        _ => "Sandals",
    };

    // ─── Main Hand weapons (40 items) ────────────────────────────────────

    private static void RegisterMainHandWeapons()
    {
        // Warrior (3 archetypes × 5 tiers): sword, axe, hammer.
        string[] warriorSword = { "Sharp Sword", "Sharper Sword", "Big Sword", "Super Sword", "Mega Sword" };
        string[] warriorAxe = { "Chopper", "Sharp Chopper", "Big Chopper", "Super Chopper", "Mega Chopper" };
        string[] warriorHammer = { "Smasher", "Hard Smasher", "Big Smasher", "Super Smasher", "Mega Smasher" };
        for (int t = 1; t <= 5; t++)
        {
            Register(Weapon($"mainhand_warrior_sword_t{t}", warriorSword[t - 1], t, PlayerClass.Warrior, dmg: WeaponDmg(t), str: 1));
            Register(Weapon($"mainhand_warrior_axe_t{t}", warriorAxe[t - 1], t, PlayerClass.Warrior, dmg: WeaponDmg(t) + 1, str: 1));
            Register(Weapon($"mainhand_warrior_hammer_t{t}", warriorHammer[t - 1], t, PlayerClass.Warrior, dmg: WeaponDmg(t) + 2, str: 2));
        }

        // Ranger (3 archetypes × 5 tiers): short bow, long bow, crossbow.
        string[] shortie = { "Shortie", "Solid Shortie", "Quality Shortie", "Mean Shortie", "Top-Shelf Shortie" };
        string[] longer = { "Longer", "Solid Longer", "Quality Longer", "Mean Longer", "Top-Shelf Longer" };
        string[] crank = { "Crank", "Solid Crank", "Quality Crank", "Mean Crank", "Top-Shelf Crank" };
        for (int t = 1; t <= 5; t++)
        {
            Register(Weapon($"mainhand_ranger_shortbow_t{t}", shortie[t - 1], t, PlayerClass.Ranger, dmg: WeaponDmg(t), dex: 1));
            Register(Weapon($"mainhand_ranger_longbow_t{t}", longer[t - 1], t, PlayerClass.Ranger, dmg: WeaponDmg(t) + 1, dex: 1));
            Register(Weapon($"mainhand_ranger_crossbow_t{t}", crank[t - 1], t, PlayerClass.Ranger, dmg: WeaponDmg(t) + 2, dex: 2));
        }

        // Mage (2 archetypes × 5 tiers): staff, wand.
        for (int t = 1; t <= 5; t++)
        {
            Register(Weapon($"mainhand_mage_staff_t{t}", MageStaffName(t), t, PlayerClass.Mage, dmg: WeaponDmg(t), @int: 2));
            Register(Weapon($"mainhand_mage_wand_t{t}", MageWandName(t), t, PlayerClass.Mage, dmg: WeaponDmg(t) - 1, @int: 3));
        }
    }

    private static ItemDef Weapon(string id, string name, int tier, PlayerClass affinity,
        int dmg = 0, int str = 0, int dex = 0, int @int = 0) => new()
        {
            Id = id,
            Name = name,
            Description = $"Tier-{tier} main-hand weapon.",
            Category = ItemCategory.Weapon,
            Slot = EquipSlot.MainHand,
            Tier = tier,
            ClassAffinity = affinity,
            BuyPrice = TierBuyPrice(tier),
            SellPrice = TierSellPrice(tier),
            LevelRequirement = TierLevelReq(tier),
            BonusDamage = dmg,
            BonusStr = str,
            BonusDex = dex,
            BonusInt = @int,
        };

    private static int WeaponDmg(int t) => 2 + t * 2;       // 4/6/8/10/12

    private static string MageStaffName(int t) => t switch
    {
        1 => "Novice Staff of Beginner Channeling",
        2 => "Apprentice Staff of Moderate Incantation",
        3 => "Adept's Staff of Focused Arcana",
        4 => "Master's Implement of Superior Arcane Transmission",
        5 => "Archmage's Staff of Transcendent Theurgy",
        _ => "Staff",
    };
    private static string MageWandName(int t) => t switch
    {
        1 => "Novice Wand of Minor Targeting",
        2 => "Apprentice Rod of Steady Aim",
        3 => "Adept's Rod of Precise Channeling",
        4 => "Master's Rod of Superior Arcane Targeting",
        5 => "Archmage's Rod of Transcendent Focus",
        _ => "Wand",
    };

    // ─── Off Hand (30 items) ─────────────────────────────────────────────

    private static void RegisterOffHand()
    {
        // Warrior shields: Small shields + Tower shields × 5 tiers = 10.
        string[] smallShield = { "Bash Shield", "Strong Bash Shield", "Big Bash Shield", "Super Bash Shield", "Mega Bash Shield" };
        string[] towerShield = { "Big Shield", "Bigger Shield", "Huge Shield", "Super Shield", "Mega Shield" };
        for (int t = 1; t <= 5; t++)
        {
            Register(OffHand($"offhand_warrior_shield_small_t{t}", smallShield[t - 1],
                ItemCategory.Shield, t, PlayerClass.Warrior, sta: t));
            Register(OffHand($"offhand_warrior_shield_tower_t{t}", towerShield[t - 1],
                ItemCategory.Shield, t, PlayerClass.Warrior, sta: t + 1, hp: t * 5));
        }

        // Ranger defensive melee: Knives + Claws × 5 tiers = 10.
        string[] knife = { "Stabby", "Solid Stabby", "Quality Stabby", "Mean Stabby", "Top-Shelf Stabby" };
        string[] claw = { "Puncher", "Solid Puncher", "Quality Puncher", "Mean Puncher", "Top-Shelf Puncher" };
        for (int t = 1; t <= 5; t++)
        {
            Register(OffHand($"offhand_ranger_knife_t{t}", knife[t - 1],
                ItemCategory.DefensiveMelee, t, PlayerClass.Ranger, dex: t, dmg: t));
            Register(OffHand($"offhand_ranger_claw_t{t}", claw[t - 1],
                ItemCategory.DefensiveMelee, t, PlayerClass.Ranger, dex: t + 1, dmg: t));
        }

        // Mage spellbooks: Grimoires + Codices × 5 tiers = 10.
        for (int t = 1; t <= 5; t++)
        {
            Register(OffHand($"offhand_mage_grimoire_t{t}", MageGrimoireName(t),
                ItemCategory.Spellbook, t, PlayerClass.Mage, @int: t + 1));
            Register(OffHand($"offhand_mage_codex_t{t}", MageCodexName(t),
                ItemCategory.Spellbook, t, PlayerClass.Mage, @int: t + 2));
        }
    }

    private static ItemDef OffHand(string id, string name, ItemCategory cat, int tier, PlayerClass affinity,
        int str = 0, int dex = 0, int sta = 0, int @int = 0, int hp = 0, int dmg = 0) => new()
        {
            Id = id,
            Name = name,
            Description = $"Tier-{tier} off-hand item.",
            Category = cat,
            Slot = EquipSlot.OffHand,
            Tier = tier,
            ClassAffinity = affinity,
            BuyPrice = TierBuyPrice(tier),
            SellPrice = TierSellPrice(tier),
            LevelRequirement = TierLevelReq(tier),
            BonusStr = str,
            BonusDex = dex,
            BonusSta = sta,
            BonusInt = @int,
            BonusHp = hp,
            BonusDamage = dmg,
        };

    private static string MageGrimoireName(int t) => t switch
    {
        1 => "Lesser Grimoire of Foundational Theurgies",
        2 => "Grimoire of Moderate Arcana",
        3 => "Grimoire of Advanced Theurgy",
        4 => "Master's Grimoire of Superior Theurgy",
        5 => "Archmage's Grimoire of Transcendent Theurgy",
        _ => "Grimoire",
    };
    private static string MageCodexName(int t) => t switch
    {
        1 => "Tome of Lesser Illumination of Great Knowledge",   // Intentional malaprop per catalog.
        2 => "Codex of Moderate Illumination",
        3 => "Codex of Advanced Arcana",
        4 => "Master's Treatise on Supreme Thaumaturgy",
        5 => "The Archmage's Supreme Compendium of Transcendent Theurgy",
        _ => "Codex",
    };

    // ─── Quivers (9 items, no tiers) ─────────────────────────────────────

    private static void RegisterQuivers()
    {
        Register(Quiver("ammo_quiver_basic", "Basic Quiver", "", 1.0f, "No element. Plain arrows."));
        Register(Quiver("ammo_quiver_hot", "Hot Quiver", "fire", 1.2f, "Burn damage over time."));
        Register(Quiver("ammo_quiver_cold", "Cold Quiver", "frost", 1.0f, "Slow on hit."));
        Register(Quiver("ammo_quiver_heavy", "Heavy Quiver", "stun", 1.1f, "Heavy impact staggers enemies."));
        Register(Quiver("ammo_quiver_nasty", "Nasty Quiver", "poison", 1.0f, "Poison DoT."));
        Register(Quiver("ammo_quiver_zap", "Zap Quiver", "lightning", 1.1f, "Chain to nearby enemies."));
        Register(Quiver("ammo_quiver_quiet", "Quiet Quiver", "shadow", 1.0f, "Stealth bonus + shadow damage."));
        Register(Quiver("ammo_quiver_sharp", "Sharp Quiver", "bleed", 1.1f, "Physical DoT on crit."));
        Register(Quiver("ammo_quiver_bright", "Bright Quiver", "holy", 1.0f, "Extra damage vs undead. Heals on hit vs undead."));
    }

    private static ItemDef Quiver(string id, string name, string element, float projMult, string desc) => new()
    {
        Id = id,
        Name = name,
        Description = desc,
        Category = ItemCategory.Quiver,
        Slot = EquipSlot.Ammo,
        Tier = 0,
        ClassAffinity = PlayerClass.Ranger,
        BuyPrice = 80,
        SellPrice = 32,
        LevelRequirement = 1,
        Element = element,
        ProjectileDamageMultiplier = projMult,
    };

    // ─── Neck (15 items) ─────────────────────────────────────────────────

    private static void RegisterNeck()
    {
        string[] focus = { "offense", "defense", "utility" };
        string[] focusName = { "Might", "Warding", "Fortune" };
        string[] focusDesc = { "STR / damage", "Defense", "Gold find" };
        for (int t = 1; t <= 5; t++)
        {
            for (int f = 0; f < 3; f++)
            {
                string id = $"neck_t{t}_{focus[f]}";
                string name = $"{MetalLadder[t - 1]} Chain of {focusName[f]}";
                int str = f == 0 ? t : 0;
                int sta = f == 1 ? t : 0;
                int dmg = f == 0 ? t : 0;
                Register(new ItemDef
                {
                    Id = id,
                    Name = name,
                    Description = focusDesc[f],
                    Category = ItemCategory.Neck,
                    Slot = EquipSlot.Neck,
                    Tier = t,
                    ClassAffinity = null,
                    BuyPrice = TierBuyPrice(t),
                    SellPrice = TierSellPrice(t),
                    LevelRequirement = TierLevelReq(t),
                    BonusStr = str,
                    BonusSta = sta,
                    BonusDamage = dmg,
                });
            }
        }
    }

    // ─── Rings (40 items) ────────────────────────────────────────────────

    private static void RegisterRings()
    {
        // 4 core stats + 4 combat focuses × 5 tiers = 40.
        string[] focus = { "str", "dex", "sta", "int", "crit", "haste", "dodge", "block" };
        string[] focusName = { "Strength", "Dexterity", "Vigor", "Intellect", "Precision", "Haste", "Evasion", "Bulwark" };
        string[] focusDesc = {
            "STR", "DEX", "STA", "INT",
            "Crit chance", "Attack speed", "Dodge chance", "Block chance"
        };
        for (int t = 1; t <= 5; t++)
        {
            for (int f = 0; f < 8; f++)
            {
                string id = $"ring_t{t}_{focus[f]}";
                string name = $"{MetalLadder[t - 1]} Ring of {focusName[f]}";
                var def = new ItemDef
                {
                    Id = id,
                    Name = name,
                    Description = focusDesc[f],
                    Category = ItemCategory.Ring,
                    Slot = EquipSlot.Ring,
                    Tier = t,
                    ClassAffinity = null,
                    BuyPrice = TierBuyPrice(t),
                    SellPrice = TierSellPrice(t),
                    LevelRequirement = TierLevelReq(t),
                    BonusStr = f == 0 ? t : 0,
                    BonusDex = f == 1 ? t : 0,
                    BonusSta = f == 2 ? t : 0,
                    BonusInt = f == 3 ? t : 0,
                    // Combat focuses (crit/haste/dodge/block) currently carry no ItemDef
                    // hook — they'll land with COMBAT-01 spec. Stats are 0 for now.
                };
                Register(def);
            }
        }
    }

    // ─── Consumables (28 items) ──────────────────────────────────────────

    private static void RegisterConsumables()
    {
        // HP potions (4).
        Register(Consumable("consumable_hp_small", "Small Health Potion", "Restores 30 HP.", 25, 10, heal: 30));
        Register(Consumable("consumable_hp_medium", "Medium Health Potion", "Restores 80 HP.", 75, 30, heal: 80));
        Register(Consumable("consumable_hp_large", "Large Health Potion", "Restores 180 HP.", 150, 60, heal: 180));
        Register(Consumable("consumable_hp_greater", "Greater Health Potion", "Restores 400 HP.", 350, 140, heal: 400));

        // MP potions (4).
        Register(Consumable("consumable_mp_small", "Small Mana Potion", "Restores 20 MP.", 25, 10, mana: 20));
        Register(Consumable("consumable_mp_medium", "Medium Mana Potion", "Restores 60 MP.", 75, 30, mana: 60));
        Register(Consumable("consumable_mp_large", "Large Mana Potion", "Restores 140 MP.", 150, 60, mana: 140));
        Register(Consumable("consumable_mp_greater", "Greater Mana Potion", "Restores 320 MP.", 350, 140, mana: 320));

        // Buff scrolls (5).
        Register(Consumable("consumable_scroll_might", "Scroll of Might", "+20% physical damage for 120s.", 100, 40));
        Register(Consumable("consumable_scroll_focus", "Scroll of Focus", "+20% spell damage for 120s.", 100, 40));
        Register(Consumable("consumable_scroll_warding", "Scroll of Warding", "+25% defense for 120s.", 100, 40));
        Register(Consumable("consumable_scroll_haste", "Scroll of Haste", "+20% move + attack speed for 120s.", 120, 48));
        Register(Consumable("consumable_scroll_sight", "Scroll of Sight", "Reveals stairs on map for 120s.", 80, 32));

        // Elemental bombs (3).
        Register(Consumable("consumable_bomb_fire", "Fire Bomb", "AoE fire damage + burn (itemLevel-scaled).", 150, 60));
        Register(Consumable("consumable_bomb_shock", "Shock Bomb", "AoE shock damage + stun (itemLevel-scaled).", 150, 60));
        Register(Consumable("consumable_bomb_frost", "Frost Bomb", "AoE frost damage + freeze (itemLevel-scaled).", 120, 48));

        // Food (3).
        Register(Consumable("consumable_food_bread", "Traveler's Bread", "+5 HP/s for 30s.", 25, 10));
        Register(Consumable("consumable_food_stew", "Hearty Stew", "+12 HP/s for 30s.", 80, 32));
        Register(Consumable("consumable_food_feast", "Guild Feast", "+25 HP/s and +5 MP/s for 30s.", 200, 80));

        // Bandages (2).
        Register(Consumable("consumable_bandage_rough", "Rough Bandage", "Restore 50 HP over 3-second channel.", 40, 16));
        Register(Consumable("consumable_bandage_fine", "Fine Bandage", "Restore 150 HP over 3-second channel.", 120, 48));

        // Antidotes (2).
        Register(Consumable("consumable_antidote_small", "Small Antidote", "Cleanse poison DoT.", 60, 24));
        Register(Consumable("consumable_antidote_strong", "Strong Antidote", "Cleanse poison + immunity for 60s.", 180, 72));

        // Teleport stones (2).
        Register(Consumable("consumable_teleport_town", "Town Teleport Stone", "Return to town instantly.", 200, 80));
        Register(Consumable("consumable_teleport_dungeon", "Dungeon Teleport Stone", "Return to deepest floor reached.", 250, 100));

        // Sacrificial Idol (1).
        Register(Consumable("consumable_sacrificial_idol", "Sacrificial Idol",
            "Negates backpack + equipment loss on death. Consumed on use.", 200, 80));

        // Elixirs (2).
        Register(Consumable("consumable_elixir_xp", "Elixir of Insight", "+25% XP gain for 300s.", 300, 120));
        Register(Consumable("consumable_elixir_luck", "Elixir of Fortune", "+25% drop rate for 300s.", 300, 120));
    }

    private static ItemDef Consumable(string id, string name, string desc, int buy, int sell,
        int heal = 0, int mana = 0) => new()
        {
            Id = id,
            Name = name,
            Description = desc,
            Category = ItemCategory.Consumable,
            Slot = EquipSlot.None,
            Tier = 0,
            BuyPrice = buy,
            SellPrice = sell,
            HealAmount = heal,
            ManaAmount = mana,
        };

    // ─── Materials (22 items) ────────────────────────────────────────────

    private static void RegisterMaterials()
    {
        // Tiered generic: Ore × 5, Bone × 5, Hide × 5 = 15.
        string[] oreNames = { "Iron Ore", "Steel Ingot", "Mithril Ore", "Orichalcum Ore", "Dragonite Ore" };
        string[] boneNames = { "Rough Bone", "Standard Bone", "Fine Bone", "Masterwork Bone", "Top-Shelf Bone" };
        string[] hideNames = { "Rough Hide", "Standard Hide", "Fine Hide", "Masterwork Hide", "Top-Shelf Hide" };
        for (int t = 1; t <= 5; t++)
        {
            Register(Material($"material_ore_t{t}", oreNames[t - 1], t, "Smeltable ore. Used for metal-focused affixes."));
            Register(Material($"material_bone_t{t}", boneNames[t - 1], t, "Bone fragment. Used for bone-focused affixes."));
            Register(Material($"material_hide_t{t}", hideNames[t - 1], t, "Treated hide. Used for armor-focused affixes."));
        }

        // Species-signature: 7 (per docs/systems/monster-drops.md § Species table).
        Register(Material("material_sig_skeleton", "Bone Dust", 0, "Finely-ground bone; primary reagent for necro-adjacent Mage affixes."));
        Register(Material("material_sig_goblin", "Goblin Tooth", 0, "Crooked, tough; used for piercing/crit affixes."));
        Register(Material("material_sig_bat", "Echo Shard", 0, "Crystalline resonance; used for sound / stealth quiver imbues."));
        Register(Material("material_sig_wolf", "Wolf Pelt", 0, "Pristine hide; used for movement-speed armor affixes."));
        Register(Material("material_sig_orc", "Orc Tusk", 0, "Dense ivory; used for heavy-damage / bash affixes."));
        Register(Material("material_sig_darkmage", "Arcane Residue", 0, "Raw magical sludge; used for elemental-damage affixes."));
        Register(Material("material_sig_spider", "Chitin Fragment", 0, "Brittle-but-sharp shell; used for poison / bleed affixes."));
    }

    private static ItemDef Material(string id, string name, int tier, string desc) => new()
    {
        Id = id,
        Name = name,
        Description = desc,
        Category = ItemCategory.Material,
        Slot = EquipSlot.None,
        Tier = tier,
        BuyPrice = 0,
        SellPrice = 5 + tier * 5,
    };

    // ─── Tier economics (shared by armor/weapon/offhand/neck/ring) ───────

    private static int TierBuyPrice(int t) => 60 * t * t;                 // 60/240/540/960/1500
    private static int TierSellPrice(int t) => (60 * t * t) * 4 / 10;     // 40% buy
    private static int TierLevelReq(int t) => t switch
    {
        1 => 1,
        2 => 10,
        3 => 25,
        4 => 50,
        5 => 100,
        _ => 1,
    };
}
