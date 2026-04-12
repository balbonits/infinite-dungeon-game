using System.Collections.Generic;
using System.Linq;
using Godot;

namespace DungeonGame;

/// <summary>
/// Central registry of all skill definitions. Organized by class.
/// All skills from docs/systems/skills.md are registered here.
/// Pure data — no Godot dependency.
/// </summary>
public static class SkillDatabase
{
    private static readonly Dictionary<string, SkillDef> Skills = new();
    private static readonly Dictionary<string, List<string>> CategorySkills = new(); // categoryId → baseSkillIds
    private static readonly Dictionary<string, List<string>> BaseToSpecific = new(); // baseSkillId → specificSkillIds

    static SkillDatabase()
    {
        RegisterWarriorSkills();
        RegisterRangerSkills();
        RegisterMageSkills();
    }

    public static SkillDef? Get(string id) => Skills.GetValueOrDefault(id);

    public static IEnumerable<SkillDef> GetByClass(PlayerClass cls) =>
        Skills.Values.Where(s => s.Class == cls);

    public static IEnumerable<SkillDef> GetBaseSkillsInCategory(string categoryId) =>
        CategorySkills.TryGetValue(categoryId, out var ids) ? ids.Select(id => Skills[id]) : [];

    public static IEnumerable<SkillDef> GetSpecificSkills(string baseSkillId) =>
        BaseToSpecific.TryGetValue(baseSkillId, out var ids) ? ids.Select(id => Skills[id]) : [];

    public static string[] GetCategories(PlayerClass cls)
    {
        return cls switch
        {
            PlayerClass.Warrior => new[] { "warrior_body", "warrior_mind" },
            PlayerClass.Ranger => new[] { "ranger_arms", "ranger_instinct" },
            PlayerClass.Mage => new[] { "mage_arcane", "mage_conduit" },
            _ => [],
        };
    }

    public static string GetCategoryName(string categoryId)
    {
        return categoryId switch
        {
            "warrior_body" => "Body",
            "warrior_mind" => "Mind",
            "ranger_arms" => "Arms",
            "ranger_instinct" => "Instinct",
            "mage_arcane" => "Arcane",
            "mage_conduit" => "Conduit",
            _ => categoryId,
        };
    }

    // --- Registration Helpers ---

    private static void RegisterBase(string id, string name, string desc, string categoryId,
        PlayerClass cls, PassiveBonusType passiveType, float multiplier, int baseXp = 5)
    {
        var def = new SkillDef
        {
            Id = id,
            Name = name,
            Description = desc,
            CategoryId = categoryId,
            Type = SkillType.Base,
            Class = cls,
            PassiveType = passiveType,
            PassiveMultiplier = multiplier,
            BaseXpPerUse = baseXp,
        };
        Skills[id] = def;
        if (!CategorySkills.ContainsKey(categoryId))
            CategorySkills[categoryId] = new List<string>();
        CategorySkills[categoryId].Add(id);
    }

    private static void RegisterSpecific(string id, string name, string desc,
        string parentBaseId, PlayerClass cls, int baseXp = 10,
        int manaCost = 0, float cooldown = 2.0f, AttackConfig? combat = null)
    {
        var def = new SkillDef
        {
            Id = id,
            Name = name,
            Description = desc,
            CategoryId = Skills[parentBaseId].CategoryId,
            ParentBaseSkillId = parentBaseId,
            Type = SkillType.Specific,
            Class = cls,
            BaseXpPerUse = baseXp,
            ManaCost = manaCost,
            Cooldown = cooldown,
            CombatConfig = combat,
        };
        Skills[id] = def;
        if (!BaseToSpecific.ContainsKey(parentBaseId))
            BaseToSpecific[parentBaseId] = new List<string>();
        BaseToSpecific[parentBaseId].Add(id);
    }

