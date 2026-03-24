# Makefile — AI-drivable development automation
# Run `make help` to see all available targets.

GODOT   := godot
GDLINT  := gdlint
GDFORMAT := gdformat
GUT_CLI := addons/gut/gut_cmdln.gd
TEST_DIR := res://tests/
GD_DIRS := scripts/ scenes/

.PHONY: help setup import test lint format format-fix check run tiles clean

help: ## Show available targets
	@grep -E '^[a-zA-Z_-]+:.*?## .*$$' $(MAKEFILE_LIST) | sort | awk 'BEGIN {FS = ":.*?## "}; {printf "\033[36m%-15s\033[0m %s\n", $$1, $$2}'

setup: ## Configure git hooks and verify tools
	git config core.hooksPath .githooks
	@echo "Git hooks configured (.githooks/)"
	@command -v $(GODOT) >/dev/null 2>&1 && echo "Godot: $$($(GODOT) --version 2>/dev/null || echo 'installed')" || echo "Godot: NOT FOUND — install via 'brew install --cask godot'"
	@command -v $(GDLINT) >/dev/null 2>&1 && echo "gdlint: OK" || echo "gdlint: NOT FOUND — install via 'pip3 install gdtoolkit'"
	@command -v $(GDFORMAT) >/dev/null 2>&1 && echo "gdformat: OK" || echo "gdformat: NOT FOUND — install via 'pip3 install gdtoolkit'"
	@[ -f $(GUT_CLI) ] && echo "GUT: OK" || echo "GUT: NOT FOUND — run 'git clone https://github.com/bitwes/Gut.git addons/gut'"

import: ## Run Godot import (required once after clone)
	$(GODOT) --headless --import 2>&1 || true

test: ## Run all GUT tests headlessly
	$(GODOT) --headless -s $(GUT_CLI) -gdir=$(TEST_DIR) -gexit

lint: ## Lint all GDScript files
	@find $(GD_DIRS) -name '*.gd' 2>/dev/null | head -1 > /dev/null && $(GDLINT) $(GD_DIRS) || echo "No .gd files to lint yet."

format: ## Check GDScript formatting (no changes)
	@find $(GD_DIRS) -name '*.gd' 2>/dev/null | head -1 > /dev/null && $(GDFORMAT) --check $(GD_DIRS) || echo "No .gd files to check yet."

format-fix: ## Auto-format GDScript files
	@find $(GD_DIRS) -name '*.gd' 2>/dev/null | head -1 > /dev/null && $(GDFORMAT) $(GD_DIRS) || echo "No .gd files to format yet."

check: lint format test ## Run lint + format check + tests

run: ## Launch the game
	$(GODOT) --path .

tiles: ## Generate tile assets
	cd $(CURDIR) && python3 scripts/generate_tiles.py

clean: ## Remove Godot editor cache
	rm -rf .godot/
