# Analytics, Bug Reporting & Player Feedback

## Summary

Technical architecture for opt-in player analytics, in-game bug reporting, crash logging, and feedback collection. The game is and will always be **offline-first** — analytics are 100% opt-in, never required to play.

For the game design side of player engagement, see [player-engagement.md](../systems/player-engagement.md).

## Current State

Design phase. No analytics, bug reporting, or feedback systems are implemented.

## Core Principle

**The game works 100% offline, forever.** No network calls at startup. No "connecting to server" screens. No features gated behind connectivity. Analytics are a separate, isolated system — if you rip the telemetry module out entirely, the game works identically.

## Design

### Consent Flow

**First-launch prompt:**
- Appears before or during the first play session
- Two equally prominent buttons: "Yes, send anonymous data" / "No thanks"
- No dark patterns (the "No" option is equally sized and visible)
- Example text: *"Help us improve A Dungeon in the Middle of Nowhere by sending anonymous gameplay data. You can change this anytime in Settings."*
- Default state: **OFF** (opt-in, GDPR compliant)

**Settings toggle:**
- Located in Settings > Privacy
- Label: "Send anonymous usage data" with brief description
- Checkbox or toggle, off by default
- Adjacent buttons:
  - **"What data do we collect?"** — shows plain-English list of collected event types
  - **"View pending data"** — shows the actual JSON queued for upload (full transparency)
  - **"Delete my data"** — deletes all local telemetry files immediately

**Re-consent on scope changes:**
- If a game update expands what data is collected, the consent prompt reappears
- The player is never silently enrolled in additional data collection

**No gameplay incentives for opting in.** No bonus items, no XP boosts, no cosmetics for enabling analytics. Consent must be freely given.

### Data Collected (When Opted In)

| Category | Data Points | Why |
|----------|------------|-----|
| Session | Start/end time, duration, platform, OS, game version | Session length patterns, platform distribution |
| Progression | Floors reached, levels gained, bosses defeated | Pacing and difficulty calibration |
| Deaths | Floor, enemy type, player level, cause | Difficulty spike identification |
| Builds | Active skills, equipment, stat allocation | Balance and build diversity analysis |
| Performance | Average FPS, load times, resolution | Optimization priorities |

**NOT collected (ever):**
- No personally identifiable information (PII)
- No IP addresses (or immediately discarded)
- No keyboard/mouse input streams
- No screenshots or screen recordings
- No file system information
- No Steam ID, email, or account linkage

**Identity:** Each install generates a random UUID (not tied to any account or platform ID). This UUID cannot be linked back to a specific person.

### Offline-First Telemetry Architecture

```
┌─────────────┐     ┌──────────────┐     ┌────────────────┐     ┌─────────────┐
│ Game Events  │────▶│ Event Buffer │────▶│ Local File     │────▶│ Upload Queue│
│ (in-memory)  │     │ (Array)      │     │ (JSONL on disk)│     │ (background)│
└─────────────┘     └──────────────┘     └────────────────┘     └─────────────┘
                                                                       │
                                                                       ▼
                                                                ┌─────────────┐
                                                                │ Backend API │
                                                                └─────────────┘
```

**Event lifecycle:**

1. Something trackable happens in-game (death, level-up, floor clear, item equipped)
2. **Consent check:** If `opted_in == false`, stop here. No event generated, no data stored.
3. Create event dictionary: `{ "event": "player_death", "timestamp": ..., "floor": ..., "enemy": ..., "session_id": "uuid" }`
4. Append to in-memory buffer (Array)
5. **Periodic flush:** Every 5 minutes OR on session end, write buffer to JSONL file in `user://telemetry/pending/`
6. **Upload attempt:** On next session start, if opted in and online:
   - POST each pending file to backend API (background thread, non-blocking)
   - On HTTP 200: delete the file
   - On failure: leave file for next attempt (no aggressive retry)
7. **Auto-cleanup:** Local files in `pending/` older than 30 days are automatically deleted

**File format:** JSONL (JSON Lines) — one JSON object per line. Human-readable if the player inspects it.

**Threading:** Upload runs in a background thread. Never blocks gameplay. Never retries during a session if the first attempt fails.

### Bug Reporting

**In-game bug report UI:**
- **"Report Bug" button** in the pause menu — visible and easy to find
- On open, auto-captures:
  - Screenshot of the current game state
  - Game version and platform
  - Recent event log (last 50 game events)
  - Current floor, player level, active build
- Player provides:
  - **Category dropdown:** Visual, Gameplay, Crash, Audio, Performance, Other
  - **Description text field:** short free-text (500 char limit)
- On submit:
  - Saved locally as JSON + screenshot PNG in `user://bug_reports/`
  - If online: uploaded to backend immediately
  - If offline: queued for upload on next online session
  - Confirmation: "Thank you! Your report has been saved."