    // --- Warrior Skills ---
    private static void RegisterWarriorSkills()
    {
        const PlayerClass W = PlayerClass.Warrior;

        // Body > Unarmed
        RegisterBase("w_unarmed", "Unarmed", "Hand-to-hand combat", "warrior_body", W, PassiveBonusType.Damage, 1.5f);
        RegisterSpecific("w_punch", "Punch", "Fast straight strikes, high attack speed", "w_unarmed", W);
        RegisterSpecific("w_kick", "Kick", "Leg strikes, knockback effect", "w_unarmed", W);
        RegisterSpecific("w_grapple", "Grapple", "Holds and throws, close-range crowd control", "w_unarmed", W);
        RegisterSpecific("w_elbow_knee", "Elbow/Knee", "Short-range burst damage", "w_unarmed", W);

        // Body > Bladed
        RegisterBase("w_bladed", "Bladed", "Swords, axes, and daggers", "warrior_body", W, PassiveBonusType.Damage, 1.5f);
        RegisterSpecific("w_slash", "Slash", "Wide arc swing, can hit multiple enemies", "w_bladed", W);
        RegisterSpecific("w_thrust", "Thrust", "Precision stab, high single-target damage", "w_bladed", W);
        RegisterSpecific("w_cleave", "Cleave", "Heavy overhead strike, hits multiple enemies", "w_bladed", W);
        RegisterSpecific("w_parry", "Parry", "Deflect incoming melee attack", "w_bladed", W);

        // Body > Blunt
        RegisterBase("w_blunt", "Blunt", "Clubs, hammers, and maces", "warrior_body", W, PassiveBonusType.Damage, 1.5f);
        RegisterSpecific("w_smash", "Smash", "Heavy overhead hit, bonus vs armored", "w_blunt", W);
        RegisterSpecific("w_sweep", "Sweep", "Low arc swing, knockback effect", "w_blunt", W);
        RegisterSpecific("w_crush", "Crush", "Charged hit, chance to stun", "w_blunt", W);
        RegisterSpecific("w_shatter", "Shatter", "Break enemy guard or shields", "w_blunt", W);

        // Body > Polearms
        RegisterBase("w_polearms", "Polearms", "Spears, halberds, and quarterstaffs", "warrior_body", W, PassiveBonusType.Damage, 1.5f);
        RegisterSpecific("w_pole_thrust", "Thrust", "Long-range poke", "w_polearms", W);
        RegisterSpecific("w_pole_sweep", "Sweep", "Wide arc, crowd control", "w_polearms", W);
        RegisterSpecific("w_brace", "Brace", "Set weapon against charging enemies", "w_polearms", W);
        RegisterSpecific("w_vault", "Vault", "Use polearm to reposition", "w_polearms", W);

        // Body > Shields
        RegisterBase("w_shields", "Shields", "Shield techniques", "warrior_body", W, PassiveBonusType.Defense, 1.2f);
        RegisterSpecific("w_block", "Block", "Active damage reduction stance", "w_shields", W);
        RegisterSpecific("w_shield_bash", "Shield Bash", "Offensive shield strike, staggers", "w_shields", W);
        RegisterSpecific("w_deflect", "Deflect", "Reflect incoming projectiles", "w_shields", W);
        RegisterSpecific("w_bulwark", "Bulwark", "Sustained defensive stance", "w_shields", W);

        // Mind > Inner
        RegisterBase("w_inner", "Inner", "Self-focused mental discipline", "warrior_mind", W, PassiveBonusType.Regen, 0.3f);
        RegisterSpecific("w_battle_focus", "Battle Focus", "Increases accuracy and crit chance", "w_inner", W);
        RegisterSpecific("w_pain_tolerance", "Pain Tolerance", "Passive damage reduction", "w_inner", W);
        RegisterSpecific("w_second_wind", "Second Wind", "Self-heal over time, cooldown-based", "w_inner", W);
        RegisterSpecific("w_iron_will", "Iron Will", "Resist debuffs and status effects", "w_inner", W);

        // Mind > Outer
        RegisterBase("w_outer", "Outer", "Enemy-focused mental presence", "warrior_mind", W, PassiveBonusType.Chance, 0.5f);
        RegisterSpecific("w_war_cry", "War Cry", "AoE shout that weakens nearby enemies", "w_outer", W);
        RegisterSpecific("w_intimidate", "Intimidate", "Single-target fear or stagger", "w_outer", W);
        RegisterSpecific("w_menacing", "Menacing Presence", "Passive aura that debuffs enemies", "w_outer", W);
        RegisterSpecific("w_battle_roar", "Battle Roar", "AoE shout that slows enemy attack speed", "w_outer", W);
    }

