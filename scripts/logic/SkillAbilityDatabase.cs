using System.Collections.Generic;
using System.Linq;

namespace DungeonGame;

/// <summary>
/// Central registry of all mastery and ability definitions.
/// All data from docs/systems/skills.md.
/// Pure data — no Godot dependency.
/// </summary>
public static class SkillAbilityDatabase
{
    private static readonly Dictionary<string, MasteryDef> Masteries = new();
    private static readonly Dictionary<string, AbilityDef> Abilities = new();
    private static readonly Dictionary<string, List<string>> CategoryMasteries = new();
    private static readonly Dictionary<string, List<string>> MasteryAbilities = new();

    static SkillAbilityDatabase()
    {
        RegisterInnate();
        RegisterWarrior();
        RegisterRanger();
        RegisterMage();
    }

    // --- Query: Masteries ---

    public static MasteryDef? GetMastery(string id) => Masteries.GetValueOrDefault(id);

    public static IEnumerable<MasteryDef> GetMasteriesByClass(PlayerClass cls) =>
        Masteries.Values.Where(m => m.Class == cls);

    public static IEnumerable<MasteryDef> GetMasteriesInCategory(string categoryId) =>
        CategoryMasteries.TryGetValue(categoryId, out var ids) ? ids.Select(id => Masteries[id]) : [];

    // --- Query: Abilities ---

    public static AbilityDef? GetAbility(string id) => Abilities.GetValueOrDefault(id);

    public static IEnumerable<AbilityDef> GetAbilitiesByClass(PlayerClass cls) =>
        Abilities.Values.Where(a => a.Class == cls);

    public static IEnumerable<AbilityDef> GetAbilitiesForMastery(string masteryId) =>
        MasteryAbilities.TryGetValue(masteryId, out var ids) ? ids.Select(id => Abilities[id]) : [];

    // --- Query: Categories ---

    public static string[] GetCategories(PlayerClass cls) => cls switch
    {
        PlayerClass.Warrior => ["warrior_body", "warrior_mind"],
        PlayerClass.Ranger => ["ranger_weaponry", "ranger_survival"],
        PlayerClass.Mage => ["mage_elemental", "mage_aether", "mage_attunement"],
        _ => [],
    };

    public static string GetCategoryName(string categoryId) => categoryId switch
    {
        "warrior_body" => "Body",
        "warrior_mind" => "Mind",
        "ranger_weaponry" => "Weaponry",
        "ranger_survival" => "Survival",
        "mage_elemental" => "Elemental",
        "mage_aether" => "Aether",
        "mage_attunement" => "Attunement",
        "innate" => "Innate",
        _ => categoryId,
    };

    /// <summary>Get all innate masteries (available to all classes).</summary>
    public static IEnumerable<MasteryDef> GetInnateMasteries() =>
        GetMasteriesInCategory("innate");

    /// <summary>Total mastery count across all classes + innate.</summary>
    public static int MasteryCount => Masteries.Count;

    /// <summary>Total ability count across all classes.</summary>
    public static int AbilityCount => Abilities.Count;

    // --- Registration Helpers ---

    private static void RegisterMastery(string id, string name, string desc, string categoryId,
        PlayerClass cls, PassiveBonusType passiveType, float multiplier, int baseXp = 5)
    {
        Masteries[id] = new MasteryDef
        {
            Id = id,
            Name = name,
            Description = desc,
            CategoryId = categoryId,
            Class = cls,
            PassiveType = passiveType,
            PassiveMultiplier = multiplier,
            BaseXpPerUse = baseXp,
        };
        if (!CategoryMasteries.ContainsKey(categoryId))
            CategoryMasteries[categoryId] = new List<string>();
        CategoryMasteries[categoryId].Add(id);
    }

