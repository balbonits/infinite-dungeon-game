# Agile Methodology

Quick-reference for project workflow. Rules, not essays. Read once, follow always.

---

## 1. Methodology

| Aspect | Rule |
|---|---|
| Framework | Continuous flow (kanban). No sprints, no story points, no velocity tracking. |
| Cadence | Work is always flowing. Pick up the next highest-priority ticket when the current one is done. |
| Spec-first | Every system is fully documented in `docs/` before code exists. No exceptions. |
| Ticket-driven | One ticket = one branch = one focused change = one squash commit. |
| Prioritized | Tiers P0--P4 + Backlog + Tech Debt. Work the highest tier first, top to bottom within a tier. |
| Who does what | AI writes all code. Product owner directs vision and approves outcomes. |

---

## 2. Ticket System

### Ticket Structure

Every ticket is **atomic** -- one clear deliverable, one reviewable change, one testable outcome. If a ticket has two deliverables, it is two tickets.

### Ticket IDs

IDs are scoped by epic. The epic prefix tells you what kind of work it is:

| Prefix | Epic | Example |
|---|---|---|
| `SPEC-*` | Spec completion (design docs) | `SPEC-04a` |
| `SETUP-*` | Project/environment setup | `SETUP-02b` |
| `P1-*` | Prototype parity (MVP) | `P1-07c` |
| `P2-*` | Post-MVP milestone | `P2-01` |
| `P3-*` | Planned future features | `P3-05` |
| `P4-*` | Someday features | `P4-02` |
| `RES-*` | Research (feeds another ticket) | `RES-28` |
| `TEST-*` | Test creation/execution | `TEST-12` |
| `INFRA-*` | Infrastructure/tooling | `INFRA-03` |

### Status Flow

```
To Do --> In Progress --> Done
                    \--> Blocked
```

| Status | Meaning |
|---|---|
| To Do | Ready for pickup. All dependencies are Done. |
| In Progress | Actively being worked. One ticket per agent at a time. |
| Done | Deliverable complete, tests pass, evidence captured (if visual). |
| Blocked | Cannot proceed. Dependency is not Done. The blocking ticket ID must be noted. |

### Dependency Rules

- Dependencies are tracked explicitly in each ticket's `Deps` field.
- A ticket cannot move to In Progress while any dependency is not Done.
- Research tickets (`RES-*`) inherit the priority of the ticket they feed into and must complete first.
- `SPEC-*` tickets gate their corresponding implementation tickets. No code until the spec is locked.

### One Ticket = One Branch

| Rule | Detail |
|---|---|
| Branch name | Matches ticket ID: `git checkout -b P1-04d` |
| No multi-ticket branches | Each ticket gets its own branch, even if they touch the same file. |
| Squash on merge | All checkpoint commits squash into one clean commit before pushing. |
| Delete after merge | Feature branch is deleted once merged to `main`. |

---

## 3. Priority Tiers

| Tier | Name | Meaning | When to work it |
|---|---|---|---|
| **P0** | Critical | Blocks all other work. Drop everything. | Immediately. Rare -- build-breaking bugs, data loss, total blockers only. |
| **P1** | Urgent (MVP) | Must ship for MVP. | First, after any P0. This is the default active tier. |
| **P2** | Important | Needed for a specific post-MVP milestone. | After all P1 tickets are Done. |
| **P3** | Upcoming | Planned feature with known scope. Scheduled but not urgent. | After current milestone completes. |
| **P4** | Someday | Known work without a timeline. Will get done eventually. | When capacity allows and no higher-priority work exists. |
| **Backlog** | Unprioritized | Captured but not triaged. Needs evaluation before work begins. | Only after triage promotes it to a numbered tier. |
| **Tech Debt** | Maintenance | Refactors, cleanup, infra. No user-facing change. | As capacity allows. Never urgent, always tracked. |

**Tier rules:**

- P0 is an emergency. If nothing is on fire, there are no P0 tickets.
- P1 defines MVP scope. Every ticket required for a playable prototype is P1.
- Research tickets (`RES-*`) inherit the priority of the ticket they unblock.
- Tech Debt is never prioritized above user-facing work unless it blocks a P1.
- Backlog items do not get worked until promoted. Promotion requires: clear scope, a ticket ID, and an assigned priority tier.

---

## 4. Dev Cycle

Every ticket follows this sequence. Do not skip steps.

```
branch --> research --> plan --> cross-check --> test --> code --> verify --> commit --> push
```

