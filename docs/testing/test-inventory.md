# Test Inventory

Every test suite in the project, grouped by framework layer. Generated 2026-04-20. Re-run `scripts/build/test-inventory.sh` (TEST-12 follow-up) to refresh.

**Rule of thumb for which layer to put a new test in:**
- **Pure logic** (no Godot types) → xUnit unit (`tests/unit/`)
- **Cross-system flow in pure logic** → xUnit integration (`tests/integration/`)
- **In-game scene behavior + keyboard nav** → GoDotTest (`scripts/testing/tests/`)
- **Full-scene smoke / sandbox exercise** → GdUnit4 E2E (`tests/e2e/`)

---

## Layer 1 — xUnit unit (`tests/unit/`)

Pure-logic tests, no Godot runtime. Fast (<1s per suite). Run via `make test-unit` (all) or `make test-unit-one TEST=<FullyQualified>` (single).

### Core mechanics & combat math
| Suite | Tests | Scope |
|---|---|---|
| CombatFormulasTests | 21 | COMBAT-01 §7: SoftCap curve, overflow conversions, Crit / Flurry / Phase / Block hard caps |
| StatBlockTests | 25 | Stat DR curve, derived multipliers (melee/spell/attack-speed/dodge), class level-up bonuses |
| EquipmentSetTests | 24 | 19-slot equipment + class-affinity 1.25× + save/restore |
| EquipmentCombatStatsTests | 13 | COMBAT-01 cache lifecycle + ring-focus accumulation |
| DungeonIntelligenceTests | 18 | AUDIT-12: adaptive AI director, pressure score, modifier bounds |
| DungeonPactsTests | 22 | Voluntary difficulty modifiers, heat tiers |
| ZoneSaturationTests | 20 | Per-zone saturation + decay + stat/reward multipliers |
| MagiculeAttunementTests | 26 | 40-node post-cap tree, keystones, floor tracking |

### Loot, items, crafting
| Suite | Tests | Scope |
|---|---|---|
| ContainerLootTableTests | 21 | LOOT-01: Jar/Crate/Chest rolls, spawn counts, slot uniqueness |
| LootTableTests | 4 | Legacy (level-only) gold + item-drop helper |
| MonsterDropTableTests | 6 | Per-species signature materials + thematic bias |
| CraftingTests | 19 | Affix application, recycle quality ladder (AUDIT-11) |
| AffixDatabaseTests | 12 | Tier 1-6 affix registry (AUDIT-10) |
| DepthGearTierTests | 15 | Quality distribution at floor 1/50/100/150 |
| InventoryTests | 24 | Slot stacking, gold, one-type-per-slot, locks |
| BankTests | 19 | Deposit/withdraw, expansion purchase |

### Progression & state
| Suite | Tests | Scope |
|---|---|---|
| ProgressionTrackerTests | 31 | SP/AP pools, milestone bonuses, XP-per-point |
| SkillBarTests | 20 | Hotbar binding, cooldowns, key slots |
| SkillAbilityDatabaseTests | 24 | 103-ability roster across 3 classes |
| AbilityStateTests | 7 | Ability level + XP tracking |
| MasteryStateTests | 14 | Mastery level + category thresholds |
| AchievementSystemTests | 18 | Counter-based unlocks, 30-achievement roster |
| QuestSystemTests | 12 | Kill/ClearFloor/DepthPush quest types |
| ConstantsTests | 15 | AUDIT-08: GetMaxHp closed form + Int64 saturation |

### Save, death, integration-y
| Suite | Tests | Scope |
|---|---|---|
| SaveSystemTests | 14 | ISaveStorage abstraction, JSON round-trip |
| SaveManagerGuardTests | 1 | AUDIT-03 source-level guard against null-slot silent overwrite |
| DeathPenaltyTests | 24 | Buyout costs, EXP loss, idol consumption |
| DeathPenaltyIntegrationTests | 20 | Full 5-option sacrifice flow scenarios |
| FullRunTests | 11 | End-to-end run from splash through dungeon |
| NumberFormatTests | 9 | Gold abbreviation (1k, 1.5m) |
| ToastDismissGuardTests | 5 | AUDIT-06 source-level guard against double-dismiss |

**xUnit unit total: ~552 test methods across 31 suites.**

---

