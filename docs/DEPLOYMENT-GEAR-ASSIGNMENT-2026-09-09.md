# Alden's offline gear assignment — September 9, 2026

The user asked for their companion to grind better gear until they return from work. The current AFK scheduler required a connected owner, so an explicit saved offline assignment was added and activated for **Alden Ashford, serial 52621**, owned by the played **Rictor Quake, serial 5176**. It uses normal five-minute Grind expedition rewards and training, keeps found items in the shared pack, and improves equipped evolving gear without replacing it.

## Validation and live state

- Final content suite: **992 passed, zero failed, zero skipped**. This includes reward timing, no duplicate payouts, preservation of equipped items, safe pack limits, saved-record round trip, collection of an existing completed mission, and returning a controlled internalized companion to the owner's location on login.
- Rehearsal used a verified copy of all 18 live save files. Alden had an already-finished mission. Its rewards were collected once, and save/reload retained the resulting pack and stats. Repeating assignment preserved its original deadline.
- Live assignment began at **21:23:59 EDT**. The pending previous mission was collected, raising raw Str/Dex/Int from **938/913/907 to 943/918/912**, with shared-pack item count **418 to 426**.
- A final return-path correction was deployed after tests: a completed offline mission leaves the companion on the internal map. A controlled companion now returns to the owner's location on login before normal recall handling. Shrunken pets have no control master and do not qualify for this bypass. The saved record's layout did not change.
- The corrected live build loaded **151,804 items and 34,456 mobiles** and listened on **127.0.0.1:2593**, process **2572**, at **21:26:41 EDT**. Assignment state, original **21:28:59 EDT** next-reward deadline, stats and all **426 pack items** persisted. It remained running with the prior expedition already collected.
- Original dungeon locations and all existing controller counts persisted, including the estate owned by character 5176 with 251 fixtures.
- **317 source/assembly hashes** verified after deployment. Client/map files were not changed.
- First new live gear run confirmed and saved at **21:29:15 EDT**: **1 completed run**, Str/Dex/Int **948/923/917**, **429 shared-pack items**. The assignment remained running with the next deadline at **21:33:59 EDT**. Repeating the start action did not reset its deadline. World initialization preserved 5,357 spawners and supplied 45 banks with zero errors; stderr remained empty.

## Artifacts and rollback

- Final bundle: `E:\(Offline UO)\uo-offline-haven-rc4\artifacts\HavenGearAssignmentRelease20260909b`
- Final DLL SHA-256: `5AFCB127EA7C2D8A8BE067839CA4B43070A75B15EA80CA41733A174578C56664`
- Backup before the final return correction: `D:\Uo Offline\uo-modernuo\haven-world-backups\gear-assignment-20260909-212623`
- Backup before adding the offline-assignment feature: `D:\Uo Offline\uo-modernuo\haven-world-backups\gear-assignment-20260909-212340`
- Current logs: `D:\Uo Offline\uo-modernuo\server-haven-gear-grind-reload.log` and corresponding `-error.log`.
- Current deployment metadata: `D:\Uo Offline\uo-modernuo\haven-world-update.json`.

The operator scripts are `artifacts/gear-assignment-return-release.py` for the final correction and `artifacts/gear-assignment-release.py` for the initial feature release. Stop the server before either `rollback` action. Each preserves the failed current Saves/source and restores its own coherent saved-world/source/assembly/metadata backup. To remove the new saved record type entirely, use the earlier feature backup together with its earlier assembly. Do not restore an old assembly by itself against newer Saves. Restart explicitly and confirm a successful load and completed save.

The source includes existing uncommitted changes based on `c189dba9b72e8e8430bfa1ac06d9d6bf0382f9b0`; no commit or push was made. See [assignment behavior](COMPANION-OFFLINE-GEAR-GRIND.md) for the gameplay controls and reward rules.
