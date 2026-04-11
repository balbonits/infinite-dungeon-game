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
    public Inventory PlayerInventory { get; private set; } = new(25);

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
        PlayerInventory.Gold = 100; // Starting gold
    }

    public void AwardXp(int amount)
    {
        Xp += amount;
        int xpToLevel = Constants.Leveling.GetXpToLevel(Level);
        while (Xp >= xpToLevel)
        {
            Xp -= xpToLevel;
            Level += 1;
            MaxHp = Constants.PlayerStats.GetMaxHp(Level);
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
}
