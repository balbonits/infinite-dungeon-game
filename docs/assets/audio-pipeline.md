# Audio Pipeline — POL-01 Research + License Policy

Status: **Research locked (spec-phase)** — curated OGA candidates + license discipline + directory contract. Impl ticket opens once PO approves source picks.

---

## §1 License Policy (Load-Bearing)

The project targets eventual commercial release on storefronts that may include Apple App Store / Xbox Live / PlayStation (DRM platforms) per [SPEC-EXPORT-PLATFORMS-01](../dev-tracker.md). Monetization plan is documented in [SPEC-MONETIZATION-ADS-01](../systems/monetization-ads.md). Every audio asset we ship must satisfy BOTH constraints: commercial-distribution-friendly AND DRM-platform-compatible.

### §1.1 Accepted licenses (in order of preference)

| License | Commercial | DRM platforms | Attribution | Notes |
|---------|------------|---------------|-------------|-------|
| **CC0 / Public Domain** | ✅ | ✅ | Not required | First preference — zero friction |
| **OGA-BY 3.0 / 4.0** | ✅ | ✅ | Required | Second preference — explicitly DRM-friendly (this is its distinguishing feature vs CC-BY) |
| **CC-BY 3.0 / 4.0** | ✅ | ⚠️ **blocked** | Required | Avoid unless we confirm we will never ship on DRM platforms. Store in `assets/audio/_cc-by-review/` directory and escalate to PO before use. |

### §1.2 Rejected licenses (do not ship)

| License | Reason |
|---------|--------|
| **CC-BY-SA 3.0 / 4.0** | Viral / copyleft — derivative works must ship under the same license. Any audio we modify (remix, chop, loop-repair) would taint any assets we bundle with it. Also blocked on DRM platforms. |
| **CC-BY-NC** (any variant) | Non-commercial-only. Blocks monetization. |
| **CC-BY-ND** | No-derivatives — we can't trim, loop, or integrate. |
| **GPL 2.0 / 3.0** (for assets) | Designed for source code; legal application to audio is ambiguous. When in doubt, pass. |
| **Freeware / "free for personal use"** | Not an open-source license. Not enforceable, not transferable on resale. |
| **Unspecified license** | No license = no rights. Even if the work appears free, skip. |

### §1.3 Attribution protocol

Every non-CC0 audio file lands in `assets/audio/` with a companion entry in `assets/audio/CREDITS.md`. One row per file, format:

```
| assets/audio/<path> | <title> | <artist> | <license> | <source URL> |
```

CC0 files are still credited in CREDITS.md (one row with `CC0` in the license column) — we credit the artist even when they don't require it, because it's gracious and makes future audits easier.

### §1.4 Safety net — PR gate

Every PR that adds an audio asset must include an updated `CREDITS.md` row and a one-line PR-description link to the OGA source page. A reviewer who clicks the link should be able to verify the license in under 30 seconds. If the page has been edited since download, the OGA "Revisions" tab has the canonical license-at-time-of-download.

---

## §2 Directory Contract

```
assets/audio/
├── CREDITS.md                       # Attribution table — every file tracked here
├── music/
│   ├── menu/                        # Splash screen, title
│   ├── town/                        # Town hub ambient
│   ├── dungeon/                     # Dungeon exploration
│   │   ├── zone_low.ogg             # Zones 1-5 (F1-50), calm exploration
│   │   ├── zone_deep.ogg            # Zones 6-10 (F51-100), tense exploration
│   │   └── zone_infinite.ogg        # Floor 101+ density ≥1.0 per magic.md — ominous
│   └── boss/
│       └── boss_generic.ogg         # Shared boss theme (all 8 zone bosses)
└── sfx/
    ├── ui/                          # Button focus, click, toast, dialog
    ├── combat/                      # Attack swings, hits, crits, projectiles
    ├── character/                   # Footsteps, hurt, death, level-up
    ├── enemy/                       # Enemy hit, enemy death
    ├── environment/                 # Chest open, container break, door, stairs
    ├── economy/                     # Gold pickup, item pickup, equip, buy/sell
    └── notification/                # Quest complete, achievement, skill-up, XP gain
```

**Rationale for flat category grouping:** the spec roadmap does not include per-zone music variation at MVP — one track per zone-band is sufficient. When ART-SPEC-AUDIO-BIOME-01 eventually lands (post-MVP), `music/dungeon/` can subdivide without breaking the existing paths.

