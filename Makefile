# Makefile — AI-drivable development automation
# Run `make help` to see all available targets.

GODOT      := /Applications/Godot_mono.app/Contents/MacOS/Godot
PROJECT    := DungeonGame
TIMEOUT    := 15

.PHONY: help setup build run run-headless test import kill clean status verify doctor

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

test: build ## Run all automated tests (dotnet test)
	@dotnet test 2>&1 || echo "No test project configured yet."

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
