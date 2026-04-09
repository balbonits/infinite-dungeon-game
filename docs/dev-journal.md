# Development Journal

A running log of everything we build, test, learn, and decide — from zero to game. This project is built entirely by AI, directed by a product owner who is learning game development for the first time.

---

## Session 1 — 2026-04-08

### What Happened

**Started from:** 26 locked specs, 165 tickets, zero code. No C# project, no scenes, no scripts.

**Ended with:** A working Godot 4 + C# pipeline, rendered dungeon room with a character, and scripted movement demo.

### What We Built (in order)

#### Test 1: Hello World
- **What I was asked:** "Create a Hello World thing — see if we could even start a basic app from you coding everything."
- **What I coded:**
  - `DungeonGame.csproj` — C# project file (Godot.NET.Sdk 4.6.2, net8.0)
  - `scripts/HelloWorld.cs` — prints to console, auto-quits in headless mode
  - `scenes/hello_world.tscn` — scene with two labels
  - Updated `project.godot` for .NET runtime, set main scene, removed old GUT plugin
- **What the user saw:** Console output confirming C#, Godot, and .NET versions. Then launched windowed — saw text labels on screen.
- **Result:** PASS. Pipeline verified end to end.
- **Evidence:** `docs/evidence/hello-world/notes.md`

#### Test 2: Asset Render
- **What I was asked:** "Next is basic assets render. Have a character & a floor show on screen."
- **What I coded:**
  - `scripts/AssetTest.cs` — procedurally creates a 15x11 tile room with DCSS assets
  - `scenes/asset_test.tscn` — scene with 3x zoom camera
  - Used DCSS paper doll system: base body (`human_male.png`) + armor overlay (`chainmail.png`) + weapon overlay (`long_sword.png`)
  - Floor: `grey_dirt_0_new.png`, Walls: `brick_dark_0.png`
- **What the user saw:** A dungeon room with dark brick walls, grey dirt floor, and a warrior (human + chainmail + longsword) standing in the center. Top-down view at 3x zoom.
- **User feedback:** "yup! top-down, but it's there. good test on rendering."
- **Result:** PASS. DCSS tiles render correctly. Paper doll layering works.
- **Evidence:** `docs/evidence/asset-render/notes.md`

#### Test 3: Scripted Movement Demo
- **What I was asked:** "Next, input. Have a scripted demo of the character going up, down, left, and right. Have a 1 sec between commands."
- **What I coded:**
  - Updated `scripts/AssetTest.cs` with a state machine: waiting → moving → waiting
  - Scripted sequence: UP → DOWN → LEFT → RIGHT with 1s pauses
  - Movement: 96 px/s, 2 tiles per move, smooth linear interpolation
  - All 3 paper doll layers move together (Node2D container)
  - Auto-quits after demo completes
- **What the user saw:** The warrior moved smoothly in all 4 directions with pauses between each move, then the window closed automatically.
- **User feedback:** "i saw it"
- **Result:** PASS. Smooth movement, layers stay composited.
- **Evidence:** `docs/evidence/movement-demo/notes.md`

### Tooling Created

| Tool | Purpose |
|------|---------|
| `make build` | dotnet build |
| `make run` | Build + launch windowed |
| `make run-headless` | Build + run + auto-quit (CI/testing) |
| `make verify` | Full pipeline check (build + headless run + confirm output) |
| `make doctor` | Environment health check (Godot, .NET, .csproj, main scene) |
| `make kill` | Kill lingering Godot processes |
| `make branch T=X` | Create ticket branch |
| `make done` | Squash-merge branch to main |
| `make status` | Git + build + versions + ticket count |

### Problems Hit

| Problem | How We Fixed It |
|---------|----------------|
| `godot` in PATH was non-.NET version | Found `Godot_mono.app` in Applications, used full path in Makefile |
| No .csproj existed | Created manually, Godot updated SDK version on import |
| Headless Godot didn't auto-quit | Added `DisplayServer.GetName() == "headless"` check + `GetTree().Quit()` |
| Godot process lingered after headless run | Added `make kill` target |
| `.import` files flooded git status | Added `*.import` to .gitignore |

### What We Learned

1. **Godot .NET requires the Mono variant** — the standard `godot` binary from homebrew doesn't support C#. Need `/Applications/Godot_mono.app`.
2. **DCSS paper doll system works** — layer sprites on a Node2D container and they composite via transparency. Body + armor + weapon all stack correctly.
3. **32x32 DCSS tiles render clean** — `TextureFilter = Nearest` preserves pixel art at any zoom level.
4. **Headless mode needs explicit quit** — Godot won't exit on its own after `_Ready()` unless you call `GetTree().Quit()`.
5. **Godot generates `.import` files** for every asset it scans — these are binary cache files that shouldn't be committed.
6. **`_Process(delta)` works for frame-based movement** — multiply speed by delta for frame-rate independent movement.

### Decisions Made This Session

| Decision | Rationale |
|----------|-----------|
| Flipped CLAUDE.md from "docs only" to coding | All 26 specs locked, ready to implement |
| Makefile rewritten for C# (not GDScript) | Old targets were for GUT/gdlint, now uses dotnet build/test |
| Main scene set to asset_test.tscn | Current test scene, will change as we progress |
| Godot mono path hardcoded in Makefile | Reliable — won't accidentally use non-.NET version |

### What's Next

The three pipeline tests (hello world → asset render → movement) proved:
- C# compiles and runs ✓
- Assets load and display ✓
- Movement code works ✓

Next logical step: **player-controlled movement with arrow keys** — the first real gameplay input. This is P1-04c (arrow key input) and P1-04d (isometric transform).

---

*This journal is append-only. Each session adds a new section. Never edit previous sessions — they're a historical record.*