    private static void RegisterAbility(string id, string name, string desc,
        string parentMasteryId, PlayerClass cls, int manaCost = 0, float cooldown = 2.0f,
        int baseXp = 10, AttackConfig? combat = null)
    {
        Abilities[id] = new AbilityDef
        {
            Id = id,
            Name = name,
            Description = desc,
            ParentMasteryId = parentMasteryId,
            CategoryId = Masteries[parentMasteryId].CategoryId,
            Class = cls,
            ManaCost = manaCost,
            Cooldown = cooldown,
            BaseXpPerUse = baseXp,
            CombatConfig = combat,
        };
        if (!MasteryAbilities.ContainsKey(parentMasteryId))
            MasteryAbilities[parentMasteryId] = new List<string>();
        MasteryAbilities[parentMasteryId].Add(id);
    }

    // ═══════════════════════════════════════════════════════════════════
    // INNATE (4 masteries, available to all classes)
    // ═══════════════════════════════════════════════════════════════════

    private static void RegisterInnate()
    {
        const string Cat = "innate";
        // Innate use PlayerClass.Warrior as placeholder — accessed via GetInnateMasteries()
        const PlayerClass C = PlayerClass.Warrior;

        RegisterMastery("innate_haste", "Haste", "Magicule-enhanced burst of speed + dodge chance", Cat, C, PassiveBonusType.Chance, 0.5f, 2);
        RegisterMastery("innate_sense", "Sense", "Magicule-enhanced perception, detect through walls", Cat, C, PassiveBonusType.Regen, 0.3f, 2);
        RegisterMastery("innate_fortify", "Fortify", "Magicule-reinforced body, damage resistance", Cat, C, PassiveBonusType.Defense, 1.2f, 2);
        RegisterMastery("innate_armor", "Armor", "Armor proficiency, class-specific equipment mastery", Cat, C, PassiveBonusType.Defense, 1.2f, 2);
    }

    // ═══════════════════════════════════════════════════════════════════
    // WARRIOR (8 masteries, 33 abilities)
    // ═══════════════════════════════════════════════════════════════════

