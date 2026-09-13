# Island live installation — September 12, 2026

Installed on the Haven ServUO preview at `127.0.0.1:2699`, around 13:40–13:42 EDT. The original ModernUO server was not changed.

## What went live

- Corsair island terrain, settlement, docks, southeast cove and community center.
- A private, nondecaying native castle assigned to Rictor Quake's account. Three estate storage stations share one private vault. Existing homes and possessions were not moved or replaced.
- Account-bound storage recovery using `[islandstores`, including native house-transfer handling.
- Three pirate patrol sites and the separate Blackwake cove expedition: three waves, then Admiral Blackwake. Cove rewards include ship supplies; special weapon/shield sets and maps remain chance drops.
- `[island` travels to the estate. The travel stone also lists **Corsair island estate** and **Island community center**. Existing New Haven travel provides the return route.
- Protection for filled public crates when their controller is removed, encounter damage boundaries, and separation of island services from Haven plaza maintenance.

## Installation and restart sequence

1. Rehearsed installation against the copied world. Fixed a placement check that incorrectly depended on the owner's current facet; the check now uses a temporary Trammel probe without moving the owner.
2. Added critic-requested safeguards: island travel requires all installation components; a failure during world saving is fatal and requires backup restoration. Construction failures before saving remove newly created controllers in reverse order.
3. Verified the permanent client package's 525 files and matching server/client Trammel map pairs.
4. Cleanly saved and stopped the live server. Copied all 43 save files and verified every SHA-256 against the backup.
5. Deployed ten source files, changed the server data path and built Release with zero warnings/errors. The installer verified map hashes, resolved the owner uniquely, built the island, checked routes and saved successfully at 17:40:49 UTC.
6. With the regular client closed by the user, changed only its map directory setting, preserving its account, port and other settings.
7. Performed a second clean save/restart to verify live reload. The server listened again at 17:41:42 UTC. Authenticated login, server-list and game-relay probes passed after both starts. Rictor Quake entered the live world at 17:42:09 UTC.
8. Stopped the isolated test server and closed the disposable test client. Reopened the regular Haven client.

## Backup and installed files

Backup: `E:/Backups/Haven/Prototypes/servuo-before-island-20260912-133230`.
Contains verified Saves, Config, Scripts, Server, Ultima, root runtime files and the prior client settings. No backup restoration was needed.

Server data: `D:/Uo Offline/Haven-Island-Server-Data`.
Client data: `D:/Uo Offline/Haven-Island-Client-Data`.
Both map1 and map1x SHA-256: `5e8f232f803a1f4fe080df3ca33b75e333e0b53484ec2582e2921f488b32a48f`.
The matching MUL files are selected; conflicting Trammel UOP files are absent/disabled. Original source assets remain intact.

Deployed sources: HavenIslandBlueprint, HavenIslandFoundation, HavenIslandCommons, HavenIslandEncounters, HavenIslandEstate, HavenIslandInstall, HavenCoveEncounter, HavenMiniChamp, HavenStarterHub and HavenPreview.

## Validation and limits

The final isolated release run passed settlement/community routes, native castle placement, southern boat clearance, patrol lifecycle, cove progression/rewards, native drag/drop, connected withdrawals, visitor rejection, replay protection and original/transferred-owner recovery. Earlier two-process persistence tests passed exact vault-item, owner/account, station, patrol and active-cove reload checks. Live installation and the subsequent reload/login also passed.

The critic scored the implementation 8/10 before this deployment. A complete client visual walkthrough and hands-on boat, stair and combat-boundary play test were **not completed**: the test client was logged in manually, then minimized, and desktop input safeguards prevented further automated control. Do not interpret the automated checks as a visual/play approval. Shared stores support deposits and withdrawals; they do not automatically feed crafting recipes.

Live evidence: `island-install.log`, `session-20260912-134041.log`, `session-20260912-134135.log` in the preview server directory.
