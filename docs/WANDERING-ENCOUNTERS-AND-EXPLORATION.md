# Wandering encounters and exploration — staged

Deployment remains on hold. These additions are in the isolated verification build; they are not installed in the running shard or client.

Validation: the combined Haven test suite passes **207 tests, 0 failures, 0 skipped** against the isolated island terrain (`artifacts/exploration-encounters-full01.log`). Coverage includes encounter waves, participant and helper credit, duplicate callback protection, journal serialization, spending validation, retreat cleanup, pet expiry/ownership, shovel travel, satchel weight propagation and real mounted water movement. Native patch 0047 also applies cleanly to the independent patch-audit tree. The live UOContent assembly hash remains unchanged.

## Wanderer's Chronicle

Use `[encounters` to open Overview, Recent Log, or Rewards. `[encounters off` disables random encounters and cancels an active one without a penalty; `[encounters on` enables them. The setting, current/highest difficulty, wins, assisted clears, retreats/failures, credited kills, spendable points, lifetime earned points and last 20 result/redemption messages are saved per character. Log timestamps are UTC.

While a real player is moving outdoors, a check becomes eligible every 12–20 minutes and has a 50% encounter chance. Guarded towns, houses, combat, casting and invalid terrain are excluded. AFK characters do not trigger new encounters. This is a custom system inspired by the requested behavior, not a port of UOAlive's implementation.

Three waves lead to a champion. Each wave requires `3 + difficulty / 4` kills, with at most three regular enemies alive. Difficulty ranges from 1 to 20. Creatures return to the encounter area if they move more than 20 tiles from its origin. Leaving the 28-tile boundary, dying, or exceeding 15 minutes ends the encounter and lowers the next difficulty by one, never below 1. A successful clear raises it by one, capped at 20. Logout and restart cancel without a difficulty penalty. Cleanup deletes only the encounter's own enemies.

Native looting rights determine credited participants, including normal pet/companion owner attribution. A credited kill grants `1 + difficulty / 5` points; a successful clear gives each recorded participant another `20 + difficulty * 5`. Repeated death callbacks cannot award points twice. Helpers record assisted clears; only the encounter owner advances their personal difficulty. Points remain after retreat, so the journal also records lifetime earnings separately from the spendable balance.

The champion leaves an unlocked, participant-only chest for 20 minutes: scaled gold, gems, Astral shards, build-oriented clothing, and chances at a native runic hammer, recipe scroll, or exploration utility. Chest contents are shared among the credited participants. The points and clear bonus are individual. A clear can also spawn a rare custom pet; ordinary pets never receive custom rarity bonuses from this system.

### Point rewards

| Reward | Points |
|---|---:|
| 25 Astral shards | 250 |
| Agapite runic hammer, 25 uses | 500 |
| Rare custom pet encounter; rarity may be Rare, Epic or Legendary | 1,000 |
| Guaranteed Legendary custom pet encounter | 5,000 |

The reward selection opens a confirmation showing price and balance. Ownership, points, backpack capacity and pet-spawn conditions are rechecked when redeeming. Pet rewards summon a **wild** custom animal outdoors after your encounter ends, rather than granting a tame pet. Normal taming requirements apply. It has 30 minutes to be tamed; an animal that has acquired an owner is never deleted by the expiry timer. Legendary custom pets retain the previously requested one-slot starting cost and innate rarity benefits.

## Exploration additions

- The portable travel book opens **The Wayfarer's Atlas**, with town/dungeon tabs, facet selection, ten destinations per page and Commons access. Dungeon destinations share the stone's list. Normal ownership, combat and travel restrictions remain.
- **The Gilded Pathfinder** uses existing golden-hued shovel art. Target an owned, decoded and unfinished treasure map to travel to a safe tile beside its dig location. It does not decode the map, dig the chest, defeat guardians or bypass travel restrictions.
- **Mercy's Endless Bandage** uses the native bandage healing/veterinary flow without being consumed.
- **Gatherer's Resource Satchel** reduces supported resource weight by 90%, including nested weight propagation, while preserving normal item-count limits. It rejects equipment and containers. The Arcane Supplies stone sells it for 15,000 wallet gold.
- Eligible wild creatures have a 0.2% chance to award an exploration utility and an additional 2.5% chance for build-oriented clothing. Tamed/summoned creatures and service NPCs are excluded. Clothing spans shoes, sandals, doublets, pants, sashes, aprons, shirts, surcoats and boots, with seven fighter/caster/support/crafting profiles. Only the Legendary tier evolves; ordinary drops do not.
- A **Tidebound sea horse charter** grants a one-slot mount ready to bond when fed. `[tide` provides cargo, SOS navigation, fishing-pole and net controls. Mounted riders can move on water and gain +10 Fishing skill modifier; the native racial minimum may mask a modifier at very low skill. Normal fishing tools and nets are still required. Navigation stops for manual movement, combat, logout, invalid SOS, timeout or blocked routes; it does not complete the SOS automatically. Dismounting on water returns the rider to their last safe land. Companions can swim while following a rider on this mount.

The sea horse's cargo uses a validated deposit/withdraw gump because mounted creatures are internalized by the native mount implementation. Native patch `0047-tide-steed-riding-and-fishing.patch` adds rider-change integration and the mounted-fishing exception.

## Separate outstanding content

These additions do not constitute a complete High Seas pirate ship/cannon/cargo system, Blackthorn invasion dungeon, or Shadowguard port. Their era, map and encounter dependencies remain separate work. The earlier staged pirate island camp and native Doom setup must not be described as those later systems being active. SOS steering and all new gumps still require in-client acceptance checks after deployment is authorized.
