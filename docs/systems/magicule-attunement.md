# Magicule Attunement

## Summary

A post-cap passive tree fueled by attunement points earned from clearing deep dungeon floors. The tree offers permanent, death-surviving stat bonuses and build-defining keystones that give endgame characters a second axis of progression beyond stats and gear. Smaller than PoE's passive tree (40 nodes) but deeper per node, with meaningful choices at every branch.

## Current State

**Spec status: LOCKED.** Design complete. Not yet implemented. Depends on stats (locked), floor scaling (locked), magic (locked), and dungeon (locked).

## Design

### Overview

After clearing floor 50 for the first time, the player unlocks the **Attunement Crystal** in town. This crystal displays a passive tree of 40 nodes arranged in concentric rings. The player spends attunement points to unlock nodes, gaining permanent bonuses that persist through death and never reset.

Attunement is the endgame infinite progression system. While stats have diminishing returns and gear has affix caps, attunement bonuses are flat and permanent, giving deep-dungeon players a tangible reward for continued play.

### Earning Attunement Points

```
attunement_points_earned = 1 per floor cleared past floor 50
```

- Floor 51 clear: +1 point
- Floor 52 clear: +1 point
- Floor 100 clear: +1 point
- Revisiting a previously cleared floor does NOT grant additional points
- Points are tracked per character, earned once per floor per character
- A "cleared floor" means reaching the exit staircase (or defeating the boss on boss floors)
- Dying on a floor grants zero points for that floor
- Points survive death and are never lost

At floor 100, a player who cleared every floor from 51 onward has earned 50 points -- enough to unlock all small nodes and several medium nodes, but not enough for everything. By floor 150, they have 100 points and can reach the keystones. Full tree completion requires clearing past floor 200.

### Tree Structure

The tree is arranged in 3 concentric rings around a central origin node, with 4 branches radiating outward (one per stat: STR, DEX, STA, INT). Each branch has nodes on all 3 rings.

```
Ring 1 (Inner):  12 small nodes  — 2 points each — stat bonuses
Ring 2 (Middle):  8 medium nodes — 5 points each — mechanic modifiers
Ring 3 (Outer):   4 large nodes  — 15 points each — build-defining keystones
Between rings:   16 connector nodes — 1 point each — small stat bonuses
```

**Total nodes:** 40
**Total points to complete tree:** 12*2 + 8*5 + 4*15 + 16*1 = 24 + 40 + 60 + 16 = **140 points**
**Floors required for full completion:** Floor 50 + 140 = **floor 190**

### Pathing Rules

- The origin node is unlocked automatically when the system activates (floor 50 clear)
- Nodes must be unlocked along connected paths from the origin outward
- Each branch can be progressed independently
- To unlock a Ring 2 node, the player must have unlocked at least 2 Ring 1 nodes in that branch
- To unlock a Ring 3 keystone, the player must have unlocked the Ring 2 node in that branch
- Connector nodes bridge between branches, allowing limited cross-branch pathing
- No respec. Attunement is permanent. Choose carefully.

### Node Definitions

#### Ring 1: Small Nodes (12 total, 2 points each)

STR Branch (3 nodes):
| Node | Effect |
|------|--------|
| Hardened Muscles | +5 flat melee damage |
| Bone Density | +30 max HP |
| Crushing Force | +3% melee damage |

DEX Branch (3 nodes):
| Node | Effect |
|------|--------|
| Nimble Fingers | +3% attack speed |
| Fleet Footed | +8% dodge chance (additive, before DR cap) |
| Sharp Eyes | +2% flat crit chance |

STA Branch (3 nodes):
| Node | Effect |
|------|--------|
| Thick Skin | +20 flat defense |
| Deep Breath | +0.5 HP regen/sec |
| Enduring Body | +50 max HP |

INT Branch (3 nodes):
| Node | Effect |
|------|--------|
| Expanded Mind | +40 max mana |
| Efficient Processing | +5% processing efficiency (mana cost reduction) |
| Magicule Affinity | +8% spell damage |

#### Connector Nodes (16 total, 1 point each)

Four connectors between each pair of adjacent branches (STR-DEX, DEX-STA, STA-INT, INT-STR). Each connector grants a hybrid bonus:

| Connection | 4 Nodes Each Grant |
|------------|-------------------|
| STR-DEX | +2 flat melee damage, +1% attack speed |
| DEX-STA | +2% dodge chance, +15 max HP |
| STA-INT | +10 max HP, +15 max mana |
| INT-STR | +3% spell damage, +2 flat melee damage |

Connector nodes enable paths between branches. A player who wants a STA keystone but started in the STR branch can path through STR-DEX and DEX-STA connectors to reach it.

#### Ring 2: Medium Nodes (8 total, 5 points each)

Two medium nodes per branch, each offering a mechanic modifier rather than a flat stat:

STR Branch:
| Node | Effect |
|------|--------|
| Overkill | When a melee hit kills an enemy, 25% of excess damage splashes to the nearest enemy within 64px |
| Berserker's Edge | Below 30% HP, melee damage +20% |

DEX Branch:
| Node | Effect |
|------|--------|
| Chain Shot | 10% chance on ranged hit to fire a free projectile at a second target within 128px |
| Afterimage | After dodging an attack, gain +30% movement speed for 2 seconds |

