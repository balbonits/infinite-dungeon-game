# State Machines for Games

## Why This Matters
Our enemies have a basic chase→attack loop with no states, which means they can attack while dead, chase while in cooldown, and stack on top of each other with no behavioral variety. State machines are how every game organizes entity behavior — player, enemy, UI, animation. Without them, logic becomes spaghetti.

## Core Concepts

### What Is a Finite State Machine (FSM)?
An FSM is an entity that can be in exactly ONE state at any time. Each state defines:
- **What happens while in this state** (behavior)
- **When to transition to another state** (conditions)
- **What happens on entry/exit** (setup/teardown)

### Player States
A typical ARPG player has these states:

```
IDLE ←→ WALK (input)
WALK → ATTACK (button press)
ATTACK → IDLE (animation complete)
ANY → HIT (took damage, brief stun)
ANY → DEAD (HP ≤ 0)
```

Each state controls:
- **Idle**: No movement velocity. Can accept input. Play idle animation.
- **Walk**: Velocity from input. Play walk animation. Can attack.
- **Attack**: Movement reduced/stopped. Play attack animation. Deal damage at specific frame. Return to idle after animation ends.
- **Hit**: Brief stagger (100-200ms). Flash red. Can't input. Return to idle.
- **Dead**: No input. Play death animation. Game over or respawn.

### Enemy AI States
Our `MonsterBehavior.cs` already uses this pattern:

```
IDLE → ALERT (player in aggro range)
ALERT → CHASE (alert timer expires)
CHASE → ATTACK (in attack range)
ATTACK → COOLDOWN (always, after attack)
COOLDOWN → CHASE (melee, timer expires)
COOLDOWN → REPOSITION (ranged, move to preferred distance)
REPOSITION → CHASE (at preferred distance)
RETREAT → CHASE (at safe distance)
ANY → DEAD (HP ≤ 0)
```

Key design decisions:
- **Swarmers skip ALERT** — they rush immediately (scarier)
- **Bruisers have longer cooldown** — creates vulnerability windows
- **Ranged enemies have REPOSITION and RETREAT** — they maintain distance
- **Leash range** (1.5x aggro range) — enemies give up chase if player runs far enough

### Implementing an FSM in C#

**Approach 1: Enum + Switch (simple, our approach)**
```csharp
public enum EnemyState { Idle, Alert, Chase, Attack, Cooldown, Dead }

private EnemyState _state = EnemyState.Idle;
private float _timer;

public override void _Process(double delta)
{
    switch (_state)
    {
        case EnemyState.Idle:
            if (DistanceToPlayer() < AggroRange)
                TransitionTo(EnemyState.Alert);
            break;
            
        case EnemyState.Alert:
            _timer -= (float)delta;
            if (_timer <= 0)
                TransitionTo(EnemyState.Chase);
            break;
            
        case EnemyState.Chase:
            MoveTowardPlayer(delta);
            if (DistanceToPlayer() < AttackRange)
                TransitionTo(EnemyState.Attack);
            if (DistanceToPlayer() > AggroRange * 1.5f)
                TransitionTo(EnemyState.Idle);  // Leash
            break;
            
        case EnemyState.Attack:
            PerformAttack();
            TransitionTo(EnemyState.Cooldown);
            break;
            
        case EnemyState.Cooldown:
            _timer -= (float)delta;
            if (_timer <= 0)
                TransitionTo(EnemyState.Chase);
            break;
            
        case EnemyState.Dead:
            break;  // Do nothing
    }
}

private void TransitionTo(EnemyState newState)
{
    OnExit(_state);
    _state = newState;
    OnEnter(_state);
}

private void OnEnter(EnemyState state)
{
    switch (state)
    {
        case EnemyState.Alert: _timer = 0.3f; break;
        case EnemyState.Cooldown: _timer = 1.0f; break;
        case EnemyState.Dead: PlayDeathAnimation(); break;
    }
}
```

**Approach 2: Pure Function (our MonsterBehavior.cs approach)**
```csharp
// Stateless — no object, just a function
public static MonsterAIState GetNextState(
    MonsterAIState current, MonsterArchetype archetype,
    float distToPlayer, float hp, float maxHp,
    float alertTimer, float cooldownTimer)
{
    if (hp <= 0) return MonsterAIState.Dead;
    
    return current switch
    {
        MonsterAIState.Idle when distToPlayer <= aggroRange => MonsterAIState.Alert,
        MonsterAIState.Alert when alertTimer <= 0 => MonsterAIState.Chase,
        // ...etc
    };
}
```

Pure functions are easier to test (no object setup, just pass values and check return).

### Animation State Machines
Animation states mirror gameplay states but aren't always 1:1:

| Gameplay State | Animation State | Why Different |
|---------------|-----------------|---------------|
| Idle | IdleAnim | Same |
| Walk | WalkAnim | Same |
| Attack | AttackWindUp → AttackStrike → AttackRecovery | Attack has sub-phases |
| Hit | HitStagger → HitRecover | Brief sub-animation |
| Dead | DeathAnim (plays once) | Single animation, no loop |

Godot's `AnimationTree` can handle this with a state machine node, but for our needs, a simple enum-based approach with `AnimatedSprite2D.Play("animation_name")` works fine.

### When FSM Isn't Enough
FSMs struggle when:
- **Too many states** → "state explosion" (100+ states with similar logic)
- **Need concurrent behaviors** → FSM is one state at a time; can't "patrol AND scan for player"
- **Need priority-based decisions** → utility AI or behavior trees handle "what's most important right now"

For our game, FSMs are sufficient. We have 5 enemy archetypes with 9 states each. That's manageable.

## Common Mistakes
1. **States that can't exit** — soft lock (player stuck in attack forever because animation didn't end)
2. **Missing "dead" transition from every state** — enemy keeps chasing while dead
3. **No enter/exit callbacks** — timer not reset on state entry, so cooldown is wrong
4. **Updating logic in wrong state** — enemy moves during attack animation
5. **Animation not synced to state** — visual shows idle but logic is in attack
6. **Transitions in wrong order** — checking attack range before checking if dead
7. **No leash** — enemy chases player across the entire map

## Checklist
- [ ] Every entity has an explicit state enum
- [ ] `Dead` is reachable from ANY state (always check HP first)
- [ ] Each state has clear entry conditions and exit conditions
- [ ] Timers are reset in OnEnter, not in the transition caller
- [ ] Animation matches current state
- [ ] Leash range prevents infinite chasing
- [ ] States are tested: `MonsterBehavior.GetNextState()` with various inputs

## Sources
- [Game Programming Patterns: State (Robert Nystrom)](https://gameprogrammingpatterns.com/state.html)
- [Godot StateMachine Tutorial](https://docs.godotengine.org/en/stable/tutorials/plugins/running_code_in_the_editor.html)
- [GDC: Building a Better Centaur (AI architecture)](https://www.gdcvault.com/play/1018058/Building-a-Better-Centaur-AI)
- [Behavior Trees for Game AI](https://www.gamedeveloper.com/design/behavior-trees-for-ai-how-they-work)
