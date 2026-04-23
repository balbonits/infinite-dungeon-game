using Godot;
using System;

namespace DungeonGame.Autoloads;

public partial class GameState : Node
{
    public static GameState Instance { get; private set; } = null!;

    [Signal] public delegate void StatsChangedEventHandler();
    [Signal] public delegate void PlayerDiedEventHandler();

    private int _hp = 100;
    public int Hp
    {
        get => _hp;
        set
        {
            _hp = Math.Clamp(value, 0, MaxHp);
            EmitSignal(SignalName.StatsChanged);
            if (_hp <= 0 && !IsDead)
            {
                IsDead = true;
                EmitSignal(SignalName.PlayerDied);
            }
        }
    }

    private int _maxHp = 100;
    public int MaxHp
    {
        get => _maxHp;
        set
        {
            _maxHp = value;
            EmitSignal(SignalName.StatsChanged);
        }
    }

    private int _xp = 0;
    public int Xp
    {
        get => _xp;
        set
        {
            _xp = value;
            EmitSignal(SignalName.StatsChanged);
        }
    }

    private int _level = 1;
    public int Level
    {
        get => _level;
        set
        {
            _level = value;
            EmitSignal(SignalName.StatsChanged);
        }
    }

    private int _mana = 0;
    public int Mana
    {
        get => _mana;
        set
        {
            _mana = Math.Clamp(value, 0, MaxMana);
            EmitSignal(SignalName.StatsChanged);
        }
    }

    private int _maxMana = 0;
    public int MaxMana
    {
        get => _maxMana;
        set
        {
            _maxMana = value;
            EmitSignal(SignalName.StatsChanged);
        }
    }

    private int _floorNumber = 1;
    public int FloorNumber
    {
        get => _floorNumber;
        set
        {
            _floorNumber = value;
            if (value > DeepestFloor)
                DeepestFloor = value;
            EmitSignal(SignalName.StatsChanged);
        }
    }

    public bool IsDead { get; set; } = false;
    public int DeepestFloor { get; set; } = 1;
    public PlayerClass SelectedClass { get; set; } = PlayerClass.Warrior;
    public Inventory PlayerInventory { get; set; } = new(Constants.PlayerStats.BackpackStartingSlots);
    public StatBlock Stats { get; private set; } = new();
    public ProgressionTracker Progression { get; set; } = new(PlayerClass.Warrior);
    public Bank PlayerBank { get; set; } = new();
    public QuestTracker Quests { get; set; } = new();
    public AchievementTracker Achievements { get; set; } = new();

    // Skill hotbar (Diablo-style, keys 1-4)
    public SkillBar SkillHotbar { get; set; } = new();

    // Endgame systems (END-01 through END-05)
    public ZoneSaturation Saturation { get; set; } = new();
    public DungeonPacts Pacts { get; set; } = new();
    public DungeonIntelligence Intelligence { get; set; } = new();
    public MagiculeAttunement Attunement { get; set; } = new();

    // Equipment (SYS-11)
    public EquipmentSet Equipment { get; set; } = new();

    /// <summary>
    /// Shared RNG for combat rolls (crit / dodge / block / flurry). New per-run,
    /// not serialized. Named so deterministic-replay concerns can thread a fixed
    /// seed through this single field later without touching every callsite.
    /// </summary>
    public Random CombatRng { get; private set; } = new();

    /// <summary>
    /// COMBAT-01 §8: in-game time (seconds) when the active Phase i-frame
    /// window expires. `0` = no active Phase. Consumed on the next incoming
    /// hit OR auto-expires at the stored time, whichever comes first.
    /// Transient — not serialized (phase is a ~500ms window, save/load never
    /// fires mid-combat).
    /// </summary>
    private double _phaseExpiresAt;
    public bool IsPhased => _phaseExpiresAt > 0 && _phaseTimeSource() < _phaseExpiresAt;

    // Indirection so tests can inject a fake time source. Default = process ticks.
    private System.Func<double> _phaseTimeSource = () => Time.GetTicksMsec() / 1000.0;
    public void SetPhaseTimeSourceForTests(System.Func<double> source) => _phaseTimeSource = source;
    public double GetPhaseExpiresAtForTests() => _phaseExpiresAt;
    public void SetPhaseExpiresAtForTests(double value) => _phaseExpiresAt = value;

    /// <summary>
    /// Index (0..SaveManager.SlotCount-1) of the save slot this character owns.
    /// Set when a slot is loaded (Load Game) OR reserved (New Game splash flow), and
    /// preserved across <see cref="Reset"/> — the slot is character identity, not run state.
    /// Null only at boot before any New-Game reservation or Load-Game has happened.
    /// </summary>
    public int? CurrentSaveSlot { get; set; }

