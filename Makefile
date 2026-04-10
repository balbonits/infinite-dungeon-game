# Makefile — AI-drivable development automation
# Run `make help` to see all available targets.

GODOT      := /Applications/Godot_mono.app/Contents/MacOS/Godot
PROJECT    := DungeonGame
TIMEOUT    := 15

.PHONY: help setup build run run-headless test test-game test-game-headless test-game-capture test-game-check t import kill clean status verify doctor test-bsp test-drunkard test-cellular test-dungeon test-procgen

# ─── Core ────────────────────────────────────────────────────────────────────

help: ## Show available targets
	@grep -E '^[a-zA-Z_-]+:.*?## .*$$' $(MAKEFILE_LIST) | sort | awk 'BEGIN {FS = ":.*?## "}; {printf "\033[36m%-18s\033[0m %s\n", $$1, $$2}'

build: ## Build C# project (dotnet build)
	@dotnet build 2>&1 | tail -5

run: build ## Build and launch the game (windowed)
	@$(GODOT) --path . &

run-headless: build ## Build and run headless (auto-quits, for CI/testing)
	@$(GODOT) --headless --path . 2>&1

import: ## Run Godot import (required after adding new assets/scenes)
	@$(GODOT) --headless --import --quit 2>&1

test: ## Run unit tests (xUnit, no Godot needed)
	@cd tests && dotnet test --verbosity minimal 2>&1

e2e: build ## Run E2E demo test (headless Godot, console assertion)
	@bash tests/e2e_demo_test.sh

e2e-visual: build ## Run E2E visual capture test (screenshots + video)
	@bash tests/e2e_visual_test.sh

test-game: build ## Launch the actual game (play through the full loop)
	@$(GODOT) --path . &

test-game-headless: build ## Run full game loop E2E test (headless, CI-ready)
	@$(GODOT) --path . --headless scenes/tests/test_game.tscn 2>&1

test-game-capture: build ## Capture screenshots + video of the game loop test
	@bash tests/e2e_game_capture.sh

test-game-check: build ## Automated regression test (headless + unit + evidence check)
	@bash tests/e2e_game_visual_test.sh

test-all: test test-game-headless ## Run all tests (unit + full game loop)

# ─── Universal Test Runner ─────────────────────────────────────────────────
# Usage: make t S=<scene> [F=--headless|--capture|--check]
# Examples:
#   make t S=test-game                    # windowed (default)
#   make t S=test-game F=--headless       # headless
#   make t S=test-hero F=--capture        # capture hero screenshots
#   make t S=test-dungeon F=--check       # regression check
#   make t S=test-game F=--capture        # capture game loop

t: build ## Universal: make t S=<scene> [F=--headless|--capture|--check]
	@[ -z "$(S)" ] && echo "Usage: make t S=<scene-name> [F=--headless|--capture|--check]" && echo "Examples:" && echo "  make t S=test-game" && echo "  make t S=test-hero F=--capture" && echo "  make t S=test-dungeon F=--headless" && exit 1 || bash tests/run-test.sh $(S) $(F)

iso: build ## Run isometric asset demo (validates rendering)
	@$(GODOT) --path . --main-scene res://scenes/iso_demo.tscn &

# ─── Category Runners ───────────────────────────────────────────────────────

test-creatures: build ## Browse all creatures (Up/Down to switch)
	@$(GODOT) --path . --main-scene res://scenes/tests/test_creatures.tscn &

test-characters: build ## Launch all character viewers (hero)
	@$(GODOT) --path . --main-scene res://scenes/tests/test_hero.tscn &

test-env: build ## Launch all environment viewers
	@$(GODOT) --path . --main-scene res://scenes/tests/test_floors.tscn &
	@$(GODOT) --path . --main-scene res://scenes/tests/test_walls.tscn &
	@$(GODOT) --path . --main-scene res://scenes/tests/test_doors.tscn &
	@$(GODOT) --path . --main-scene res://scenes/tests/test_crates.tscn &
	@$(GODOT) --path . --main-scene res://scenes/tests/test_roads.tscn &
	@$(GODOT) --path . --main-scene res://scenes/tests/test_water.tscn &
	@$(GODOT) --path . --main-scene res://scenes/tests/test_objects.tscn &
	@$(GODOT) --path . --main-scene res://scenes/tests/test_town.tscn &
	@$(GODOT) --path . --main-scene res://scenes/tests/test_tilemap.tscn &
	@$(GODOT) --path . --main-scene res://scenes/tests/test_items.tscn &

