# Flow: Combat

**Script:** `scripts/Player.cs` (HandleAttack, ExecuteAttack)

## Auto-Attack

Combat is fully automatic. No button press needed. Player attacks nearest enemy within range every cooldown tick.

### Per-Class Attack Config

| Class | Primary Attack | Range | Cooldown | Fallback |
|-------|---------------|-------|----------|----------|
| Warrior | Melee slash | 78px | 0.42s | — |
| Ranger | Arrow projectile | 250px | 0.55s | — |
| Mage | Magic bolt projectile | 200px | 0.80s | Staff melee (78px, 0.50s) when enemy close |

### Attack Loop (every frame)

```
HandleAttack(delta):
1. Decrement _attackTimer by delta
2. If timer > 0 → return (on cooldown)
3. FindNearestEnemy():
   - Search _attackArea.GetOverlappingBodies() for group "enemies"
   - Return closest or null
4. If no enemy → return
5. Choose attack:
   - If _meleeFallback exists AND distance <= fallback range → use melee
   - Else → use primary (projectile or extended melee)
6. ExecuteAttack(attack, target)
7. Emit EventBus.PlayerAttacked signal
```

### Damage Calculation

```
baseDamage = Constants.PlayerStats.GetDamage(Level)

If projectile:
  statBonus = stats.SpellDamageMultiplier (INT-based)
If melee:
  statBonus = 1.0 + stats.MeleePercentBoost / 100
  flatBonus = stats.MeleeFlatBonus (STR-based)

finalDamage = (baseDamage + flatBonus) * attack.DamageMultiplier * statBonus
cooldown = attack.Cooldown / stats.AttackSpeedMultiplier (DEX speeds it up)
```

## Ability Hotbar

**Script:** `scripts/ui/AbilityBarHud.cs` *(was `SkillBarHud.cs`)*

4 slots activated by shoulder + face button combos:

| Slot | Input | Constants |
|------|-------|-----------|
| 0 | Q + W | shoulder_left + action_triangle |
| 1 | Q + S | shoulder_left + action_cross |
| 2 | E + W | shoulder_right + action_triangle |
| 3 | E + S | shoulder_right + action_cross |

Detection: `Input.IsActionPressed(shoulder)` AND `Input.IsActionJustPressed(face)`

### Ability Activation

```
1. DetectSlot() returns slot index (0-3) or -1
2. AbilityBar.TryActivate(slotIndex, cooldown) → returns ability ID or null
3. If null (on cooldown or empty) → return
4. Check mana cost: GameState.Mana >= ability.ManaCost
5. Deduct mana, execute attack with ability's AttackConfig
6. Record use — grants Ability XP to the ability used AND Skill XP to its parent mastery
```

## Enemy Death

```
1. Enemy HP reaches 0
2. Enemy emits death signal
3. EventBus.EnemyDefeated(position, tier) emitted
4. XP awarded: Constants.EnemyStats.GetXpReward(level)
5. Gold dropped: LootTable.GetGoldDrop(level)
6. Item roll: LootTable.RollItemDrop(level)
7. Achievement counter: IncrementCounter("enemies_killed")
8. Saturation: ZoneSaturation.RecordKill(zone)
9. Quest: QuestTracker.RecordEnemyKill(floor)
```

## Grace Period (Invincibility)

On spawn/level-up: `StartGracePeriod()`
- Duration: `Constants.PlayerStats.GracePeriod`
- Player flickers (modulate alpha at ~10 Hz)
- Cannot take damage while active