    public override void _Ready()
    {
        Instance = this;
        GameSettings.Load();
    }

    public void Reset()
    {
        IsDead = false;
        MaxHp = Constants.PlayerStats.StartingHp;
        Hp = Constants.PlayerStats.StartingHp;
        int baseMana = Constants.PlayerStats.GetClassBaseMana(SelectedClass);
        MaxMana = baseMana;
        Mana = baseMana;
        Xp = 0;
        Level = 1;
        FloorNumber = 1;
        DeepestFloor = 1;
        PlayerInventory = new Inventory(Constants.PlayerStats.BackpackStartingSlots);
        PlayerInventory.Gold = Constants.PlayerStats.StartingGold;
        Stats = new StatBlock();
        Progression = new ProgressionTracker(SelectedClass);
        PlayerBank = new Bank();
        Quests = new QuestTracker();
        Quests.GenerateQuests(1);
        Achievements = new AchievementTracker();
        SkillHotbar = new SkillBar();
        Saturation = new ZoneSaturation();
        Pacts = new DungeonPacts();
        Intelligence = new DungeonIntelligence();
        Attunement = new MagiculeAttunement();
        Equipment = new EquipmentSet();
        // COMBAT-01 §7: fresh run → fresh RNG stream. Without this, back-to-
        // back runs in the same app session share the same PRNG sequence and
        // Crit/Flurry/Dodge/Block rolls correlate across runs, which is
        // surprising and breaks the "each new game is a clean deal" feel.
        CombatRng = new Random();
        _phaseExpiresAt = 0;
        // CurrentSaveSlot is intentionally preserved across Reset(): it identifies
        // which save file this character owns, not run state. The New Game flow
        // reserves a slot on the splash screen BEFORE ClassSelect.Reset() runs;
        // wiping it here would cause the first autosave to fall through to slot 0
        // and silently overwrite an existing save.
        EquipStartingGear();
    }

    private void EquipStartingGear()
    {
        // Per docs/systems/equipment.md: starting weapon + off-hand/ammo, no armor or accessories.
        // Uses tier-1 catalog items (ITEM-01).
        string mainHandId = SelectedClass switch
        {
            PlayerClass.Warrior => "mainhand_warrior_sword_t1",
            PlayerClass.Ranger => "mainhand_ranger_shortbow_t1",
            PlayerClass.Mage => "mainhand_mage_staff_t1",
            _ => "mainhand_warrior_sword_t1",
        };
        (string offOrAmmoId, EquipSlot slot) = SelectedClass switch
        {
            PlayerClass.Warrior => ("offhand_warrior_shield_small_t1", EquipSlot.OffHand),
            PlayerClass.Ranger => ("ammo_quiver_basic", EquipSlot.Ammo),
            PlayerClass.Mage => ("offhand_mage_grimoire_t1", EquipSlot.OffHand),
            _ => ("offhand_warrior_shield_small_t1", EquipSlot.OffHand),
        };
        var main = ItemDatabase.Get(mainHandId);
        if (main != null) Equipment.ForceEquip(EquipSlot.MainHand, main);
        var off = ItemDatabase.Get(offOrAmmoId);
        if (off != null) Equipment.ForceEquip(slot, off);
    }

    public void AwardXp(int amount)
    {
        Xp += amount;
        int xpToLevel = Constants.Leveling.GetXpToLevel(Level);
        while (Xp >= xpToLevel)
        {
            Xp -= xpToLevel;
            Level += 1;

            // Apply class stat bonuses for this level
            Stats.ApplyClassLevelBonus(SelectedClass);

            // Spec (point-economy.md): SP — 2 per level, +1 at milestones; AP — 3 per level, +2 at milestones
            bool isMilestone = Level % 10 == 0;
            Progression.SkillPoints += isMilestone ? 3 : 2;
            Progression.AbilityPoints += isMilestone ? 5 : 3;

            // Spec (leveling.md): stat points — 3 per level, +2 extra at milestones (every 10th)
            if (isMilestone)
                Stats.FreePoints += 2; // bonus on top of the 3 from ApplyClassLevelBonus

            // Achievement counters
            Achievements.SetCounter("player_level", Level);
            string classCounter = SelectedClass switch
            {
                PlayerClass.Warrior => "warrior_level",
                PlayerClass.Ranger => "ranger_level",
                PlayerClass.Mage => "mage_level",
                _ => "player_level",
            };
            Achievements.SetCounter(classCounter, Level);

            // COMBAT-01 §5: unified recompute folds stat allocation AND
            // equipment overlays into MaxHp / MaxMana. Replaces two separate
            // calls that used only Stats.BonusMaxHp / BonusMaxMana and
            // ignored gear.
            RecomputeDerivedStats();
            // Spec: HP restore = floor(max_hp * 0.15)
            int healAmount = (int)(MaxHp * Constants.PlayerStats.HealOnLevelUpPercent);
            Hp = Math.Min(MaxHp, Hp + healAmount);
            Mana = MaxMana; // Full mana restore on level-up
            xpToLevel = Constants.Leveling.GetXpToLevel(Level);
        }
    }