    // --- Ranger Skills ---
    private static void RegisterRangerSkills()
    {
        const PlayerClass R = PlayerClass.Ranger;

        // Arms > Drawn
        RegisterBase("r_drawn", "Drawn", "Bows and crossbows", "ranger_arms", R, PassiveBonusType.Damage, 1.5f);
        RegisterSpecific("r_power_shot", "Power Shot", "High-damage single shot, slow draw", "r_drawn", R);
        RegisterSpecific("r_rapid_fire", "Rapid Fire", "Fast consecutive shots, reduced damage", "r_drawn", R);
        RegisterSpecific("r_arc_shot", "Arc Shot", "Arcing trajectory, hits behind cover", "r_drawn", R);
        RegisterSpecific("r_pin_shot", "Pin Shot", "Pins enemy in place briefly", "r_drawn", R);

        // Arms > Thrown
        RegisterBase("r_thrown", "Thrown", "Throwing knives, axes, projectiles", "ranger_arms", R, PassiveBonusType.AttackSpeed, 0.8f);
        RegisterSpecific("r_knife_throw", "Knife Throw", "Fast, low-damage throw", "r_thrown", R);
        RegisterSpecific("r_axe_throw", "Axe Throw", "Slower, heavier throw, higher damage", "r_thrown", R);
        RegisterSpecific("r_fan_throw", "Fan Throw", "Multiple projectiles in a spread arc", "r_thrown", R);
        RegisterSpecific("r_bounce_shot", "Bounce Shot", "Projectile ricochets between enemies", "r_thrown", R);

        // Arms > Firearms
        RegisterBase("r_firearms", "Firearms", "Pistols, rifles, and other guns", "ranger_arms", R, PassiveBonusType.Damage, 1.5f);
        RegisterSpecific("r_quick_draw", "Quick Draw", "Fast shot from holster", "r_firearms", R);
        RegisterSpecific("r_steady_shot", "Steady Shot", "Aimed shot, high accuracy", "r_firearms", R);
        RegisterSpecific("r_burst_fire", "Burst Fire", "Multiple rapid shots", "r_firearms", R);
        RegisterSpecific("r_snipe", "Snipe", "Long-range precision shot", "r_firearms", R);

        // Arms > Melee
        RegisterBase("r_melee", "Melee", "Defensive offhand weapons", "ranger_arms", R, PassiveBonusType.Defense, 1.2f);
        RegisterSpecific("r_parry", "Parry", "Deflect incoming melee attack", "r_melee", R);
        RegisterSpecific("r_r_block", "Block", "Reduce damage with offhand", "r_melee", R);
        RegisterSpecific("r_riposte", "Riposte", "Counter-attack after a successful parry", "r_melee", R);
        RegisterSpecific("r_disarm", "Disarm", "Knock weapon from enemy's grip", "r_melee", R);

        // Instinct > Precision
        RegisterBase("r_precision", "Precision", "Offensive mental calculation", "ranger_instinct", R, PassiveBonusType.Chance, 0.5f);
        RegisterSpecific("r_steady_aim", "Steady Aim", "Increases accuracy while stationary", "r_precision", R);
        RegisterSpecific("r_weak_spot", "Weak Spot", "Identify enemy vulnerabilities", "r_precision", R);
        RegisterSpecific("r_range_calc", "Range Calc", "Improved damage at long range", "r_precision", R);
        RegisterSpecific("r_lead_shot", "Lead Shot", "Predict enemy movement", "r_precision", R);

        // Instinct > Awareness
        RegisterBase("r_awareness", "Awareness", "Defensive situational reading", "ranger_instinct", R, PassiveBonusType.Chance, 0.5f);
        RegisterSpecific("r_threat_sense", "Threat Sense", "Detect enemies before visual range", "r_awareness", R);
        RegisterSpecific("r_dodge_roll", "Dodge Roll", "Quick roll to evade attacks", "r_awareness", R);
        RegisterSpecific("r_disengage", "Disengage", "Create distance from nearby enemies", "r_awareness", R);
        RegisterSpecific("r_tumble", "Tumble", "Recover from knockback or stagger", "r_awareness", R);

        // Instinct > Trapping
        RegisterBase("r_trapping", "Trapping", "Tactical preparation", "ranger_instinct", R, PassiveBonusType.Damage, 1.5f);
        RegisterSpecific("r_snare", "Snare", "Place a trap that roots enemies", "r_trapping", R);
        RegisterSpecific("r_tripwire", "Tripwire", "Line trap that triggers knockdown", "r_trapping", R);
        RegisterSpecific("r_decoy", "Decoy", "Place a dummy that draws attention", "r_trapping", R);
        RegisterSpecific("r_ambush", "Ambush", "Bonus damage on first attack", "r_trapping", R);
    }

