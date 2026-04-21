# UI Component Model — Dumb UI, Smart BE

**Status:** Canonical. Every UI change, every new screen, every "where should this logic live?" decision routes through this document.

**TL;DR:** UI components are **dumb**. They take data in, render, and emit events out. **Screens** / **smart containers** do the thinking — they fetch data, shape it, pass it into dumb components, and handle the events that come back. This is the same discipline as React's *presentational vs container components* and the MV (Model/View) separation, applied to our Godot C# codebase.

---

## Why this exists

Before this paradigm was locked, the codebase showed the classic anti-pattern: screens built as one-off UI trees, each customizing its own stylebox, focus handler, keyboard routing, and data parsing. Two screens that should have looked identical (Load Game's character cards and New Game's class cards) looked and behaved differently, because each screen re-implemented the "card" idea from scratch. A bug in one card didn't fix the other. A border-color change had to be made in five places. Testing required standing up the save system to verify a render.

The paradigm fixes this: components have ONE job (render), screens have ONE job (orchestrate), and the seam between them is well-defined props-in / events-out.

---

## The two sides

### Dumb UI (presentational components)

A dumb UI component:

- **Takes data via its constructor / factory method.** That's its "props."
- **Renders.** Nothing else.
- **Emits events when the user interacts** (Godot signals, or `Action` callbacks supplied at construction).
- **Knows nothing about where data came from.** Not `SaveManager`, not `GameState`, not `SceneTree`.
- **Knows nothing about what happens next.** Not scene transitions, not save-slot selection, not quest progression.
- **Is pure in its render**: same data in → same pixels out. No hidden reads from globals, no surprise re-renders from unrelated state.

Examples: `Card`, `CharacterCard`, `EmptyCard`, `ClassCard`, a future `StatGrid`, a future `PortraitThumbnail`, a future `HotbarSlot`.

### Smart BE (screens, controllers, orchestrators)

A smart container:

- **Fetches data** from game systems (`SaveManager`, `GameState`, `SkillDatabase`, etc.).
- **Shapes it** into the neutral data shape each dumb component expects.
- **Mounts dumb components**, wiring data in and event handlers out.
- **Handles the events** — translates "card selected" into "load save 2" or "begin New Game as Warrior."
- **Owns the screen's logic** — back-nav, state transitions, focus discipline at the screen level.
- **Does NOT** duplicate visual concerns. Styling, framing, focus-ring: those live in the dumb components.

Examples: `LoadGameScreen`, `ClassSelect`, `SplashScreen`, `PauseMenu`, `DeathScreen`.

---

## Core rules

### 1. Props down, events up

Data flows in through constructor/factory args. Events flow out through signals or injected callbacks.

```csharp
// Smart container mounts a dumb component:
var card = CharacterCard.Create(
    summary: adapter.ToSummary(save),
    onSelected: () => LoadSave(save.Slot));

// Dumb component accepts the data, emits events — never reaches for SaveManager itself.
```

A dumb component never reaches upward (`GetParent().LoadSave(...)`), never reaches sideways (`SaveManager.Instance.Load(...)`), never reaches into globals from its render path. If it needs something, the caller hands it in.

### 2. Neutral data shapes (DTOs) at the seam

Dumb components take **neutral data shapes**, not source-specific types.

- **Wrong:** `CharacterCard.Create(SaveData save, ...)` — couples the card to the save format. A Hall of Fame entry or a post-death recap won't have a `SaveData` to pass.
- **Right:** `CharacterCard.Create(CharacterSummary summary, ...)` where `CharacterSummary` is a record struct with only the fields the card needs. Callers adapt whatever their source is into `CharacterSummary` before handing it to the card.

This one rule is the difference between a component that works in one place and one that's genuinely reusable.

### 3. Single responsibility

- `Card` draws a card frame. That's it.
- `CharacterCard` renders a character summary inside a Card. That's it.
- `LoadGameScreen` orchestrates the load-game flow. That's it. It doesn't draw the card frame; `Card` does that.

If a class knows how to do more than one thing, split it.

### 4. Composition

Small, single-purpose components combine to make bigger ones. `CharacterCard` composes `Card` + a character-summary layout. The layout itself can be further composed of `PortraitThumbnail` + `StatGrid` + small labels if reuse emerges.

This is the Godot-idiomatic version of React composition: subclassing small bases + nesting primitives.

### 5. Pure render

A dumb component's render must be a function of its input data. No reads from `GameState.Instance`, no reads from `ProjectSettings`, no reading mid-render from autoloads. If the component needs something, the caller passes it in.

