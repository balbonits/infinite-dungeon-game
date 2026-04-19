# ADR-008: Post-MVP Gear Visibility on Player Sprites

**Status:** Accepted (as a staged post-MVP plan, not MVP scope)
**Date:** 2026-04-18
**Depends on:** [ADR-007](007-top-down-oga-pivot.md) (top-down + OGA / LPC pivot)

## Context

MVP ships with **one spritesheet per player class** — the LPC-generated `warrior_full_sheet.png`, `ranger_full_sheet.png`, `mage_full_sheet.png` produced by the automation pipeline at `tools/lpc-generator/game-batch.mjs`. The sheet bakes in a locked outfit: warrior in bronze plate + longsword, ranger in green tunic + bow, mage in blue robe + staff. Equipment changes during a run do **not** visually update the sprite.

The product owner has flagged that, post-MVP, gear changes should reflect on the player's character — "Diablo does it too." The LPC generator is specifically designed for this: it composes a character from layered PNGs at runtime. We already have the full LPC asset library vendored at `tools/lpc-generator/` (gitignored, local only) and an automated batch pipeline that proves the composition model works.

This ADR captures the two-stage post-MVP plan, so we don't re-litigate it every session and so the art pipeline stays aimed at the right target.

## Decision

**Two-stage post-MVP plan**, gated by playtest signal:

### Stage 1 — Weapon-only overlay (~3 days of work)

Ship right after MVP. Minimum viable bar, maximum perceptual payoff per hour:

- Each class has **one** baked body sheet (current LPC output — fine as-is).
- The **currently-equipped weapon** renders as a second sprite layered on top of the body, using LPC's existing weapon-layer PNGs (`tools/lpc-generator/spritesheets/weapon/`).
- Equipment-change signal → swap the weapon texture → next frame shows the new weapon.
- No shader work. No 855 MB library vendoring. No layered armor. Just weapon.

This matches Diablo's minimum-viable version (their early builds showed weapon changes first, armor came later). It covers the single most-looked-at gear slot.

### Stage 2 — Full layered compositor (~3–4 weeks of work) — **playtest-gated**

Only commit to Stage 2 **after MVP playtesting confirms the gear-feedback gap is felt** — i.e., players say "I just equipped legendary chest armor and my guy looks identical." If playtesters don't notice, Stage 2 is deferred indefinitely. Don't build what nobody will notice.

When/if Stage 2 fires, scope:

- **Vendor a pruned subset** of `tools/lpc-generator/spritesheets/` into the game's asset pipeline. Target 30–80 MB, not 855 MB. We only ship layers that correspond to items our game actually has. Pruning is a one-time asset-authoring task (~3 days).
- **Port the palette-remap shader** to Godot 4's shading language. LPC's WebGL approach (palette encoded as a small texture, fragment shader does RGB lookup with tolerance 1) ports almost line-for-line. ~half a week.
- **Build a `CharacterRig` C# node** that holds N `Sprite2D` children (one per visible slot) + a palette uniform per child. Reacts to inventory-change signals. ~1 week + ~1 week for animation-sync edge cases (not every layer supports every animation row; LPC gotcha #6 in [lpc-automation-pipeline.md](../assets/lpc-automation-pipeline.md)).
- **Visible slots (max 4 for 64×64 sprites):** weapon, chest/body armor, helm, offhand/cape. Gloves/boots cut — at 64×64 the pixels aren't there to read. This cut is deliberate and documented — adding them later remains cheap if playtest demands it.
- **Tier-count math:** 3 classes × 4 gear tiers × 4 slots ≈ 192+ unique silhouettes. Pre-rendering all of these ahead of time is untenable; runtime compositing becomes the only reasonable approach at this scope.

## Rationale

- **Art-first priority survives into post-MVP.** The pivot in ADR-007 was because art is what unblocks content feeling real. Weapon-visibility is the lowest-cost way to extend that same "art matters" principle into the loot loop.
- **Don't build what playtesting hasn't validated.** The full compositor is 3–4 real weeks. That's a lot to spend on a feature-richness guess. Stage-gate the expensive half.
- **Stage 1 is a stepping stone, not throwaway.** The weapon-overlay code IS the core of the compositor (one body sprite + one weapon sprite, layered). Stage 2 just scales the approach to N slots + palette recolor. Nothing built for Stage 1 is wasted.
- **Staying inside the LPC ecosystem keeps asset pipeline cost flat.** No new art commissions, no new generation pipeline, no new style to QA against existing character sheets.

## Licensing posture (applies to Stage 2, not Stage 1)

LPC's licensing is a per-layer mix: CC0, CC-BY, CC-BY-SA 4.0, OGA-BY, GPL. CC-BY-SA 4.0 is the strictest and binds the whole composite if any one layer uses it.

Two implications for Stage 2:

1. **Art becomes CC-BY-SA 4.0** (assuming we use any CC-BY-SA layers). Code license is unaffected — the engine code that loads CC-BY-SA art is a separable work.
2. **CC-BY-SA's DRM clause is ambiguous vs. Steam.** The LPC upstream README itself flags this. Mitigation (to decide when Stage 2 fires, not now):
   - **Option A** — restrict Stage 2 to CC0 + OGA-BY layers only (OGA-BY explicitly permits DRM). Smaller layer pool but avoids the question.
   - **Option B** — ship the CC-BY-SA art bundle as a DRM-free downloadable alongside the Steam build (satisfies the "remove DRM if requested" reading of the clause).
   - **Option C** — legal review at that time.

Stage 1 (weapon-only) escapes the question — one weapon sprite layered over a baked body sheet is trivial to source from CC0/OGA-BY alone.

## Consequences

- The MVP character sheets at `assets/characters/player/*/` are **final-for-MVP** — don't regenerate them for Stage 2 (we'll rebuild characters from individual layers at that point, not from a baked sheet).
- The LPC generator at `tools/lpc-generator/` stays local-only (gitignored) as the asset-authoring tool — not as a game-runtime dependency.
- `docs/assets/lpc-automation-pipeline.md` continues to own the batch-generation flow (unchanged by this ADR).
- The weapon-only overlay (Stage 1) will get its own implementation ticket (SPEC-GEAR-VISIBILITY-STAGE1-01) when MVP ships and we're picking up post-MVP work. Not authoring that spec yet.
- Stage 2 remains blocked on a **playtest-feedback gate**. Don't reopen this ADR just because the idea is exciting.

## Reversibility

- Stage 1 is ~3 days; easily rolled back by removing the weapon overlay node if it causes issues.
- Stage 2 is 3–4 weeks. Before committing, re-evaluate whether the playtest feedback justifies the cost. If MVP playtesters don't mention the gear-feedback gap, Stage 2 does not fire.

## Out of scope

- MVP gear visibility (stays baked per sheet).
- Enemy gear variation (monsters don't have equipment in our design).
- Armor tinting / customization at character creation (deferred separately).
- Shield overlays beyond the weapon slot (deferred to Stage 2).