| Step | What happens |
|---|---|
| **1. Branch** | `git checkout -b TICKET-ID` from `main`. One branch per ticket. |
| **2. Research** | Read the relevant spec doc(s). Read existing code if modifying. Check for a `RES-*` ticket. |
| **3. Plan** | State what will change, list files to touch, confirm plan matches spec. |
| **4. Cross-check** | Research agent reviews the plan against best practices and known pitfalls. Update plan. |
| **5. Test** | Write or reference test cases before implementation. Tests define "done." |
| **6. Code** | Write the minimum code that passes tests and satisfies the spec. Nothing more. |
| **7. Verify** | Run tests. Check that the diff contains only task-related changes. Capture evidence if visual. |
| **8. Commit** | Squash checkpoints into one clean commit. Message describes what changed and why. |
| **9. Push** | Push to remote. Delete branch after merge. Stop. Do not continue to the next ticket. |

Full protocol with substeps: [ai-workflow.md](ai-workflow.md).

---

## 5. Scope Discipline

Scope violations are the #1 AI failure mode. These rules exist to prevent them.

| Rule | Explanation |
|---|---|
| Do exactly what is asked | Not more, not less. The ticket defines the work. |
| No extras | No "while I'm here" improvements, no adjacent refactors, no future-proofing. |
| No assumptions | If the ticket or spec is ambiguous, ask. Do not guess intent. |
| No unspecified features | If it is not in a locked spec doc, it does not get built. |
| No placeholder code | No TODO comments, no stub implementations, no "we'll fill this in later." |
| Diff audit | Before committing, review the diff. Every changed line must trace back to the ticket. |
| Stop when done | After pushing, stop. Do not pick up the next ticket without direction. |

### Three-Tier Boundary System

| Tier | Action | Examples |
|---|---|---|
| **Always do** | No approval needed | Read specs first, use static typing, follow naming conventions, run tests, keep scripts under 300 lines |
| **Ask first** | Need product owner approval | Adding new files/scenes not in spec, changing autoloads, modifying project.godot, adding dependencies, refactoring beyond current task, changing spec docs |
| **Never do** | Hard stop, no exceptions | Add unspecified features, make assumptions about intent, skip tests, change code outside task scope, suppress warnings, delete archive files, add paid assets |

---

## 6. Docs First

| Principle | Detail |
|---|---|
| Specs before code | Every system is fully specified in `docs/` before implementation begins. |
| Docs are source of truth | If code and docs disagree, one needs updating. Neither is silently "right." |
| Tests define done | A ticket is not done until its test cases pass. Test cases are written before code. |
| Locked specs gate code | A `SPEC-*` ticket must be Done before its corresponding `P*-*` ticket can start. |
| Design decisions are documented | Not in chat, not in comments, not in memory. In `docs/`. |
| Spec changes need approval | Modifying a locked spec requires product owner sign-off. |

---

## 7. Quality Gates

Before any code is considered "done," it passes all five gates:

| Gate | Check |
|---|---|
| **Spec compliance** | Does the code do exactly what the spec describes? Nothing more, nothing less? |
| **Tests pass** | Are there test cases, and do they all pass? (`dotnet test`, manual verification, or both) |
| **Scope check** | Does the diff contain ONLY changes related to the current ticket? |
| **Style check** | Does the code follow C# conventions and project naming rules? (`dotnet format --verify-no-changes`) |
| **Boundary check** | Are dependencies explicit and one-directional? No hidden coupling between systems? |

A ticket that fails any gate is not Done. Fix the failure, do not override the gate.

---

## 8. Evidence

Visual changes require proof. Evidence is committed with the ticket branch.

### What to capture

| Type | Format | When |
|---|---|---|
| Screenshot | PNG | Static UI, HUD layout, menu screens, tile rendering |
| Screen recording | MP4 or GIF | Movement, combat, animations, transitions, game feel |

### Where to store

```
docs/evidence/{TICKET-ID}/
  screenshot-name.png
  recording-name.mp4
  notes.md              <-- one-liner per file describing what it validates
```

### Rules

- Folder name matches ticket ID exactly.
- `notes.md` is required -- describes what each file validates.
- Keep recordings short (5--15 seconds, trimmed to the relevant action).
- Disable debug overlays before capture (set `debug_visible = false`).
- For changes to existing features: capture after. For new features: capture the first working version as baseline.
- Evidence is included in the squashed commit.

---

## 9. Communication

The product owner is a non-developer. All communication follows these rules:

| Rule | Detail |
|---|---|
| User is the client | They direct game vision, make design decisions, approve outcomes. They do not write, review, or debug code. |
| Player experience framing | Present options as "the player will see X" or "this makes combat feel Y." Not as architecture tradeoffs. |
| Technical decisions are autonomous | AI makes all implementation choices. User approves what the game does, not how it is built. |
| Ask about game feel | "How should this feel to the player?" Not "Which data structure should we use?" |
| Do, don't explain | When the user says "do X," do it. Do not explain the plan unless asked. |
| Escalate ambiguity | If the spec is unclear or the request is ambiguous, ask before acting. Do not guess. |
| No jargon unless asked | Avoid code-level terminology in status updates. "Enemies now drop loot" not "Added LootTable.Roll() to EnemyDefeated signal handler." |
