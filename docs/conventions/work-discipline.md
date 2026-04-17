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

## Special Case: External AI Feedback (Copilot, Cursor, ChatGPT, other AIs)

When another AI reviews or comments on this project's code (GitHub Copilot PR reviews, Cursor suggestions, ChatGPT snippets pasted into chat, etc.), treat every suggestion as a **hypothesis, not a finding**. The AI that wrote it has no more certainty than we do — and often less context.

User directive: *"advices are grains of salt, best not to take too much of it."*

Procedure:

1. **Read the claim fully.** Don't skim.
2. **Trace the mechanism in our codebase.** Open the cited files. Confirm the code path actually does what the AI says.
3. **Verify behavior claims via primary sources.** If the AI asserts "Godot does X" or "this library does Y", check the official docs — not another AI's summary of the docs. Web search, framework docs, package READMEs.
4. **If fully supported by facts → apply the fix** and cite the verification (e.g., "Copilot PR #3 review; confirmed via Godot docs on `Modulate` cascade").
5. **If partially supported → narrow the fix** to what's actually verified. Don't expand beyond evidence.
6. **If unsupported → ignore and move on.** Record the rejected claim in the dev journal so future sessions don't reconsider it blindly.

Why this matters: AIs produce confident-sounding output. Uncritically applying one AI's suggestion through another AI's hands amplifies errors. Verification at every step breaks the amplification chain.

Example — Session 20 had 4 Copilot review claims on PR #3. All 4 turned out valid (traced + Godot-docs-confirmed), but the default posture must be skeptical. See `docs/dev-journal.md` Session 20 for the verification table.

Cost: two extra commits, ~15 minutes of rework, and a public-facing doc that would have been wrong if not caught.

### Replying to External AI Reviews (async 2-AI dialogue)

Beyond verify-and-fix, we can also **reply** to Copilot's review comments. This creates an audit-trail dialogue — useful when a claim is rejected (so future reviews don't re-flag it) or when acknowledging a fix with a commit SHA for PR readers.

**Threaded reply to a specific inline finding** (nests under Copilot's comment on the PR line):
```bash
gh api --method POST \
  /repos/:owner/:repo/pulls/:PR/comments/:COMMENT_ID/replies \
  -f body="<reply text>"
```

**Top-level PR comment** (general, not threaded):
```bash
gh pr comment :PR --body "<text>"
```

**When to reply:**
- **Rejecting a claim** — note why a Copilot suggestion was NOT applied, with the verification evidence. Prevents re-flagging on future reviews and leaves a record for PR readers.
  > *"Verified the login is actually `Copilot` via live API query — not a bug. Keeping as-is."*
- **Acknowledging a fix with a SHA** — link the commit that addresses the finding. Useful for PR audit trail.
  > *"Addressed in 3a44449 — added pr-copilot-* targets to .PHONY."*
- **Explaining a partial fix** — when you applied part of the suggestion (the defensive improvement) but rejected the premise (the underlying bug claim).

**When NOT to reply:**
- Force-push already happened and Copilot re-reviewed — the new review implicitly accepts (no re-flag) or re-flags. No reply needed.
- Purely cosmetic "thanks for the review" — adds noise.

**What Copilot does with replies:**
- Reads them on re-review as PR conversation context
- Doesn't chat back — it posts a new review when re-triggered
- The reply sits in the PR thread permanently, visible to anyone reading the PR history

**Why document this publicly:** this repo is a paradigm showcase for AI+Human natural-language programming. The full communication loop — *two AIs discussing code on a human's PR while the human directs the conversation* — is itself novel collaboration pattern worth making visible. See [`docs/development-paradigm.md`](../development-paradigm.md) for the bigger picture.

## See Also

- [AGENTS.md — Work Discipline section](../../AGENTS.md#work-discipline--slow-is-smooth-smooth-is-fast) — the same principle, summarized inline
- [docs/conventions/ai-workflow.md](ai-workflow.md) — the full workflow protocol
- [docs/conventions/ai-context-files.md](ai-context-files.md) — a direct application of this principle (Rule #6: verify before adding)
- [docs/development-paradigm.md](../development-paradigm.md) — why this repo is built this way
