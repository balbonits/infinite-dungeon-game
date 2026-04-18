# NPC Dialogue Voices

## Summary

Voice conventions for the three town NPCs — Blacksmith, Guild Maid, Village Chief — so dialogue writers (human or AI) can match each NPC's speech pattern without re-inventing it per line. SPEC-NPC-DIALOGUE-VOICES-01 (Phase G).

Inspired by the Maoyuu homage (title-only NPCs, distinct job-role voices) from [project_npc_naming.md](../../.claude/agent-memory/) and the PO's 2026-04-18 direction that Blacksmith should read as a "pioneer smith learning," Guild Maid as "crisp service," and Village Chief as "wise elder."

## Current State

**Spec status: LOCKED** via SPEC-NPC-DIALOGUE-VOICES-01 (Phase G). Load-bearing for the three other Phase G specs ([village-chief-dialogue.md](village-chief-dialogue.md), [blacksmith-menu.md](../ui/blacksmith-menu.md), [guild-maid-menu.md](../ui/guild-maid-menu.md)), and for any future dialogue additions (e.g., shopkeeper banter in the Blacksmith's Shop tab).

## Design

### Why voice conventions matter

The PC is addressed as `{Class} Guildmaster` in all NPC dialogue (per [NPC naming convention](../../.claude/agent-memory/)). All three NPCs are stationary town-dwellers who repeat their interactions across many play sessions, so their speech patterns become a big part of town's character. Each NPC's voice must be distinct enough that a player reading a blind dialogue line can tell which NPC said it.

### Voice profiles

#### Blacksmith — "pioneer smith learning"

A smith in a frontier village. Skilled at the basics but openly aware there's more to the craft than they've mastered. Warm, casual, thinks out loud. Never pretends to be a master; often credits an adventurer (the PC included) with bringing in unfamiliar materials that teach them something new. The "pioneer" framing is important — this isn't a grizzled veteran smith with all the answers; it's someone who moved out here to build a craft and is still growing into it.

**Speech traits:**
- Short sentences. Technical where the craft demands it, plain where it doesn't.
- Occasionally trails off mid-thought when something catches their attention ("That hide's got an odd grain to it — ... anyway, what'll it be?").
- Admits limits matter-of-factly ("Never worked orichalcum before. You bring me some, we'll figure it out together.").
- Warm but not chatty — respects the adventurer's time.
- Uses craft terminology (forge, quench, recycle, temper) naturally. Doesn't explain what the words mean.

**Sample lines:**
- Greeting: "Guildmaster. What're we working on?"
- Forge tab intro: "Bring me what you want improved. I'll see what I can do."
- Unknown material: "Hm. I've not touched this one yet. Let me see what she teaches me."
- Out-of-stock in Shop: "All out. Come back once the caravan rolls through, or bring me what's missing and I'll trade."

#### Guild Maid — "crisp service"

Professional, efficient, formal. Not cold — warm in the way a well-trained front-desk clerk is warm. Says exactly what needs saying, no wasted words. Takes the work seriously; takes herself lightly. Moves the adventurer through services quickly so they can get back to what matters (the dungeon).

**Speech traits:**
- Clean, clipped sentences. Subject-verb-object, minimal hedging.
- Uses the PC's title (`{Class} Guildmaster`) as a neutral address; never shortens.
- Occasional dry observation delivered with the same flat tone as routine service ("The Bank is, as ever, where you left it.").
- Never commiserates or enthuses. Confirms, provides, moves on.
- Addresses the adventurer with consistent respect regardless of class or condition (post-death, dirty, injured — all the same).

**Sample lines:**
- Greeting: "Mage Guildmaster. Bank, teleport, or both today?"
- Transaction confirm: "Deposited. 1,420 gold in bank. Will that be all?"
- Teleport ready: "The Stone is aligned to Floor 14. Press through when ready."
- Post-death return: "Welcome back, Warrior Guildmaster. Your stored belongings are untouched."

#### Village Chief — "wise elder"

Elderly, considered, spends a sentence or two before getting to the point. Tells you a little more than you asked for because the context matters. Carries the weight of being the frontier settlement's voice — he's the one who pitches the dungeon work to arriving adventurers and he means it. Not ponderous; just unhurried.

**Speech traits:**
- Longer sentences than the other two NPCs. Allows subordinate clauses.
- Uses the `{Class} Guildmaster` title with a softening honorific the first time per session ("Warrior Guildmaster, if I may.").
- References the village's stakes — what's at risk, what the dungeon gives them, what the PC's work makes possible.
- Wise-elder cadence — rhetorical pauses ("And yet..."), gentle repetition for emphasis.
- Never lectures; invites. Offers context, leaves the decision with the PC.

**Sample lines:**
- Greeting (first meeting): "Warrior Guildmaster, if I may. The village watches your return with more hope than yesterday, and yesterday was already a good day."
- Greeting (returning): "Mage Guildmaster. A moment, if you have one."
- Quest offer: "There is a thing the village needs, and I'd rather ask you than assume. If the depths have been willing to you, there's a favor I'd put to you."
- Quest decline: "Another time, then. The need doesn't lessen, but nor does your discretion. Safe runs."
- Quest complete: "You carried more than I asked. The village will remember this — and so, I'll wager, will the dungeon."

### Voice-distinction tests

If a dialogue line is unclear who said it, apply these tests:

- **Line length:** Blacksmith < Guild Maid < Village Chief (roughly).
- **Formality:** Guild Maid > Village Chief > Blacksmith (Guild Maid is ALWAYS formal; Chief is warm-formal; Blacksmith is casual-warm).
- **Vocabulary:** Blacksmith uses craft terms; Guild Maid uses service terms (transaction, deposit, align); Chief uses village-stakes terms (village, return, favor, memory).
- **Address to PC:** Guild Maid always uses full `{Class} Guildmaster`; Chief uses it with occasional honorifics; Blacksmith often just says "Guildmaster" without the class prefix.
- **Emotional register:** Blacksmith is warm-curious; Guild Maid is flat-polite; Chief is grave-hopeful.

If you can't write a line in a way that passes at least three of these tests, rewrite it until it does.

### Voice consistency across play states

**Post-death return.** Each NPC acknowledges the adventurer came back from the dungeon but in their own voice.

- **Blacksmith:** "Rough one?" (short, no fuss, doesn't probe.)
- **Guild Maid:** "Welcome back, {Class} Guildmaster. Your stored belongings are untouched." (same line every time; consistency IS the warmth.)
- **Village Chief:** "The village was worried. The village is glad. Both at once is the cost of hope, it turns out." (acknowledges the loss, frames it as communal; wise-elder reflection, not pity.)

**Low HP / visible wounds.** Blacksmith notices craft-related things ("Armor's taken a beating — I can look at it"); Guild Maid behaves identically to normal; Village Chief acknowledges without comment ("Sit if you like. The talk can wait.").

**High-value transaction.** Blacksmith shows quiet pride ("That's the best piece I've put through this forge"); Guild Maid does not change tone ("Noted. 84,000 gold deposited."); Village Chief acknowledges with measured gravity ("A haul like this is why the village endures").

### Edge cases

- **Shop tab inside Blacksmith's menu** uses Blacksmith's voice. The Shop is his side-business moving materials the caravan brings in — it's not a separate shopkeeper NPC with their own voice.
- **Teleport interaction under Guild Maid** uses Guild Maid's voice. No separate "Teleporter" NPC exists; the teleportation is a Guild service the Maid administrates.
- **No quest pickup under Guild Maid.** If a player tries to ask about quests while talking to Guild Maid, she routes: "Village Chief handles that, {Class} Guildmaster. He's usually by the fountain." (keeps her in character; doesn't break service flow.)
- **No banking under Village Chief.** If a player mentions banking, Chief routes: "That's Guild Maid's domain, Guildmaster. My ledger's of a different sort." (sets up the quest-as-ledger metaphor subtly.)

---

## Acceptance Criteria

- [ ] Any new dialogue line can be written against this spec and be traceable to exactly one of the three NPCs by voice alone.
- [ ] All three NPCs have distinct speech patterns along the length / formality / vocabulary / address / emotional-register dimensions.
- [ ] Post-death return, low-HP, and high-value-transaction variants are specced for each NPC.
- [ ] Cross-NPC routing (Guild Maid → Chief for quests, Chief → Maid for banking) is specced and uses each NPC's in-voice phrasing.
- [ ] Every existing and future dialogue doc (starting with [village-chief-dialogue.md](village-chief-dialogue.md)) cites this spec as the voice contract.

## Implementation Notes

- **Voice rules are prose-level, not code-level.** No engine-side enforcement; dialogue authors (human or AI) consult this doc when writing lines.
- **Localization caveat:** voice conventions described here are English-first. If the game ever localizes, the five voice-distinction tests should be re-scored per language (sentence-length conventions differ across languages, e.g., German's compound nouns shift the "line length" axis). Out of scope for SPEC-NPC-DIALOGUE-VOICES-01.

## Open Questions

None — spec is locked.
