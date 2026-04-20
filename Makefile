# Makefile — AI-drivable development automation
# Run `make help` to see all available targets.

# Force bash + pipefail so recipes like `dotnet build | tail -5` and
# `godot --run-tests | grep ... || true` propagate the real exit code
# instead of masking failures with tail/grep's zero exit. Copilot PR #33
# round-3 finding: without this, `make build` reports success even when
# `dotnet build` failed, because `tail` exited 0. Same for test pipes.
#
# pipefail only — intentionally NOT adding -e (errexit). Recipes like
# `sandbox-headless` rely on being able to continue after a non-zero to
# capture `EXIT=$$?` and act on it; -e would abort before that line runs.
SHELL      := /bin/bash
.SHELLFLAGS := -o pipefail -c

GODOT      := /Applications/Godot_mono.app/Contents/MacOS/Godot
PROJECT    := DungeonGame

# Roll forward to whatever .NET runtime is installed locally (project
# targets net8.0; devs may have net10+). Without this, `dotnet test`
# aborts with "framework not found" instead of running the adapter.
export DOTNET_ROLL_FORWARD := LatestMajor

.PHONY: help build build-fast refresh run run-headless import test test-unit test-integration test-e2e test-coverage test-gdunit test-ui test-ui-headless test-ui-suite sandbox sandbox-headless sandbox-headless-all pr-copilot-request pr-copilot-wait pr-copilot-status kill clean clean-all status verify doctor branch squash done export-all export-mac export-win export-linux

# ─── Core ────────────────────────────────────────────────────────────────────

help: ## Show available targets
	@grep -E '^[a-zA-Z_-]+:.*?## .*$$' $(MAKEFILE_LIST) | sort | awk 'BEGIN {FS = ":.*?## "}; {printf "\033[36m%-18s\033[0m %s\n", $$1, $$2}'

build: ## Build C# project + run Godot import (so art changes are never stale)
	@dotnet build DungeonGame.csproj 2>&1 | tail -5
	@$(GODOT) --headless --import --quit 2>&1 | tail -3

build-fast: ## Build C# only — skip the Godot import pass (for pure C# iterations)
	@dotnet build DungeonGame.csproj 2>&1 | tail -5

refresh: ## Force re-import Godot assets (run after art / tile / sheet changes)
	@$(GODOT) --headless --import --quit 2>&1 | tail -3

run: build ## Build and launch the game (windowed)
	@$(GODOT) --path . &

run-headless: build ## Build and run headless (auto-quits, for CI/testing)
	@$(GODOT) --headless --path . 2>&1

import: ## Run Godot import (required after adding new assets/scenes)
	@$(GODOT) --headless --import --quit 2>&1

test: ## Run ALL tests (unit + integration)
	@dotnet test tests/unit/DungeonGame.Tests.Unit.csproj --verbosity minimal 2>&1
	@dotnet test tests/integration/DungeonGame.Tests.Integration.csproj --verbosity minimal 2>&1

test-unit: ## Run unit tests only
	@dotnet test tests/unit/DungeonGame.Tests.Unit.csproj --verbosity normal 2>&1

test-unit-one: ## Run a single xUnit unit test. Usage: make test-unit-one TEST=Namespace.ClassName.MethodName
	@[ -z "$(TEST)" ] && echo "Usage: make test-unit-one TEST=<fully.qualified.MethodName>" && echo "Example: make test-unit-one TEST=DungeonGame.Tests.Unit.CombatFormulasTests.SoftCap_At60Raw_Returns30" && exit 1 || true
	@dotnet test tests/unit/DungeonGame.Tests.Unit.csproj --filter "FullyQualifiedName=$(TEST)" --verbosity normal 2>&1

test-integration: ## Run integration tests only
	@dotnet test tests/integration/DungeonGame.Tests.Integration.csproj --verbosity normal 2>&1

test-integration-one: ## Run a single xUnit integration test. Usage: make test-integration-one TEST=Namespace.ClassName.MethodName
	@[ -z "$(TEST)" ] && echo "Usage: make test-integration-one TEST=<fully.qualified.MethodName>" && exit 1 || true
	@dotnet test tests/integration/DungeonGame.Tests.Integration.csproj --filter "FullyQualifiedName=$(TEST)" --verbosity normal 2>&1

test-e2e: ## Run E2E tests (requires GODOT_BIN set)
	@dotnet test tests/e2e/DungeonGame.Tests.E2E.csproj --verbosity normal 2>&1

