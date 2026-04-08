# Subagent & Persona Research Reference

Research compiled April 2026 from official Anthropic docs, community blogs, X/Twitter, GitHub repos, and game-dev implementations. This is a living reference — update as new patterns emerge.

## Key Concepts

**Subagents** are specialized AI assistants that run in their own context window with a custom system prompt, specific tool access, and independent permissions. They report results back to the parent conversation.

**Three levels of multi-agent in Claude Code:**

| Level | Mechanism | Communication | Cost |
|-------|-----------|---------------|------|
| Subagents | Spawned by main agent, own context | Report back to parent only | 1x baseline |
| Agent Teams | Multiple sessions, team lead + teammates | Mailbox system + shared task list | 3-7x tokens |
| Swarms | Experimental orchestration | Task board + messaging | Varies |

## How To Define Custom Agents

Create markdown files in `.claude/agents/` with YAML frontmatter:

```markdown
---
name: agent-name              # Required. Lowercase + hyphens
description: When to use this  # Required. Claude matches tasks against this
tools: Read, Grep, Glob        # Optional. Allowlist (inherits all if omitted)
model: sonnet                   # Optional. sonnet/opus/haiku
maxTurns: 50                    # Optional. Prevents runaway sessions
memory: project                 # Optional. user/project/local
isolation: worktree             # Optional. Git worktree for file safety
effort: high                    # Optional. low/medium/high/max
permissionMode: default         # Optional. default/acceptEdits/auto/dontAsk
---

System prompt goes here. This defines the agent's persona and instructions.
```

**Directory priority (highest to lowest):**
1. Managed settings (org-wide)
2. `--agents` CLI flag (session only)
3. `.claude/agents/` (project — version controlled)
4. `~/.claude/agents/` (user-level — all projects)

## Invocation Methods

1. **Automatic:** Claude matches requests against agent descriptions
2. **@mention:** Type `@` then select agent from autocomplete
3. **Session-wide:** `claude --agent agent-name`
4. **Natural language:** "Use the design-lead agent to..."

**Tip:** Auto-delegation is unreliable. Use @mention for reliability.

## Context Rules

| Subagent Receives | Does NOT Receive |
|-------------------|-----------------|
| Its own system prompt | Parent's conversation history |
| The prompt string from parent | Parent's tool results |
| Project CLAUDE.md | Parent's system prompt |
| Tool definitions (or subset) | Other subagents' results |

**The ONLY channel from parent to subagent is the prompt string.** Include file paths, decisions, and context directly.

## Model Selection

| Model | Speed | Cost | Best For |
|-------|-------|------|---------|
| Haiku 4.5 | Fastest | Cheapest | Quick lookups, simple queries |
| Sonnet 4.6 | Fast | Mid | Daily work, ~90% of Opus quality |
| Opus 4.6 | Slower | 5x Sonnet | Architecture, complex reasoning |

**Cost strategy:** Default to Sonnet, use Opus only for design/architecture decisions.

## Anti-Patterns

1. **Subagents cannot spawn subagents.** No nesting.
2. **Auto-delegation is inconsistent.** Use @mention for reliability.
3. **No mid-stream dialogue.** Can't ask a running subagent questions.
4. **File-based agents load at startup only.** Restart session after creating new agent files.
5. **Background agents auto-deny unapproved permissions.** Pre-approve everything needed.
6. **Don't overload one subagent.** Each should excel at ONE specific task.
7. **Don't use subagents for quick tasks.** They need context-gathering time.

## Game Dev Reference: Claude Code Game Studios

GitHub: `Donchitos/Claude-Code-Game-Studios` — a full game studio template with 49 agents organized hierarchically:

- **Tier 1 Directors (Opus):** Creative Director, Technical Director, Producer
- **Tier 2 Leads (Sonnet):** Game Designer, Lead Programmer, Art Director, QA Lead
- **Tier 3 Specialists (Sonnet/Haiku):** Gameplay Programmer, AI Programmer, Level Designer

**Key patterns from Game Studios:**
- Vertical delegation: directors → leads → specialists
- Same-tier agents consult horizontally but can't make binding cross-domain decisions
- Disagreements escalate to shared parents
- Path-scoped coding standards

**Our approach is simpler:** 7 team leads (not 49 agents). We scale up only if needed.

## Persistent Memory

Subagents can remember across sessions:
- `memory: user` — across all projects
- `memory: project` — project-specific, version-controllable
- `memory: local` — project-specific, not version-controlled

**Tip:** Ask agents to "consult your memory before starting" and "save what you learned" after tasks.

## Agent Teams (Future)

For true parallel execution with inter-agent communication:

```json
// .claude/settings.json
{
  "env": {
    "CLAUDE_CODE_EXPERIMENTAL_AGENT_TEAMS": "1"
  }
}
```

Requires Opus model. Teams have: lead, teammates, shared task list, mailbox system. 3-7x token cost. Use only when agents genuinely need to communicate with each other.

## Sources

- [Official subagents docs](https://code.claude.com/docs/en/sub-agents)
- [Agent Teams docs](https://code.claude.com/docs/en/agent-teams)
- [Agent SDK](https://platform.claude.com/docs/en/agent-sdk/overview)
- [Claude Code Game Studios](https://github.com/Donchitos/Claude-Code-Game-Studios)
- [ClaudeLab @mention guide](https://claudelab.net/en/articles/claude-code/claude-code-custom-subagents-at-mention-guide)
- [Context management tips](https://www.richsnapp.com/article/2025/10-05-context-management-with-subagents-in-claude-code)
- [Anti-patterns](https://stevekinney.com/courses/ai-development/subagent-anti-patterns)