STA Branch:
| Node | Effect |
|------|--------|
| Second Wind | When HP drops below 20%, recover 15% max HP once per floor (resets at floor entry) |
| Iron Constitution | Reduce all damage-over-time effects by 40% |

INT Branch:
| Node | Effect |
|------|--------|
| Spell Echo | 8% chance to cast the same spell twice (second cast costs no mana, deals 50% damage) |
| Mana Shield | When mana is above 50%, 20% of incoming damage is redirected to mana (1 damage = 2 mana) |

#### Ring 3: Keystones (4 total, 15 points each)

Build-defining nodes. Each keystone fundamentally changes how the character plays. Only one keystone can be active at a time -- unlocking a second keystone deactivates the first. This prevents stacking all four.

| Keystone | Branch | Effect |
|----------|--------|--------|
| **Juggernaut** | STR | All melee attacks gain +100% knockback distance. Enemies knocked into walls take 50% of the hit's damage again as Physical damage. Movement speed reduced by 10%. |
| **Phantom** | DEX | Dodge chance cap raised from the natural DR limit to 60% effective. Missed dodges still reduce damage by 15%. Cannot wear body armor (slot is locked empty). |
| **Undying** | STA | On fatal damage, instead of dying, become invulnerable for 3 seconds and regenerate 30% max HP. Triggers once per dungeon run (resets on town return). All damage taken increased by 10% permanently. |
| **Arcane Overload** | INT | All spell damage +40%. Mana costs +25%. When mana reaches 0, release a magicule burst dealing 200% of max mana as Dark damage to all enemies within 128px. Burst triggers once per exhaustion cycle (recharges when mana refills to 100%). |

### Lore Integration

The Attunement Crystal is a fragment of the dungeon's core, brought to the surface by a previous adventurer. By channeling processed mana (attunement points) into the crystal, the adventurer permanently alters their magicule processing pathways. The brain literally rewires itself to handle magicules differently.

This is why attunement survives death: the changes are at the cellular level, deeper than the memories the dungeon eats. The dungeon can strip EXP (action memories) but cannot undo the fundamental rewiring of the nervous system. Attunement represents the adventurer evolving beyond what the dungeon can take back.

The four branches correspond to the four fundamental ways the body processes magicules: physical reinforcement (STR), reflexive channeling (DEX), cellular fortification (STA), and conscious manipulation (INT).

### Interaction with Existing Systems

- **Stats (stats.md):** Attunement flat bonuses bypass the diminishing returns curve. +5 flat melee damage from Hardened Muscles is always +5, unlike STR's diminishing flat bonus. This makes attunement valuable even at extreme stat levels.
- **Death (death.md):** Attunement points and unlocked nodes are never lost on death. This is the one progression axis that death cannot touch.
- **Dungeon Pacts (dungeon-pacts.md):** Pacts increase difficulty, which pushes the player deeper, which earns more attunement points. The two systems feed each other.
- **Item Affixes (items.md):** Attunement bonuses stack additively with equipment affixes of the same type (+crit chance from attunement + crit chance from gear = total crit chance).
- **Elemental Damage (elemental-damage.md):** Mana Shield (Ring 2 INT node) redirects damage after resistance mitigation. Iron Constitution (Ring 2 STA node) reduces DoT effects including ambient Dark DPS.

## Acceptance Criteria

- [ ] Attunement Crystal unlocks in town after first clear of floor 50
- [ ] Attunement points earned at rate of 1 per new floor cleared past floor 50
- [ ] Points are per-character and tracked in save data
- [ ] Revisiting a cleared floor does not grant additional points
- [ ] Dying on a floor grants zero points for that floor
- [ ] Points and unlocked nodes survive death (never lost)
- [ ] Tree displays 40 nodes in 3 rings with 4 branches
- [ ] Pathing requires connected unlocks from origin outward
- [ ] Ring 2 nodes require 2 Ring 1 unlocks in the same branch
- [ ] Ring 3 keystones require the Ring 2 node in the same branch
- [ ] Only one keystone can be active at a time
- [ ] All 12 small nodes grant the exact stat bonuses listed
- [ ] All 16 connector nodes grant the exact hybrid bonuses listed
- [ ] All 8 medium nodes apply their mechanic modifiers correctly
- [ ] All 4 keystones apply their full effects including drawbacks
- [ ] Flat stat bonuses bypass the diminishing returns curve
- [ ] No respec mechanism exists for attunement
- [ ] Tree UI shows current points, unlocked nodes, available paths, and point costs

## Implementation Notes

- Attunement data is a bitmask or boolean array of 40 node states plus the active keystone index, stored on the character save
- A separate `floors_cleared` bitfield (or set) tracks which floors past 50 have been cleared, to prevent re-earning points
- Flat bonuses from attunement should be added after the diminishing returns calculation in StatSystem, not before (they bypass DR)
- Medium node effects (Overkill, Chain Shot, etc.) need hooks in combat resolution, dodge handling, and damage application
- Keystones modify fundamental systems: Juggernaut modifies knockback physics, Phantom modifies dodge cap, Undying hooks into the death flow before the death screen, Arcane Overload hooks into mana exhaustion
- The tree visualization can be a simple radial graph -- no need for the complexity of PoE's web. Four clear branches with rings.

## Open Questions

None.
