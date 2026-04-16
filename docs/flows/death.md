# Flow: Death & Respawn

**Script:** `scripts/ui/DeathScreen.cs`
**Trigger:** `GameState.PlayerDied` signal (HP reaches 0)
**Existing spec:** `docs/systems/death.md`, `docs/ui/death-screen.md`

## Trigger

```
1. Enemy hits player → GameState.TakeDamage(amount)
2. Hp setter: _hp = Math.Clamp(value, 0, MaxHp)
3. If _hp <= 0 AND !IsDead:
   a. IsDead = true
   b. Emit PlayerDied signal
4. Main.OnPlayerDied():
   a. GetTree().Paused = true
   b. DeathScreen.ShowDeathFlow()
5. DeathScreen plays the SoulsBorne cinematic, THEN shows the menu.
```

## "YOU DIED" Cinematic

Inspired by FromSoftware's Souls games. Plays for ~6.3 seconds before the mitigation menu appears. The menu panel is completely hidden (`Modulate.A = 0`, `Visible = false`) during this cinematic.

| Phase | Duration | What happens |
|-------|----------|--------------|
| 1. Overlay fade | 1.2s | Black overlay with subtle red tint fades in over the scene |
| 2. Text fade in | 1.5s | Big red "YOU DIED" text fades in slowly — drama beat |
| 3. Hold | 2.5s | Player absorbs the loss; no input accepted |
| 4. Text fade out | 0.8s | "YOU DIED" disappears; overlay stays dark |
| 5. Panel reveal | 0.3s | Mitigation menu panel fades in, first button auto-focused |

During the cinematic:
- `DeathScreen.IsPlayingCinematic` returns `true`
- The tween uses `Tween.TweenPauseMode.Process` so it runs even though the tree is paused
- No keyboard input is accepted (menu isn't focusable yet)

Once the panel appears, the normal 3-step flow begins.

## 3-Step UI Flow

### Step 1: Choose Destination

- Title: "You are dead"
- Button: "Return to Town" → calls `ShowStep2()`
- Auto-focus first button
- No cancel option — player must proceed forward

### Step 2: Mitigations

Penalties calculated from **deepest floor ever reached** (not current floor):

| Penalty | Formula |
|---------|---------|
| XP loss % | `DeathPenalty.GetExpLossPercent(deepestFloor)` |
| Items lost | `DeathPenalty.GetItemsLost(deepestFloor)` |
| XP protect cost | `DeathPenalty.GetExpProtectionCost(deepestFloor)` |
| Backpack protect cost | `DeathPenalty.GetBackpackProtectionCost(deepestFloor)` |

Options:
- Toggle: "Protect XP (XXg)" — if player can afford
- Auto-check: Sacrificial Idol detected → backpack auto-protected
- Toggle: "Protect Backpack (XXg)" — if no idol and can afford
- Button: "Confirm" → `ApplyPenaltiesAndRespawn()`

### Step 3: Apply Penalties & Respawn

```
ApplyPenaltiesAndRespawn():
1. If NOT protectXp:
   → Deduct CalculateXpLoss(currentXp, deepestFloor) from GameState.Xp
2. Else: deduct protection gold cost

3. If hasIdol:
   → ConsumeSacrificialIdol() (removes from inventory)
4. Else if NOT protectBackpack:
   → ApplyItemLoss(inventory, itemsLost) (removes random items)
5. Else: deduct protection gold cost

6. Reset:
   → IsDead = false
   → Hp = MaxHp
   → FloorNumber = 1
7. Hide death screen
8. Unpause game
9. Main.Instance.LoadTown()
```

## Input

| Input | Action |
|-------|--------|
| Up/Down | Navigate buttons |
| S / action_cross | Press focused button |
| Escape | Quit game (no back/cancel) |

## AutoPilot Sequence (force death)

```csharp
// Force death via debug kill command
// (assumes debug console 'kill' command exists)
// OR: GameState.Instance.Hp = 0 directly

// Wait for death screen
await act.WaitUntil(() => GameState.Instance.IsDead, 5f);
await act.WaitSeconds(0.5);

// Step 1: Press "Return to Town"
act.Press("action_cross");
await act.WaitSeconds(0.5);

// Step 2: Press "Confirm" (skip mitigations on floor 1)
act.Press("move_down"); // navigate to Confirm
await act.WaitSeconds(0.2);
act.Press("action_cross");
await act.WaitSeconds(0.5);

// Wait for respawn transition
await act.WaitForTransition();
await act.WaitSeconds(1.0);

// Verify: alive in town
verify.Alive();
```