If rendering depends on a global setting (e.g., "show keyboard hints"), read that global at the screen level and pass the resolved value in as a prop.

### 6. Reusability is the proof

A component is built correctly when it can be dropped into a new context with only a data-adapter change. If CharacterCard is right, a future Hall of Fame screen imports it, builds a `CharacterSummary` from its own data, and gets pixel-identical rendering + identical focus behavior + identical keyboard activation for free.

The question to ask during design: *"If I needed this in three other places, would the current shape work?"*

---

## Systems & unifying

### Systems mentality

The game isn't "screens built from scratch." It's **systems composed in different configurations**. Existing systems in this codebase include:

- **Theme system** — one set of colors, fonts, sizes, styleboxes. Never per-screen.
- **Save system** — one way to persist state. Never per-feature.
- **Input system** — one set of `Constants.InputActions` names. Never magic strings.
- **Window/modal stack** — one way to manage overlay lifecycle and focus traps.
- **Card system** (this document) — one way to render selectable cards.
- **Spec-driven dev** — the meta-system: specs in `docs/` are the source of truth; code derives from specs.

**Before writing a new UI thing, ask: "Is there a system for this yet?"**

- If yes: use it. If it doesn't quite fit, extend it — don't duplicate it.
- If no: does there need to be one? Does this class of problem recur, or is this the only place?

Writing the same thing twice in two screens is the smell. The second time is an error, not a shortcut.

### Unifying principle

**Same behavior gets the same mechanism.** If two things should look or act alike, they share one piece of code. Divergence is the enemy — each per-screen custom implementation is cheap once and expensive every time the contract changes.

**Single source of truth.** When the card focus ring changes color, that change happens in ONE place. Every card updates automatically. The opposite pattern — each screen defining its own focus stylebox — means the color change takes N-screens' worth of edits and two of them will get missed.

### Pre-coding reflex

Before starting any UI work, run these checks:

1. **Is there a system for this?** (Card system, theme system, modal stack, etc.) Use it.
2. **Am I about to write something this codebase already has?** Stop. Find the existing thing.
3. **Am I about to write something that will need to be duplicated later?** Stop. Make it a system now, not later.
4. **Is the thing I'm writing doing more than one job?** Split it.
5. **Does my component know about things it shouldn't?** (`SaveManager` from a card, `GameState` from a grid slot.) Move those concerns up to the screen level.

If any of these check-results surprise you, pause and think before typing.

---

## Worked example: the Card hierarchy

The first system authored under this paradigm. Four types:

```
Card (abstract-ish base — PanelContainer)
  ├── CharacterCard    (takes CharacterSummary, fires Selected)
  ├── EmptyCard        (takes slot label, fires Selected)
  └── ClassCard        (takes ClassData,       fires Selected)
```

### `Card` — the base

Responsibilities:

- Draws the card outline (stylebox variants for Normal / Highlighted / Selected).
- Enforces the size / dimensions (so every card across the game has identical footprint).
- Owns focus-entered / focus-exited / mouse-entered / mouse-exited visuals.
- Owns keyboard + mouse activation routing (Enter / Space / S / left-click → "selected").
- Exposes a content area (a `VBoxContainer`) that subclasses populate.
- Emits a `Selected` signal (or invokes an injected `onSelected` callback).

Knows nothing else. No save system, no game state, no navigation.

### `CharacterCard` — shows a character

Takes a `CharacterSummary` (neutral DTO):

```csharp
public readonly record struct CharacterSummary(
    PlayerClass Class,
    int Level,
    int Str, int Dex, int Sta, int Int,
    int Hp, int MaxHp,
    int Mana, int MaxMana,
    int Gold,
    int Floor, int DeepestFloor,
    float XpPct,
    string TimestampLabel);
```

Populates the content area with portrait + level + stats grid + HP/MP + floor/gold + XP% + timestamp. Fires `Selected` when activated.

### `EmptyCard` — shows an empty slot

