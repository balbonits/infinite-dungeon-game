# Village Chief Dialogue Tree

## Summary

The Village Chief is the quest-giver for the town. His dialogue tree drives quest acceptance, in-progress check-ins, and completion flow. SPEC-VILLAGE-CHIEF-DIALOGUE-01 (Phase G). Voice follows [npc-dialogue-voices.md §Village Chief](npc-dialogue-voices.md).

## Current State

**Spec status: LOCKED** via SPEC-VILLAGE-CHIEF-DIALOGUE-01 (Phase G). Depends on [npc-dialogue-voices.md](npc-dialogue-voices.md) (voice contract). Unblocks `NPC-ROSTER-REWIRE-01` impl for the Village Chief service-menu wiring.

The Chief is a **fresh** NPC — no prior sprite or dialogue exists. This spec and the paired [SPEC-NPC-ART-01 §Village Chief](../world/npc-art.md) are the full first-pass definition.

## Design

### Role

The Village Chief is:
- **The frontier settlement's voice.** He represents the village to visiting adventurers, frames the stakes, pitches the work.
- **The quest giver.** Every quest the PC can accept comes from him. Guild Maid explicitly routes quest questions to the Chief.
- **NOT a banker, shopkeeper, or smith.** He provides context and offers; he does not process transactions.

Voice profile (see [voices spec](npc-dialogue-voices.md)): elderly, considered, warm-formal, wise-elder. Longer sentences. References village stakes. Uses `{Class} Guildmaster` as address with occasional honorifics.

### Dialogue state machine

The Chief has six dialogue states, gated by quest progression:

```
first_meeting ──► idle ──┬──► quest_offered ──┬──► quest_in_progress ──► quest_complete ──► idle
                         │                    │
                         │                    └──► quest_declined ──► idle
                         │
                         └──► no_quests_available ──► idle
```

**Transitions:**

- `first_meeting` → `idle` after the greeting is acknowledged.
- `idle` → `quest_offered` if there's an unaccepted quest in the Chief's queue.
- `idle` → `no_quests_available` if no quest is currently offerable.
- `quest_offered` → `quest_in_progress` on accept.
- `quest_offered` → `quest_declined` on decline.
- `quest_declined` → `idle` after a short acknowledgment.
- `quest_in_progress` → `quest_complete` when the quest's completion condition is met AND the player returns to the Chief.
- `quest_complete` → `idle` after reward delivery.

The state machine does NOT gate dialogue on the Chief being "busy" or player-waiting scenarios — he's always available when approached.

### Dialogue trees by state

#### State: `first_meeting`

Triggered the first time the PC speaks to the Chief per save slot.

> **Chief:** `{Class} Guildmaster, if I may. I'm the one they send when the village wants a thing said. Not glamorous, but it keeps the lights on. We're a frontier settlement with a dungeon at our doorstep — you know that better than we do, most likely. What brings you to my porch today?`

Player options:
- **"I just wanted to introduce myself."** → `idle` (Chief: "Then consider us introduced. You'll know where to find me when you've an ear for village matters.")
- **"Is there something I can do for the village?"** → `quest_offered` (if available) OR `no_quests_available`

Save this interaction as `firstMeetingDone = true`; future visits skip `first_meeting`.

#### State: `idle`

Default state when the Chief has no pending quest to offer or the PC walks up mid-nothing.

> **Chief:** `{Class} Guildmaster. A moment, if you have one.`

Player options:
- **"What does the village need?"** → `quest_offered` (if available) OR `no_quests_available`
- **"How is everyone?"** → flavor response, stays in `idle` (Chief: "They carry on. The forge smokes; the Maid counts. The dungeon gives and takes. A day like any other, which is itself a blessing.")
- **"Just passing through."** → stays in `idle` (Chief: "Safe runs, Guildmaster.") + dismiss panel

#### State: `quest_offered`

The Chief has a quest to offer. Quest content is parameterized — this spec defines the wrapper, not the quest bodies themselves (which come from [quests.md](../systems/quests.md)).

> **Chief:** `{Class} Guildmaster. There is a thing the village needs, and I'd rather ask you than assume. {quest_brief: one or two sentences on the ask in-voice.} If the depths have been willing to you, there's a favor I'd put to you.`

After a beat, summarize:

> **Chief:** `What I'm asking is this: {quest_objective_summary}. In return, {quest_reward_summary}. You'd have as long as you need, within reason.`

Player options:
- **"I accept."** → `quest_in_progress`. Chief: "You carry the village's thanks before you've earned them. Come back when it's done — or come back if it becomes impossible. Either is fine."
- **"Not now."** → `quest_declined`. Chief: "Another time, then. The need doesn't lessen, but nor does your discretion. Safe runs."
- **"Tell me more."** → quest-specific elaboration (one paragraph of extra context from the quest body), then returns to this same menu.

#### State: `no_quests_available`

The PC asked for work but the queue is empty.

> **Chief:** `Nothing pressing at present, {Class} Guildmaster. The village's wants are small today. Check back when you're next up this way — they rarely stay small for long.`