**File format:** `.ogg` (Vorbis) for music and long SFX, `.wav` for short impulses (UI clicks, footsteps). Godot supports both natively and `.ogg` compresses ~10× better than `.wav` for multi-second clips.

---

## §3 Asset Inventory — Music (9 tracks)

| Path | Trigger | Intent | Loopable | Length target |
|------|---------|--------|----------|---------------|
| `music/menu/title.ogg` | SplashScreen | Calm fantasy overture, invites the player in | ✅ | 60-90s loop |
| `music/menu/death.ogg` | DeathScreen | Funereal minor-key, no percussion | ✅ | 30-60s loop |
| `music/town/town.ogg` | Town scene | Warm fireside ambient — safe-zone reward | ✅ | 90-120s loop |
| `music/dungeon/zone_low.ogg` | F1-50 | Exploration, some menace — low tension baseline | ✅ | 120-180s loop |
| `music/dungeon/zone_deep.ogg` | F51-100 | Deeper menace, echoing space | ✅ | 120-180s loop |
| `music/dungeon/zone_infinite.ogg` | F101+ | Dread, asymmetric rhythms — density ≥1.0 per [magic.md](../systems/magic.md) | ✅ | 120-180s loop |
| `music/boss/boss_generic.ogg` | Any boss encounter | Orchestral-percussive, high-BPM | ✅ | 90-120s loop |
| `music/sting/levelup.ogg` | Level-up event | 2-4s stinger, no loop | ❌ | 2-4s one-shot |
| `music/sting/victory.ogg` | Boss defeated | 4-6s stinger, triumphant | ❌ | 4-6s one-shot |

**Total music budget: 9 tracks.** Cheaper than feature-film-style per-floor music, more expensive than single-track jingle. Sits right for ARPG scope.

---

## §4 Asset Inventory — SFX (~45 files)

### §4.1 UI (8)
- `sfx/ui/button_focus.wav` — keyboard-nav focus change
- `sfx/ui/button_click.wav` — button press
- `sfx/ui/dialog_open.wav` — any window open (pause, bank, shop)
- `sfx/ui/dialog_close.wav` — any window close
- `sfx/ui/toast_info.wav`, `toast_success.wav`, `toast_warning.wav`, `toast_error.wav` — 4 Toast variants per existing `Toast.cs` enum

### §4.2 Combat (12)
- `sfx/combat/attack_swing_sword.wav` — Warrior melee
- `sfx/combat/attack_swing_bow.wav` — Ranger draw+release
- `sfx/combat/attack_cast_bolt.wav` — Mage magic bolt cast
- `sfx/combat/hit_flesh.wav` — player lands hit on living enemy
- `sfx/combat/hit_skeleton.wav` — player lands hit on skeletal/bone enemy
- `sfx/combat/hit_armor.wav` — player lands hit on armored (Orc, Dark Mage)
- `sfx/combat/crit.wav` — critical-hit stinger (overlaid on normal hit)
- `sfx/combat/player_hurt.wav` — player takes damage
- `sfx/combat/projectile_arrow.wav` — arrow in flight
- `sfx/combat/projectile_magic.wav` — magic bolt / fireball / frost bolt
- `sfx/combat/projectile_impact.wav` — projectile hits target
- `sfx/combat/block.wav` — block triggered (COMBAT-01 dependency)

### §4.3 Character (4)
- `sfx/character/footstep_stone.wav` — dungeon floor
- `sfx/character/footstep_grass.wav` — town
- `sfx/character/player_death.wav` — death-cinematic trigger
- `sfx/character/levelup.wav` — pairs with `music/sting/levelup.ogg`

### §4.4 Enemy (4)
- `sfx/enemy/enemy_hit.wav` — enemy takes hit (shared)
- `sfx/enemy/enemy_death.wav` — enemy dies (shared)
- `sfx/enemy/enemy_spell_cast.wav` — Dark Mage cast telegraph (tied to SPEC-SPECIES-DARKMAGE-01 wind-up)
- `sfx/enemy/boss_roar.wav` — boss encounter start + phase-shift stinger