Takes a slot index / label. Populates the content area with an "Empty Slot N" placeholder. Fires `Selected` when activated (or stays non-interactive if the surface doesn't allow creating-on-empty).

### `ClassCard` — shows a class preview

Takes a `ClassData` (the class template record). Populates the content area with class name + portrait + description + base stats + starting-skill row. Fires `Selected` when activated.

### How screens use these

`LoadGameScreen` (smart):

- Queries `SaveManager` for each slot.
- For each slot: adapts `SaveData` → `CharacterSummary` (or uses `EmptyCard` if the slot is empty).
- Mounts the card, wires `Selected` to "load this save."
- Handles back-nav and screen-level focus discipline.
- Never touches a stylebox or a focus stylebox.

`ClassSelect` (smart):

- Owns the static list of 3 class templates.
- For each class: mounts a `ClassCard` with that class's data.
- Wires `Selected` to "pick this class for New Game."
- Handles the confirm / back-to-menu flow.
- Never touches a stylebox or a focus stylebox.

Both screens converge on the same `Card` base, so they look and behave identically — not because each one was hand-tuned to match, but because they share the mechanism.

---

## Anti-patterns (what we've moved away from)

### "Each screen customizes everything"

Building a one-off `PanelContainer` inline in every screen, with its own stylebox, its own hover handler, its own focus handler, its own keyboard input. The cost compounds: five screens' worth of divergent implementations and no shared tests.

### "Dumb components reading from globals"

A card that calls `SaveManager.Instance` from its render path. Now the card can't be used anywhere `SaveManager` isn't initialized (tests, previews, sandboxes) and its output isn't a pure function of its props.

### "Screens that know about styleboxes"

A screen that imports `StyleBoxFlat` and mutates the panel stylebox inline. That means the visual contract is scattered across screens instead of living in the component layer. The next border-color change takes N edits.

### "Data-source coupling at the seam"

A component that takes `SaveData` (or any other source-specific type) as its input. Now the component is welded to that source. A second use case needs a different source, and the component has to be rewritten or forked.

### "Customize first, unify later"

The intention is always good ("I'll refactor it later when I need reuse"). The reality is the customizations accrete, the seams drift, and unifying becomes a months-long project instead of a ten-minute design call. Unify at the point of the second use, not after the fifth.

---

## Data flow rules

```
Game state (autoloads, DBs)
      ↓ (read by screen)
Screen (smart)
      ↓ (adapter: source-specific → neutral DTO)
Dumb component (CharacterCard, etc.)
      ↓ (render)
User sees pixels.
      ↓ (interaction)
Dumb component emits Signal / invokes callback.
      ↑ (event bubbles to screen)
Screen translates event → game-state change / navigation.
```

Arrows only go the direction shown. If a dumb component needs to reach up, the design is wrong — the caller should have passed in what's needed, or the event handler should be translating this on the screen side.

---

## Testing implications

- Dumb components are **trivially testable**: construct one with a hand-built DTO, assert rendered children / state. No save system, no autoloads, no scene stack.
- Screens are **integration-testable**: they need real data flow, so they live in the Layer-3 (GoDotTest) or Layer-4 (GdUnit4) buckets.
- Visual regression becomes tractable: once `Card`'s baseline is pinned via a screenshot, every subclass inherits coverage. A new subclass (ClassCard, CharacterCard, EmptyCard) just needs its content snapshot pinned.

---

## How this plays with Godot specifics

- Godot's theme inheritance cascades through `Control` → `Control` (not through `Node` / `CanvasLayer`). Screens that mount dumb components are responsible for ensuring the theme chain is intact.
- Godot signals are the "events up" channel. Dumb components declare `[Signal] public delegate void SelectedEventHandler();`. Screens connect and act.
- Subclassing a `PanelContainer` / `Control` base is the idiomatic composition method in Godot C# — the equivalent of React's "extend a component."
- `partial class` applies to all Godot node classes (no exceptions) — dumb and smart alike.

---

## When this paradigm doesn't apply

- **Non-UI code** — game logic, save system, RNG utilities, AI behavior. Those are data-and-logic concerns, not view concerns.
- **One-off debug UI** that will never exist in production (`DebugConsole`, `DebugPanel`) — the discipline still helps, but the cost of skipping it is smaller.
- **Truly singleton views** — the HUD is one thing, exists in one place, will never be reused. Building it as a dumb + smart pair is still good discipline but isn't the hill to die on.

Default: apply this paradigm. Opt out only with explicit rationale.

---

## Related

- [AGENTS.md §3. Development Principles](../../AGENTS.md#3-development-principles) — KISS / DRY / composition rules this paradigm enforces.
- [AGENTS.md §4. C# Conventions](../../AGENTS.md#4-c-conventions) — "Call down, signal up" scene-architecture rule this paradigm is the UI-layer expression of.
- [docs/ui/](../ui/) — individual UI specs (pause menu, HUD, death screen) that this paradigm applies to.
