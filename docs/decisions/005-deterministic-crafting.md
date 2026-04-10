# ADR-005: Deterministic Crafting Over RNG

**Status:** Accepted
**Date:** 2026-04-08

**Context:** Traditional ARPGs (Diablo, Path of Exile) use random number generation for crafting -- you spend materials and hope for a good outcome. This creates excitement through gambling but also creates frustration when players sink resources into bad rolls. The game draws heavy inspiration from Diablo 2's affix system but the product owner wanted to eliminate crafting frustration while preserving the thrill of loot discovery.

**Decision:** The Blacksmith adds exact affixes to equipment with no RNG. The player picks the specific affix they want, pays materials and gold, and gets exactly that result. Excitement comes from finding base items and rare materials in the dungeon, not from gambling on craft outcomes.

Crafting rules:
- Maximum 3 prefixes + 3 suffixes per item (6 total affixes)
- Player chooses the exact affix and pays the cost -- deterministic every time
- Item level gates which affix tiers are available (a floor 5 item can only receive low-tier affixes)
- Affixes cannot be removed once applied, so choices still have weight
- Monsters drop base items only -- no magical drops from enemies
- Recycling unwanted gear at the Blacksmith yields materials, so no drop feels wasted

**Consequences:**
- Players have full control over their build through crafting -- theorycrafting is about planning, not praying
- The excitement loop shifts to dungeon exploration: finding the right base item quality, the right item level, and the rare materials needed for high-tier affixes
- Build diversity comes from affix combinations and the 10 ring slot stacking system, not from lucky drops
- Item level becomes a critical stat -- players are motivated to descend deeper for higher item-level bases that unlock stronger affix tiers
- Balancing effort shifts from drop rate tuning to material cost tuning and affix tier gating
- No "bricking" items with bad rolls, reducing player frustration and removing the need for re-roll mechanics