    private static void RegisterWarrior()
    {
        const PlayerClass W = PlayerClass.Warrior;

        // ── Body ──────────────────────────────────────────────────────

        // Unarmed (4 abilities)
        RegisterMastery("w_unarmed", "Unarmed", "Hand-to-hand combat", "warrior_body", W, PassiveBonusType.Damage, 1.5f);
        RegisterAbility("w_punch", "Punch", "Fast straight strikes, high attack speed", "w_unarmed", W, manaCost: 5, cooldown: 0.6f);
        RegisterAbility("w_kick", "Kick", "Leg strike with knockback", "w_unarmed", W, manaCost: 8, cooldown: 1.2f);
        RegisterAbility("w_grappling", "Grappling", "Holds, throws, pins", "w_unarmed", W, manaCost: 12, cooldown: 2.5f);
        RegisterAbility("w_elbow_strike", "Elbow Strike", "Short-range burst damage", "w_unarmed", W, manaCost: 10, cooldown: 1.0f);

        // Bladed (4 abilities)
        RegisterMastery("w_bladed", "Bladed", "Swords, axes, and daggers", "warrior_body", W, PassiveBonusType.Damage, 1.5f);
        RegisterAbility("w_slash", "Slash", "Wide arc swing, multi-target", "w_bladed", W, manaCost: 8, cooldown: 1.0f);
        RegisterAbility("w_thrust", "Thrust", "Precision stab, high single-target", "w_bladed", W, manaCost: 10, cooldown: 1.2f);
        RegisterAbility("w_cleave", "Cleave", "Heavy overhead, multi-target", "w_bladed", W, manaCost: 15, cooldown: 2.0f);
        RegisterAbility("w_parry", "Parry", "Deflect attack, counter window", "w_bladed", W, manaCost: 6, cooldown: 3.0f);

        // Blunt (4 abilities)
        RegisterMastery("w_blunt", "Blunt", "Clubs, hammers, and maces", "warrior_body", W, PassiveBonusType.Damage, 1.5f);
        RegisterAbility("w_smash", "Smash", "Heavy overhead, bonus vs armored", "w_blunt", W, manaCost: 12, cooldown: 1.5f);
        RegisterAbility("w_bump", "Bump", "Blunt thrust, knocks enemy back ~1 tile", "w_blunt", W, manaCost: 8, cooldown: 1.0f);
        RegisterAbility("w_crush", "Crush", "Charged hit, chance to stun", "w_blunt", W, manaCost: 15, cooldown: 2.5f);
        RegisterAbility("w_shatter", "Shatter", "Break enemy guard/shields", "w_blunt", W, manaCost: 10, cooldown: 2.0f);

        // Polearms (5 abilities)
        RegisterMastery("w_polearms", "Polearms", "Spears, halberds, and quarterstaffs", "warrior_body", W, PassiveBonusType.Damage, 1.5f);
        RegisterAbility("w_pierce", "Pierce", "Long-range stab, keeps distance", "w_polearms", W, manaCost: 8, cooldown: 1.0f);
        RegisterAbility("w_sweep", "Sweep", "Horizontal AoE swing", "w_polearms", W, manaCost: 12, cooldown: 1.5f);
        RegisterAbility("w_brace", "Brace", "Set against charge, counter bonus", "w_polearms", W, manaCost: 6, cooldown: 3.0f);
        RegisterAbility("w_vault", "Vault", "Polearm reposition/dodge", "w_polearms", W, manaCost: 10, cooldown: 2.0f);
        RegisterAbility("w_haft_blow", "Haft Blow", "Thrust blunt end for knockback", "w_polearms", W, manaCost: 8, cooldown: 1.2f);

        // Shields (4 abilities)
        RegisterMastery("w_shields", "Shields", "Shield techniques", "warrior_body", W, PassiveBonusType.Defense, 1.2f);
        RegisterAbility("w_block", "Block", "Active damage reduction stance", "w_shields", W, manaCost: 5, cooldown: 0.5f);
        RegisterAbility("w_shield_bash", "Shield Bash", "Offensive strike, staggers", "w_shields", W, manaCost: 10, cooldown: 1.5f);
        RegisterAbility("w_deflect", "Deflect", "Reflect projectiles", "w_shields", W, manaCost: 8, cooldown: 2.0f);
        RegisterAbility("w_bulwark", "Bulwark", "Sustained defense, reduced movement", "w_shields", W, manaCost: 15, cooldown: 5.0f);

        // Dual Wield (4 abilities)
        RegisterMastery("w_dual_wield", "Dual Wield", "Two-weapon fighting", "warrior_body", W, PassiveBonusType.AttackSpeed, 0.8f);
        RegisterAbility("w_dual_stab", "Dual Stab", "Simultaneous stab, upped crit chance", "w_dual_wield", W, manaCost: 10, cooldown: 0.8f);
        RegisterAbility("w_dual_slash", "Dual Slash", "X-shaped slash, upped bleed chance", "w_dual_wield", W, manaCost: 12, cooldown: 1.0f);
        RegisterAbility("w_spin_attack", "Spin Attack", "Single spin AoE", "w_dual_wield", W, manaCost: 15, cooldown: 2.0f);
        RegisterAbility("w_rapid_combo", "Rapid Combo", "3-strike combo; +1 strike every 5 levels, caps at 15 (Omnislash)", "w_dual_wield", W, manaCost: 20, cooldown: 3.0f);

        // ── Mind ──────────────────────────────────────────────────────

        // Discipline (4 abilities)
        RegisterMastery("w_discipline", "Discipline", "Self-focused mental toughness", "warrior_mind", W, PassiveBonusType.Defense, 1.2f);
        RegisterAbility("w_focus", "Focus", "Heightened awareness, +accuracy +crit", "w_discipline", W, manaCost: 10, cooldown: 8.0f);
        RegisterAbility("w_endure", "Endure", "Damage reduction + debuff resistance", "w_discipline", W, manaCost: 12, cooldown: 10.0f);
        RegisterAbility("w_deep_breaths", "Deep Breaths", "Self-heal over time, cooldown-based", "w_discipline", W, manaCost: 15, cooldown: 15.0f);
        RegisterAbility("w_blood_lust", "Blood Lust", "Kills extend effect. +ATK/DMG/SPD/regen. BUT -DEF/-MP capacity", "w_discipline", W, manaCost: 20, cooldown: 20.0f);

        // Intimidation (4 abilities)
        RegisterMastery("w_intimidation", "Intimidation", "Enemy-focused magicule projection", "warrior_mind", W, PassiveBonusType.Chance, 0.5f);
        RegisterAbility("w_shout", "Shout", "AoE weakens nearby enemies", "w_intimidation", W, manaCost: 10, cooldown: 6.0f);
        RegisterAbility("w_intimidate", "Intimidate", "Single-target fear/stagger", "w_intimidation", W, manaCost: 8, cooldown: 4.0f);
        RegisterAbility("w_ugly_mug", "Ugly Mug", "Debuff aura on nearby enemies", "w_intimidation", W, manaCost: 12, cooldown: 10.0f);
        RegisterAbility("w_battle_roar", "Battle Roar", "AoE slows enemy attack speed + chance to stun", "w_intimidation", W, manaCost: 15, cooldown: 8.0f);
    }

