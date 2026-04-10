public enum MonsterArchetype
{
    Melee,      // Direct chase, close range attack
    Ranged,     // Maintains distance, projectile attacks
    Bruiser,    // Slow, high HP, heavy hits, telegraphed
    Swarmer,    // Fast, low HP, spawns in groups
    Support     // Buffs allies, debuffs player
}

public enum MonsterAIState
{
    Idle,
    Alert,
    Chase,
    Attack,
    Cooldown,
    Reposition,
    Retreat,
    Flee,
    Dead
}

public enum MonsterRarity
{
    Normal,     // Base stats, spawns in packs
    Empowered,  // 2x HP, 1.5x rewards, 1 modifier
    Named       // 3x HP, 3x rewards, 2-3 modifiers
}