test-e2e-one: ## Run a single GdUnit4 E2E test. Usage: make test-e2e-one TEST=Namespace.ClassName.MethodName
	@[ -z "$(TEST)" ] && echo "Usage: make test-e2e-one TEST=<fully.qualified.MethodName>" && exit 1 || true
	@dotnet test tests/e2e/DungeonGame.Tests.E2E.csproj --filter "FullyQualifiedName=$(TEST)" --verbosity normal 2>&1

test-coverage: ## Run tests + generate HTML coverage report (./coverage/report/index.html)
	@dotnet test tests/unit/DungeonGame.Tests.Unit.csproj \
		--collect:"XPlat Code Coverage" --results-directory ./coverage/unit 2>&1
	@dotnet test tests/integration/DungeonGame.Tests.Integration.csproj \
		--collect:"XPlat Code Coverage" --results-directory ./coverage/integration 2>&1
	@dotnet tool run reportgenerator \
		-reports:"./coverage/**/*.xml" \
		-targetdir:"./coverage/report" \
		-reporttypes:"Html" 2>&1
	@echo "Coverage report: ./coverage/report/index.html"

test-gdunit: ## Run GdUnit4 E2E/scene tests (requires GODOT_BIN or Godot on PATH)
	@dotnet test tests/e2e/DungeonGame.Tests.E2E.csproj \
		--settings .runsettings \
		--verbosity normal \
		-m:1 2>&1

test-ui: kill build ## Run UI tests WITH game window visible — default. Seeds screenshot baselines, lets watcher verify visually.
	@$(GODOT) --path . --run-tests --quit-on-finish 2>&1 | tee .ui-test.log \
		| (grep -E "(✓|✗|Test|═══|passed|failed|Screenshot|SEEDED|MISMATCH|Info \(GoTest\))" || true)
	@if grep -q "✗" .ui-test.log 2>/dev/null; then rm -f .ui-test.log; echo "❌ UI test failures reported"; exit 1; fi
	@rm -f .ui-test.log

test-ui-headless: kill build ## Run UI tests headless (CI). Skips VerifyScreenshot — screenshots require a rendering context.
	@$(GODOT) --headless --path . --run-tests --quit-on-finish 2>&1 | tee .ui-test.log \
		| (grep -E "(✓|✗|Test|═══|passed|failed|Info \(GoTest\))" || true)
	@if grep -q "✗" .ui-test.log 2>/dev/null; then rm -f .ui-test.log; echo "❌ UI test failures reported"; exit 1; fi
	@rm -f .ui-test.log

test-ui-suite: kill build ## Run a specific GoDotTest suite WINDOWED: make test-ui-suite SUITE=SplashTests
	@[ -z "$(SUITE)" ] && echo "Usage: make test-ui-suite SUITE=<name>" && echo "Example: make test-ui-suite SUITE=SplashTests" && exit 1 || true
	@$(GODOT) --path . --run-tests=$(SUITE) --quit-on-finish 2>&1 | tee .ui-test.log \
		| (grep -E "(✓|✗|Test|═══|passed|failed|Screenshot|SEEDED|MISMATCH|Info \(GoTest\))" || true)
	@if grep -q "✗" .ui-test.log 2>/dev/null; then rm -f .ui-test.log; echo "❌ UI test failures reported"; exit 1; fi
	@rm -f .ui-test.log

test-ui-one: kill build ## Run one GoDotTest suite WINDOWED (single-method filter = TEST-12 follow-up). Usage: make test-ui-one SUITE=SplashTests
	@[ -z "$(SUITE)" ] && echo "Usage: make test-ui-one SUITE=<ClassName>" && echo "Example: make test-ui-one SUITE=SplashTests" && echo "" && echo "NOTE: Chickensoft.GoDotTest's --run-tests flag filters at the SUITE (class) level only." && echo "      Method-level filtering needs a small wrapper in Main.RunTests — tracked as TEST-12 follow-up." && exit 1 || true
	@$(GODOT) --path . --run-tests=$(SUITE) --quit-on-finish 2>&1 | tee .ui-test.log \
		| (grep -E "(✓|✗|Test|═══|passed|failed|Screenshot|SEEDED|MISMATCH|Info \(GoTest\))" || true)
	@if grep -q "✗" .ui-test.log 2>/dev/null; then rm -f .ui-test.log; echo "❌ UI test failed"; exit 1; fi
	@rm -f .ui-test.log

# ─── PR / Copilot helpers ──────────────────────────────────────────────────
# Usage patterns:
#   make pr-copilot-request PR=4     — ask Copilot to review PR #4
#   make pr-copilot-wait PR=4        — block until Copilot's next review lands
#   make pr-copilot-status PR=4      — show latest Copilot review + comments

