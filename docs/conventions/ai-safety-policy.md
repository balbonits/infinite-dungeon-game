# Generative-AI Safety Policy (SPEC-AI-SAFETY-01)

Status: **Locked (policy v1)** — 2026-04-20. Revisions require PO sign-off + a dated entry in §11.

This policy governs all generative-AI use in the project — both **dev-time** (sprite/tile/icon generation via PixelLab, code/docs/specs generation via Claude, tilesets via LPC) and any **runtime** AI that ships with the game (future — not present in MVP). It exists because applicable laws now mandate active prevention of prohibited output, an in-app user reporting channel, and demonstrable use of those reports to improve filtering. This document is how we satisfy those obligations.

> **Scope at MVP:** the game currently ships ZERO runtime generative AI — all AI-generated assets are baked at dev time and shipped as static files. Every runtime surface the player sees is deterministic. This policy still applies: (a) dev-time pipelines can still produce prohibited output that would ship with the game, (b) future runtime-AI features (procedural dialogue, emergent narrative, etc.) must design against this policy from day one, and (c) the in-app reporting channel is a MVP blocker once *any* AI-generated surface is player-facing.

---

## §1 Prohibited-Content Taxonomy

The following categories are **never acceptable output** from any AI pipeline in the project, full stop. Any such output must be caught at generation time, rejected before it reaches a shipping build, and — if discovered post-ship — pulled immediately.

| # | Category | Example shapes |
|---|----------|----------------|
| 1 | **Child sexual abuse material (CSAM)** | Any depiction of minors in sexualized contexts. Absolute prohibition — no fictional / stylized / artistic exception. |
| 2 | **Non-consensual deepfakes** | Photorealistic or suggestive imagery of identifiable real persons without their explicit consent. |
| 3 | **Scams / fraud content** | Phishing prompts, fake store-page copy, counterfeit endorsements. |
| 4 | **Hate speech** | Content targeting protected classes (race, religion, gender, sexual orientation, disability, national origin) with dehumanization, slurs, incitement. |
| 5 | **Deceptive election content** | Fabricated quotes / imagery / claims about real political figures, polling places, voting procedures, or election outcomes. |
| 6 | **Bullying / harassment material** | Targeted attacks on real individuals or identifiable groups designed to intimidate. |
| 7 | **Sexually explicit content meant to gratify** | Pornographic output regardless of subject. Narrative/artistic nudity is allowed in cases where it serves a game-design purpose and carries no gratification intent; borderline cases escalate to PO. |

**Adjacent gray zones** (not automatic rejection, but require PO review before ship): graphic violence beyond ARPG baseline; religious symbolism in a potentially offensive framing; political satire; real-location depictions in tragic/violent contexts.

---

## §2 Dev-Time Pipeline Guardrails

Every dev-time AI pipeline in the project must satisfy all three of the following. Each pipeline's spec doc (e.g., `docs/assets/prompt-templates.md`, `docs/assets/npc-pipeline.md`) carries a §Policy Reference pointing back here.

### §2.1 Prompt-time classification

- **Prompt review before generation.** Prompts must be reviewed (by the human operator OR an AI lead agent acting under this policy) for intent-to-produce §1 categories *before* a generation call is made. Prompts that plainly target §1 are rejected at author time.
- **Named-real-person guard.** Prompts referencing identifiable real persons (by name, likeness description, or distinguishing identifier) require PO sign-off. Default-deny unless the person is a public figure in a context that is plainly non-deceptive (e.g., licensed-historical reference with documented consent).
- **IP-clean rule already in §11 of `docs/assets/prompt-templates.md`** covers the "no named IP / fresh-authored identity" half of this — this policy strengthens it with the real-person consent gate.

### §2.2 Output-time classification

- **Content classifier pass.** Before committing any generated asset to the repo, the operator runs a content-classification check. For text output, this is a review pass; for image output, this is either a visual review OR a classifier-tool pass when the batch is too large for per-asset eyeballing (bulk tile generation, etc).
- **Sample-then-batch.** For large batch generation (e.g., a tile atlas, a dialogue corpus), generate **one sample first**, review for §1-category concerns, then authorize the batch. This mirrors the existing [feedback_one_image_first.md](../../.claude/projects/-Users-johndilig-Projects-infinite-dungeon-game/memory/feedback_one_image_first.md) discipline — repurposed here as a safety gate in addition to a theme gate.
- **Zero-tolerance stop.** If a classifier or review finds a §1-category output at any stage, the batch is discarded, the prompt is reviewed for shape-that-produced-it, and the seed values are logged in the PR description so the configuration doesn't silently recur.

