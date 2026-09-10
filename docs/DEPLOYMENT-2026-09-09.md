# Haven deployment — September 9, 2026

The implemented release is deployed and running. The player explicitly authorized deployment, restarts and troubleshooting, superseding the earlier AFK deployment hold. Historical feature documents describe their original staging status; this report is the current deployment record.

## Verification and fixes

- Full content suite: **967 passed, zero failed, zero skipped**.
- Server suite: **820 passed, zero failed, 17 skipped**. Native map-fixture skips are not passes; Haven tests independently exercised the installed map data.
- Rehearsal used a hash-verified copy of the real world save with the actual live runtime. Installation, save, reload and repeated installation passed without duplicate controllers.
- Fixed Stormscale's forced ranged AI overriding learned Magery, and Doom generation deleting unrelated item types at the same elevation. Added focused regression coverage. Corrected poison-registry test isolation and reran the complete content suite.
- Retained live engine/runtime dependencies, configuration, expansion selection and legacy serialization migrations. No existing custom types were removed. Deployed source includes the existing uncommitted feature work based on commit `c189dba9b72e8e8430bfa1ac06d9d6bf0382f9b0`.

## Live rollout and persistence

All times below are EDT on September 9, 2026.

1. Waited for the old server's completed 19:05 save, stopped it and hash-verified an 18-file world backup before replacing release files.
2. Started the new release at 19:05:01. It loaded the existing world and began listening on local port 2593.
3. Activated the Commons, Corsair's Rest, native Doom gauntlet, Ancient Hunt, Abyss expedition and Frostbound den through their existing idempotent setup commands.
4. Saved at 19:08:29, then performed a controlled restart at 19:08:54. Reload read **143,485 items and 34,742 mobiles**, exactly matching that saved world.
5. Rechecked persistent controllers and ownership. World population initialized with **5,357 spawners preserved, 45 banks supplied and zero errors**. The next automatic save completed at 19:10.

Persistent installation after restart:

| Content | Verified state |
|---|---|
| Haven Commons | One community center; 13 market trades created by its build |
| Corsair's Rest | One estate, 251 owned fixtures; owner is played Rictor Quake, serial 5176 |
| Doom gauntlet | Six controllers |
| Ancient Hellhound hunt | One encounter controller |
| Abyss expedition | One director, 13 sites, two director-owned fixtures |
| Frostbound bear | One den |

The played character retains Player access and its saved New Haven position. Setup did not create a guild, spend its treasury, or fabricate artifact stock. Market goods use the implemented finite production and earned-stock systems.

At verification, the sole live ModernUO process is **PID 25916**, listening on **127.0.0.1:2593**. Both new server error logs are empty. Existing waypoint/AuditNav warnings match the previous release; bot navigation still has known route debt and has not been certified free of every pathfinding issue.

## Client and terrain

Server Trammel terrain and its baked navigation cache match the release manifest. The TazUO profile uses a private data copy at `D:/Uo Offline/uo-modernuo/TazUO-Haven-Data`; its current graphics/animations are retained while Trammel geometry matches the server. The shared EA installation was not changed. Independent verification found exactly one profile setting changed: `ultimaonlinedirectory`.

TazUO launched successfully and displayed its login screen. No authentication was automated. In-game map presentation, new pet rendering, gumps and combat pacing still need player visual/playtest acceptance; this release does not claim those were observed live.

## Access and remaining work

- `[home` travels to the played character's Corsair's Rest, subject to ordinary travel restrictions. The island chart is in that character's bank.
- Bank dungeon portals offer Haven Commons. `[market` opens the global market directory.
- `[guildcrew` opens Fellowship management; `[c` opens companion controls.
- `[abyss` reaches the compatible Ancient Hunt; its ritual requires 110 base Taming and Lore.

Deployed source also contains the compatible mastery work, companion roles and adjustable missions, pet training/signatures/rarity colors, wandering encounters, discovery rewards, resource carrying/ledger features and existing Haven progression/services. This is deployment of implemented work, not completion of every proposed later-era system.

**Still pending:** full Shadowguard and Blackthorn encounters, High Seas pirate vessels, full SA boss/artifact systems and native Imbuing/unraveling, remaining mastery edge cases and complete guild crafting supply coverage.

## Recovery and evidence

- Frozen release: `E:/(Offline UO)/uo-offline-haven-rc4/artifacts/HavenCompleteRelease20260909`.
- Coherent backup: `D:/Uo Offline/uo-modernuo/haven-world-backups/complete-20260909-1902`.
- Deployed UOContent SHA256: `9F6CEC0FA9F0A79E268750BD92FCA97B759D4DA36D1A7F8FBA1CFFB780677EC2`.
- Current logs: `D:/Uo Offline/uo-modernuo/server-haven-complete-reload.log` and `server-haven-complete-reload-error.log`.
- Bundle contains hashed payload manifest, deployment/rollback scripts, rehearsal evidence and `live-preinstall.json`, `live-installed.json`, `live-saved.json`, `live-reloaded.json`.

Rollback must restore the matching pre-release world save **together with** its old code, maps, navigation cache, configuration and client profile. The new save contains newly introduced types, so reverting only the DLL is not a valid rollback. A prepared rollback script is retained but was not needed or executed.

The optional local release inbox is enabled only in the current server process environment. It exposes no network endpoint and accepts only status, save and the fixed setup sequence on the game thread. Normal startup remains compatible without this environment setting.