Returns to `idle`. Do NOT show this as a false-empty; it should only appear if the quest system genuinely has no offer.

#### State: `quest_in_progress`

PC returns mid-quest. The Chief acknowledges the open work without re-pitching.

> **Chief:** `{Class} Guildmaster. The favor I asked — {quest_objective_short} — I trust is in hand?`

Player options:
- **"Still working on it."** → returns to `idle`. Chief: "Take the time you need. The village isn't going anywhere."
- **"Remind me what you needed."** → short restatement of `quest_objective_summary` without the full opening pitch, then back to this menu.
- **"I have to stop."** → presents a confirm: "The village understands. Would you like to cancel the request?" → Yes cancels the quest (back to `idle`); No returns to this menu.

If the player HAS completed the quest but hasn't explicitly confirmed: the dialogue prompt gates on `questCompleted = true`, in which case we jump to `quest_complete` instead of showing the in-progress menu.

#### State: `quest_complete`

PC has met the completion condition and approached the Chief.

> **Chief:** `{Class} Guildmaster. You carried more than I asked. The village will remember this — and so, I'll wager, will the dungeon. Here is what was promised: {quest_reward_summary}. You have our gratitude, for whatever gratitude is worth on that side of the dungeon door.`

The reward is delivered as part of this dialogue (gold/items appear in the PC's inventory). Transition to `idle` after the reward is acknowledged.

The Chief does NOT chain a second quest offer in the same interaction — the player has to walk away and return to see the next offered quest. This deliberate beat lets the reward feel like a closing moment rather than a conveyor-belt hand-off.

#### State: `quest_declined`

PC said "not now" to a quest offer.

> **Chief:** `Another time, then. The need doesn't lessen, but nor does your discretion. Safe runs.`

Returns to `idle`. The declined quest returns to the offerable queue; the next time the PC asks for work, the same quest is offered again (no punitive cooldown).

### Cross-NPC routing

If the PC mentions non-Chief services:

- **"Where do I store my gold?"** → `Chief: That's Guild Maid's domain, Guildmaster. My ledger's of a different sort.` (Doesn't open the Guild Maid menu; player walks over.)
- **"Can you repair this?"** → `Chief: The Blacksmith'll see to that better than I could. He's down the road, hammer in hand.`
- **"Can you teleport me?"** → `Chief: Guild Maid handles the Stone. She'll align it for you if you're heading back.`

These routes are conversational, not UI navigations. They set expectation without blocking the player.

### Post-death return

PC returns from a dungeon run that ended in death.

> **Chief:** `The village was worried. The village is glad. Both at once is the cost of hope, it turns out.`

If there was a quest in progress, the Chief does NOT cancel it — the PC can resume. If the quest required specific items that were lost on death, that's handled by the quest system's state, not by this dialogue.

### Tone-breaking restrictions

Things the Chief does NOT say:

- Combat advice ("Watch out for the skeletons on floor 3!") — he doesn't go into the dungeon and won't pretend to know.
- Class-specific commentary that sounds like favoritism ("A fine warrior you are" — except in a general-respect sense; never "better than a mage would do").
- Jokes or levity — the wise-elder voice is warm but grave.
- Pressure to accept a quest ("The village really needs you to...") — he offers; he never cajoles.
- Meta-commentary ("You've played a lot today, Guildmaster" — no fourth-wall breaks).

---

## Acceptance Criteria

- [ ] Every state in the state machine has at least one reachable entry path and one reachable exit path.
- [ ] Every dialogue line passes the voice-distinction tests in [npc-dialogue-voices.md §Voice-distinction tests](npc-dialogue-voices.md#voice-distinction-tests).
- [ ] `first_meeting` triggers exactly once per save slot.
- [ ] `quest_offered` only presents when the quest queue has an offerable quest; otherwise routes to `no_quests_available`.
- [ ] `quest_complete` delivers reward inline and does not auto-chain a second offer.
- [ ] Decline returns the quest to the offer queue (no punitive cooldown).
- [ ] Cross-NPC routing lines acknowledge the request without opening another NPC's menu.
- [ ] Post-death return line fires once when the PC's prior run ended in death.

## Implementation Notes

- **Quest-body templating:** `{quest_brief}`, `{quest_objective_summary}`, `{quest_objective_short}`, `{quest_reward_summary}` are string-substitution slots fed from each individual quest's data in [quests.md](../systems/quests.md). The Chief dialogue shell is the same for every quest; the variables change.
- **State persistence:** `firstMeetingDone` lives in save state per `{Class}` slot. Quest states live in the existing quest system.
- **UI panel hookup:** opens the same `NpcPanel` as other NPCs (per [npc-interaction.md](npc-interaction.md)) with state-dependent dialogue text + button layout.
- **Voice uniformity guard:** if a developer adds a new quest whose `quest_brief` or `quest_reward_summary` doesn't read in-voice, the voice-distinction tests should fail on review. Include a voice-check as a gate in the quest-add PR review template.

## Open Questions

None — spec is locked.
