using System;

public static class EntityFactory
{
    public static EntityData CreatePlayer(string name = "Hero")
    {
        var entity = new EntityData
        {
            Id = Guid.NewGuid().ToString(),
            Name = name,
            Type = EntityType.Player,
            Level = 1,
            HP = 108,
            MaxHP = 108,
            MP = 65,
            MaxMP = 65,
            STR = 5,
            DEX = 5,
            INT = 5,
            VIT = 5,
            BaseDamage = 12,
            BaseDefense = 0,
            AttackSpeed = 0.42f,
            AttackRange = 78f,
            HitboxRadius = 12f,
            MoveSpeed = 190f,
            Gold = 100,
            InventorySize = 25,
        };
        return entity;
    }

    public static EntityData CreateEnemy(string name, int tier, int floorNumber = 1)
    {
        float floorMult = 1 + (floorNumber - 1) * 0.5f;
        int baseHP = tier switch { 1 => 30, 2 => 42, 3 => 54, _ => 30 };
        int baseXP = tier switch { 1 => 10, 2 => 15, 3 => 20, _ => 10 };

        var entity = new EntityData
        {
            Id = Guid.NewGuid().ToString(),
            Name = name,
            Type = EntityType.Enemy,
            Level = tier,
            HP = (int)(baseHP * floorMult),
            MaxHP = (int)(baseHP * floorMult),
            BaseDamage = 3 + tier,
            BaseDefense = tier * 2,
            AttackSpeed = 1.0f,
            AttackRange = 60f,
            HitboxRadius = 14f,
            MoveSpeed = 120f,
            Tier = tier,
            XPReward = (int)(baseXP * floorMult),
            GoldReward = (int)(5 * floorMult * tier),
        };
        return entity;
    }

    public static EntityData CreateNPC(string name)
    {
        var entity = new EntityData
        {
            Id = Guid.NewGuid().ToString(),
            Name = name,
            Type = EntityType.NPC,
            Level = 1,
            HP = 100,
            MaxHP = 100,
            MP = 0,
            MaxMP = 0,
            MoveSpeed = 80f,
            HitboxRadius = 12f,
        };
        return entity;
    }
}