### §2.3 Shipping-time audit trail

- **Provenance record.** Every AI-generated shipped asset records: source pipeline (PixelLab / LPC / Claude / etc), prompt block name (CHAR-HUM-ISO / TILE-ISO-ATLAS / ability-icon / etc), generation-time commit SHA of the project. This lets us re-generate on demand and audit retroactively if a §1-category issue surfaces.
- **CREDITS.md reference.** Shipped AI-generated assets are listed in the project's credits alongside attribution for non-AI assets. Transparency is itself a safety feature — players who want to know what's AI-generated can find out in one place.

---

## §3 Runtime Guardrails (Future — no MVP code yet)

When the game adds its first runtime-generating AI feature (procedural dialogue, emergent name generator, NPC-response system, any ML model evaluated during a play session), the following apply before that feature ships:

### §3.1 Per-surface classification

- **Blocklist / classifier on the inference path.** Every AI-generated string or image emitted during play passes through a content classifier *before* rendering. The classifier targets §1 categories specifically and additionally blocks slurs, explicit language, and real-person references unless surfaced via a curated whitelist.
- **Fallback template.** If the classifier rejects an output, the surface renders a safe fallback (a neutral template string, a placeholder sprite, or simply nothing) rather than the raw AI output.
- **Logging.** Rejected generations are logged locally (attached to `docs/evidence/ai-rejections.ndjson` or equivalent) with a timestamp + surface + classifier-reason. No user PII, no full raw output — just the metadata needed to audit whether the classifier is over- or under-filtering.

### §3.2 Deterministic-by-default

- **Seed exposure.** Runtime AI surfaces expose their seeds in a per-save-slot log so any problematic output the player encounters can be exactly reproduced for debug purposes.
- **Kill switch.** Every runtime-AI feature ships behind a configuration toggle in `GameSettings` so the studio can remote-disable it if a §1-category escape surfaces at scale (e.g., via a content-update save file).

---

## §4 In-App Reporting / Flagging

### §4.1 When required

- **Trigger: any player-visible surface containing AI-generated content.** Once a single AI-generated surface is in front of the player at runtime (text, image, audio, whatever), the reporting channel must be available. This is the MVP blocker mentioned in the scope preamble.
- **Scope of "surface":** includes surfaces rendered at dev-time and shipped as static files (like our current sprites), because the player sees them at play time and the law treats shipped content no differently than runtime-generated content for reporting purposes.

### §4.2 Placement + UX contract

- **Global shortcut.** Bind a dedicated key (default: `F10` — chosen because F1-F4 are taken by debug tooling and F11/F12 are window managers) to open the Report dialog from any game state.
- **Context menu on surfaces.** For surfaces where the player has an interaction prompt already (NPC dialog, shop item tooltip, pause-menu panel), add a "Report this content" option that pre-populates the report with the surface identifier.
- **Reporting form fields.** Category picker drawn from §1 taxonomy (labeled in plain language, not internal shorthand), plus an optional free-text field ("What seemed wrong about this?"), plus auto-captured context (current save slot ID pseudonymized, current scene, surface identifier, build SHA). No PII; no "email for follow-up" unless the player explicitly opts in and the project has actually-deployed email handling (not present in MVP).
- **Post-submit confirmation.** "Thanks — report received. We review these every week." Hard commitment: we actually do review them, see §5.
- **No punishment for false positives.** Players are explicitly told frivolous/mistaken reports are fine — safety filters only improve when the signal is generous.

### §4.3 Local vs. network submission

- **MVP: local-only.** Reports write to `user://ai_reports/` (JSON-lines, one report per line, pseudonymized). A support script pulls them from playtester machines manually. Rationale: MVP has no cloud backend and we are not collecting PII.
- **Post-launch: cloud submission.** When the game has a network-enabled distribution channel (`SPEC-EXPORT-PLATFORMS-01`), reports can opt in to cloud submission per the deferred `SPEC-ANALYTICS-BACKEND-01`. Default remains "local save + manual export" — submission is opt-in, always.

---

## §5 Using Reports to Improve Filters (the Third Obligation)

The law isn't satisfied by "we have a report button." It requires demonstrable use of those reports to improve the filter over time. Our compliance path:

### §5.1 Weekly triage