    // --- Mage Skills ---
    private static void RegisterMageSkills()
    {
        const PlayerClass M = PlayerClass.Mage;

        // Arcane > Fire
        RegisterBase("m_fire", "Fire", "Fire magic — heat, flame, combustion", "mage_arcane", M, PassiveBonusType.AttackSpeed, 0.8f);
        RegisterSpecific("m_fireball", "Fireball", "Projectile explosion, area damage", "m_fire", M,
            manaCost: 25, cooldown: 1.5f, combat: new AttackConfig { Range = 200, Cooldown = 1.5f, DamageMultiplier = 1.8f, IsProjectile = true, ProjectileSpeed = 280, ProjectileTexture = Constants.Assets.FireballProjectile, ProjectileScale = 1.0f, ProjectileTint = new Godot.Color("FF6B35"), TargetMode = TargetMode.AreaOfEffect, AoeRadius = 80, MaxTargets = 5, Effect = VisualEffect.Projectile, EffectColor = new Godot.Color("FF6B35") });
        RegisterSpecific("m_flame_wall", "Flame Wall", "Line of fire that damages enemies", "m_fire", M,
            manaCost: 30, cooldown: 3.0f, combat: new AttackConfig { Range = 150, DamageMultiplier = 1.2f, TargetMode = TargetMode.PlayerCentricAoe, AoeRadius = 100, MaxTargets = 8, Effect = VisualEffect.Slash, EffectColor = new Godot.Color("FF4500") });
        RegisterSpecific("m_ignite", "Ignite", "Set target ablaze, damage over time", "m_fire", M,
            manaCost: 15, cooldown: 2.0f, combat: new AttackConfig { Range = 150, DamageMultiplier = 0.8f, IsProjectile = true, ProjectileSpeed = 350, ProjectileTexture = Constants.Assets.FireballProjectile, ProjectileScale = 0.6f, ProjectileTint = new Godot.Color("FF8C00"), Effect = VisualEffect.Projectile, EffectColor = new Godot.Color("FF8C00") });
        RegisterSpecific("m_inferno", "Inferno", "Large area sustained fire", "m_fire", M,
            manaCost: 50, cooldown: 5.0f, combat: new AttackConfig { Range = 120, DamageMultiplier = 2.5f, TargetMode = TargetMode.PlayerCentricAoe, AoeRadius = 120, MaxTargets = 10, Effect = VisualEffect.Slash, EffectColor = new Godot.Color("FF2200") });

        // Arcane > Water
        RegisterBase("m_water", "Water", "Water and ice magic", "mage_arcane", M, PassiveBonusType.AttackSpeed, 0.8f);
        RegisterSpecific("m_frost_bolt", "Frost Bolt", "Ice projectile, slows target", "m_water", M,
            manaCost: 18, cooldown: 1.2f, combat: new AttackConfig { Range = 220, DamageMultiplier = 1.4f, IsProjectile = true, ProjectileSpeed = 320, ProjectileTexture = Constants.Assets.FrostBoltProjectile, ProjectileScale = 1.0f, ProjectileTint = new Godot.Color("88DDFF"), Effect = VisualEffect.Projectile, EffectColor = new Godot.Color("88DDFF") });
        RegisterSpecific("m_freeze", "Freeze", "Immobilize target in ice", "m_water", M,
            manaCost: 35, cooldown: 4.0f, combat: new AttackConfig { Range = 150, DamageMultiplier = 0.5f, IsProjectile = true, ProjectileSpeed = 400, ProjectileTexture = Constants.Assets.FrostBoltProjectile, ProjectileScale = 0.8f, ProjectileTint = new Godot.Color("AAEEFF"), Effect = VisualEffect.Projectile, EffectColor = new Godot.Color("AAEEFF") });
        RegisterSpecific("m_tidal_wave", "Tidal Wave", "Wide frontal wave", "m_water", M,
            manaCost: 40, cooldown: 4.0f, combat: new AttackConfig { Range = 130, DamageMultiplier = 1.6f, TargetMode = TargetMode.Cone, ConeAngle = 90, AoeRadius = 130, MaxTargets = 8, Effect = VisualEffect.Slash, EffectColor = new Godot.Color("4488FF") });
        RegisterSpecific("m_mist_veil", "Mist Veil", "Obscuring mist, reduces accuracy", "m_water", M,
            manaCost: 20, cooldown: 6.0f, combat: new AttackConfig { TargetMode = TargetMode.Self, Effect = VisualEffect.None });

        // Arcane > Air
        RegisterBase("m_air", "Air", "Air and electricity magic", "mage_arcane", M, PassiveBonusType.AttackSpeed, 0.8f);
        RegisterSpecific("m_lightning", "Lightning", "Fast bolt, high single-target damage", "m_air", M,
            manaCost: 22, cooldown: 1.0f, combat: new AttackConfig { Range = 250, DamageMultiplier = 2.0f, IsProjectile = true, ProjectileSpeed = 600, ProjectileTexture = Constants.Assets.LightningProjectile, ProjectileScale = 1.0f, ProjectileTint = new Godot.Color("FFEE44"), Effect = VisualEffect.Projectile, EffectColor = new Godot.Color("FFEE44") });
        RegisterSpecific("m_gust", "Gust", "Knockback wind blast", "m_air", M,
            manaCost: 15, cooldown: 2.0f, combat: new AttackConfig { Range = 100, DamageMultiplier = 0.6f, TargetMode = TargetMode.Cone, ConeAngle = 120, AoeRadius = 100, MaxTargets = 6, Effect = VisualEffect.Slash, EffectColor = new Godot.Color("CCFFEE") });
        RegisterSpecific("m_chain_shock", "Chain Shock", "Lightning jumps between enemies", "m_air", M,
            manaCost: 30, cooldown: 2.5f, combat: new AttackConfig { Range = 200, DamageMultiplier = 1.3f, IsProjectile = true, ProjectileSpeed = 500, ProjectileTexture = Constants.Assets.LightningProjectile, ProjectileScale = 0.8f, TargetMode = TargetMode.MultiTarget, ChainCount = 4, ChainRange = 120, Effect = VisualEffect.Projectile, EffectColor = new Godot.Color("FFEE44") });
        RegisterSpecific("m_tempest", "Tempest", "Area storm, sustained damage", "m_air", M,
            manaCost: 45, cooldown: 5.0f, combat: new AttackConfig { Range = 100, DamageMultiplier = 2.0f, TargetMode = TargetMode.PlayerCentricAoe, AoeRadius = 110, MaxTargets = 10, Effect = VisualEffect.Slash, EffectColor = new Godot.Color("AADDFF") });

        // Arcane > Earth
        RegisterBase("m_earth", "Earth", "Earth and stone magic", "mage_arcane", M, PassiveBonusType.AttackSpeed, 0.8f);
        RegisterSpecific("m_stone_spike", "Stone Spike", "Rock eruption, single-target", "m_earth", M,
            manaCost: 20, cooldown: 1.4f, combat: new AttackConfig { Range = 180, DamageMultiplier = 1.6f, IsProjectile = true, ProjectileSpeed = 350, ProjectileTexture = Constants.Assets.StoneSpikeProjectile, ProjectileScale = 1.0f, ProjectileTint = new Godot.Color("8B6914"), Effect = VisualEffect.Projectile, EffectColor = new Godot.Color("8B6914") });
        RegisterSpecific("m_quake", "Quake", "Area tremor, damages and staggers", "m_earth", M,
            manaCost: 35, cooldown: 4.0f, combat: new AttackConfig { Range = 100, DamageMultiplier = 1.8f, TargetMode = TargetMode.PlayerCentricAoe, AoeRadius = 100, MaxTargets = 8, Effect = VisualEffect.Slash, EffectColor = new Godot.Color("8B4513") });
        RegisterSpecific("m_petrify", "Petrify", "Turn target to stone temporarily", "m_earth", M,
            manaCost: 30, cooldown: 5.0f, combat: new AttackConfig { Range = 150, DamageMultiplier = 0.3f, IsProjectile = true, ProjectileSpeed = 300, ProjectileTexture = Constants.Assets.StoneSpikeProjectile, ProjectileScale = 0.7f, Effect = VisualEffect.Projectile, EffectColor = new Godot.Color("696969") });
        RegisterSpecific("m_earthen_armor", "Earthen Armor", "Stone coating, damage absorption", "m_earth", M,
            manaCost: 25, cooldown: 8.0f, combat: new AttackConfig { TargetMode = TargetMode.Self, Effect = VisualEffect.None });

        // Arcane > Light
        RegisterBase("m_light", "Light", "Light and energy magic", "mage_arcane", M, PassiveBonusType.Damage, 1.5f);
        RegisterSpecific("m_energy_blast", "Energy Blast", "Concentrated energy projectile", "m_light", M,
            manaCost: 28, cooldown: 1.8f, combat: new AttackConfig { Range = 220, DamageMultiplier = 2.2f, IsProjectile = true, ProjectileSpeed = 350, ProjectileTexture = Constants.Assets.EnergyBlastProjectile, ProjectileScale = 1.0f, ProjectileTint = new Godot.Color("FFD700"), Effect = VisualEffect.Projectile, EffectColor = new Godot.Color("FFD700") });
        RegisterSpecific("m_radiance", "Radiance", "Burst of light around caster", "m_light", M,
            manaCost: 30, cooldown: 3.0f, combat: new AttackConfig { Range = 100, DamageMultiplier = 1.5f, TargetMode = TargetMode.PlayerCentricAoe, AoeRadius = 100, MaxTargets = 10, Effect = VisualEffect.Slash, EffectColor = new Godot.Color("FFFACD") });
        RegisterSpecific("m_heal", "Heal", "Restore HP to self", "m_light", M,
            manaCost: 35, cooldown: 4.0f, combat: new AttackConfig { TargetMode = TargetMode.Self, Effect = VisualEffect.None });
        RegisterSpecific("m_purify", "Purify", "Remove debuffs from self", "m_light", M,
            manaCost: 20, cooldown: 6.0f, combat: new AttackConfig { TargetMode = TargetMode.Self, Effect = VisualEffect.None });

        // Arcane > Dark
        RegisterBase("m_dark", "Dark", "Shadow and void magic", "mage_arcane", M, PassiveBonusType.Damage, 1.5f);
        RegisterSpecific("m_drain_life", "Drain Life", "Steal HP from target", "m_dark", M,
            manaCost: 20, cooldown: 2.0f, combat: new AttackConfig { Range = 150, DamageMultiplier = 1.0f, IsProjectile = true, ProjectileSpeed = 250, ProjectileTexture = Constants.Assets.ShadowBoltProjectile, ProjectileScale = 0.7f, ProjectileTint = new Godot.Color("880088"), Effect = VisualEffect.Projectile, EffectColor = new Godot.Color("880088") });
        RegisterSpecific("m_curse", "Curse", "Debuff target damage and defense", "m_dark", M,
            manaCost: 25, cooldown: 5.0f, combat: new AttackConfig { Range = 150, DamageMultiplier = 0.3f, IsProjectile = true, ProjectileSpeed = 300, ProjectileTexture = Constants.Assets.ShadowBoltProjectile, ProjectileScale = 0.6f, ProjectileTint = new Godot.Color("660066"), Effect = VisualEffect.Projectile, EffectColor = new Godot.Color("660066") });
        RegisterSpecific("m_shadow_bolt", "Shadow Bolt", "Dark projectile, high damage", "m_dark", M,
            manaCost: 30, cooldown: 1.5f, combat: new AttackConfig { Range = 200, DamageMultiplier = 2.4f, IsProjectile = true, ProjectileSpeed = 320, ProjectileTexture = Constants.Assets.ShadowBoltProjectile, ProjectileScale = 1.0f, ProjectileTint = new Godot.Color("4B0082"), Effect = VisualEffect.Projectile, EffectColor = new Godot.Color("4B0082") });
        RegisterSpecific("m_void_zone", "Void Zone", "Area of darkness, damages enemies", "m_dark", M,
            manaCost: 40, cooldown: 5.0f, combat: new AttackConfig { Range = 120, DamageMultiplier = 2.0f, TargetMode = TargetMode.AreaOfEffect, AoeRadius = 90, MaxTargets = 8, Effect = VisualEffect.Slash, EffectColor = new Godot.Color("2D0040") });

        // Conduit > Restoration
        RegisterBase("m_restoration", "Restoration", "Body defense and self-repair", "mage_conduit", M, PassiveBonusType.Regen, 0.3f);
        RegisterSpecific("m_mend", "Mend", "Quick self-heal, low mana cost", "m_restoration", M,
            manaCost: 12, cooldown: 2.0f, combat: new AttackConfig { TargetMode = TargetMode.Self, Effect = VisualEffect.None });
        RegisterSpecific("m_barrier", "Barrier", "Magical shield absorbs damage", "m_restoration", M,
            manaCost: 30, cooldown: 8.0f, combat: new AttackConfig { TargetMode = TargetMode.Self, Effect = VisualEffect.None });
        RegisterSpecific("m_cleanse", "Cleanse", "Remove physical ailments", "m_restoration", M,
            manaCost: 18, cooldown: 6.0f, combat: new AttackConfig { TargetMode = TargetMode.Self, Effect = VisualEffect.None });
        RegisterSpecific("m_regeneration", "Regeneration", "Sustained HP recovery", "m_restoration", M,
            manaCost: 25, cooldown: 10.0f, combat: new AttackConfig { TargetMode = TargetMode.Self, Effect = VisualEffect.None });

        // Conduit > Amplification
        RegisterBase("m_amplification", "Amplification", "Neural enhancement", "mage_conduit", M, PassiveBonusType.Regen, 0.3f);
        RegisterSpecific("m_mana_surge", "Mana Surge", "Burst of mana recovery", "m_amplification", M,
            manaCost: 0, cooldown: 12.0f, combat: new AttackConfig { TargetMode = TargetMode.Self, Effect = VisualEffect.None });
        RegisterSpecific("m_quick_cast", "Quick Cast", "Temporarily reduce cast time", "m_amplification", M,
            manaCost: 20, cooldown: 10.0f, combat: new AttackConfig { TargetMode = TargetMode.Self, Effect = VisualEffect.None });
        RegisterSpecific("m_attunement", "Attunement", "Increase elemental affinity", "m_amplification", M,
            manaCost: 15, cooldown: 10.0f, combat: new AttackConfig { TargetMode = TargetMode.Self, Effect = VisualEffect.None });
        RegisterSpecific("m_focus_channel", "Focus Channel", "Reduce mana cost while stationary", "m_amplification", M,
            manaCost: 10, cooldown: 8.0f, combat: new AttackConfig { TargetMode = TargetMode.Self, Effect = VisualEffect.None });

        // Conduit > Overcharge
        RegisterBase("m_overcharge", "Overcharge", "Push nervous system beyond limits", "mage_conduit", M, PassiveBonusType.Damage, 1.5f);
        RegisterSpecific("m_neural_burn", "Neural Burn", "Boost spell damage, drains HP", "m_overcharge", M,
            manaCost: 0, cooldown: 10.0f, combat: new AttackConfig { TargetMode = TargetMode.Self, Effect = VisualEffect.None });
        RegisterSpecific("m_mana_frenzy", "Mana Frenzy", "No mana cost, HP cost instead", "m_overcharge", M,
            manaCost: 0, cooldown: 15.0f, combat: new AttackConfig { TargetMode = TargetMode.Self, Effect = VisualEffect.None });
        RegisterSpecific("m_pain_conduit", "Pain Conduit", "Convert damage into mana", "m_overcharge", M,
            manaCost: 0, cooldown: 12.0f, combat: new AttackConfig { TargetMode = TargetMode.Self, Effect = VisualEffect.None });
        RegisterSpecific("m_last_resort", "Last Resort", "Near death: massively amplify all", "m_overcharge", M,
            manaCost: 0, cooldown: 30.0f, combat: new AttackConfig { TargetMode = TargetMode.Self, Effect = VisualEffect.None });
    }
}
