# Frontier expeditions

Implementation: Haven-compatible Shadowguard, Blackthorn invasion battles, corsair boarding expeditions, and Chelonia. The original-location migration moves Shadowguard into its Eodon fortress and native instance rooms on Ter Mur, and Blackthorn into the dungeon beneath its Trammel castle. Chelonia and corsair expeditions retain their custom terrain. The existing server ruleset remains in use. See [original-location migration](ORIGINAL-DUNGEON-MIGRATION.md) for map and deployment details.

## Travel

The Commons' Frontier Voyages board and `[frontiers` open the destination menu. After original-location migration, the Wayfarer's Atlas lists Shadowguard under Ter Mur dungeons, Blackthorn under Trammel dungeons, and Chelonia under Trammel towns. Direct commands are `[shadowguard`, `[blackthorn`, `[chelonia` and `[voyage`. Normal travel restrictions apply.

`[expeditions` shows completed Roofs, Blackthorn rifts, voyages and earned event currencies. Its relic choices are a caster grimoire, a hybrid signet and utility boots. Each costs 50 Minax credits or doubloons, with ownership, balance and backpack-capacity checks. These special rewards evolve through the existing Legendary Artifact progression.

## Shadowguard

The [official Shadowguard guide](https://uo.com/wiki/ultima-online-wiki/world/dungeons/shadowguard/) informs the five prerequisites, party-leader entry, room failure lifecycle and Roof. Haven uses the original room layouts with its own encounter numbers and simplified puzzles. One party occupies each room at a time; occupied rooms reject new entries rather than resetting their existing encounter. There is no persistent waiting queue.

- **Bar:** rack bottles break the protection on three pirates after three successful throws each. A 15% drinking mishap costs stamina without counting a hit.
- **Orchard:** deliberately easy at the player's request. All eight pairs show their matching partner by name. Wrong choices give a hint rather than punishment. The room displays pair progress.
- **Armory:** kill three guards to earn phylacteries; the brazier purifies each for use against one protected armor. Supplies are held by the encounter, avoiding inventory clutter and cross-room exploits.
- **Fountain:** water elementals provide sixteen canal pieces. Fill the two clearly numbered channels and open the valve. This is a simplified waterworks challenge, not the official freeform rotating/expiring-canal system.
- **Belfry:** ring the bell, defeat three drakes, and use their collected wings through the bell to reach the raised dragon platform. Every party member can use the unlocked access.
- **Roof:** all five seals are required for every entering human player. Four lieutenants appear sequentially. Juo'nar uses cold damage and undead assistance; Ozymandias attacks at range and dismounts; Anon rotates elements and absorbs matching pure damage; Virtuebane is bard-immune and uses fire/dismount attacks. Their bounded summons vary with the active boss. These are compatible encounter implementations, not exact copies of official boss AI or artifact tables.

Companions and party bots can accompany players. Full-party death/logout, timeout, abandonment and restart cancel the active attempt without rewards. Earlier room seals remain saved. Owned corpses are returned to the entrance on completion, cancellation or individual exit. Regions reject outside combat/beneficial assistance and return unauthorized arrivals to the entrance.

Roof completion pays each present player 50,000 gold, three 110–120 power scrolls, 35 Haven marks and ten Astral shards. A 25–50% luck-adjusted chance awards a special evolving relic. Completion consumes the five room seals; repeated completion callbacks cannot pay again.

### Companion puzzle assistance

Use **Dungeon puzzle assistance** in the companion Orders menu, the room's **Companion: solve puzzle** button, or `[puzzle`. Use `[puzzle stop` to cancel. Helpers walk toward the relevant fixture and invoke the same guarded puzzle action as a player. They do not grant missing supplies or skip guardian kills. Combat pauses puzzle work; completed/abandoned tasks restore following. The Belfry helper unlocks access and can reach the platform; players use the bell for their own ascent.

Only these implemented puzzle handlers are supported. The companion does not automatically understand arbitrary third-party scripts or every native dungeon puzzle.

## Blackthorn

The [official Castle Blackthorn guide](https://uo.com/wiki/ultima-online-wiki/world/dungeons/dungeon-castle-blackthorn/) describes protected captains supported by minions and invasion fragments of towns. Haven provides a compatible invasion wing on surveyed terrain, with three successive town fragments. It does not replace the original castle map with a full later-era dungeon layout.

Each wave has two captains, each with three guards. A captain unlocks only after its own guards die. Defeating both captains exposes the beacon; destroying it advances the battle. Three waves close the rift. Player/pet/companion contributions and nearby party support are credited; party bots credit their nearby human party members.

Each captain earns one Minax credit for participating players still nearby. Completion adds twelve credits, 40,000 gold, fifteen Haven marks and five Astral shards. Minax credits are a per-character expedition ledger balance, not native Minax artifact items. `[expeditions` shows and spends them. The battle rests five minutes after success and cancels after thirty minutes.

## Chelonia and the tide tortoise

Chelonia is inspired by volcanic wildlife islands: sandy shores, a green nesting interior, rock outcrops, sparse coastal vegetation and a long public dock. It is distinct from the player's private Corsair's Rest estate. No custom animation files are required: the current private TazUO data includes dragon-turtle hatchling body 1294.

The **Chelonian tide tortoise** walks on land, swims, fights at sea and carries an owner-accessible pack. It starts with 180–210 Dexterity/stamina, 650–800 health, and no stat loss on taming. Rarity modifies those initial rolls through the existing custom-pet system. Its natural shades run from olive to jade and respect applied pet dyes.

Its **Tidal Jet** hits engaged enemies within six tiles for cold damage and drains stamina, with a twelve-second cooldown. **Living Shell** reduces incoming melee damage below half health by 20–35% according to rarity. Ordinary pets and other players are not eligible secondary victims of the signature. Normal pet training and custom-pet innate abilities remain available.

Two wild tortoises can inhabit the sanctuary. Spawn rolls are 45% Rare, 35% Epic and 20% Legendary; their Taming requirements are 90/100/110. Legendary starts at one follower slot. Replacement waits five minutes after taming or death. Owned or previously tamed animals are never deleted by sanctuary cleanup. Food: fish and produce.

## Corsair voyages

Chelonia's dispatch board starts a boarding raid on a real native large boat, the Saltfang. The ship is anchored; this is a playable boarding/cargo feature, not the full later-era moving-warship/cannon system. Nearby players can board through the dispatch board with following pets. `[voyage exit` returns to shore.

Defeat the crew to expose its captain, then break the cargo seal; two waves finish the raid. Earned rewards include 30,000 gold, fifteen Haven marks, five Astral shards, wood/iron commodity deeds and sealed cargo. Redeem that cargo beside the dispatch board for 10–20 doubloons. The [official Rising Tide description](https://uo.com/wiki/ultima-online-wiki/combat/pvm-player-versus-monster/rising-tide/) informed the cargo-turn-in concept; Haven's currencies, rewards and boarding mechanics are custom.

Completion, timeout or restart rescues everybody aboard before removing the encounter-owned vessel. Corpses, dropped movable items and hold contents are preserved ashore. Other player boats are not deleted or moved. An occupied spawn footprint refuses the raid. The ship's next successful raid is delayed by five minutes.

## Validation and remaining work

Tests cover actual staged terrain, ship fitting/boarding, six room flows, companion puzzle actions, party qualifications/rewards, actual enemy death attribution, Anon's absorption, stale completion replay, cargo redemption, full-backpack purchases, serialization/restart recovery, and tortoise defenses/water attacks. The final content suite passed all 984 tests with zero failures or skips. A real live-save copy passed setup, save/reload and repeated setup without duplicate controllers.

Cross-entity recovery is deferred until the whole world has loaded. Setup owns explicit fixture lists, refuses partial installations, and checks dynamic objects before building. Matching server/client maps and rebuilt Trammel navigation cache are required. No proprietary map binaries are checked into source control.

Setup may relocate unowned, never-tamed aquatic wildlife stranded by new land to validated empty water outside the island footprint. It plans every relocation before building or moving anything. Owned/previously owned creatures, other mobiles and existing world items still block setup. Patch 0050 was independently checked against the prior native source with `git apply --check --ignore-space-change` because that checkout uses CRLF context.

Full official Shadowguard maps/artifact tables and freeform Fountain complexity, the complete Blackthorn dungeon map, ship cannons/moving naval AI, full SA bosses/Imbuing, and arbitrary dungeon-puzzle AI remain separate work. In-client visual and combat-balance acceptance is not implied by automated tests.