    // ═══════════════════════════════════════════════════════════════════
    // RANGER (7 masteries, 37 abilities)
    // ═══════════════════════════════════════════════════════════════════

    private static void RegisterRanger()
    {
        const PlayerClass R = PlayerClass.Ranger;

        // ── Weaponry ──────────────────────────────────────────────────

        // Bowmanship (5 abilities)
        RegisterMastery("r_bowmanship", "Bowmanship", "Bows and crossbows", "ranger_weaponry", R, PassiveBonusType.Damage, 1.5f);
        RegisterAbility("r_dead_eye", "Dead Eye", "Aimed shot, high damage, slow draw", "r_bowmanship", R, manaCost: 12, cooldown: 1.5f);
        RegisterAbility("r_pepper", "Pepper", "Fast consecutive shots, reduced per-hit damage", "r_bowmanship", R, manaCost: 15, cooldown: 0.3f);
        RegisterAbility("r_lob", "Lob", "Arcing trajectory, hits behind cover", "r_bowmanship", R, manaCost: 10, cooldown: 2.0f);
        RegisterAbility("r_pin", "Pin", "Pins enemy in place, movement denial", "r_bowmanship", R, manaCost: 12, cooldown: 4.0f);
        RegisterAbility("r_flame_arrow", "Flame Arrow", "Fire-imbued shot, DoT on impact", "r_bowmanship", R, manaCost: 18, cooldown: 3.0f);

        // Throwing (5 abilities)
        RegisterMastery("r_throwing", "Throwing", "Throwing knives, axes, and projectiles", "ranger_weaponry", R, PassiveBonusType.Damage, 1.5f);
        RegisterAbility("r_flick", "Flick", "Fast knife throw, low damage, quick cooldown", "r_throwing", R, manaCost: 5, cooldown: 0.5f);
        RegisterAbility("r_chuck", "Chuck", "Heavy throw (axe), higher damage, slower", "r_throwing", R, manaCost: 10, cooldown: 1.5f);
        RegisterAbility("r_fan", "Fan", "Multiple projectiles in spread arc", "r_throwing", R, manaCost: 15, cooldown: 2.5f);
        RegisterAbility("r_ricochet", "Ricochet", "Bounces between enemies", "r_throwing", R, manaCost: 12, cooldown: 2.0f);
        RegisterAbility("r_frost_blade", "Frost Blade", "Cold-imbued throw, slows target", "r_throwing", R, manaCost: 18, cooldown: 3.0f);

        // Firearms (5 abilities)
        RegisterMastery("r_firearms", "Firearms", "Pistols, rifles, and other guns", "ranger_weaponry", R, PassiveBonusType.Damage, 1.5f);
        RegisterAbility("r_quick_draw", "Quick Draw", "Fast shot from holster, short range", "r_firearms", R, manaCost: 8, cooldown: 0.8f);
        RegisterAbility("r_bead", "Bead", "Aimed shot, high accuracy", "r_firearms", R, manaCost: 10, cooldown: 1.2f);
        RegisterAbility("r_spray", "Spray", "Multiple rapid shots, spread increases", "r_firearms", R, manaCost: 15, cooldown: 0.4f);
        RegisterAbility("r_snipe", "Snipe", "Long-range precision, high damage, long cooldown", "r_firearms", R, manaCost: 20, cooldown: 5.0f);
        RegisterAbility("r_shock_round", "Shock Round", "Lightning-imbued bullet, chains to nearby enemy", "r_firearms", R, manaCost: 22, cooldown: 4.0f);

        // CQC (4 abilities)
        RegisterMastery("r_cqc", "CQC", "Close Quarters Combat — backup plan", "ranger_weaponry", R, PassiveBonusType.Defense, 1.2f);
        RegisterAbility("r_parry", "Parry", "Deflect incoming melee attack", "r_cqc", R, manaCost: 6, cooldown: 2.0f);
        RegisterAbility("r_hunker", "Hunker", "Reduce damage with offhand buckler", "r_cqc", R, manaCost: 5, cooldown: 0.5f);
        RegisterAbility("r_riposte", "Riposte", "Counter-strike after parry/guard", "r_cqc", R, manaCost: 8, cooldown: 2.5f);
        RegisterAbility("r_shiv", "Shiv", "Quick dirty stab, chance to stagger", "r_cqc", R, manaCost: 6, cooldown: 1.0f);

        // ── Survival ──────────────────────────────────────────────────

        // Awareness (8 abilities)
        RegisterMastery("r_awareness", "Awareness", "The ghillie suit — see without being seen", "ranger_survival", R, PassiveBonusType.Chance, 0.5f);
        RegisterAbility("r_keen_senses", "Keen Senses", "Increased detection range", "r_awareness", R, manaCost: 0, cooldown: 0f);
        RegisterAbility("r_tip_toes", "Tip Toes", "Active evasion/concealment toggle", "r_awareness", R, manaCost: 8, cooldown: 1.0f);
        RegisterAbility("r_disengage", "Disengage", "Step back 1 tile + i-frames", "r_awareness", R, manaCost: 12, cooldown: 5.0f);
        RegisterAbility("r_steady_breathing", "Steady Breathing", "Slight HP recovery, better MP recovery", "r_awareness", R, manaCost: 0, cooldown: 0f);
        RegisterAbility("r_rangefinding", "Rangefinding", "Better hit & crit chance while standing still", "r_awareness", R, manaCost: 0, cooldown: 0f);
        RegisterAbility("r_tracking", "Tracking", "Better movement speed, decreased range while moving", "r_awareness", R, manaCost: 0, cooldown: 0f);
        RegisterAbility("r_steady_aim", "Steady Aim", "Charge 1-shot, guaranteed crit & hit, locks into stance", "r_awareness", R, manaCost: 15, cooldown: 6.0f);
        RegisterAbility("r_weak_spot", "Weak Spot", "Single target +attack speed +hit chance for 5-10s", "r_awareness", R, manaCost: 10, cooldown: 8.0f);

        // Trapping (5 abilities)
        RegisterMastery("r_trapping", "Trapping", "The patient hunter's toolkit", "ranger_survival", R, PassiveBonusType.Damage, 1.5f);
        RegisterAbility("r_snare", "Snare", "Place trap that roots enemies", "r_trapping", R, manaCost: 12, cooldown: 4.0f);
        RegisterAbility("r_tripwire", "Tripwire", "Line trap triggers knockdown", "r_trapping", R, manaCost: 10, cooldown: 5.0f);
        RegisterAbility("r_decoy", "Decoy", "Dummy that draws enemy attention", "r_trapping", R, manaCost: 15, cooldown: 8.0f);
        RegisterAbility("r_bait", "Bait", "Lure that attracts enemies to a specific spot", "r_trapping", R, manaCost: 8, cooldown: 6.0f);
        RegisterAbility("r_ambush", "Ambush", "Bonus damage on first strike against unaware enemies", "r_trapping", R, manaCost: 0, cooldown: 0f);

        // Sapping (5 abilities)
        RegisterMastery("r_sapping", "Sapping", "Homemade explosives and area denial", "ranger_survival", R, PassiveBonusType.Damage, 1.5f);
        RegisterAbility("r_frag", "Frag", "AoE explosion damage", "r_sapping", R, manaCost: 15, cooldown: 3.0f);
        RegisterAbility("r_smoke_bomb", "Smoke Bomb", "Obscure area, reduces enemy accuracy", "r_sapping", R, manaCost: 12, cooldown: 6.0f);
        RegisterAbility("r_flashbang", "Flashbang", "AoE blind/stun, brief", "r_sapping", R, manaCost: 12, cooldown: 5.0f);
        RegisterAbility("r_caltrops", "Caltrops", "Scatter on ground, slows enemies in area", "r_sapping", R, manaCost: 10, cooldown: 4.0f);
        RegisterAbility("r_sticky_bomb", "Sticky Bomb", "Attach to enemy, delayed explosion", "r_sapping", R, manaCost: 18, cooldown: 6.0f);
    }

