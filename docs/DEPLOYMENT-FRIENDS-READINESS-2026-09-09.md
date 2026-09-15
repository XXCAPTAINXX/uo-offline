# Mastery defense and friend testing readiness — September 9, 2026

Deployed the native Saving Throw disarm check, Resilience poison resistance and reduced new bleed/Mortal Strike/Curse durations. The same valid-party lookup supports owners and their pets and rejects expired/out-of-range effects. Mastery balance and compatibility differences are documented in [Book of Masteries](BOOK-OF-MASTERIES.md). Added `[HavenOriginalDungeonsSetup` for administrator setup on independent test worlds.

The sharing pass also removed a workspace-only Shadowguard reference dependency from the map preparation tool, added a repeatable private-data preparation command, a [friend installation guide](FRIENDS-TESTING.md), and a [player test checklist](PLAYER-TEST-CHECKLIST.md). The README now distinguishes this Haven branch from the upstream T2A baseline. Generated UO assets, private saves and compiled packages are excluded from source control.

## Evidence

- **1,020 content tests passed**, zero failed/skipped, against newly generated private maps. Tests exercise actual paired Peacemaking casts, party pets, native wound/bleed/curse timers, range/expiry cleanup and Saving Throw skill/mastery requirements, alongside the existing regression suite.
- Native patch 0051 applied cleanly to the independent pinned-engine audit checkout. The six native source baselines also matched the live install before replacement.
- All **51 numbered patches** were applied to a completely fresh pinned ModernUO checkout. This caught and fixed CRLF line endings in patch 0050. After patching, every native Server/UOContent C# source file matched the tested checkout (zero differences); the custom source also matches the frozen build payload.
- `prepare_haven_test_data.py` completed from independent classic inputs and the locally available modern Classic assets. All generated client/server geometry copies were hash-verified. Sources and active profiles were untouched by the preparation run.
- **326 installed source/assembly files verified** against the frozen payload. The live restart preserved Alden's complete mission journal and offline assignment exactly. A post-update save completed at **22:26:23 EDT**. Server PID **12924** listens on port **2593**; stderr was empty.
- No live client/map/profile changes were part of this code deployment. In-client acceptance and a first installation on a friend's PC remain unverified and are explicitly assigned to the checklist.

Bundle: `artifacts/HavenFriendsReadinessRelease20260909`. It contains the manifest, test log, installed hashes and before/start/save status proofs. Coherent pre-update backup: `D:\Uo Offline\uo-modernuo\haven-world-backups\friends-readiness-20260909-222555`.

Rollback requires stopping the server and restoring saved world, custom/native source and assemblies together through `artifacts/friends-readiness-release.py rollback`. Current public source status is recorded by the branch commit; personal account/save files and proprietary assets are not part of the source release.

## Remaining work

The checklist covers gameplay presentation, pathfinding, encounter pacing and multiplayer contention that automated tests cannot fully establish. Full official dungeon/artifact ports, native Imbuing, moving naval/cannon AI, remaining guild recipe provisioning and per-player multiplayer island allocation are not claimed complete. Network hosting settings and port exposure were not changed.