pr-copilot-request: ## Request Copilot PR review: make pr-copilot-request PR=4
	@[ -z "$(PR)" ] && echo "Usage: make pr-copilot-request PR=<num>" && exit 1 || true
	@gh pr edit $(PR) --add-reviewer "@copilot"

pr-copilot-wait: ## Wait until Copilot's review count increases: make pr-copilot-wait PR=4
	@[ -z "$(PR)" ] && echo "Usage: make pr-copilot-wait PR=<num>" && exit 1 || true
	@REPO=$$(gh repo view --json nameWithOwner -q .nameWithOwner); \
	BEFORE=$$(gh api repos/$$REPO/pulls/$(PR)/reviews -q '[.[] | select(.user.login=="copilot-pull-request-reviewer[bot]")] | length'); \
	echo "Waiting for new Copilot review on PR #$(PR) (current count: $$BEFORE)..."; \
	for i in $$(seq 1 40); do \
		NOW=$$(gh api repos/$$REPO/pulls/$(PR)/reviews -q '[.[] | select(.user.login=="copilot-pull-request-reviewer[bot]")] | length'); \
		if [ "$$NOW" -gt "$$BEFORE" ]; then \
			echo "✓ Copilot review landed (now $$NOW total). Latest:"; \
			gh api repos/$$REPO/pulls/$(PR)/reviews -q 'map(select(.user.login=="copilot-pull-request-reviewer[bot]")) | last | .body' | head -5; \
			exit 0; \
		fi; \
		sleep 15; \
	done; \
	echo "✗ Timed out after 10 min waiting for Copilot review"; exit 1

pr-copilot-status: ## Show latest Copilot review + inline comments: make pr-copilot-status PR=4
	@[ -z "$(PR)" ] && echo "Usage: make pr-copilot-status PR=<num>" && exit 1 || true
	@REPO=$$(gh repo view --json nameWithOwner -q .nameWithOwner); \
	echo "=== Reviews ==="; \
	gh api repos/$$REPO/pulls/$(PR)/reviews -q '.[] | select(.user.login=="copilot-pull-request-reviewer[bot]") | "[\(.submitted_at)] \(.state)\n\(.body[:400])\n"'; \
	echo ""; \
	echo "=== Inline comments ==="; \
	gh api repos/$$REPO/pulls/$(PR)/comments -q '.[] | select(.user.login=="Copilot" or .user.login=="copilot-pull-request-reviewer[bot]") | "\(.path):\(.line)\n  \(.body[:400])\n"'

# ─── Sandbox ─────────────────────────────────────────────────────────────────
# SCENE values: sprite-viewer, tile-viewer, projectile-viewer,
#               floor-gen, inventory, loot-table, bank, death-penalty, skill-tree,
#               combat, movement, enemy, stats

sandbox: build ## Launch a sandbox scene visually: make sandbox SCENE=floor-gen
	@[ -z "$(SCENE)" ] && echo "Usage: make sandbox SCENE=<name>" && exit 1 || true
	@$(GODOT) --path . --main-scene "res://scenes/sandbox/$$(make -s _sandbox-path SCENE=$(SCENE))" &

sandbox-headless: build ## Run a sandbox headless (console output): make sandbox-headless SCENE=floor-gen
	@[ -z "$(SCENE)" ] && echo "Usage: make sandbox-headless SCENE=<name>" && exit 1 || true
	@$(GODOT) --headless --path . \
		--main-scene "res://scenes/sandbox/$$(make -s _sandbox-path SCENE=$(SCENE))" \
		--quit-after 18000 2>&1; \
	EXIT=$$?; \
	[ $$EXIT -eq 0 ] && echo "✅ Sandbox passed" || echo "❌ Sandbox failed (exit $$EXIT)"; \
	exit $$EXIT

sandbox-headless-all: build ## Run ALL sandbox headless checks in sequence
	@for scene in sprite-viewer tile-viewer floor-gen inventory loot-table bank death-penalty combat movement enemy stats full-run; do \
		echo ""; echo "── $$scene ──"; \
		$(MAKE) sandbox-headless SCENE=$$scene || true; \
	done