test-ui-all: build ## Launch all UI viewers
	@$(GODOT) --path . --main-scene res://scenes/tests/test_ui.tscn &
	@$(GODOT) --path . --main-scene res://scenes/tests/test_buttons.tscn &

test-visual: build ## Launch ALL visual test viewers
	@$(MAKE) test-creatures test-characters test-env test-ui-all

# ─── Creatures ──────────────────────────────────────────────────────────────

test-slime: build ## View slime (starts creature browser on slime)
	@$(GODOT) --path . --main-scene res://scenes/tests/test_slime.tscn &

test-skeleton: build ## View skeleton
	@$(GODOT) --path . --main-scene res://scenes/tests/test_skeleton.tscn &

test-goblin: build ## View goblin
	@$(GODOT) --path . --main-scene res://scenes/tests/test_goblin.tscn &

test-zombie: build ## View zombie
	@$(GODOT) --path . --main-scene res://scenes/tests/test_zombie.tscn &

test-ogre: build ## View ogre
	@$(GODOT) --path . --main-scene res://scenes/tests/test_ogre.tscn &

test-werewolf: build ## View werewolf
	@$(GODOT) --path . --main-scene res://scenes/tests/test_werewolf.tscn &

test-elemental: build ## View elemental
	@$(GODOT) --path . --main-scene res://scenes/tests/test_elemental.tscn &

test-magician: build ## View magician
	@$(GODOT) --path . --main-scene res://scenes/tests/test_magician.tscn &

# ─── Characters ─────────────────────────────────────────────────────────────

tool-sprite-align: build ## Sprite alignment debugger (WASD nudge, +/- scale, diamond frame)
	@$(GODOT) --path . --main-scene res://scenes/tests/tool_sprite_align.tscn &

tool-sprite-frames: build ## Sprite animation frame inspector (sheet view, strip export)
	@$(GODOT) --path . --main-scene res://scenes/tests/tool_sprite_frames.tscn &

test-hero: build ## View hero with toggleable equipment layers
	@$(GODOT) --path . --main-scene res://scenes/tests/test_hero.tscn &

# ─── Environment ────────────────────────────────────────────────────────────

test-tilemap: build ## View isometric tilemap rendering
	@$(GODOT) --path . --main-scene res://scenes/tests/test_tilemap.tscn &

test-floors: build ## Cycle through ISS floor themes
	@$(GODOT) --path . --main-scene res://scenes/tests/test_floors.tscn &

test-walls: build ## View ISS wall blocks + animated torch
	@$(GODOT) --path . --main-scene res://scenes/tests/test_walls.tscn &

test-doors: build ## View doorways and passages (closed, open, archway)
	@$(GODOT) --path . --main-scene res://scenes/tests/test_doors.tscn &

test-crates: build ## View SBS crate sprite sheets (64x64)
	@$(GODOT) --path . --main-scene res://scenes/tests/test_crates.tscn &

test-roads: build ## View SBS road and pathway tiles (128x64)
	@$(GODOT) --path . --main-scene res://scenes/tests/test_roads.tscn &

test-water: build ## View SBS water and autotile transitions (128x64)
	@$(GODOT) --path . --main-scene res://scenes/tests/test_water.tscn &

test-objects: build ## View SBS objects (stairs, copings, temple kit)
	@$(GODOT) --path . --main-scene res://scenes/tests/test_objects.tscn &

test-town: build ## View SBS town building and roof tiles
	@$(GODOT) --path . --main-scene res://scenes/tests/test_town.tscn &

test-items: build ## View dungeon items & objects (crates, stairs, copings, temple)
	@$(GODOT) --path . --main-scene res://scenes/tests/test_items.tscn &

# ─── UI ─────────────────────────────────────────────────────────────────────

test-ui: build ## View UI elements (HUD, icons, HP/MP orbs)
	@$(GODOT) --path . --main-scene res://scenes/tests/test_ui.tscn &

test-buttons: build ## View button sprites (round, square, states)
	@$(GODOT) --path . --main-scene res://scenes/tests/test_buttons.tscn &

# ─── Effects ────────────────────────────────────────────────────────────────