    /// <summary>
    /// COMBAT-01 §5: unified MaxHp / MaxMana recompute. Folds equipment stat
    /// overlays into the StatBlock curve so all four prior callsites
    /// (StatAllocDialog, PauseMenu, DebugConsole, level-up inside AwardXp)
    /// end up at the same derived numbers. Must be called after any equipment
    /// change, stat allocation, or level-up.
    /// </summary>
    public void RecomputeDerivedStats()
    {
        // COMBAT-01 §1 overlay model: equipment stats stack onto allocated
        // stats BEFORE the DR curve, producing a single effective value per
        // stat. Using StatBlock.BonusMaxHp / BonusMaxMana directly would
        // apply DR to allocated stats only and lose the spec invariant.
        var es = Equipment.GetCombatStats(SelectedClass);
        int effectiveSta = Stats.Sta + (int)es.Sta;
        int effectiveInt = Stats.Int + (int)es.Int;
        int staDerivedHp = (int)(StatBlock.GetEffective(effectiveSta) * 5.0f);
        int intDerivedMana = (int)(StatBlock.GetEffective(effectiveInt) * 4.0f);

        MaxHp = Constants.PlayerStats.GetEffectiveMaxHp(
            Level,
            staDerivedHp + (int)es.BonusHp);
        MaxMana = Constants.PlayerStats.GetClassBaseMana(SelectedClass)
                + intDerivedMana;
    }

    public void TakeDamage(int amount)
    {
        if (IsDead) return;

        // COMBAT-01 §8 resolution order: Phase → Dodge → Block → Hp subtract.
        var es = Equipment.GetCombatStats(SelectedClass);

        // 1. PHASE — i-frame carryover from a prior dodge. Consumes on this hit.
        if (IsPhased)
        {
            _phaseExpiresAt = 0;
            EventBus.Instance?.EmitSignal(EventBus.SignalName.PlayerPhased);
            return;
        }

        // If IsPhased was false but _phaseExpiresAt is still non-zero, a
        // prior Phase window elapsed without being consumed. Clear it so
        // future HUD code keying off `_phaseExpiresAt > 0` (buff-bar icon,
        // etc.) doesn't see a ghost-active Phase.
        if (_phaseExpiresAt > 0) _phaseExpiresAt = 0;

        // 2. DODGE — COMBAT-01 §1 overlay: equipment DEX stacks onto allocated
        //    DEX before the DR curve, then derives dodge chance. Combat-ring
        //    DodgeRaw is additive on top. Overflow of the combined raw becomes
        //    Phase duration (ms, hard-capped at 500).
        int effDex = Stats.Dex + (int)es.Dex;
        float dodgeRaw = StatBlock.ComputeDodgeChance(effDex) * 100f + es.DodgeRaw;
        float dodgeEff = CombatFormulas.SoftCap(dodgeRaw);
        if (CombatRng.NextSingle() < dodgeEff / 100f)
        {
            float phaseMs = CombatFormulas.PhaseDurationMs(dodgeRaw);
            _phaseExpiresAt = _phaseTimeSource() + phaseMs / 1000.0;
            EventBus.Instance?.EmitSignal(EventBus.SignalName.PlayerDodged);
            return;
        }

        // 3. BLOCK — soft-capped chance; overflow raises reduction % over 50%.
        int incoming = amount;
        float blockRaw = es.BlockRaw;
        float blockEff = CombatFormulas.SoftCap(blockRaw);
        if (CombatRng.NextSingle() < blockEff / 100f)
        {
            float reduction = CombatFormulas.BlockReduction(blockRaw);
            incoming = (int)(incoming * (1f - reduction));
            EventBus.Instance?.EmitSignal(EventBus.SignalName.PlayerBlocked);
        }

        Hp -= incoming;
    }

    /// <summary>
    /// Evaluate achievements and show toasts for any newly unlocked.
    /// Call after significant game events.
    /// </summary>
    public void CheckAchievements()
    {
        var newlyUnlocked = Achievements.Evaluate();
        foreach (var achievement in newlyUnlocked)
        {
            string msg = $"Achievement: {achievement.Name}";
            if (achievement.GoldReward > 0)
            {
                PlayerInventory.Gold += achievement.GoldReward;
                msg += $" (+{achievement.GoldReward}g)";
            }
            Ui.Toast.Instance?.Success(msg);
        }
    }
}