## Layer 2 — xUnit integration (`tests/integration/`)

Cross-system logic flows, still pure logic. Run via `make test-integration` or `make test-unit-one` (same filter mechanism).

| Suite | Tests | Scope |
|---|---|---|
| DeathFlowTests | ~4 | Death → sacrifice → respawn chain |
| StatProgressionTests | ~4 | Level-up stat gains propagating to MaxHp/MaxMana |
| EconomyFlowTests | ~3 | Shop buy/sell → bank transfer → death loss |

**Integration total: 11 test methods across 3 suites.**

---

## Layer 3 — GoDotTest (`scripts/testing/tests/`, windowed)

In-game scene-based tests. Boot Godot in a real window, drive real scenes with keyboard input. **Run windowed by default** (PO ban on `--headless` for UI verification). Run via `make test-ui` (all) or `make test-ui-one SUITE=<Name> TEST=<Method>` (single).

### Flow / screen coverage

| Suite | Tests | Spec | Scope |
|---|---|---|---|
| SplashTests | 10 | [docs/flows/splash.md](../flows/splash.md) | Title, New Game, Continue, Load Game entry, Settings |
| SlotsFullTests | 2 | [docs/flows/load-game.md](../flows/load-game.md) | 3-slot-full → slots-full dialog → overwrite path |
| ClassSelectTests | 6 | [docs/flows/class-select.md](../flows/class-select.md) | 3 class cards, keyboard nav, confirm → town |
| TownTests | 7 | [docs/flows/town.md](../flows/town.md) | 3 NPCs, dungeon entrance, HUD visible |
| NpcTests | 5 | [docs/flows/npc-dialogue-voices.md](../flows/npc-dialogue-voices.md) | NPC interact, service panel, dialog close |
| GuildWindowTests | 3 | [docs/ui/guild-maid-menu.md](../ui/guild-maid-menu.md) | Bank + Teleport tabs, gold display |
| PauseMenuTests | 6 | [docs/ui/pause-menu-tabs.md](../ui/pause-menu-tabs.md) | 8-tab cycle, Esc toggle, tab-specific content |
| DeathTests | 3 | [docs/systems/death.md](../systems/death.md) | 5-option dialog, sacrifice paths |
| DeathCinematicTests | 2 | [docs/systems/death.md](../systems/death.md) | YOU DIED fade-in/hold/fade-out timing |
| TransitionTests | 3 | [docs/ui/screen-transitions.md](../ui/screen-transitions.md) | Fade overlay alpha at midpoint + transparent between |

**GoDotTest total: 47 test methods across 10 suites** (matches the `make test-ui` green-path summary).

### Known gaps in this layer
- `BlacksmithTests` — not yet written (shop/forge/craft/recycle tabs per BLACKSMITH-MENU-IMPL-01).
- `BackpackTests` — not yet written (slot grid, item actions).
- `SkillsTabTests` / `AbilitiesTabTests` — not yet written.
- `StatsTabTests` / `StatAllocDialogTests` — not yet written.
- `LedgerTabTests` (achievements) — not yet written.
- `TutorialTests` — not yet written.
- `SettingsTests` — not yet written.
- `DungeonFloorTransitionTests` — partial via TransitionTests; doesn't cover stairs / AscendDialog / floor-10 / zone-boundary flavor.
- `ContainerInteractTests` — not yet written (LOOT-01 interact + payout + sprite swap).
- `CombatRingFocusTests` — not yet written (COMBAT-01 crit/haste/dodge/block observable outcomes).

---

## Layer 4 — GdUnit4 E2E (`tests/e2e/`)

Full-scene scaffolded tests via ISceneRunner. **Currently red on CI** (AUDIT-18: GodotGdUnit4RestClient timeout post-GODOT_BIN fix). Once wired, run via `make test-e2e` or `make test-e2e-one TEST=<FQN>`.

### Smoke
| Suite | Tests | Scope |
|---|---|---|
| SmokeTests | 9 | Basic scene-loads (splash, town, dungeon) |

### Mechanic sandboxes (`tests/e2e/mechanics/`)
| Suite | Tests | Scope |
|---|---|---|
| MovementSandboxTests | ~3 | 8-dir movement, velocity-based sprite rotation |
| CombatSandboxTests | ~3 | Auto-attack, damage floater, hit flash |
| StatsSandboxTests | ~3 | Stat allocation → MaxHp/MaxMana recompute |
| EnemySandboxTests | ~2 | Species spawn, drop-table exercise |