- **"Copy to clipboard" fallback:** For fully offline players, a button copies the report text to clipboard for manual submission via Discord/forums

**Bug report data is separate from analytics consent.** Players can report bugs without opting into analytics.

### Crash Reporting

**On crash:**
- Godot writes crash logs to `user://logs/`
- On next launch, the game checks for crash logs from the previous session

**Recovery flow:**
- If crash log found: "It looks like the game crashed last time. Would you like to send the crash report to help us fix the issue?"
- Two buttons: "Send report" / "No thanks"
- This consent is per-crash, not a blanket opt-in
- Crash report includes: log file, game version, platform, last known game state
- Does NOT include: save files, personal data, analytics data

**Future:** If the project adopts GDExtension (C++ native code), consider Sentry for native crash dump processing with stack traces.

### Player Feedback Collection

**In-game micro-surveys:**
- Triggered at natural break points: after a boss kill, after first floor clear, on session end
- Maximum **1–2 questions** per survey
- Always **skippable** with a single button press
- Frequency cap: **max once per 10 sessions** — never feel intrusive
- Example: "How would you rate this dungeon floor? [1-5 stars]" + optional text field
- Stored locally, uploaded with telemetry if opted in

**"Send Feedback" in pause menu:**
- Similar to bug report but with different categories: Suggestion, Praise, Question, Other
- Description text field
- Optional contact field (email — only if the player wants a response)
- Stored and uploaded same as bug reports

**Community channels (primary feedback source):**
- Discord server with dedicated channels: `#bug-reports`, `#suggestions`, `#feedback`
- Steam Community discussions (when published)
- itch.io comments (during early development)
- These channels provide the richest, most actionable feedback for a small team

### Privacy & Legal

**Anonymous by design:**
- Random install UUID, not tied to any account or platform
- No IP address retention (if logged by the backend, immediately discarded or hashed)
- Data cannot be linked back to a specific person

**GDPR compliance:**
- Opt-in default (OFF until explicitly enabled)
- Right to deletion ("Delete my data" button deletes all local files)
- Clear disclosure of what is collected and why
- Plain-language privacy policy accessible from the consent screen and Settings menu
- If minor players are possible: additional COPPA considerations (deferred)

**Privacy policy:**
- Hosted on the game's website/itch.io page
- Accessible in-game from the consent prompt and Settings > Privacy
- Written in plain language, not legalese
- Updated whenever collection scope changes

### Backend (Future Implementation)

**Phase 1 — Steamworks (lightest weight):**
- Use Steam Stats and Achievements API via GodotSteam
- Track progression milestones as achievements (% of players who beat each boss, etc.)
- No additional consent needed (covered by Steam's ToS)
- No custom backend required

**Phase 2 — Custom telemetry (when needed):**
- Lightweight REST API accepting JSON event batches
- Options (AI-agnostic, per project guidelines):
  - Cloudflare Workers + D1 (SQLite) — free tier
  - Supabase — free tier, PostgreSQL
  - Self-hosted service on a VPS with SQLite/PostgreSQL
- Dashboard for analysis (Grafana, Metabase, or custom)
- All tooling choices follow the project's AI-agnostic and free-tools-only principles

### Studying Player Behavior

**Playtesting (primary method during development):**
- Silent observation: watch someone play without helping, note confusion/frustration/disengagement
- Think-aloud protocol: player narrates their thoughts while playing
- Post-session survey: "What was most fun? Most frustrating? Would you play again?"
- Session recording (OBS) for later analysis

**Key metrics to track (when analytics are active):**

| Metric | What It Reveals | Healthy Range |
|--------|----------------|---------------|
| Session length | Is the game engaging enough per sitting? | 20–60 minutes |
| Session frequency | How often do players return? | 3–5x/week |
| Progression speed | Too fast (bored soon) or too slow (frustrated)? | ~1 level per 15–30 min early |
| Drop-off floors | Where do players quit permanently? | Look for spikes |
| Death frequency per floor | Is difficulty balanced? | Gradual increase with depth |
| Build diversity | Are players experimenting? | Multiple viable builds |
| Floor push depth | How deep do players go before returning to town? | Varies by build/skill |

**Heatmaps (from death/event data):**
- Death location clusters = difficulty spikes or unfair encounters
- Loot pickup vs ignore = reward relevance
- Floor visit frequency = content engagement

## Open Questions

- Exact backend choice for custom telemetry (deferred to implementation phase)
- Should crash reports include a mini save-state snapshot for reproduction?
- How to handle analytics for players who switch between offline and online frequently?
- Should the "View pending data" screen show raw JSON or a formatted summary?
- Integration with Steam's built-in bug reporting and review systems
- How to handle data from very old game versions (schema evolution)
