---
name: Documentation Automation Research
description: Research findings on AI self-maintaining docs -- Stop hooks, CLAUDE.md post-task rules, conventional commits, doc-test patterns
type: project
---

Completed comprehensive research (2026-04-09) on making AI automatically maintain documentation in sync with code.

**Key findings:**
- Claude Code Stop hooks (prompt type) can enforce doc-update checklists after every code change
- SessionStart hooks can inject dynamic context (test counts, recent commits) at session start
- CLAUDE.md rules should include a "Post-Task Protocol" section with imperative doc-update rules
- Volatile numbers (test counts, ticket status) should NOT be hardcoded in AGENTS.md -- use dynamic commands instead
- Conventional Commits enable automated CHANGELOG generation via git-cliff
- Two-layer enforcement works best: soft rules in CLAUDE.md + hard enforcement via Stop hook

**Why:** AGENTS.md has stale numbers (says "219 tests" when there are 480+), user has to manually remind AI to update journal, docs drift from code over time.

**How to apply:** When implementing these changes, create .claude/settings.json with hooks, update CLAUDE.md with Post-Task Protocol, and remove volatile numbers from AGENTS.md Current State section.
