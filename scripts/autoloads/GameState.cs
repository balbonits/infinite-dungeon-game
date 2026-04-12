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
    public Inventory PlayerInventory { get; set; } = new(25);
    public StatBlock Stats { get; private set; } = new();
    public SkillTracker Skills { get; set; } = new(PlayerClass.Warrior);
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

    public override void _Ready()
    {
        Instance = this;
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
        PlayerInventory = new Inventory(25);
        PlayerInventory.Gold = 100;
        Stats = new StatBlock();
        Skills = new SkillTracker(SelectedClass);
        PlayerBank = new Bank();
        Quests = new QuestTracker();
        Quests.GenerateQuests(1);
        Achievements = new AchievementTracker();
        SkillHotbar = new SkillBar();
        Saturation = new ZoneSaturation();
        Pacts = new DungeonPacts();
        Intelligence = new DungeonIntelligence();
        Attunement = new MagiculeAttunement();
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

            // Spec (leveling.md): skill points — 2 per level, +1 extra at milestones (every 10th)
            bool isMilestone = Level % 10 == 0;
            Skills.SkillPoints += isMilestone ? 3 : 2;

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

            // Recalculate MaxHp including STA bonus (spec: leveling.md)
            MaxHp = Constants.PlayerStats.GetMaxHp(Level) + Stats.BonusMaxHp;
            // Spec: HP restore = floor(max_hp * 0.15)
            int healAmount = (int)(MaxHp * Constants.PlayerStats.HealOnLevelUpPercent);
            Hp = Math.Min(MaxHp, Hp + healAmount);

            // Recalculate MaxMana including INT bonus (spec: stats.md)
            MaxMana = Constants.PlayerStats.GetClassBaseMana(SelectedClass) + Stats.BonusMaxMana;
            Mana = MaxMana; // Full mana restore on level-up
            xpToLevel = Constants.Leveling.GetXpToLevel(Level);
        }
    }

    public void TakeDamage(int amount)
    {
        if (IsDead)
            return;
        Hp -= amount;
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