    // ═══════════════════════════════════════════════════════════════════
    // MAGE (8 masteries, 33 abilities)
    // ═══════════════════════════════════════════════════════════════════

    private static void RegisterMage()
    {
        const PlayerClass M = PlayerClass.Mage;

        // ── Elemental ─────────────────────────────────────────────────

        // Fire (4 abilities)
        RegisterMastery("m_fire", "Fire", "Heat, flame, combustion", "mage_elemental", M, PassiveBonusType.Damage, 1.5f);
        RegisterAbility("m_fireball", "Fireball", "Projectile explosion, area damage on impact", "m_fire", M, manaCost: 30, cooldown: 1.5f);
        RegisterAbility("m_flame_wall", "Flame Wall", "Line of fire, damages enemies passing through", "m_fire", M, manaCost: 40, cooldown: 5.0f);
        RegisterAbility("m_ignite", "Ignite", "Set target ablaze, damage over time", "m_fire", M, manaCost: 25, cooldown: 3.0f);
        RegisterAbility("m_inferno", "Inferno", "Large area sustained fire, high mana cost", "m_fire", M, manaCost: 80, cooldown: 12.0f);

        // Water (4 abilities)
        RegisterMastery("m_water", "Water", "Cold, frost, tides", "mage_elemental", M, PassiveBonusType.Damage, 1.5f);
        RegisterAbility("m_frost_bolt", "Frost Bolt", "Ice projectile, slows target on hit", "m_water", M, manaCost: 25, cooldown: 1.2f);
        RegisterAbility("m_freeze", "Freeze", "Immobilize target in ice", "m_water", M, manaCost: 35, cooldown: 6.0f);
        RegisterAbility("m_tidal_wave", "Tidal Wave", "Wide frontal wave, pushes and damages", "m_water", M, manaCost: 50, cooldown: 8.0f);
        RegisterAbility("m_mist_veil", "Mist Veil", "Obscuring mist, reduces enemy accuracy", "m_water", M, manaCost: 30, cooldown: 10.0f);

        // Air (4 abilities)
        RegisterMastery("m_air", "Air", "Wind, lightning, storms", "mage_elemental", M, PassiveBonusType.Damage, 1.5f);
        RegisterAbility("m_lightning", "Lightning", "Fast bolt, high single-target damage", "m_air", M, manaCost: 28, cooldown: 1.0f);
        RegisterAbility("m_gust", "Gust", "Knockback wind blast, repositions enemies", "m_air", M, manaCost: 20, cooldown: 3.0f);
        RegisterAbility("m_chain_shock", "Chain Shock", "Lightning jumps between nearby enemies", "m_air", M, manaCost: 45, cooldown: 4.0f);
        RegisterAbility("m_tempest", "Tempest", "Area storm, sustained damage and disruption", "m_air", M, manaCost: 70, cooldown: 15.0f);

        // Earth (4 abilities)
        RegisterMastery("m_earth", "Earth", "Stone, tremors, petrification", "mage_elemental", M, PassiveBonusType.Damage, 1.5f);
        RegisterAbility("m_stone_spike", "Stone Spike", "Rock eruption from ground, single-target", "m_earth", M, manaCost: 25, cooldown: 1.5f);
        RegisterAbility("m_quake", "Quake", "Area tremor, damages and staggers nearby", "m_earth", M, manaCost: 45, cooldown: 6.0f);
        RegisterAbility("m_petrify", "Petrify", "Turn target to stone temporarily, hard CC", "m_earth", M, manaCost: 40, cooldown: 8.0f);
        RegisterAbility("m_earthen_armor", "Earthen Armor", "Coat self in stone, temporary damage absorption", "m_earth", M, manaCost: 35, cooldown: 10.0f);

        // ── Aether ────────────────────────────────────────────────────

        // Aether (5 abilities) — Light + Dark as one mastery
        RegisterMastery("m_aether", "Aether", "Cosmic force — light and dark as one", "mage_aether", M, PassiveBonusType.Damage, 1.5f);
        RegisterAbility("m_nova", "Nova", "Radiant energy burst around caster, AoE damage", "m_aether", M, manaCost: 60, cooldown: 6.0f);
        RegisterAbility("m_weld", "Weld", "Burst heal — fuses wounds with raw energy", "m_aether", M, manaCost: 50, cooldown: 8.0f);
        RegisterAbility("m_purify", "Purify", "Cleanse all debuffs and status effects", "m_aether", M, manaCost: 45, cooldown: 10.0f);
        RegisterAbility("m_drain", "Drain", "Gravitational pull on target's life force, heals caster", "m_aether", M, manaCost: 40, cooldown: 4.0f);
        RegisterAbility("m_singularity", "Singularity", "Gravity well, pulls enemies in and damages over time", "m_aether", M, manaCost: 80, cooldown: 15.0f);

        // ── Attunement ────────────────────────────────────────────────

        // Restoration (4 abilities)
        RegisterMastery("m_restoration", "Restoration", "Body defense and self-repair", "mage_attunement", M, PassiveBonusType.Defense, 1.2f);
        RegisterAbility("m_mend", "Mend", "Quick self-heal, low mana cost", "m_restoration", M, manaCost: 20, cooldown: 3.0f);
        RegisterAbility("m_barrier", "Barrier", "Magical shield that absorbs incoming damage", "m_restoration", M, manaCost: 35, cooldown: 8.0f);
        RegisterAbility("m_cleanse", "Cleanse", "Remove physical ailments (poison, bleed, burn)", "m_restoration", M, manaCost: 25, cooldown: 5.0f);
        RegisterAbility("m_regeneration", "Regeneration", "Sustained HP recovery over time", "m_restoration", M, manaCost: 30, cooldown: 12.0f);

        // Amplification (4 abilities)
        RegisterMastery("m_amplification", "Amplification", "Neural enhancement — expanding magical capacity", "mage_attunement", M, PassiveBonusType.Regen, 0.3f);
        RegisterAbility("m_mana_surge", "Mana Surge", "Burst mana recovery", "m_amplification", M, manaCost: 0, cooldown: 15.0f);
        RegisterAbility("m_quick_cast", "Quick Cast", "Temporarily reduce cast time of all spells", "m_amplification", M, manaCost: 30, cooldown: 12.0f);
        RegisterAbility("m_resonance", "Resonance", "Boost damage of an attuned element", "m_amplification", M, manaCost: 25, cooldown: 10.0f);
        RegisterAbility("m_focus_channel", "Focus Channel", "Reduce mana cost of all spells while stationary", "m_amplification", M, manaCost: 20, cooldown: 8.0f);

        // Overcharge (4 abilities)
        RegisterMastery("m_overcharge", "Overcharge", "Push past safe limits — power at bodily cost", "mage_attunement", M, PassiveBonusType.Damage, 1.5f);
        RegisterAbility("m_neural_burn", "Neural Burn", "Greatly boost spell damage, drains HP over time", "m_overcharge", M, manaCost: 30, cooldown: 15.0f);
        RegisterAbility("m_mana_frenzy", "Mana Frenzy", "Eliminate mana costs, HP damage per cast instead", "m_overcharge", M, manaCost: 40, cooldown: 20.0f);
        RegisterAbility("m_pain_gate", "Pain Gate", "Convert incoming damage into mana", "m_overcharge", M, manaCost: 25, cooldown: 10.0f);
        RegisterAbility("m_last_resort", "Last Resort", "Near death — massively amplify all abilities", "m_overcharge", M, manaCost: 0, cooldown: 30.0f);
    }
}
