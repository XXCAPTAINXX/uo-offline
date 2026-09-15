# Original dungeon locations — deployed September 9, 2026

Shadowguard now occupies its original Eodon fortress and native room locations on Ter Mur. Blackthorn uses the original Trammel castle stairs, foyer and dungeon town fragments. The Atlas includes a Ter Mur facet tab; direct commands and the Frontier Voyages board use the moved destinations. Existing Haven encounter mechanics, the easy Orchard and companion puzzle assistance remain in place.

## Verified result

- **987 content tests passed; zero failures and zero skips**, against the final staged geometry. Tests exercise room approaches, native actor positions, Belfry ascent, three Blackthorn waves and rewards, repeated migration, preservation of progression, existing stock exit correction, rejection of unrelated conflicting teleporters, and actual companion walking/solving in the Bar and Orchard.
- Copied live-world rehearsal: migrated, saved, restarted, repeated migration without duplicates, then saved again. It caught an existing stock stair exit aimed at blocked terrain; the final migration safely reuses the stock teleporter and corrects that landing. Final tests include this case.
- Live pre-update save completed at **20:34:47 EDT**. The stopped-world backup verified all **18 save files** and **38 rollback file entries**, including previously absent client files.
- **348 installed file hashes** verified: source/assemblies, six rebuilt navigation caches, server geometry and matching private client geometry.
- Live migration succeeded, then saved at **20:35:37 EDT**. Restart loaded **152,083 items and 34,477 mobiles**. Repeating migration remained successful with **six Shadowguard chambers, two frontier battle controllers, one frontier hub and one Chelonia controller**. The second battle remains the pirate expedition.
- The Commons, six Doom controllers, Ancient Hunt, thirteen Abyss expedition sites and Frostbound den persisted. Rictor Quake's estate still belongs to played character **5176**, with **251 fixtures**.
- At **20:36:18 EDT**, world initialization reported **0 spawners added, 5,357 preserved, 45 banks supplied and 0 errors**. Final explicit save completed at **20:36:38 EDT**. Server stderr was empty and port **2593** was listening under process **26832** at verification.
- TazUO restarted with the matching private data and reached its login screen. This verifies client startup, not an authenticated in-game visual walkthrough. Shared EA installation and other shard profiles were not modified.

## Release and recovery

- Bundle: `E:\(Offline UO)\uo-offline-haven-rc4\artifacts\HavenOriginalDungeonRelease20260909`
- Coherent backup: `D:\Uo Offline\uo-modernuo\haven-world-backups\original-dungeons-20260909-203454`
- Final `UOContent.dll` SHA-256: `66203CC28C7E09ED71F499B283E5E08882BDF4AC3F2F2B12EB29EDAA22B3EF1B`
- Server logs: `D:\Uo Offline\uo-modernuo\server-haven-original-dungeons-reload.log` and corresponding `-error.log`.
- Source base: `c189dba9b72e8e8430bfa1ac06d9d6bf0382f9b0`, including the existing working-tree changes. No commit or push was performed.

The release tool is `artifacts/original-release.py` in the workspace. Its `verify` action checks all installed payload hashes. Its `rollback` action requires the server and TazUO to be stopped first: it preserves the failed current world/source inside the backup, restores the coherent pre-migration save/source/assemblies/maps/caches/metadata, and verifies restored hashes. It does not stop or start processes automatically. Restart the server from its Distribution directory with `HAVEN_RELEASE_INBOX` set to `D:\Uo Offline\uo-modernuo\HavenReleaseInbox`, then verify status and a completed save. Restoring old assemblies alone against the migrated world is not a valid rollback.

The old custom encounter fixtures were removed by ownership and loose characters/items recovered. Empty former island terrain remains to avoid making old marked locations unsafe. This release restores original geography and compatible encounters; a complete official later-era mechanics/loot port remains separate. See [migration details and attribution](ORIGINAL-DUNGEON-MIGRATION.md) and [retained encounter rules](FRONTIER-EXPEDITIONS.md).
