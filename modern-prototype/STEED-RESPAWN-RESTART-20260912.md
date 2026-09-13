# Steed respawn correction — September 12, 2026

Preview cleanly saved and restarted around 02:57 EDT; online on port 2699. Authenticated account login, server list and game relay passed.

## Fix
The native non-group spawner checked available slots before cleaning controlled/deleted entries. A tamed steed could therefore leave the habitat permanently full. HavenVampiricSteedSpawner now runs Defrag before the native Spawn availability check. This removes tracking only, preserving the tamed animal. The existing 10–15-second interval and one-wild-steed limit remain.

Only HavenPetHabitats.cs was deployed. The previous timer-only fix did not address this bug. Island and pet book changes were not deployed.

## Verification
SteedRespawnSmoke.Run was invoked from the isolated inventory test harness: tamed predecessor preserved, new wild replacement created, full population did not duplicate, and deleted wild steed replaced. Full inventory run reached COMPLETE. Verification and live Release builds each passed with zero warnings/errors. No manual in-game timing test was performed.

## Backup
E:/Backups/Haven/Prototypes/servuo-before-steed-fix-20260912-025614
All 43 save files matched backup SHA256 hashes. Config, scripts, engine sources and root files were also backed up. Original server untouched.
