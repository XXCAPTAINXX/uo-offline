# Haven Commons, the market, Doom, and Corsair's Rest

Status: staged source and isolated test terrain only. No live installation, world spawning, restart, or client-map replacement has occurred. The user's deployment hold remains active.

## Market and community center

Haven Commons is a 33-by-41 stone courtyard with perimeter walls, four broad entrances, columns, lighting, plants and seating. Thirteen stalls cover smithing, tailoring, carpentry, tinkering, fletching, inscription, alchemy, cooking, gathered resources, adventuring loot, artifacts, jewelry and set pieces. The side workshop has an anvil, forge, spinning wheel, loom, repair-all bench and training-supplies stone. Other services include a banker, provisioner, player/pet healers, combat training sentinel, and five harmless resettable lock/trap chests. Native Remove Trap prerequisites still apply. The floors and fixtures use actual client tile heights and named art; client appearance still needs visual QA.

The rune-library browser lists towns and the coordinates in the shard's `Data/treasure.cfg`, for Trammel and Felucca. Travel finds an unoccupied, house-free nearby landing. It is a coordinate browser, not a searchable atlas or a treasure-map decoding service. The dungeon portal provides the existing dungeon destinations. Bank dungeon portals now offer Haven Commons; its return gate leads to New Haven bank.

The mall's artisans produce finite stock through timed workshop jobs. Gathering is simulated, one real resource batch per completed job. Crafting consumes resources and checks native recipe skill, success and exceptional quality. Recipes requiring missing nonstackable ingredients or unearned recipe unlocks are skipped. Smiths/tailors collect small BODs and occasionally large BODs; native combine validation consumes real matching products or completed small deeds. Completed unmatched deeds are listed for sale. Large orders must match amount, material, quality and item type. Stock caps at 24 per stall. One work step occurs per minute; downtime never produces an unlimited backlog.

Purchases use wallet gold, validate price/ownership/space again, and reject stale purchase replays. Stored stock cannot be lifted, used or targeted directly. Artisan resource packs cannot be lifted. Artifact and set brokers stock actual earned items consigned by bots through corpse-loot and Doom reward hooks; they do not manufacture named artifacts. Bots equip suitable weapon upgrades before consignment. Set pieces are sold individually. There is no global market search or automatic bot-only Doom raiding in this baseline. Sales are recorded on each stall.

Bot appraisal now values AOS properties, leeches, casting bonuses and slayers. ML magic stock uses native runic attributes instead of legacy Force/Power/Vanquishing rolls. Mages consider elemental resistance, avoid poisoning immune creatures, and are no longer restricted to first-circle spells at close range under AOS. This is an improvement to existing behavior, not understanding of every later-era encounter.

## Setup after the deployment hold is lifted

Do not run these against the live world during the hold.

* `[HavenIslandsBuild`: only after the tested terrain is installed for BOTH server and client. Builds the Commons at **3968,2858,0 Trammel** and the estate at the surveyed second island. The issuing character becomes estate owner and receives a chart in their bank. Existing centers/estates are preserved. Terrain and the Commons footprint are checked before construction.
* `[HavenCenterBuild`: alternative manual site, requiring a clear, level 35-by-43 area immediately north of the GM. Does not edit terrain. Rejects existing center/house overlap.
* `[HavenMarketBuild`: smaller standalone stall arrangement on suitable Haven ground. Prefer the Commons for the requested community building.
* `[HavenMarketPlace`: manual standalone arrangement. Do not combine it with another setup unless a second market is intended.
* `[guildcrew recruit` and `[guildcrew`: persistent guild recruitment and roster; see the guild document.

Building controllers own only their own fixtures and residents. Cleanup deletes those references, not a spatial sweep of unrelated objects. Save/load retains work, stock, prices, recipe identity, owner and fixture references.

## Two surveyed islands

`tools/map/plan_haven_islands.py` surveyed two separate 176-by-176 ocean footprints starting at **3904,2800** and **4128,2800**, Trammel. It checked land, static index and land/static diff blocks. Neither footprint contains existing land, static structures or diffs in the supplied client data. This is not a scan of live boats or dynamic world objects; recheck those before installation.

`tools/map/stage_haven_islands.py` produces independent client-data copies in a NEW output directory. It refuses existing outputs and outputs inside source data. It validates ocean/static/diff blocks again, patches only island tiles, and preserves unrelated differences between map1 and map1x. The generated manifest records source and result hashes. No proprietary MUL assets belong in git.

The isolated final prototype is `verification/HavenIslandDataFinal`. It changes 25,593 terrain tiles, forming grass interiors, sandy shores and gently rising beaches. Commons has a southern public dock. Corsair's Rest reserves a 40-by-40 owner housing plot, native harvestable trees and mining floor outcrops, sheep/hind spawns, a small easy corsair camp, a long dock with a boarding extension, and a return gate. Its blessed travel chart only works for its owner. The island is reachable by boat; ownership is not a ban on all visitors. Existing house limits still apply.

Tests place a full native castle at **4196,2868,0**, reject a stranger's housing permission, build the Commons on the real staged island, and check a native large boat at **4237,2960,-5** with an unobstructed southward route. They do not replace an in-client boarding and visual review. The pirate camp currently uses ordinary Brigand loot; bespoke treasure events and a unique pirate boss remain future work.

Before installation: back up live world and client data, check dynamic boats/houses at both sites, ensure the owner character is selected, apply matching client/server terrain, rebuild any navigation caches affected by it, then construct and inspect the world additions. No installation action is authorized by this document.

## Doom

The native six-room Doom gauntlet and artifact types exist in this source. Read-only inspection of the supplied live save found **zero GauntletSpawner records**. This does not prove every other Doom entrance/decor/spawn is absent; it identifies the missing gauntlet controllers.

`[HavenDoomStatus` reports controller count. `[HavenDoomSetup` uses the native generator only when none exist. A partial/existing encounter is preserved for manual review. Tests build six linked rooms, then ensure repeating setup does not reset them.

Eligible Doom boss kills have a 10% chance of a matching reforging recipe. Reforging requires the original matching artifact, recipe, 100 relevant crafting skill, 100 iron ingots and 20 diamonds. It preserves the item and existing properties, adds +10 damage increase, +10 spell damage, +2 hit/mana regeneration, and enables shared special-gear evolution. Evolution adds damage/spell damage and milestone regeneration; weapons also gain swing speed and mana leech. It cannot be applied twice. Bots can consign actual earned Doom artifacts.

This follows the original-artifact-plus-recipe idea described in [InsaneUO's gear update](https://insaneuo.com/2023/02/13/february-focus-gear-updates/), with this shard's own costs and bonuses. It is not an exact reproduction of their item stats or crafting system.

## Later dungeon work

Shadowguard is not implemented here. See [LATER-DUNGEON-AUDIT.md](LATER-DUNGEON-AUDIT.md) for source findings and the port sequence. No nonworking Shadowguard destination has been added.