### System sandboxes (`tests/e2e/systems/`)
| Suite | Tests | Scope |
|---|---|---|
| FloorGenSandboxTests | ~4 | Room-carve, walls, stairs placement |
| InventorySandboxTests | ~4 | Backpack capacity, merge, lock |
| LootTableSandboxTests | ~4 | Drop frequency under seeded RNG |
| BankSandboxTests | ~3 | Deposit/withdraw/transfer UI flow |
| DeathPenaltySandboxTests | ~2 | Sacrifice choice → post-respawn state |

### Asset sandboxes (`tests/e2e/assets/`)
Screenshot / atlas verification — these ARE the regression net for visual drift. Currently blocked by AUDIT-18.

| Suite | Tests | Scope |
|---|---|---|
| SpriteViewerTests | ~4 | Class + NPC LPC sprite atlas loads |
| TileViewerTests | ~3 | Floor + wall tile atlas per biome |
| NpcAtlasTests | ~3 | Blacksmith / Guild Maid / Village Chief sheets |
| ProjectileViewerTests | ~3 | 9 projectile sprite + tint variants |
| EffectSpriteTests | ~3 | FlashFx color variants, FloatingText variants |

**GdUnit4 E2E total: 53 test methods across 14 suites** (blocked on AUDIT-18).

---

## Grand totals

| Layer | Suites | ~Tests | Run via |
|---|---|---|---|
| xUnit unit | 31 | 552 | `make test-unit` |
| xUnit integration | 3 | 11 | `make test-integration` |
| GoDotTest (windowed) | 10 | 47 | `make test-ui` |
| GdUnit4 E2E | 14 | ~53 | `make test-e2e` (blocked) |
| **Total** | **58** | **~663** | |

---

## Running tests — full + single

Every layer has both an all-suites target and a single-target-at-a-time variant:

```bash
# ─── xUnit unit (fast, no Godot) ────────────────────────────────────────────
make test-unit                                          # all 552 tests
make test-unit-one TEST=DungeonGame.Tests.Unit.CombatFormulasTests.SoftCap_At60Raw_Returns30
#      └─ full method FQN; filters to exactly 1 test

# ─── xUnit integration ──────────────────────────────────────────────────────
make test-integration
make test-integration-one TEST=DungeonGame.Tests.Integration.DeathFlowTests.DeathResetsHp

# ─── GoDotTest WINDOWED (in-game scenes, keyboard nav) ──────────────────────
make test-ui                                            # all 47 in a window
make test-ui-suite SUITE=SplashTests                    # one suite, all methods
make test-ui-one SUITE=SplashTests                      # same as test-ui-suite today
# NOTE: Chickensoft.GoDotTest's --run-tests filter is at the suite-class level
# only; per-method filtering needs a wrapper in Main.RunTests (tracked as
# TEST-12 follow-up: parse --test-method= arg + filter the test list before
# GoTest.RunTests invocation). Until that lands, "run one test" = "run its
# whole suite" and visually watch for the one you care about.

# ─── GdUnit4 E2E (currently red on CI — AUDIT-18 timeout) ───────────────────
make test-e2e                                           # all 53 (blocked)
make test-e2e-one TEST=DungeonGame.Tests.E2E.SmokeTests.Splash_LoadsCleanly
```

**UI verification discipline** (per AGENTS.md §0b, PO-banned headless default):
- `test-ui` and `test-ui-suite` / `test-ui-one` are **windowed** — Godot opens a real window.
- `test-ui-headless` exists for CI only (CI has no display).
- Never run `godot --headless` for local UI verification.

---

## Follow-ups (TEST-12)

- Populate the "Known gaps in Layer 3" list as real suites (blacksmith / backpack / stats-tab / etc).
- Unblock GdUnit4 E2E (AUDIT-18) so the asset-sandbox tests actually gate on baselines.
- Wire `ScreenshotHelper` baselines into every GoDotTest suite in Layer 3 + every asset sandbox in Layer 4. Commit baseline PNGs to `docs/evidence/screenshots/<suite>/<test>.png`.
- Auto-regen this inventory in CI so "new suite added, doc stale" fails review.
