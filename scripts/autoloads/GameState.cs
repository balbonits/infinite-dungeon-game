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

    private int _floorNumber = 1;
    public int FloorNumber
    {
        get => _floorNumber;
        set
        {
            _floorNumber = value;
            EmitSignal(SignalName.StatsChanged);
        }
    }

    public bool IsDead { get; set; } = false;
    public PlayerClass SelectedClass { get; set; } = PlayerClass.Warrior;
    public Inventory PlayerInventory { get; set; } = new(25);
    public StatBlock Stats { get; private set; } = new();
    public SkillTracker Skills { get; set; } = new(PlayerClass.Warrior);
    public Bank PlayerBank { get; set; } = new();
    public QuestTracker Quests { get; set; } = new();
    public AchievementTracker Achievements { get; set; } = new();

    public override void _Ready()
    {
        Instance = this;
    }

    public void Reset()
    {
        IsDead = false;
        MaxHp = Constants.PlayerStats.StartingHp;
        Hp = Constants.PlayerStats.StartingHp;
        Xp = 0;
        Level = 1;
        FloorNumber = 1;
        PlayerInventory = new Inventory(25);
        PlayerInventory.Gold = 100;
        Stats = new StatBlock();
        Skills = new SkillTracker(SelectedClass);
        PlayerBank = new Bank();
        Quests = new QuestTracker();
        Quests.GenerateQuests(1);
        Achievements = new AchievementTracker();
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

            // Award skill points (2 per level, 3 at milestones every 25 levels)
            int skillPointsAwarded = (Level % 25 == 0) ? 3 : 2;
            Skills.SkillPoints += skillPointsAwarded;

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

            // Recalculate MaxHp including STA bonus
            MaxHp = Constants.PlayerStats.GetMaxHp(Level) + Stats.BonusMaxHp;
            Hp = Math.Min(MaxHp, Hp + Constants.PlayerStats.HealOnLevelUp);
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
