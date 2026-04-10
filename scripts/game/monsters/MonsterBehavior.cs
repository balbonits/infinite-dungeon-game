using System;

public static class MonsterBehavior
{
    public static float GetAggroRange(MonsterArchetype archetype) => archetype switch
    {
        MonsterArchetype.Melee => 384f,
        MonsterArchetype.Ranged => 512f,
        MonsterArchetype.Bruiser => 320f,
        MonsterArchetype.Swarmer => 448f,
        MonsterArchetype.Support => 384f,
        _ => 384f
    };

    public static float GetAttackRange(MonsterArchetype archetype) => archetype switch
    {
        MonsterArchetype.Melee => 30f,
        MonsterArchetype.Ranged => 320f,
        MonsterArchetype.Bruiser => 40f,
        MonsterArchetype.Swarmer => 25f,
        MonsterArchetype.Support => 256f,
        _ => 30f
    };

    public static float GetPreferredDistance(MonsterArchetype archetype) => archetype switch
    {
        MonsterArchetype.Ranged => 256f,
        MonsterArchetype.Support => 192f,
        _ => 0f
    };

    public static float GetAttackCooldown(MonsterArchetype archetype) => archetype switch
    {
        MonsterArchetype.Melee => 1.0f,
        MonsterArchetype.Ranged => 1.5f,
        MonsterArchetype.Bruiser => 2.0f,
        MonsterArchetype.Swarmer => 0.4f,
        MonsterArchetype.Support => 2.5f,
        _ => 1.0f
    };

    public static float GetAlertDuration(MonsterArchetype archetype) => archetype switch
    {
        MonsterArchetype.Swarmer => 0f,
        MonsterArchetype.Bruiser => 0.5f,
        _ => 0.3f
    };

    public static float GetSpeedMultiplier(MonsterArchetype archetype) => archetype switch
    {
        MonsterArchetype.Melee => 1.0f,
        MonsterArchetype.Ranged => 0.9f,
        MonsterArchetype.Bruiser => 0.6f,
        MonsterArchetype.Swarmer => 1.3f,
        MonsterArchetype.Support => 0.8f,
        _ => 1.0f
    };

    public static MonsterAIState GetNextState(
        MonsterAIState currentState,
        MonsterArchetype archetype,
        float distanceToPlayer,
        float currentHP,
        float maxHP,
        float alertTimer,
        float cooldownTimer)
    {
        if (currentHP <= 0) return MonsterAIState.Dead;

        float aggroRange = GetAggroRange(archetype);
        float attackRange = GetAttackRange(archetype);
        float preferredDist = GetPreferredDistance(archetype);

        switch (currentState)
        {
            case MonsterAIState.Idle:
                if (distanceToPlayer <= aggroRange)
                    return GetAlertDuration(archetype) > 0 ? MonsterAIState.Alert : MonsterAIState.Chase;
                return MonsterAIState.Idle;

            case MonsterAIState.Alert:
                if (alertTimer <= 0) return MonsterAIState.Chase;
                return MonsterAIState.Alert;

            case MonsterAIState.Chase:
                if (distanceToPlayer > aggroRange * 1.5f) return MonsterAIState.Idle;
                if (distanceToPlayer <= attackRange) return MonsterAIState.Attack;
                return MonsterAIState.Chase;

            case MonsterAIState.Attack:
                return MonsterAIState.Cooldown;

            case MonsterAIState.Cooldown:
                if (cooldownTimer <= 0)
                {
                    if (preferredDist > 0 && distanceToPlayer < preferredDist * 0.7f)
                        return MonsterAIState.Retreat;
                    if (preferredDist > 0)
                        return MonsterAIState.Reposition;
                    return MonsterAIState.Chase;
                }
                return MonsterAIState.Cooldown;

            case MonsterAIState.Reposition:
                if (distanceToPlayer <= attackRange && distanceToPlayer >= preferredDist * 0.7f)
                    return MonsterAIState.Chase;
                if (distanceToPlayer < preferredDist * 0.5f)
                    return MonsterAIState.Retreat;
                return MonsterAIState.Reposition;

            case MonsterAIState.Retreat:
                if (distanceToPlayer >= preferredDist)
                    return MonsterAIState.Chase;
                return MonsterAIState.Retreat;

            case MonsterAIState.Flee:
                return MonsterAIState.Flee;

            case MonsterAIState.Dead:
                return MonsterAIState.Dead;

            default:
                return MonsterAIState.Idle;
        }
    }
}
