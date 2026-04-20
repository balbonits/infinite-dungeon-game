# Monetization & Ads

## Summary

A pitch-grade monetization model for *A Dungeon in the Middle of Nowhere*: a one-time **Supporter Edition** paid unlock as the primary revenue lever, a desktop-legal **house-ads surface** (cross-promo + studio news, not third-party rewarded video) for the free build, and an opt-in **rewarded-ad** layer reserved for a possible mobile/web port. The goal is not to maximize revenue — it is to prove a ship-ready monetization path that is consistent with Steam policy, our completionist-ARPG identity, and the game's "no manipulative systems" design philosophy.

## Current State

Spec only. Nothing is implemented. No ad SDK is integrated, no IAP surface exists, no price is set. Tagged **v1-polish, post-MVP** — this spec exists to de-risk the funding conversation, not to unblock shipping the core game. Implementation is tracked as `MONETIZATION-ADS-IMPL-01` (create once this spec is locked).

This spec depends on and cross-references:
- [docs/systems/death.md](death.md) — death penalty flow (candidate for rewarded-ad hook, mobile-only)
- [docs/flows/shop.md](../flows/shop.md) — Guild Store (candidate hook: re-roll)
- [docs/flows/splash-screen.md](../flows/splash-screen.md) — surface for house-ad card and "Buy Supporter Edition" CTA
- [docs/flows/dungeon.md](../flows/dungeon.md) — floor-descent transition (candidate interstitial slot, mobile-only)
- [docs/systems/save.md](save.md) — entitlement persistence
- [docs/architecture/analytics.md](../architecture/analytics.md) — opt-in measurement pipeline (shared)

## Design

### 1. Goal & Framing

Monetization in this game is a **funding runway signal**, not a core loop. The model tells a publisher or investor: *"if we choose to ship free-to-try with a paid unlock, here is exactly how that works, and it is consistent with the game we are actually building."* The three revenue surfaces (Supporter Edition, house ads, opt-in rewarded ads on non-Steam ports) are designed so that a player who buys day-one never sees an ad and a player who never pays still gets the complete game.

### 2. Distribution Tiers (what ships where)

| Tier | Platforms | Price | Ads | Notes |
|------|-----------|-------|-----|-------|
| **Paid Edition** | Steam (primary), itch.io, Epic | one-time purchase | none, ever | This is the default shipping SKU for desktop. |
| **Free Trial** | itch.io, direct download, future Steam demo | free | house ads only (see below) | Full content up to a soft gate (see §5). |
| **Free Mobile/Web Port** | Future post-v1 only | free | house ads + opt-in rewarded (see §6) | Reserved for a possible F2P port; not active at v1. |

