# Convention: Work Discipline — "Slow is Smooth, Smooth is Fast"

## Summary

This is the foundational working principle for every AI agent operating on this repo. It applies to every task — code, tests, docs, art, commits, research, planning. Not optional, not domain-specific.

## The Principle

> **"Slow is smooth, smooth is fast. Do it once, do it right, never do it more than once."**

Individual tasks should not be rushed. LLMs bias toward confident-sounding output even when uncertain. Rushing amplifies that bias, produces plausible-but-wrong work, and triggers expensive rework. **Rework is the enemy.** A task done twice costs more than a task done slowly once. Measured, verified work at each step is faster in total than fast guessing followed by correction cycles.

## Why This Matters in AI-Driven Development

Traditional software development with humans has implicit guards against speculation: the developer writing the code is also the one debugging it the next day, so there's natural pressure to get it right. AI agents don't feel that pressure the same way — a wrong answer costs the AI nothing; it costs the project hours.

Without a discipline rule, AI agents will:
- Invent plausible-sounding API names, filenames, or package versions
- Claim "it works" without running the code
- Skip reading specs because "I can figure it out"
- Ship partial work because the next task looks more interesting
- Repeat the same mistake in different forms across sessions

All of these trade short-term throughput for long-term rework. The discipline below prevents that trade.

## The Rules

### 1. Verify, don't speculate.

Before asserting a filename, API method, NuGet version, URL, behavior, spec claim, or performance characteristic: look it up.

Tools available:
- Web search (`WebFetch`, `WebSearch`, `claude-code-guide` agent for Claude Code questions)
- Reading actual docs (`.nuget/packages/<package>/README.md`, official doc sites)
- Grepping the codebase (`Grep`, `Glob`)
- Reading NuGet package contents (`ls ~/.nuget/packages/<package>/<version>/`)
- Reading the spec in `docs/`

If you can't verify something, say **"uncertain"** and research before proceeding. Do not fill the gap with a confident-sounding guess.

### 2. No confident wrongness.

When you're not sure, say so. These are acceptable:
- *"I believe X, but haven't verified."*
- *"The docs suggest Y, but let me confirm."*
- *"I'm uncertain about this — running a quick check."*

These are not acceptable when you guessed:
- *"X is Y."*
- *"The method is called `DoTheThing()`."*
- *"This will fix it."*

### 3. Read before writing.

Before code: read the relevant spec in `docs/`.
Before editing a file: read the file.
Before assuming a tool's behavior: read the tool's official docs.
Before changing a system: trace how it's currently used (`Grep`).

Reading takes minutes. Fixing wrong assumptions takes hours, sometimes days.

### 4. Test what you claim.

Don't claim "it works" without running it. Minimum bars:
- Any change that touches C# code → `make build` must pass
- Any logic change → `make test` must pass
- Any UI change → actually run the game and test the UI, or write a UI test
- Any doc change → re-read the doc to check it reads correctly

"Should work" ≠ "works." Until you've run it, it's unverified.

### 5. One task, done fully, then stop.

Finish the current task completely — including docs, tests, and commit — before starting the next. Half-done tasks pile up and rot. See the [Post-Task Protocol](../../AGENTS.md#2a-post-task-protocol) for the full checklist.

The user can always ask for the next task. Don't grab it preemptively.

### 6. When in doubt, ask.

Options when uncertain:
- **Ask the user.** A 30-second clarification is cheaper than a wrong implementation.
- **Spawn a research agent.** `Explore` for codebase questions, `claude-code-guide` for tool questions, `Plan` for architecture questions.
- **Do the verification yourself.** Web search, grep, read docs.

Never: guess to save time, then discover the mistake 20 minutes later.

### 7. Reflect on corrections.

When the user corrects you, the correction almost always encodes a broader principle. Don't just fix the specific instance — figure out the class of mistake and encode the lesson where it will catch the next instance:

- Broad workflow rule → `AGENTS.md`
- Specific convention → `docs/conventions/`
- Domain-specific behavior → relevant `docs/systems/` or `docs/flows/` spec
- Tool-specific quirk → `CLAUDE.md` (if truly tool-specific) or `AGENTS.md`

## The Trap to Avoid

LLMs produce large volumes of output quickly. The temptation is to treat output volume as productivity. It isn't.

**Rework-adjusted throughput** is the real measure:

```
real throughput = output shipped × (1 - rework rate)
```

A single commit that ships correct code, correct tests, and correct docs is worth ten commits that need to be revisited. Speed of individual actions is irrelevant if the project's forward progress is being eaten by correction cycles.

When you're about to guess: **stop. Verify. Then proceed.**

## Signs You're About to Violate This

Watch for these internal states — they signal a guess is coming:

- *"I'm pretty sure..."* — you're not sure. Verify.
- *"This should work."* — you haven't tested. Test.
- *"I'll just fix this one thing quickly."* — you're skipping a step. Follow the protocol.
- *"The API is probably..."* — you're guessing an API. Look it up.
- *"This is how tools like X usually work."* — don't generalize without verification.
- *"Let me make an assumption and proceed."* — no. Resolve the uncertainty first.

## Real Examples From This Repo

### Good: verified before claiming

Before writing `docs/conventions/ai-context-files.md`, verified every AI tool's filename convention against official docs. Found that `CURSOR.md` and `COPILOT.md` are **not** real conventions — those tools use `.cursor/rules/*.mdc` and `.github/copilot-instructions.md` respectively. Correction caught before going public.

### Bad: speculated, then corrected

An earlier version of `ai-context-files.md` listed `CURSOR.md`, `COPILOT.md`, `GEMINI.md` as examples of tool-specific context files. Only `GEMINI.md` is real. User pushed back ("does Gemini actually use GEMINI.md?"), forcing a verification pass and a corrective commit. This whole discipline doc exists because of that incident.

Cost: two extra commits, ~15 minutes of rework, and a public-facing doc that would have been wrong if not caught.

## See Also

- [AGENTS.md — Work Discipline section](../../AGENTS.md#work-discipline--slow-is-smooth-smooth-is-fast) — the same principle, summarized inline
- [docs/conventions/ai-workflow.md](ai-workflow.md) — the full workflow protocol
- [docs/conventions/ai-context-files.md](ai-context-files.md) — a direct application of this principle (Rule #6: verify before adding)
- [docs/development-paradigm.md](../development-paradigm.md) — why this repo is built this way
