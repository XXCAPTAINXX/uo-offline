# Staged Abyss crafting expeditions

This is a **partial Stygian Abyss restoration**, held for later deployment. It does not implement the full later-era expansion.

## Implemented

- Thirteen themed mini-champion sites, with each species' required kills, staged waves and a Renowned finale. The catalog includes Crimson Veins, Fairy Dragon Lair, Abyssal Lair, the three rat clans, Passage of Tears, Lands of the Lich, Secret Garden, Fire Temple Ruins, Enslaved Goblins, Skeletal Dragon and Lava Caldera.
- Thirty-six missing creature types, using reference appearance/stat profiles and this build's native AI/abilities. Spawns use connected walkable cells on the correct floor. Up to twelve actors per site; bounded replenishment, leash, inactivity cleanup and a two-minute clear cooldown.
- Eleven typed essences and sixteen additional SA materials. Eligible corpses can hold essences/ingredients. Each participant with looting rights who remains nearby receives five site essences, local crafting materials, a rare crafting gem and 3,000–5,000 gold directly in their backpack on completion. Repeated death notifications cannot pay twice.
- Materials work with commodity deeds, the Resource Ledger and resource carrying bags. Custom expedition caches deliberately supply materials whose later-era ambient sources are absent from this build.
- The Abyss artificer's forge near the Ancient Hunt offers eleven permanent attunements for ordinary equipment. Each uses eight matching essences and two of each listed material, needs 80 Blacksmithing/Tailoring/Tinkering/Inscription, and respects its property cap. One attunement per item; no automatic evolution is added. Evolving rewards retain their existing upgrade systems. This is a compatible crafting system, not retail Imbuing.
- **[abyss** and the Atlas's Abyss routes open a compact guide to installed sites and the Ancient Hunt. Following pets travel with the player; normal outbound travel restrictions apply. The Haven bank exit retains the existing recovery behavior.
- A three-tile-wide raised stone footbridge crosses the Fire Temple ring at x525–527, y760–772. Its ramp follows the existing raised platform instead of burying a flat deck in the stone. The full crossing to y773 passes the game's actual movement check.

## Installation and persistence

After explicit release of the deployment hold, **[HavenAbyssRestore** installs the expedition director, thirteen sites, bridge and forge. Existing installation is preserved. **[HavenAbyssSetup** still installs the separate Ancient Hunt. Setup commands have not been executed on the live server.

Controllers own their actors and fixtures; cleanup never sweeps unrelated world objects. Interrupted waves reset on load, removing only their unowned wild actors and avoiding duplicated completion rewards. Persistent custom material types and resource catalog entries are appended without changing existing catalog indices.

## Remaining expansion work

Full Stygian Dragon, Medusa and Slasher of Veils encounters, their native entry/quest mechanics, complete SA artifact tables, native Imbuing/unraveling and the remaining expansion crafting systems are not implemented by this pass. Renowned enemies use compatible native AI; the newer Necromage/Mystic AI variants are not reproduced. Named Renowned artifact tables and creature-specific advanced mechanics still need further work. The staged routes and material rewards must not be described as a complete Abyss.

UOAlive's public [Abyss guide](https://uoalive.com/wiki/Stygian_Abyss_Dungeon) and [Underworld guide](https://uoalive.com/wiki/Underworld) were checked. They did not expose a usable custom bridge layout. The crossing here was designed and tested on the actual local map. Site/wave references: [official mini-champion guide](https://uo.com/wiki/ultima-online-wiki/combat/pvm-player-versus-monster/stygian-abyss-mini-champion-spawns/) and [ServUO pub57](https://github.com/ServUO/ServUO/tree/pub57).

## Validation

The complete 228-test Haven suite passed in artifacts/abyss-snow-missions-full01.log. A subsequent additional integration test passed in artifacts/abyss-real-death01.log, exercising an actual Renowned death through the generated event, two participants, corpse contents and duplicate notification rejection. Tests also cover all thirteen complete wave sequences, installation deduplication, all travel routes, native bridge movement, material sources/uses, deed weight behavior and forge consumption. Client presentation and live-world coexistence remain to be checked after deployment authorization.