test-combat: build ## View combat effects (slash, damage, hit/die)
	@$(GODOT) --path . --main-scene res://scenes/tests/test_combat.tscn &

# ─── Dungeon Generation ────────────────────────────────────────────────────

test-bsp: build ## View BSP room partitioning
	@$(GODOT) --path . --main-scene res://scenes/tests/test_bsp.tscn &

test-drunkard: build ## View drunkard's walk corridor generation
	@$(GODOT) --path . --main-scene res://scenes/tests/test_drunkard.tscn &

test-cellular: build ## View cellular automata smoothing
	@$(GODOT) --path . --main-scene res://scenes/tests/test_cellular.tscn &

test-dungeon: build ## View full dungeon generation pipeline
	@$(GODOT) --path . --main-scene res://scenes/tests/test_dungeon_gen.tscn &

test-procgen: test-bsp test-drunkard test-cellular test-dungeon ## Launch all proc gen test scenes

# ─── Utilities ───────────────────────────────────────────────────────────────

kill: ## Kill all running Godot processes
	@pkill -f "Godot_mono" 2>/dev/null && echo "Killed Godot processes" || echo "No Godot processes running"

status: ## Show project status (git, build, godot version)
	@echo "=== Git ===" && git status --short && echo "" \
	&& echo "=== Build ===" && dotnet build --nologo -v q 2>&1 | tail -3 && echo "" \
	&& echo "=== Godot ===" && $(GODOT) --version 2>&1 && echo "" \
	&& echo "=== .NET ===" && dotnet --version 2>&1 && echo "" \
	&& echo "=== Tickets ===" && grep -c "To Do" docs/dev-tracker.md && echo "tickets remaining"

verify: build ## Build, run headless, confirm C# executes and exits cleanly
	@echo "Building..." && dotnet build --nologo -v q 2>&1 | tail -1 \
	&& echo "Running headless..." && $(GODOT) --headless --path . 2>&1 \
	&& echo "✓ Verify passed"

clean: ## Remove build artifacts and Godot cache
	@rm -rf .godot/mono/temp/bin .godot/mono/temp/obj 2>/dev/null
	@echo "Cleaned build artifacts"

clean-all: ## Remove ALL Godot cache (requires re-import)
	@rm -rf .godot/
	@echo "Cleaned all Godot cache — run 'make import' next"

doctor: ## Check if dev environment is healthy
	@echo "=== Doctor ===" \
	&& echo -n "Godot .NET: " && ($(GODOT) --version 2>&1 || echo "NOT FOUND") \
	&& echo -n ".NET SDK:   " && (dotnet --version 2>&1 || echo "NOT FOUND") \
	&& echo -n ".csproj:    " && ([ -f $(PROJECT).csproj ] && echo "OK" || echo "MISSING") \
	&& echo -n "Build:      " && (dotnet build --nologo -v q 2>&1 | grep -q "succeeded" && echo "OK" || echo "FAILED") \
	&& echo -n "Main scene: " && (grep -q "run/main_scene" project.godot && echo "OK" || echo "NOT SET") \
	&& echo -n "Input Map:  " && (grep -q "\[input\]" project.godot && echo "OK" || echo "NOT SET") \
	&& echo "" && echo "Done."

# ─── Git Helpers ─────────────────────────────────────────────────────────────

branch: ## Create a ticket branch: make branch T=P1-04d
	@[ -z "$(T)" ] && echo "Usage: make branch T=TICKET-ID" && exit 1 || true
	@git checkout -b $(T) main && echo "Created branch $(T)"

squash: ## Squash all commits on current branch into one (interactive)
	@BRANCH=$$(git rev-parse --abbrev-ref HEAD) \
	&& [ "$$BRANCH" = "main" ] && echo "Cannot squash on main" && exit 1 || true \
	@echo "Squashing all commits on $$(git rev-parse --abbrev-ref HEAD) into one..."
	@COMMITS=$$(git rev-list --count main..HEAD) \
	&& echo "$$COMMITS commits to squash"

done: ## Merge current branch to main and delete it
	@BRANCH=$$(git rev-parse --abbrev-ref HEAD) \
	&& [ "$$BRANCH" = "main" ] && echo "Already on main" && exit 1 || true \
	&& git checkout main && git merge --squash $$BRANCH \
	&& echo "Squash-merged $$BRANCH into main. Commit when ready." \
	&& echo "Then run: git branch -d $$BRANCH"
