# Frontier expansion deployed — September 9, 2026

The authorized expansion is installed on the live server. It adds compatible Shadowguard rooms and puzzle assistance, Blackthorn invasion battles, an anchored corsair boarding encounter, and Chelonia's amphibious custom tortoises. Existing Haven rules and previously deployed systems remain in place. This is an incremental release, not completion of every later-era expansion feature in the backlog.

## Player access

- `[frontiers`: travel menu; also available at the Commons' Frontier Voyages board.
- `[shadowguard`: entrance to five prerequisite rooms and the four-boss Roof. The Orchard explicitly labels matching trees and gives hints for incorrect choices.
- `[puzzle`: order your companion to work on the current supported room puzzle. `[puzzle stop` cancels. The companion Orders menu also has **Dungeon puzzle assistance**. Helpers use the actual puzzle actions and earned supplies; guardian fights still matter.
- `[blackthorn`: three invasion waves with guards, protected captains and beacons.
- `[chelonia`: wildlife island and dock. Tide tortoises swim, fight on land and sea, carry supplies and have Tidal Jet/Living Shell abilities. Rare/Epic/Legendary variants spawn with their existing custom-pet progression rules.
- `[voyage`: corsair boarding raid; `[voyage exit` returns ashore.
- `[expeditions`: progression, Minax credits/doubloons and evolving relic rewards.

See [mechanics and supported differences](FRONTIER-EXPEDITIONS.md) for requirements, rewards, recovery and cleanup behavior. The Wayfarer's Atlas includes the installed entrances.

## Verification

- **984 content tests passed; zero failures or skips.** Tests include puzzle progression, companion solving, real enemy-death attribution, Anon's elemental absorption, qualification/party rewards, replay protection, ownership, ship cleanup, tortoise combat, terrain and wildlife relocation.
- A fresh copy of all **18 live save files** was verified before rehearsal. Setup, save, restart and repeat setup passed. Cross-entity restart cleanup runs after the whole world has loaded.
- The prior release's separate engine suite passed 820 tests with 17 skips. The engine was not changed or retested for this increment; those results are historical, not new frontier coverage.
- Native AOS patch 0050 passed independent application checking with `git apply --check --ignore-space-change` against the prior native source. The built native source is included in the bundle.
- All canonical CustomBots files matched the verification build before freezing. **318 payload files** were hashed and independently checked after deployment, plus **two private client map files** (320 destination checks).
- Live setup confirmed one frontier hub, six Shadowguard rooms, two battle controllers and one Chelonia sanctuary. Three unowned, never-tamed aquatic creatures caught by new terrain were relocated to validated water. Player objects and owned/previously owned creatures are never purged by setup.
- The original Commons, estate owned by played Rictor Quake **serial 5176** with 251 fixtures, six Doom controllers, Ancient Hunt, thirteen Abyss sites and Frostbound den persisted.
- The live world saved at **19:50:26 EDT** and restarted, loading **150,287 items and 34,506 mobiles**. All new controller counts and existing ownership persisted. Repeating setup after restart created no duplicate controllers.
- After restart, initialization preserved **5,357 spawners**, supplied **45 banks** and reported **zero errors**. The final post-restart save completed at **19:52:25 EDT**. The listener remained active on `127.0.0.1:2593` with empty stderr. Backup verification also matched 310 source/assembly/map files against the previous frozen release and rechecked all 18 saved-world hashes.
- TazUO launched successfully with the matching private data and reached the login screen. No authentication was automated. In-game presentation and combat-balance acceptance remain unverified.

## Release and recovery

Frozen bundle: `E:/(Offline UO)/uo-offline-haven-rc4/artifacts/HavenFrontierRelease20260909`.

Coherent backup: `D:/Uo Offline/uo-modernuo/haven-world-backups/frontiers-20260909-194916`.

The pre-release live world snapshot finished at **19:49:19 EDT** before stopping the server and copying/verifying its 18 saved-world files. The backup includes matching previous assemblies, source, map files, private client maps, navigation cache, configuration, client profile and installation metadata. `Rollback-Release.ps1` restores the old world together with its code and maps; an assembly-only rollback is not sufficient once the new serializable types have been saved.

Live content assembly SHA-256: `611080ED2C5AEB581B2E88B74420ED6EEEA1F4BCDE7910E728B1AA9A5994FE60`.

Source baseline: `c189dba9b72e8e8430bfa1ac06d9d6bf0382f9b0`, including retained working-tree changes. No commit or push was performed. Proprietary client/map binaries remain outside source control. Server and private client maps match; the shared EA installation was not changed.

## Remaining work

Companions currently understand the implemented Shadowguard puzzle handlers, not arbitrary dungeon scripts. Shadowguard uses compact custom arenas and simplified Fountain mechanics; Blackthorn uses a compatible invasion wing. Pirate raids use a real anchored boat, with no moving-warship/cannon system. Full official maps/artifact tables, complete SA bosses/Imbuing and remaining later-era mastery edge cases stay on the backlog. A live play-through should assess visuals, pathing under combat pressure and reward pacing before further balancing.