### §4.5 Environment (6)
- `sfx/environment/chest_open.wav` — Chest container (LOOT-01)
- `sfx/environment/crate_break.wav` — Crate container
- `sfx/environment/jar_break.wav` — Jar container
- `sfx/environment/door_open.wav` — dungeon arches / doorways
- `sfx/environment/stairs_descend.wav` — floor descent (pairs with ScreenTransition)
- `sfx/environment/portal.wav` — teleporter (Guild Maid Teleport tab)

### §4.6 Economy (6)
- `sfx/economy/gold_pickup.wav` — coin chime
- `sfx/economy/item_pickup.wav` — neutral item pickup
- `sfx/economy/equip.wav` — equipment equipped
- `sfx/economy/unequip.wav` — equipment unequipped
- `sfx/economy/buy.wav` — shop purchase / crafting transaction
- `sfx/economy/sell.wav` — shop sell / recycle

### §4.7 Notification (6)
- `sfx/notification/quest_complete.wav` — Quest system
- `sfx/notification/achievement.wav` — Dungeon Ledger unlocks
- `sfx/notification/skill_levelup.wav` — skill XP → skill level-up
- `sfx/notification/ability_levelup.wav` — ability XP → ability level-up
- `sfx/notification/save.wav` — autosave complete (subtle)
- `sfx/notification/load.wav` — game loaded

**Total SFX budget: 8 UI + 12 combat + 4 character + 4 enemy + 6 env + 6 economy + 6 notification = 46 files.**

---

## §5 Curated OGA Candidate Sources

Shortlisted packs to mine (verified CC0 or OGA-BY via OGA license tag on each listing page). **Impl ticket will audition each and pick final track IDs** — this list is the starting shortlist, not the final pick.

### §5.1 Music — CC0 first-pass picks

| Pack | Artist | License | Use for | OGA URL |
|------|--------|---------|---------|---------|
| **CC0 Fantasy Music & Sounds** | Alexandr Zhelanov | CC0 | Town / menu / low-zone loops — high production value, orchestral-lite | https://opengameart.org/content/cc0-fantasy-music-sounds |
| **Fantasy Song Pack Volume 1** | Various (2026) | CC0 | Additional mood coverage — small curated pack | https://opengameart.org/content/fantasy-song-pack-volume-1 |
| **Fantasy Music and Drum Loops Pack** | — | CC0 | Deep-zone tension loops + percussion layer for boss theme | https://opengameart.org/content/fantasy-music-and-drum-loops-pack |
| **Dungeon Ambience** | — | CC0 | Infinite-zone dread layer | https://opengameart.org/content/dungeon-ambience |
| **CC0::music::fantasy tag** | Various | CC0 | Catch-all tag browse for gaps in coverage | https://opengameart.org/content/cc0musicfantasy |
| **RPG::Music tag** | Various | Mixed — filter to CC0/OGA-BY | Secondary pool | https://opengameart.org/content/rpgmusic |

### §5.2 SFX — CC0 first-pass picks

| Pack | Artist | License | Use for | OGA URL |
|------|--------|---------|---------|---------|
| **RPG Sound Pack** | Artisticdude | CC-BY 3.0 ⚠️ | **DRM-review flagged** — move to `_cc-by-review/` if used, or find CC0 substitute | https://opengameart.org/content/rpg-sound-pack |
| **50 RPG sound effects** | Kenney | CC0 | Combat hits + UI clicks — Kenney packs are a reliable CC0 workhorse | https://opengameart.org/content/50-rpg-sound-effects |
| **100 CC0 SFX #2** | Various | CC0 | Footsteps, chest/crate break, door — broad coverage | https://opengameart.org/content/100-cc0-sfx-2 |
| **RPG Sound Effect Pack** | — | CC0 | Footsteps variety | https://opengameart.org/content/rpg-sound-effect-pack |
| **MySFX** (80 CC0 RPG SFX) | — | CC0 | Sword/hit/squish combat layer | https://opengameart.org/content/mysfx |
| **CC0 Sounds Library** | — | CC0 | UI + notification coverage | https://opengameart.org/content/cc0-sounds-library |
| **Sound Effects Pack** | — | CC0 | Metallic impacts, footsteps | https://opengameart.org/content/sound-effects-pack |

### §5.3 Pass-over packs (documented for future reference)

Packs found during research but passed on for license reasons, so future AI passes don't re-audition them:

- *RPG Sound Pack (Artisticdude)* — CC-BY 3.0; fine for attribution but blocked on DRM platforms. Only use if DRM-platform distribution is ruled out, or if a CC0 substitute can't be found for a specific SFX.

