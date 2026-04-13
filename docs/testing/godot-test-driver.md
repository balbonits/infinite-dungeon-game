# GodotTestDriver Integration

## Summary

Replace hand-rolled input injection in AutoPilotActions with Chickensoft's GodotTestDriver library. This gives us battle-tested input simulation, built-in node drivers, and a fixture pattern for scene lifecycle -- eliminating the async/timing issues in our current AutoPilot while keeping the game-specific assertion layer and walkthrough scripts intact.

## Current State

**What exists:**

| Component | Location | Status |
|-----------|----------|--------|
| AutoPilot (core) | `scripts/testing/AutoPilot.cs` | Working -- step runner, logging, assertions, lifecycle |
| AutoPilotActions | `scripts/testing/AutoPilotActions.cs` | Partially working -- hand-rolled input injection has async/timing bugs |
| AutoPilotAssertions | `scripts/testing/AutoPilotAssertions.cs` | Working -- game state verification via GameState.Instance |
| FullRunSandbox | `scripts/sandbox/FullRunSandbox.cs` | Working -- 9-phase walkthrough from splash to endgame |
| FullRunTests | `tests/unit/FullRunTests.cs` | Working -- pure C# logic, no Godot dependency |
| GdUnit4 scaffold | `tests/e2e/` | Scaffolded, not automated |
| 315 xUnit tests | `tests/unit/`, `tests/integration/` | All passing |
| 14 sandbox scenes | `scenes/sandbox/` | All functional |

**Problems with current AutoPilotActions:**

1. **Input release timing** -- `ProcessPendingReleases()` runs on `_PhysicsProcess`, but actions queued mid-frame can miss the release window, causing stuck keys.
2. **Scene change races** -- `WaitForTransition()` uses fixed frame counts (5 frames settle) that are fragile across different machines and headless vs. windowed mode.
3. **Button interaction bypass** -- `ClickButton()` calls `EmitSignal(Pressed)` directly, which skips focus/hover/disabled checks that a real player would encounter.
4. **No node driver pattern** -- every interaction requires manual tree traversal via `FindButton()` and `GetPlayerNode()`, duplicating scene structure knowledge across the walkthrough.

## Design

### What Changes