**Key policy constraint:** Steam explicitly prohibits in-game paid advertising and rewarded-ad models. See [Steamworks Advertising policy](https://partner.steamgames.com/doc/marketing/advertising). This spec therefore treats rewarded ads as a **non-Steam-only** feature and treats the Steam SKU as a traditional one-time-purchase game. The "ads" half of this spec is real, but it only activates on platforms that permit it.

### 3. House-Ad Surface (desktop-legal)

What Steam permits: non-incentivized, non-paid cross-promotion and product placement. We use this to run a **house-ad slot** — a single rectangular card surfaced at rest points in the flow — that cycles through:

- Our own future titles, DLC, or soundtrack.
- Community spotlights (fan art, mod highlights) approved by us.
- A static "Buy Supporter Edition" CTA (on the Free Trial build).
- Optional: curated indie cross-promos with other studios we partner with directly (never paid placement, never an ad network).

**Slots (free-trial build only):**

| Slot | Trigger | Format | Frequency cap | Justification |
|------|---------|--------|---------------|---------------|
| Splash card | [Splash Screen](../flows/splash-screen.md) idle | 480x270 card below title, above buttons | Max **1 impression per session** (first splash entry only; if the player returns to splash mid-session, the card is suppressed) | Player is already parked on a menu; zero combat interruption. |
| Town return card | First town entry after a dungeon run | Toast-size card over the HUD, auto-dismiss 5s | Max 1 per session | Natural breath moment; player is sorting loot, not fighting. |
| Death-screen footer | [Death dialog](death.md) Accept Fate → confirm | 96px footer band on the death summary | Once per death, cannot be the primary focus | The primary content (sacrifice options) is unaffected; the ad cannot cover any button. |

Across all three slots: **maximum 5 impressions per session combined** (splash 1 + town returns up to 1 + death footer up to N deaths, capped at 5 total).

**Hard rules for house ads:**
- Never mid-combat, never mid-cinematic, never during the death cinematic fade.
- Never over a required input (buttons, text fields).
- Never animated aggressively (no flashing, no sound).
- **Dismissibility window** — the 500ms is a responsiveness SLA, not a hold-window: the ad accepts any input from the moment of first render, and the input handler must process that input within 500ms of receipt. No "unskippable" opening duration.
- House ads are **disabled entirely on the Paid Edition** — flag off, no UI reservation.

### 4. Rewarded-Ad Mechanics (mobile/web port only — reserved, not active at v1)

If and only if the game is later ported to a platform that permits rewarded advertising (mobile, itch.io web build with a non-Steam ad network), the following hooks are available. None of these ship with the Steam build.

**Eligible hooks (mapped to existing specs):**

| Hook | Spec | Reward | Why it's fair |
|------|------|--------|---------------|
| Extra Sacrificial Idol on death | [death.md](death.md) | Consume an ad in place of a gold Save-Both buyout | Player already owns the choice to Save; the ad just substitutes for gold. Zero progression impact. |
| Shop re-roll | [flows/shop.md](../flows/shop.md) | Re-roll Guild Store daily rotation once | Same result a patient player gets by waiting for the next day's stock. |
| Rested-XP top-up | [systems/leveling.md](leveling.md) — rested XP | +30 minutes of rested XP accumulation | Rested XP is already a catch-up mechanic; this just compresses time. |
| Bonus chest peek | [systems/loot-containers.md](loot-containers.md) | Re-roll a single unopened chest's contents before opening | Self-limiting: player can only peek one chest per ad-watch, cooldown 10 min. |

**Never eligible for rewarded ads (locked):**
- **EXP loss mitigation.** Death's EXP tax is the dungeon's unavoidable memory cost ([death.md](death.md)); buying it back with an ad would break the fairness contract.
- **Equipment protection.** Equipment is the long-term progression axis; gating its preservation on ads turns the game into a slot machine.
- **Combat power.** No damage boosts, no temporary stat buffs, no "watch an ad to clear this floor."
- **Boss access / floor descent.** Never gate content behind ads. Ever.
- **Pact rewards** ([dungeon-pacts.md](dungeon-pacts.md)). Pacts are the endgame expression layer; keep them ad-free.

**Frequency cap (mobile/web port):** maximum one rewarded-ad watch per 10 minutes of play, max 6 per session, hard-muted for the first 15 minutes of a new character's playtime so the first-run experience is never interrupted.

### 5. Pay-to-Remove Flow (the primary revenue lever)

**Product:** A single one-time IAP called **Supporter Edition**. This is the default Steam SKU — on Steam, the game simply *is* the Supporter Edition (no free trial on Steam at launch; Steam's paid-game model doesn't need an "unlock" flow). On itch.io and future web/mobile ports, Supporter Edition is a purchasable unlock that:

- Removes **all house ads** (splash card, town card, death footer) everywhere.
- Disables any rewarded-ad prompts (if the port has them), but rewarded-ad-equivalent rewards remain available through normal gameplay means (gold, time).
- Grants a small cosmetic token: a **Supporter Sigil** frame around the character portrait. Purely cosmetic, never a stat boost.
- Unlocks the soft content gate on the Free Trial (if one is present — see "Soft gate" below).

**Price band (not a locked number):**
- USD **$9.99 – $19.99** on desktop.
- Mobile/web port: **$4.99 – $9.99** (shorter-session expectation).
- Regional pricing follows Steam's recommended regional matrix on Steam builds.

**Soft gate for Free Trial (itch.io / demo):** the first **10 dungeon floors + the first boss fight** are free forever. Floor 11+ is Supporter-Edition-only. Rationale: floor 10 is enough to exercise all three classes, at least one build pivot, and the first major spike in loot density. A player who doesn't hit floor 10 wasn't going to convert; a player who does has a clear reason to pay.

**Subscription?** **No.** Never. This game is built around infinite progression and save-slot permanence; a subscription model would force us to add churn-retention hooks (daily logins, FOMO events) that are explicitly forbidden by §7.

**Persistence & restore:**

| Scope | Behavior |
|-------|----------|
| Steam | Owned via Steam license — no in-game IAP screen needed. Every save slot on that account is automatically Paid Edition. |
| itch.io / direct | Per-account license key entered on the [Splash Screen](../flows/splash-screen.md) → Settings. Key is stored in `user://license.json`, independent of [save slots](save.md). |
| Mobile / web port | Platform IAP (App Store / Play / web payment). "Restore Purchases" button in Settings always available and free. |
| Cross-platform | No cross-platform entitlement at v1. A player who bought on Steam does not get it free on mobile. Documented clearly on the store page. |

**Entitlement resolution rule:** the game always boots in "unknown entitlement" state, checks the platform's ownership API (Steam license / license key file / IAP receipt), and only **after** confirming free-tier runs the house-ad layer. Default on failure is **Paid Edition behavior** (hide ads) — if we can't confirm, we do not annoy the player.

**Refunds & sandbox:**
- Steam handles refunds per Steam policy.
- itch.io: refunds at developer discretion; we default to "within 14 days if <2 hours playtime."
- Dev sandbox: a `--devpaid` launch flag forces Paid Edition behavior for QA. Never read in release builds.

### 6. Technical Integration Sketch

Research-backed options only. No invented libraries.

**Steam (IAP / entitlement):**
- **GodotSteam GDExtension 4.4+** — [asset library page](https://godotengine.org/asset-library/asset/2445), [docs](https://godotsteam.com/), [repo](https://github.com/GodotSteam/GodotSteam). Primary option. C# bindings are thin (GDScript-first API), so we wrap the `Steam.*` calls behind a small `ISteamService` C# facade.
- **Godot.Steamworks.NET** — [repo](https://github.com/ryan-linehan/Godot.Steamworks.NET). C#-native alternative wrapping Steamworks.NET directly. Preferred if we want a pure-C# integration and are willing to accept a smaller community.
- For microtransaction DLC (if we ever add supporter cosmetic packs beyond the base purchase), Steam's [MicroTxn web API](https://partner.steamgames.com/doc/features/microtransactions/implementation) can be brokered via an intermediate server; community reference implementation: [steam-microtransaction-api](https://github.com/jasielmacedo/steam-microtransaction-api). Not needed for v1 — base Steam license is enough.

**Desktop house ads:**
- **No third-party SDK.** We self-host a static JSON manifest at a CDN (`https://[our-domain]/house-ads/v1/manifest.json`) plus PNG card assets. Client fetches once per session with a 24-hour local cache, falls back to a **bundled default manifest** if offline. This is compatible with Steam's policy because nothing is paid and nothing is rewarded — it's a cross-promo surface we own end-to-end.
- Fetch via Godot's `HttpRequest`. Fallback-first: if the fetch fails, the bundled manifest renders and the player never sees a loading state.

**Mobile / web port ads (reserved, v1.5+):**
- **Poing Studios Godot AdMob plugin** — [site](https://poingstudios.github.io/godot-admob-plugin/), [asset library](https://godotengine.org/asset-library/asset/2063), [repo](https://github.com/poingstudios/godot-admob-plugin). Android/iOS only, supports GDScript and C#. Only option we'd bring in if we ship a mobile port with rewarded ads.
- For an HTML5 port, evaluate a web-native network (Google AdSense for Games, CrazyGames SDK) at that time. Out of scope for v1.

**Official Godot SDK integration reference:** [Godot SDK Integrations article](https://godotengine.org/article/godot-sdk-integrations/) — the Foundation's directory of vetted third-party SDKs. Check here before adding any new SDK; do not bring in random GitHub forks.

**IAP kill-switch:** all ad/IAP code lives behind a single `MonetizationService` autoload with two feature flags: `ads_enabled` and `iap_enabled`. Flags are read from `user://monetization.json`, default to the build configuration (Steam build = ads off, iap off; free itch.io build = ads on, iap on), and can be overridden to `false` by a remote killswitch (see §8).

### 7. Player-Facing Policy

**Discoverability:**
- Settings → "Support the developer" section: shows Supporter Edition status (Owned / Not Owned), price, and a "Buy" or "Restore Purchases" button. No nag.
- Splash screen (free build only): the house-ad card defaults to the Supporter CTA on first launch, rotates to cross-promos thereafter. The CTA is dismissible and remembered — we do not re-show it every boot.

**Throttling rules (session-level):**
- Total house-ad impressions per session are capped at **5** regardless of trigger count.
- After the cap, all house-ad slots no-op silently for the remainder of the session.
- A "First 15 minutes" grace window applies to every new character — no house ads until the player has played 15 real minutes on that slot.

**Never-show-during (hard rules):**
- Mid-combat (any enemy within aggro range, any player attack within 3s).
- Mid-cinematic (death cinematic, floor-descent transition, boss intro).
- Inside any modal dialog that blocks input.
- During the first boss fight of a new character's progression (first-run protection).
- While the [pause menu](../flows/pause-menu.md) is open.

**Transparency:**
- A "Why am I seeing this?" link on every house-ad card opens a Settings sub-panel explaining: the card is a house-promo, we do not use third-party ad networks, and only the aggregate event counts listed in §9 (impression + click + dismiss) are recorded — not personal data, not identifiers, not ad-network cookies.

### 8. Anti-Patterns (hard no)

We will never ship any of the following, on any platform. This list is the design contract:

- **Loot-box gambling.** No paid RNG crates. Item drops from gameplay follow the published drop-rate tables in [systems/item-generation.md](item-generation.md) — rolls are stochastic per those tables (not seed-deterministic), but the tables are visible and no currency sits between the player and the drop.
- **Energy or stamina timers.** No "wait 2 hours to play again" or "spend gems to refill."
- **Content gated behind ads.** Boss fights, floors, classes, skills, and story beats are **never** unlocked by watching an ad.
- **Manipulative "almost won" popups.** No "one more try for $0.99" after a death.
- **Dark-pattern consent.** The No button is always as prominent as the Yes button.
- **Pay-to-win.** No stat boosts, XP multipliers, gold injections, or combat advantages purchasable for money.
- **FOMO events.** No 24-hour limited-time cosmetics, no battle passes with expiring tiers.
- **Ad retargeting or third-party tracking.** House ads are self-hosted; we don't embed trackers.
- **Silent opt-in.** Analytics and IAP receipts are handled per the rules in [analytics.md](../architecture/analytics.md) — offline-first, opt-in, GDPR-compliant.

This is a completionist's ARPG. Every monetization decision must pass the test: *"would a player who sank 400 hours into this game feel respected?"*

### 9. Measurement & Killswitch

**Minimum metrics for a funding-pitch validation (opt-in, via [analytics.md](../architecture/analytics.md) pipeline only):**

| Metric | Why the pitch cares |
|--------|----------------------|
| Free-trial → Supporter conversion rate | The core thesis number. |
| Median time-to-purchase | Calibrates the soft-gate placement (floor 10 vs 15). |
| House-ad impressions per session | Confirms the throttling is actually holding. |
| House-ad click-through rate on Supporter CTA | Validates the splash card as a conversion surface. |
| Rewarded-ad opt-in rate (mobile/web port only) | Post-v1 signal only. |
| Refund rate | Negative signal; should be <5%. |
| Ad-related uninstall / account-delete rate | Sanity check against hostility. |

All measurement is opt-in per [analytics.md](../architecture/analytics.md). A player who declines analytics never contributes to these numbers, and the pitch deck must state that honestly.

**Killswitch:**
- `MonetizationService` checks a remote JSON at boot (with offline fallback and a 1-second timeout). If the remote returns `{ "ads_disabled": true }` or `{ "iap_disabled": true }`, the corresponding subsystem is hard-off for the session.
- Use cases: store submission windows (Apple/Steam review), ad-network outage, legal issues with a specific promoted title, emergency response to a bad ad creative.
- The killswitch is **one-way in session**: once off, it stays off until next boot. Never re-enables mid-session.

### 10. Implementation Sketch (non-binding)

```
autoload/MonetizationService (singleton)
  - Entitlement { Paid, Free, Unknown }
  - AdsEnabled : bool  (computed from entitlement + killswitch + platform)
  - IapEnabled : bool
  - OnEntitlementChanged signal

scripts/monetization/
  - SteamEntitlementProvider.cs       // wraps GodotSteam or Steamworks.NET
  - LicenseFileEntitlementProvider.cs // itch.io key file
  - HouseAdService.cs                  // manifest fetch + cache + slot queries
  - KillswitchService.cs               // remote JSON fetch, 1s timeout

ui/house-ad/
  - HouseAdCard.tscn  (splash/town/death-footer all reuse this)
```

## Acceptance Criteria

A future `MONETIZATION-ADS-IMPL-01` ticket is "done" when:

- [ ] `MonetizationService` autoload exists with `Entitlement`, `AdsEnabled`, `IapEnabled` properties and `OnEntitlementChanged` signal.
- [ ] Steam build: entitlement resolves to `Paid` via GodotSteam or Steamworks.NET ownership check; `AdsEnabled = false` unconditionally.
- [ ] itch.io / direct build: license-key file at `user://license.json` drives entitlement; absence = `Free`.
- [ ] House-ad manifest fetch succeeds with a 1s timeout and falls back to a bundled default.
- [ ] Three house-ad slots (splash, town-return, death-footer) render per §3, respect the 5-impression session cap, and never render during combat / cinematic / modal / first-15-minutes.
- [ ] "Supporter Edition" purchase flow is reachable from Settings → Support the developer; "Restore Purchases" works.
- [ ] Paid-Edition build has zero ad UI (no reserved space, no fetches).
- [ ] Killswitch disables ads and/or IAP when remote flag is set, stays off for the session.
- [ ] Unit tests cover entitlement resolution order, killswitch override, and throttle logic.
- [ ] Manual QA matrix covers all four states: `{Paid, Free} x {Online, Offline}`.
- [ ] Opt-in analytics events fire for `supporter_purchase`, `supporter_restore`, `house_ad_impression`, `house_ad_click` only when consent is granted.
- [ ] All copy passes the anti-pattern review in §8.
- [ ] No third-party ad SDK is linked into the Steam build binary.

## Implementation Notes

- Treat `MonetizationService` like [save.md](save.md)'s `SaveManager` — one autoload, explicit signals, no cross-scene globals.
- The house-ad card is a dumb presenter; all gating logic (throttle, never-show-during, cap) lives in `HouseAdService`.
- Do NOT share code between the desktop house-ad path and the future mobile AdMob path — they are different surfaces with different legal profiles. Share only the `MonetizationService` entitlement layer.
- Entitlement default on any error is **Paid Edition** (ads off). Silent is better than hostile.
- First-run protection: gate ads on `GameState.PlayedMinutes >= 15` as a separate check from the session cap.
- Tag the IAP integration behind a feature flag until the store pages are live; keep it on a branch-only path until §5 is confirmed by the PO.

## Open Questions

These are the decisions the PO needs to make before `MONETIZATION-ADS-IMPL-01` can start. Everything else is locked above.

1. **Supporter Edition price.** $9.99, $14.99, or $19.99 on desktop? Spec locks the band ($9.99–$19.99) but not the number. Recommendation from design: **$14.99** — high enough to signal "real game," low enough to clear the impulse-buy threshold for an ARPG.
2. **Soft-gate depth.** Floor 10 + first boss is the design default. Confirm, or specify a different boundary (e.g., first class respec, first crafting unlock, first town NPC conversation).
3. **Free-trial distribution.** Ship a free trial on itch.io at launch? Ship only Paid on Steam at launch and add a Steam demo later? Or skip the free trial entirely and go paid-only on all platforms (ads spec becomes reserved-only documentation)?
4. **Network-of-studios house-ad partners.** Curated list of indie partners for cross-promo, or first-party only at launch? Affects manifest editorial workflow but not the technical spec.
5. **Steam ownership check failure policy.** If GodotSteam / Steamworks init fails (rare, but happens on dev machines without the Steam client running), boot into Paid Edition or Free Edition? Spec default is Paid Edition on any error — confirm this is acceptable for the Steam SKU (a player who pirates would benefit, but we prefer that over accidentally showing ads to a paying customer).
6. **Region-specific handling.** Apply Steam regional pricing only, or also adjust the free-trial soft gate by region (e.g., deeper free content in regions with lower purchase power)? Likely "use Steam regional pricing, no gate adjustment" — confirm.