---

## §6 Audio Wiring Plan (Impl Notes, Post-Approval)

*Non-normative — for the follow-up impl ticket when POL-01 moves from research to impl.*

### §6.1 Bus architecture

```
Master
├── Music  (volume bus — ducking-capable)
├── SFX
│   ├── Combat   (bus — for hitstop / low-pass during pause)
│   ├── UI       (bus — always clean, no ducking)
│   ├── Ambient
│   └── Notification
```

- Settings panel already has an Audio tab per Tracker SYS/Settings row; each bus gets a dB slider (-60 to 0).
- Music bus gets ducked by -8 dB when boss_roar fires (200ms smoothstep), restores on boss defeat.
- All buses respect global pause (Engine.TimeScale handling, but AudioStreamPlayer is time-scale-independent by default — validate during impl).

### §6.2 Scene hookup

- **MusicManager autoload** — singleton that listens for scene changes (`SceneTree.SceneChanged`) and maps to a music bank. One AudioStreamPlayer with two-stream crossfade for smooth transitions.
- **SfxManager autoload** — `SfxManager.Play(sfxId)` convenience method. Internally owns an AudioStreamPlayer pool (8 slots, round-robin) to handle rapid-fire overlapping plays without audio glitches.
- **Enum-keyed IDs** — `SfxId.CombatHitFlesh`, `MusicId.DungeonZoneLow`, etc, with a `Dictionary<SfxId, AudioStream>` preloaded at game start. Prevents string-typo bugs and lets grep find every playsite.
- **Event subscriptions (sample):**
  - `Player.DamageTaken` → `SfxManager.Play(SfxId.PlayerHurt)` + `CameraShake` from SPEC-CAMERA-SHAKE-01
  - `Enemy.Defeated` → `SfxManager.Play(SfxId.EnemyDeath)`
  - `GameState.GoldChanged` (positive delta) → `SfxManager.Play(SfxId.GoldPickup)` only if delta from monster/container pickup source
  - `Toast.Show(level)` → `SfxManager.Play(SfxId.ToastInfo + level)`

### §6.3 Settings-panel wiring

- Master volume slider (0-100 → -60 to 0 dB log-scale).
- Music volume slider.
- SFX volume slider.
- Mute-on-focus-loss toggle (default on).
- Values persist via existing `GameSettings.cs` JSON autoload.

---

## §7 What's Not in Scope Here

- **Voice acting** — not planned. Dialogue is text-only via DialogueBox.
- **Procedural music / adaptive layering** — MVP is fixed-loop music with crossfades only.
- **3D positional audio** — top-down 2D game; stereo-pan based on sprite X-distance is acceptable for combat SFX in v2, not MVP.
- **Per-zone biome music** — MVP ships one track per zone-band (low/deep/infinite). Per-zone variation is a post-MVP enhancement (call it SPEC-AUDIO-BIOME-01 when reopened).
- **Licensed composer work / paid tracks** — scope is free/open-source audio only. Paid-asset substitution is a PO decision post-MVP.

---

## §8 Acceptance Criteria (for follow-up impl ticket)

When POL-01 graduates from research to impl, the impl ticket must:

- [ ] Download audio files into `assets/audio/**/` matching the directory contract in §2.
- [ ] Populate `assets/audio/CREDITS.md` with every file's attribution row.
- [ ] Audition each track for loop-quality (no click at loop point) and length-budget compliance.
- [ ] No asset lands under a §1.2 rejected license.
- [ ] `MusicManager` and `SfxManager` autoloads wired per §6.
- [ ] Settings panel Audio tab sliders functional.
- [ ] At minimum: title screen music plays on splash, one dungeon track loops in a real floor, one combat SFX fires on player hit, one UI click fires on button press. (Smoke test — not full 9+46 coverage.)
- [ ] Follow-up ticket opens for remaining SFX coverage if not fully populated in the first impl pass.

---

## §9 Sources

- [OpenGameArt FAQ — license summary](https://opengameart.org/content/faq)
- [OGA-BY license text](https://static.opengameart.org/OGA-BY-3.0.txt)
- [CC-BY 3.0 legal code](https://creativecommons.org/licenses/by/3.0/legalcode)
- [CC0 1.0 legal code](https://creativecommons.org/publicdomain/zero/1.0/legalcode)