# Internal: resolve SCENE name → relative path under scenes/sandbox/
_sandbox-path:
	@case "$(SCENE)" in \
		sprite-viewer)    echo "assets/SpriteViewer.tscn" ;; \
		tile-viewer)      echo "assets/TileViewer.tscn" ;; \
		projectile-viewer) echo "assets/ProjectileViewer.tscn" ;; \
		floor-gen)        echo "systems/FloorGenSandbox.tscn" ;; \
		inventory)        echo "systems/InventorySandbox.tscn" ;; \
		loot-table)       echo "systems/LootTableSandbox.tscn" ;; \
		bank)             echo "systems/BankSandbox.tscn" ;; \
		death-penalty)    echo "systems/DeathPenaltySandbox.tscn" ;; \
		skill-tree)       echo "systems/SkillTreeSandbox.tscn" ;; \
		combat)           echo "mechanics/CombatSandbox.tscn" ;; \
		movement)         echo "mechanics/MovementSandbox.tscn" ;; \
		enemy)            echo "mechanics/EnemySandbox.tscn" ;; \
		stats)            echo "mechanics/StatsSandbox.tscn" ;; \
		full-run)         echo "FullRunSandbox.tscn" ;; \
		launcher)         echo "SandboxLauncher.tscn" ;; \
		*)                echo "ERROR: unknown SCENE '$(SCENE)'" >&2; exit 1 ;; \
	esac

# ─── Export / Build Executables ──────────────────────────────────────────────

export-all: build ## Export executables for all platforms (macOS, Windows, Linux) → build/
	@./scripts/build.sh

export-mac: build ## Export macOS app → build/DungeonGame.app
	@mkdir -p build && $(GODOT) --headless --path . --export-debug "macOS" build/DungeonGame.app 2>&1 | tail -3

export-win: build ## Export Windows exe → build/DungeonGame.exe
	@mkdir -p build && $(GODOT) --headless --path . --export-debug "Windows Desktop" build/DungeonGame.exe 2>&1 | tail -3

export-linux: build ## Export Linux binary → build/DungeonGame.x86_64
	@mkdir -p build && $(GODOT) --headless --path . --export-debug "Linux" build/DungeonGame.x86_64 2>&1 | tail -3

# ─── Utilities ───────────────────────────────────────────────────────────────

kill: ## Kill all running Godot processes
	@pkill -f "Godot_mono" 2>/dev/null && echo "Killed Godot processes" || echo "No Godot processes running"

status: ## Show project status (git, build, godot version)
	@echo "=== Git ===" && git status --short && echo "" \
	&& echo "=== Build ===" && dotnet build DungeonGame.csproj --nologo -v q 2>&1 | tail -3 && echo "" \
	&& echo "=== Godot ===" && $(GODOT) --version 2>&1 && echo "" \
	&& echo "=== .NET ===" && dotnet --version 2>&1

verify: build ## Build, run headless, confirm C# executes and exits cleanly
	@echo "Building..." && dotnet build DungeonGame.csproj --nologo -v q 2>&1 | tail -1 \
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
	&& echo -n "Build:      " && (dotnet build DungeonGame.csproj --nologo -v q 2>&1 | grep -q "succeeded" && echo "OK" || echo "FAILED") \
	&& echo -n "Main scene: " && (grep -q "run/main_scene" project.godot && echo "OK" || echo "NOT SET") \
	&& echo -n "Input Map:  " && (grep -q "\[input\]" project.godot && echo "OK" || echo "NOT SET") \
	&& echo "" && echo "Done."

# ─── Git Helpers ─────────────────────────────────────────────────────────────

branch: ## Create a ticket branch: make branch T=P1-04d
	@[ -z "$(T)" ] && echo "Usage: make branch T=TICKET-ID" && exit 1 || true
	@git checkout -b $(T) main && echo "Created branch $(T)"

squash: ## Squash all commits on current branch into one (interactive)
	@BRANCH=$$(git rev-parse --abbrev-ref HEAD) \
	&& [ "$$BRANCH" = "main" ] && echo "Cannot squash on main" && exit 1 || true
	@echo "Squashing all commits on $$(git rev-parse --abbrev-ref HEAD) into one..."
	@COMMITS=$$(git rev-list --count main..HEAD) \
	&& echo "$$COMMITS commits to squash"

done: ## Merge current branch to main and delete it
	@BRANCH=$$(git rev-parse --abbrev-ref HEAD) \
	&& [ "$$BRANCH" = "main" ] && echo "Already on main" && exit 1 || true \
	&& git checkout main && git merge --squash $$BRANCH \
	&& echo "Squash-merged $$BRANCH into main. Commit when ready." \
	&& echo "Then run: git branch -d $$BRANCH"

# ─── Visual Test Scenes (add as scenes are created) ─────────────────────────
# Targets will be added here as visual-first development proceeds.
# Pattern: test-<name>: build ## Description
#   @$(GODOT) --path . --main-scene res://scenes/tests/test_<name>.tscn &