- **Every week, the PO reviews the week's report batch** (local-only in MVP, cloud-pulled post-launch). The review produces one of four outcomes per report, recorded in `docs/evidence/ai-report-triage.md`:
  1. **False positive** — the reported output is OK; note why and move on.
  2. **True positive, filter miss** — the output is §1-category; the prompt/classifier/pipeline is updated *within this same week* to catch it.
  3. **True positive, borderline** — gray-zone; escalate to explicit PO decision, document in the same file.
  4. **Ambiguous** — not enough context; reach out to reporter if opt-in, else close with note.

### §5.2 Filter update loop

- Every "filter miss" outcome produces either a **new prompt-template rule** (§2.1), a **new classifier rule** (§2.2 or §3.1), or a **new test case** in the prompt-template spec that asserts the rule holds going forward. The update commit references the report ID.
- The `ai-report-triage.md` doc grows over time as a living record of what we've been shown and what we did about it. Filter-improvement commits reference entries in that doc by anchor.

### §5.3 Transparency

- Aggregate report counts (categories, total volume per month, filter updates shipped) are published in release notes for each shipped update. This is explicitly the "demonstrable use" the law requires — external visible evidence that reports are worked, not ignored.

---

## §6 Compliance with Specific Laws

This section lists the laws known at time of writing (2026-04-20). Add dated rows as new laws come into scope.

| Date | Law / framework | Key obligations satisfied by |
|------|-----------------|-------------------------------|
| 2026-04-20 | General commercial-game AI-safety practice (aggregate of 2025-26 California + EU AI Act-adjacent requirements) | §1 prohibited-content list; §2 dev-time guardrails; §4 in-app reporting; §5 triage-and-update loop |

When a specific named law is cited by the PO, add a row with the law name, effective date, and which §s of this policy cover it. If an obligation emerges that isn't covered, amend the relevant § and add the law-row in the same PR.

---

## §7 Audit Artifacts (What a Regulator Would Ask For)

Should a regulator, publisher, or platform partner ever ask "show us how you comply," we hand them:

1. **This policy doc** (`docs/conventions/ai-safety-policy.md`).
2. **Pipeline spec docs** with §Policy Reference pointers (prompt-templates.md, npc-pipeline.md, audio-pipeline.md, etc).
3. **CREDITS.md** with AI-generated asset tags + provenance.
4. **`docs/evidence/ai-report-triage.md`** showing the weekly triage cadence.
5. **Filter-update commit log** — `git log --grep="ai-safety:"` produces a filtered list of filter-improvement commits.
6. **Release notes** with the aggregate report-stats section (once we have runtime AI + shipped builds).

Everything in-repo, version-controlled, auditable without access to internal tooling.

---

## §8 Roles

- **PO (product owner):** approves policy revisions, owns the §5 weekly triage, makes the call on §1 gray-zone cases.
- **AI lead agent (Claude / equivalent):** enforces §2 guardrails on every generation; flags borderline prompts to PO rather than proceeding.
- **Art-lead / design-lead / devops-lead agents:** each own §Policy Reference blocks in their respective pipeline specs and must update them when policy changes.
- **Any human contributor / any AI agent:** responsible for authoring §Policy Reference blocks in new pipeline specs they introduce.

---

## §9 What This Policy Is NOT

- Not a waiver — no "but it's art" / "but it's parody" / "but it's a game" loopholes. §1 is absolute.
- Not a substitute for platform-specific content policies — App Store, Steam, Xbox, PlayStation each have their own policies; this document is additive.
- Not a legal opinion — if a specific law interpretation question arises, escalate to qualified counsel; this policy is operational not legal advice.
- Not frozen — revise in an own-PR under §11 when scope genuinely changes.

---

## §10 Impl Tickets (follow-up)

| ID | Description | Status |
|----|-------------|--------|
| POL-AI-REPORT-UI-01 | Report dialog (F10 keybind + context-menu integration + form) | To Do (MVP-blocker once first runtime-AI feature lands) |
| POL-AI-CREDITS-SWEEP-01 | Audit shipped assets + tag provenance in CREDITS.md | To Do (P2) |
| POL-AI-TRIAGE-INIT-01 | Create `docs/evidence/ai-report-triage.md` stub + set up weekly PO reminder | To Do (P3 — no reports yet) |
| POL-AI-PIPELINE-REF-01 | Add §Policy Reference back-pointers to every existing pipeline spec | To Do (P2) |

---

## §11 Revision Log

- **2026-04-20** — v1 locked. Initial authoring in response to generative-AI regulatory requirements. PO directive reference: user message 2026-04-20. Spec owner: design-lead agent (authored) / PO (approved).
