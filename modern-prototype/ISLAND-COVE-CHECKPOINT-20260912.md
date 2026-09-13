# Island cove and world encounter checkpoint — September 12, 2026

Isolated prototype only. No live server restart, terrain change or encounter installation.

## Completed
- Replaced the circular prototype coast with irregular shores and a southeast cove beside the estate boarding pier. Kept the southern boat corridor open.
- Created independent terrain package verification/HavenIslandCoveData from verified HavenIslandDataFinal; 6,236 tiles changed within the prior generated island footprint. Both map1 and map1x edited consistently. Existing live/source terrain was not written.
- Source survey data had changed since the original manifest, so the original-source staging preflight correctly refused it. Refinement instead verifies the previous staged hash and every generated tile before replacing it. Unknown terrain is rejected.
- Added test-only HavenIslandEncounters installation: corsair lookouts at 4172,2910; relic poachers at 4240,2845; cove smugglers at 4220,2930, Trammel.
- Three enemies per patrol, 15% chance of a captain in its first slot, two-minute replacement after the patrol is cleared. Population references and cooldowns serialize.
- Normal patrol kills add 150–300 gold, 5% chance of a level 1–3 map and 35% chance of five cannonballs. Captains add 1,500–2,500 gold, the existing pirate-themed mini-boss chance table and 35% chance of 25 cannonballs. Existing native brigand loot remains additional. Special weapon/shield sets remain a 2% captain drop, not guaranteed.
- Harmful targeting rejects targets outside the encounter's eight-tile area or inside houses. Spawn candidates exclude houses; roaming enemies return toward their home when outside the encounter area. These need player/client testing before live promotion.

## Validation
Release build: zero warnings/errors. All 11 settlement routes and eight community-center approaches passed. Castle placement remained Valid with zero displaced objects. Native SmallBoat CanFit passed along the southern corridor. All three encounter sites passed spawn, population-cap, death, cooldown and replacement checks. Test world exited without saving. Verification DataPath restored to its normal value and island test marker disabled afterward.

Terrain review image: verification/island-cove-preview.png. It renders staged terrain with schematic planned facility markers, not actual in-game buildings.

## Still required before release
Dedicated cove boss encounter progression and reward participation; final patrol balance and visual placement; persistent housing permissions and connected private storage; matched modern client/server terrain package; in-game walk/boat/combat checks and save/reload verification. Community services must be protected from the Haven plaza maintenance migration. Do not install these prototypes on live without completing those integration checks.
