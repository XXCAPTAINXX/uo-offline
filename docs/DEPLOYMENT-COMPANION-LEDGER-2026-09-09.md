# Companion ledger deposits — deployed

Place an existing Resource Ledger in the companion's shared pack to collect supported mission resource deeds automatically. Nested bags work. Normal expeditions, connected AFK runs and offline assignments use the same deposit path. The field report records exact quantities with destination “Companion Resource Ledger” before the filled deed is consumed. Unsupported items and rewards without an eligible ledger retain their ordinary delivery. No ledger is created or moved automatically.

The bound owner can use the carried ledger while nearby on the same map. Absorb pack deeds processes the ledger carrier's pack; withdrawals go into the owner's backpack. Failed withdrawals preserve the balance. The saved ledger format and material catalog indices are unchanged.

All **1,009 content tests passed**, zero failed or skipped, using local map/tile data. Added integration coverage for all eight gathering/regional routes across normal and offline delivery, nested ledgers, actual receipts, duplicate completion, serialized balance round trips, ownership/range, full-backpack withdrawal and full-ledger fallback. Existing no-ledger mission tests continue to pass.

Installed September 9, 2026 from `artifacts/HavenCompanionLedgerRelease20260909`. All 319 source/assembly payload files were hash-verified at installation. The pre-update save and restarted live status contain identical offline assignment and full mission journal data. Post-update world save completed at 21:54:04 EDT. Server PID 3604 listens on 2593; stderr was empty. No client/map changes were required. In-game visual acceptance remains unverified.

Coherent pre-update backup: `D:\Uo Offline\uo-modernuo\haven-world-backups\companion-ledger-20260909-215342`. Bundle evidence contains `manifest.json`, `tests.log`, `live-before.json`, `live-start.json`, `live-save.json`, and deployment metadata with the installed assembly hash. The rollback helper is `artifacts/companion-ledger-release.py rollback`; stop the server before restoring the backed-up world/source/assemblies together. Existing uncommitted work was retained; no commit or push was made.