| Layer | Before | After |
|-------|--------|-------|
| NuGet package | None | `Chickensoft.GodotTestDriver` added to `DungeonGame.csproj` |
| Input injection | `InputEventAction` + manual `Input.ParseInputEvent` | GodotTestDriver `StartAction()` / `EndAction()` / `HoldActionFor()` |
| Button clicks | `button.EmitSignal(Pressed)` | `ButtonDriver.ClickCenter()` via viewport mouse simulation |
| Node lookup | Manual tree traversal in each step | Typed drivers with lazy `Func<T>` producers |
| Waiting | `WaitFrames(N)` / `WaitSeconds(N)` / `WaitUntil(poll)` | `SceneTree.WithinSeconds()` / `DuringSeconds()` + existing waits |
| Scene lifecycle | Manual `ChangeSceneToFile` + timer delay | `Fixture.LoadAndAddScene<T>()` for sandbox tests; manual scene changes kept for full-run (must test real transitions) |
| AutoPilot.cs | Unchanged | Unchanged (step runner, logging, assertions) |
| AutoPilotAssertions.cs | Unchanged | Unchanged (game state verification) |
| AutoPilotActions.cs | Rewritten | Delegates to GodotTestDriver primitives; removes hand-rolled input queue |
| FullRunSandbox.cs | Minor changes | Uses new driver-based action methods; same walkthrough structure |
| FullRunTests.cs | Unchanged | Unchanged (pure C# logic, no Godot) |

### What Does NOT Change

- **AutoPilot.cs** -- step runner, logging, pass/fail tracking, `Finish()` lifecycle. No GodotTestDriver dependency.
- **AutoPilotAssertions.cs** -- game state verification. Reads `GameState.Instance` and `GetTree()` groups. No driver needed.
- **FullRunTests.cs** -- pure C# integration test. Zero Godot dependency.
- **Sandbox architecture** -- `SandboxBase`, headless mode, `make sandbox-headless` targets all stay the same.
- **GdUnit4 E2E scaffold** -- remains for scene-level tests that need the GdUnit4 test runner.
- **CI pipeline** -- no structural changes. GodotTestDriver is a NuGet package resolved at `dotnet restore`.

### Package Installation

Add to `DungeonGame.csproj`:

```xml
<PackageReference Include="Chickensoft.GodotTestDriver" Version="3.*" />
```

The package targets `netstandard2.1` and is compatible with `Godot.NET.Sdk/4.6.2` + `net8.0`. MIT licensed.

### API Mapping: AutoPilotActions (Before/After)

#### Input Actions

| AutoPilotActions (current) | GodotTestDriver equivalent | Notes |
|----------------------------|---------------------------|-------|
| `Press(string action)` | `node.StartAction(action)` then next frame `node.EndAction(action)` | GodotTestDriver injects via `Input.ActionPress` / `Input.ActionRelease`, same mechanism but with proper frame timing |
| `Hold(string action)` | `node.StartAction(action)` | Stays active until `EndAction` is called |
| `Release(string action)` | `node.EndAction(action)` | Clean release, no pending queue needed |
| `ReleaseAll()` | Call `node.EndAction()` for each tracked action | Wrapper still needed -- GodotTestDriver does not track held actions |
| `MoveDirection(Vector2, float)` | `StartAction("move_right")` + `await node.HoldActionFor(seconds, "move_right")` | Can compose multiple simultaneous actions for diagonal movement |
| `MoveToward(Vector2, float)` | Loop: compute direction, `StartAction`/`EndAction` per frame | Same logic, uses GodotTestDriver input primitives instead of `InputEventAction` |

#### Keyboard Input

| Current | GodotTestDriver | Notes |
|---------|-----------------|-------|
| Not supported | `node.PressKey(Key key)` | Available for text entry, debug console |
| Not supported | `node.ReleaseKey(Key key)` | Clean key release |
| Not supported | `node.TypeKey(Key key)` | Press + release in one call |

#### Mouse Input

| Current | GodotTestDriver | Notes |
|---------|-----------------|-------|
| `ClickButton(Button btn)` -- uses `EmitSignal` | `viewport.ClickMouseAt(position)` | Real mouse simulation, respects focus/visibility/disabled state |
| `FindButton(Node, string)` -- manual traversal | `ButtonDriver` with lazy producer | Driver resolves node from tree; errors descriptively if missing |
| Not supported | `viewport.DragMouse(from, to)` | Enables future drag-and-drop inventory testing |
| Not supported | `viewport.MoveMouseTo(position)` | Enables hover state testing |

#### Controller Input

| Current | GodotTestDriver | Notes |
|---------|-----------------|-------|
| Not supported | `node.PressJoypadButton(JoyButton, deviceID)` | Future gamepad testing |
| Not supported | `node.TapJoypadButton(JoyButton, deviceID)` | Press + release |
| Not supported | `node.HoldJoypadButtonFor(seconds, JoyButton)` | Timed hold |
| Not supported | `node.MoveJoypadAxisTo(JoyAxis, position)` | Analog stick |

#### Waiting

| AutoPilotActions (current) | GodotTestDriver equivalent | Notes |
|----------------------------|---------------------------|-------|
| `WaitFrames(int count)` | Keep as-is (no GodotTestDriver equivalent) | Simple frame counting still useful |
| `WaitSeconds(float seconds)` | Keep as-is | `CreateTimer` + `ToSignal` pattern works fine |
| `WaitUntil(Func<bool>, float)` | `GetTree().WithinSeconds(timeout, assertion)` | GodotTestDriver throws with descriptive message on timeout |
| `WaitForTransition()` | Keep as-is, but use `WithinSeconds` for the polling loop | More reliable than fixed frame counts |

### Game-Specific Drivers

Drivers wrap scene nodes with typed APIs. Each driver takes a `Func<T>` producer that lazily resolves the node from the tree. If the node does not exist when accessed, the driver throws a descriptive error.

#### PlayerDriver

```
Producer:   () => GetTree().GetNodesInGroup("player")[0] as CharacterBody2D
Purpose:    Player node interaction
Properties: GlobalPosition, IsMoving, FacingDirection, CurrentHp, IsDead
Methods:
  - MoveTo(Vector2 target, float timeout) -- hold movement actions toward target
  - Attack() -- press action_cross
  - Interact() -- press action_cross when near NPC/object
  - OpenMenu() -- press start
  - UseSkillSlot(int slot) -- press corresponding action button
```

#### NpcDriver

```
Producer:   () => tree.CurrentScene.FindChild("Shopkeeper") as Node2D
Purpose:    NPC proximity and interaction
Properties: GlobalPosition, Name, IsInRange(PlayerDriver)
Methods:
  - ApproachFrom(PlayerDriver) -- move player toward NPC position
  - Interact(PlayerDriver) -- move close + press action_cross
```

#### ShopWindowDriver

```
Producer:   () => ShopWindow.Instance
Wraps:      ControlDriver<Control>
Properties: IsVisible, ItemCount
Child drivers:
  - BuyButton: ButtonDriver
  - SellButton: ButtonDriver
  - CloseButton: ButtonDriver
  - ItemList: ItemListDriver (for item selection)
Methods:
  - BuyItem(int index) -- select item + click buy
  - SellItem(int index) -- select item + click sell
  - Close() -- click close or press action_circle
```

#### BankWindowDriver

```
Producer:   () => BankWindow.Instance
Wraps:      ControlDriver<Control>
Properties: IsVisible, InventorySlots, BankSlots
Child drivers:
  - DepositButton: ButtonDriver
  - WithdrawButton: ButtonDriver
  - ExpandButton: ButtonDriver
  - CloseButton: ButtonDriver
Methods:
  - Deposit(int inventorySlot) -- select + deposit
  - Withdraw(int bankSlot) -- select + withdraw
  - Close() -- click close or press action_circle
```

#### BlacksmithWindowDriver

```
Producer:   () => BlacksmithWindow.Instance
Wraps:      ControlDriver<Control>
Properties: IsVisible
Child drivers:
  - CraftButton: ButtonDriver
  - RecycleButton: ButtonDriver
  - CloseButton: ButtonDriver
Methods:
  - Close() -- click close or press action_circle
```

#### SplashScreenDriver

```
Producer:   () => tree.CurrentScene.FindChild("SplashScreen") as Control
Wraps:      ControlDriver<Control>
Properties: IsVisible
Child drivers:
  - NewGameButton: ButtonDriver
  - ContinueButton: ButtonDriver
Methods:
  - ClickNewGame() -- click New Game button
  - ClickContinue() -- click Continue button (if save exists)
```

#### ClassSelectDriver

```
Producer:   () => tree.CurrentScene.FindChild("ClassSelect") as Control
Wraps:      ControlDriver<Control>
Properties: IsVisible, SelectedClass
Child drivers:
  - ConfirmButton: ButtonDriver
  - ClassButtons: ButtonDriver[] (one per class)
Methods:
  - SelectClass(PlayerClass cls) -- click the class button
  - Confirm() -- click confirm
```

#### PauseMenuDriver

```
Producer:   () => tree.CurrentScene.FindChild("PauseMenu") as Control
Wraps:      ControlDriver<Control>
Properties: IsVisible
Child drivers:
  - ResumeButton: ButtonDriver
  - SettingsButton: ButtonDriver
  - QuitButton: ButtonDriver
Methods:
  - Resume() -- click resume
  - Open() -- press start action
```

#### HudDriver

```
Producer:   () => tree.CurrentScene.FindChild("Hud") as CanvasLayer
Properties: HpText, MpText, LevelText, FloorText
Child drivers:
  - HpLabel: LabelDriver
  - MpLabel: LabelDriver
  - LevelLabel: LabelDriver
  - FloorLabel: LabelDriver
  - XpBar: ControlDriver<Control>
```

#### DeathScreenDriver

```
Producer:   () => tree.CurrentScene.FindChild("DeathScreen") as Control
Wraps:      ControlDriver<Control>
Properties: IsVisible
Child drivers:
  - ConfirmButton: ButtonDriver
  - ProtectExpButton: ButtonDriver
  - ProtectItemsButton: ButtonDriver
```

### Refactored AutoPilotActions

After the refactor, `AutoPilotActions` becomes a thin game-aware wrapper that delegates to GodotTestDriver. It keeps the same public API so `FullRunSandbox` and other walkthroughs need minimal changes.

**Removed internals:**
- `_heldActions` HashSet -- no longer needed; GodotTestDriver handles action state
- `_pendingReleases` Queue -- no longer needed; no manual release scheduling
- `ProcessPendingReleases()` -- deleted; AutoPilot._PhysicsProcess no longer needs to call this

**Kept methods (new implementation):**
- `Press(action)` -- calls `_pilot.StartAction(action)`, awaits one frame, calls `_pilot.EndAction(action)`
- `Hold(action)` -- calls `_pilot.StartAction(action)`, tracks in `_activeActions` set
- `Release(action)` -- calls `_pilot.EndAction(action)`, removes from `_activeActions`
- `ReleaseAll()` -- iterates `_activeActions`, calls `EndAction` on each
- `MoveDirection(dir, seconds)` -- composes `StartAction`/`HoldActionFor` for direction
- `MoveToward(target, timeout)` -- same pathfinding logic, uses `StartAction`/`EndAction`
- `ClickButton(button)` -- uses `viewport.ClickMouseAt(button.GlobalPosition + button.Size/2)`
- `WaitFrames(count)` -- kept as-is
- `WaitSeconds(seconds)` -- kept as-is
- `WaitUntil(condition, timeout)` -- delegates to `GetTree().WithinSeconds()` where possible
- `WaitForTransition()` -- uses `WithinSeconds` for polling

**New methods (enabled by GodotTestDriver):**
- `PressKey(Key key)` -- keyboard input for debug console, text fields
- `DragItem(Vector2 from, Vector2 to)` -- `viewport.DragMouse()` for inventory drag-drop
- `HoverAt(Vector2 position)` -- `viewport.MoveMouseTo()` for tooltip testing

### Refactored FullRunSandbox Walkthrough

The 9-phase walkthrough structure stays identical. Changes are limited to using drivers instead of raw node lookups:

**Phase 1 (Splash):**
- Before: `FindButtonInScene(pilot, "New Game")` + `act.ClickButton(btn)`
- After: `splashDriver.ClickNewGame()`

**Phase 2 (Class Select):**
- Before: `FindButtonInScene(pilot, "Confirm")` with fallback to `act.Press(ActionCross)`
- After: `classSelectDriver.SelectClass(PlayerClass.Warrior)` + `classSelectDriver.Confirm()`

**Phase 3-4 (Town):**
- Before: `act.MoveDirection(Vector2.Right, 1.0f)` -- unchanged
- After: Same movement calls, but button interactions use `ShopWindowDriver` / `BankWindowDriver`

**Phase 5 (NPC Interaction):**
- Before: Move toward center-right + `act.Press(ActionCross)` + `act.Press(ActionCircle)`
- After: `npcDriver.Interact(playerDriver)` + `shopDriver.Close()`

**Phase 6 (Pause Menu):**
- Before: `act.Press(Start)` + `FindButtonInScene(pilot, "Resume")`
- After: `pauseMenuDriver.Open()` + `pauseMenuDriver.Resume()`

**Phase 7-9 (Dungeon, Combat, Verification):**
- Movement and combat phases use the same `MoveDirection` / `MoveToward` calls
- Verification phase uses `AutoPilotAssertions` (unchanged)

### Driver Initialization in FullRunSandbox

Drivers are created lazily at the start of the walkthrough. Because they use `Func<T>` producers, they do not require the target nodes to exist at creation time -- the producer is evaluated when the driver is first accessed.

```
// Pseudocode for driver setup in RunWalkthrough():
var playerDriver = new PlayerDriver(() => GetPlayerFromTree());
var splashDriver = new SplashScreenDriver(() => FindSplashScreen());
var classSelectDriver = new ClassSelectDriver(() => FindClassSelect());
var shopDriver = new ShopWindowDriver(() => ShopWindow.Instance);
var bankDriver = new BankWindowDriver(() => BankWindow.Instance);
var pauseDriver = new PauseMenuDriver(() => FindPauseMenu());
var hudDriver = new HudDriver(() => FindHud());
```

### Fixture Usage

`Fixture` is used for **sandbox scene tests** (individual system sandboxes), not for the full-run walkthrough. The full-run must test real scene transitions (`ChangeSceneToFile`), so it manages its own lifecycle.

For GdUnit4 E2E tests in `tests/e2e/`:

```
// Each test creates a Fixture, loads a sandbox, runs assertions, cleans up
var fixture = new Fixture(GetTree());
var sandbox = await fixture.LoadAndAddScene<InventorySandbox>("res://scenes/sandbox/systems/InventorySandbox.tscn");
// ... interact via drivers ...
await fixture.Cleanup();
```

## Acceptance Criteria

1. `Chickensoft.GodotTestDriver` NuGet package is referenced in `DungeonGame.csproj`
2. `dotnet build DungeonGame.csproj` succeeds with the new package
3. `AutoPilotActions.cs` uses GodotTestDriver `StartAction` / `EndAction` / `HoldActionFor` -- no `InputEventAction` or `Input.ParseInputEvent` in that file
4. `AutoPilotActions.cs` no longer has `_pendingReleases` queue or `ProcessPendingReleases()` method
5. `AutoPilot._PhysicsProcess` no longer calls `ProcessPendingReleases()`
6. At least 4 game-specific drivers exist: `PlayerDriver`, `SplashScreenDriver`, `ClassSelectDriver`, and one window driver (`ShopWindowDriver` or `BankWindowDriver`)
7. Each driver uses `Func<T>` producer pattern for lazy node resolution
8. `FullRunSandbox.cs` uses drivers for UI interactions (splash, class select, pause menu)
9. `FullRunSandbox.cs` walkthrough completes all 9 phases with zero manual intervention
10. `make sandbox-headless SCENE=full-run` exits 0 on success, 1 on failure
11. `make sandbox SCENE=full-run` shows the game playing itself visually (no human input needed)
12. `FullRunTests.cs` still passes unchanged (`make test-unit`)
13. `AutoPilotAssertions.cs` is unchanged (no GodotTestDriver dependency)
14. All 315 existing xUnit tests pass (`make test`)
15. CI pipeline (`ci.yml`) requires no structural changes -- `dotnet restore` resolves the new package

## Implementation Notes

### Phasing

Implement in 3 stages to keep the full-run working at every step:

**Stage 1: Package + Actions refactor (no drivers yet)**
1. Add `Chickensoft.GodotTestDriver` to `DungeonGame.csproj`
2. Rewrite `AutoPilotActions` internals to use `StartAction`/`EndAction`/`HoldActionFor`
3. Remove `_pendingReleases`, `ProcessPendingReleases()`, and the `_PhysicsProcess` call
4. Keep the same public API -- `FullRunSandbox` should not need changes at this stage
5. Verify: `make sandbox-headless SCENE=full-run` still passes

**Stage 2: Game-specific drivers**
1. Create `scripts/testing/drivers/` directory
2. Implement `PlayerDriver`, `SplashScreenDriver`, `ClassSelectDriver`
3. Implement `ShopWindowDriver`, `BankWindowDriver`, `PauseMenuDriver`
4. Implement `HudDriver`, `DeathScreenDriver`, `BlacksmithWindowDriver`
5. Each driver lives in its own file: `PlayerDriver.cs`, etc.
6. Verify: all drivers compile, sandbox still passes

**Stage 3: Wire drivers into FullRunSandbox**
1. Refactor `FullRunSandbox.RunWalkthrough()` to use drivers for UI phases
2. Remove `FindButtonInScene()` and `SearchForButton()` helper methods
3. Keep movement/combat phases using `MoveDirection`/`MoveToward` (no driver needed for raw movement)
4. Verify: `make sandbox-headless SCENE=full-run` exits 0

### Technical Details

- **Namespace**: `DungeonGame.Testing.Drivers` for game-specific drivers
- **File location**: `scripts/testing/drivers/*.cs`
- **GodotTestDriver requires a reference to the scene tree** -- drivers are instantiated with access to `AutoPilot.GetTree()` or passed a `SceneTree` reference
- **Thread safety**: GodotTestDriver ensures all tree modifications run on the main thread. The AutoPilot `async Task` walkthrough is already on the main thread (Godot's `ToSignal` pattern), so no threading issues
- **Headless mode**: GodotTestDriver's input simulation works in `--headless` mode because it uses `Input.ActionPress`/`Input.ActionRelease` and viewport event injection, not OS-level input
- **Diagonal movement**: To walk diagonally, call `StartAction("move_right")` and `StartAction("move_down")` simultaneously, then `EndAction` both after the duration. The `MoveDirection` wrapper handles this composition.
- **Scene transitions**: `StartAction`/`EndAction` calls survive scene changes because they operate on the global `Input` singleton, not on a specific node. The AutoPilot node (attached to `GetTree().Root`) also survives scene changes.
- **GodotTestDriver version**: Pin to `3.*` wildcard to get latest minor/patch. The library follows semver.

### CI Impact

- **No new CI job needed** -- GodotTestDriver is a NuGet package, resolved by `dotnet restore`
- **No Godot version change needed** -- the library is compatible with Godot 4.x
- **sandbox-checks job** in `ci.yml` already runs `make sandbox-headless` -- it will automatically exercise the new driver-based code

## Open Questions

(none -- spec is locked)
